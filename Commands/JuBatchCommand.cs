using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Photo.Commands
{
    internal class JuBatchCommand : ICommand
    {
        private Bitmap _workingImage;
            private Bitmap _prevFullImage;
            private Bitmap _afterFullImage;
            private Action _refreshAction;

            public JuBatchCommand(Bitmap workingImage, Bitmap prev, Bitmap after, Action refresh)
            {
                _workingImage = workingImage;
                _prevFullImage = prev;
                _afterFullImage = after;
                _refreshAction = refresh;
            }

            public void Execute()
            {
                ReplaceImage(_afterFullImage);
            }

            public void UnExecute()
            {
                ReplaceImage(_prevFullImage);
            }

            // ⭐ 이 부분을 아래 내용으로 완전히 바꾸세요!
            private void ReplaceImage(Bitmap source)
            {
                if (source == null) return;
            try
            {
                using (Graphics g = Graphics.FromImage(_workingImage))
                {
                    // 1. 기존 이미지를 완전히 덮어쓰도록 설정
                    g.CompositingMode = System.Drawing.Drawing2D.CompositingMode.SourceCopy;

                    // 2. 픽셀이 어긋나지 않도록 보간 모드 설정
                    g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.NearestNeighbor;

                    // 3. ⭐ 핵심: 단순히 0,0에 그리는게 아니라, 
                    // workingImage의 전체 크기(Rectangle)에 딱 맞춰서 그리라고 명시합니다.
                    g.DrawImage(source, new Rectangle(0, 0, _workingImage.Width, _workingImage.Height));
                }
                _refreshAction?.Invoke();
            }
            catch {
                return;
            }
            }
    }
}
