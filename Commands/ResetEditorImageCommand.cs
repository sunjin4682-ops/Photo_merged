using System;
using System.Drawing;
using System.Drawing.Imaging;

namespace Photo
{
    public partial class Main
    {
        internal sealed class ResetEditorImageCommand : ImageCommandBase
        {
            public ResetEditorImageCommand(Main mainForm) : base(mainForm)
            {
            }

            protected override void ApplyEffect()
            {
                Bitmap source = null;

                if (mainForm.editor?.OriginalBIT != null)
                {
                    source = mainForm.editor.OriginalBIT.Clone(
                        new Rectangle(0, 0, mainForm.editor.OriginalBIT.Width, mainForm.editor.OriginalBIT.Height),
                        PixelFormat.Format32bppArgb);
                }
                else if (mainForm.FaceMode?.originalImage != null)
                {
                    source = mainForm.FaceMode.originalImage.Clone(
                        new Rectangle(0, 0, mainForm.FaceMode.originalImage.Width, mainForm.FaceMode.originalImage.Height),
                        PixelFormat.Format32bppArgb);
                }

                if (source == null)
                    return;

                mainForm.ReplaceMainImage(source);
                mainForm.SyncMainImageToFaceAndEditor(syncOriginal: false, clearHistory: false);

                if (mainForm.mask != null)
                    Array.Clear(mainForm.mask, 0, mainForm.mask.Length);

                mainForm.CancelSelect();
                mainForm.MainPic.Invalidate();
            }
        }
    }
}
