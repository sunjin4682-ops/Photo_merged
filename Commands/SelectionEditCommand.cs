using System;
using System.Drawing;
using System.Drawing.Imaging;

namespace Photo
{
    public partial class Main
    {
        internal sealed class SelectionEditCommand : ImageCommandBase
        {
            public SelectionEditCommand(Main mainForm) : base(mainForm)
            {
            }

            protected override void ApplyEffect()
            {
                if (mainForm.editor.MainBIT == null || mainForm.MainPic.Image == null)
                    return;

                using Bitmap source = ((Bitmap)mainForm.MainPic.Image).Clone(
                    new Rectangle(0, 0, mainForm.MainPic.Image.Width, mainForm.MainPic.Image.Height),
                    PixelFormat.Format32bppArgb);

                mainForm.editor.LoadImage(source);

                switch (mainForm.editor.CurrentEdit)
                {
                    case ImageEditor.EditMode.Cut:
                        mainForm.editor.ApplyCut();
                        break;
                    case ImageEditor.EditMode.Mosaic:
                        mainForm.editor.ApplyMosaic();
                        break;
                    default:
                        return;
                }

                if (mainForm.editor.MainBIT == null)
                    return;

                Bitmap result = mainForm.editor.MainBIT.Clone(
                    new Rectangle(0, 0, mainForm.editor.MainBIT.Width, mainForm.editor.MainBIT.Height),
                    PixelFormat.Format32bppArgb);

                mainForm.ReplaceMainImage(result);
                mainForm.SyncMainImageToFaceAndEditor(syncOriginal: false, clearHistory: false);

                if (mainForm.mask != null)
                    Array.Clear(mainForm.mask, 0, mainForm.mask.Length);

                mainForm.MainPic.Invalidate();
            }
        }
    }
}
