using System;
using System.Collections.Generic;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Text;
using System.Threading.Tasks;



// -----------------------------------------------------------------------------
// 파일 역할: 폴라로이드 프레임/스티커 관련 UI 또는 이미지 합성 로직을 담는다.
// 병합 작업 때 빠르게 구조를 파악할 수 있도록 주석을 보강한 버전이다.
// -----------------------------------------------------------------------------

namespace Photo
{
    // 메인 편집 창과 공통 편집 상태를 관리하는 핵심 폼 클래스.
    public partial class Main
    {
       // 관련 이벤트나 리소스를 한곳에서 등록한다.
       private void RegisterFrameEvents()
        {
            if (pnlFrames == null) return;
            foreach (Control ct in pnlFrames.Controls)
            {
                if (ct is PictureBox pb)
                {
                    pb.Click += Frame_Click;
                    pb.Cursor = Cursors.Hand;
                }
            }
        }

        // --- [프레임 적용 커맨드] ---
        public class FrameCommand : ICommand
        {
            private readonly Main _form;
            private readonly Image _oldImage;     // Undo용: 실행 직전 화면
            private readonly Image _baseImage;    // Redo용: 프레임 적용 기준 원본
            private readonly Image _frameImage;
            private readonly string _frameName;

            public FrameCommand(Main form, Image currentDisplayImage, Image baseImage, Image frame, string frameName)
            {
                _form = form;
                _oldImage = currentDisplayImage != null ? (Image)currentDisplayImage.Clone() : null;
                _baseImage = baseImage != null ? (Image)baseImage.Clone() : null;
                _frameImage = frame != null ? (Image)frame.Clone() : null;
                _frameName = frameName;
            }

            public void Execute()
            {
                Image result = Pola.ApplyFrame(_baseImage, _frameImage);
                _form.UpdateMainImage(result, _frameName);
            }

            public void UnExecute()
            {
                if (_oldImage != null)
                    _form.UpdateMainImage((Image)_oldImage.Clone(), "");
            }
        }

        // 버튼/메뉴 클릭 이벤트를 처리한다.
        private void Frame_Click(object? sender, EventArgs e)
        {
            PictureBox clickedBox = sender as PictureBox;
            if (clickedBox == null || clickedBox.Image == null)
                return;

            Image baseImage = MainPic.Image;
            if (baseImage == null || MainPic.Image == null)
            {
                MessageBox.Show("먼저 사진을 불러와주세요.");
                return;
            }

            if (currentFrameName == clickedBox.Name)
            {
                ICommand undoFrameCmd = new FrameCommand(
                    this,
                    MainPic.Image,
                    baseImage,
                    null,
                    ""
                );
                commandManager.ExecuteCommand(undoFrameCmd);
            }
            else
            {
                ICommand cmd = new FrameCommand(
                    this,
                    MainPic.Image,
                    baseImage,
                    clickedBox.Image,
                    clickedBox.Name
                );
                commandManager.ExecuteCommand(cmd);
            }

            UpdateUndoRedoButtons();
        }

        public Image GetOriginalImage()
        {
            if (originalImage != null)
                return originalImage;

            if (FaceMode?.originalImage != null)
                return FaceMode.originalImage;

            if (MainPic.Image != null)
                return MainPic.Image;

            return null;
        }

        // Pola 관련 역할을 담당하는 타입이다.
        public class Pola
        {
            // 현재 효과나 상태를 실제 이미지/편집 상태에 반영한다.
            public static Bitmap ApplyFrame(Image photo, Image frame)
            {
                if (photo == null) return null;
                if (frame == null) return new Bitmap(photo);

                Bitmap canvas = new Bitmap(photo.Width, photo.Height);
                using (Graphics g = Graphics.FromImage(canvas))
                {
                    g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                    g.DrawImage(photo, 0, 0, photo.Width, photo.Height);
                    g.DrawImage(frame, 0, 0, photo.Width, photo.Height);
                }
                return canvas;
            }
        }
    }
}
