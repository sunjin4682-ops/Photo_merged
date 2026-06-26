using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Photo.Commands
{
    public class DrawCommand : ICommand
    {
        private Bitmap _drawLayer;        // 메인의 drawLayer 참조
            private Bitmap _prevSnapshot;     // 그리기 전 상태
            private Bitmap _afterSnapshot;    // 그린 후 상태
            private Action _refreshAction;    // 화면 갱신용 액션

            public DrawCommand(Bitmap drawLayer, Bitmap prev, Bitmap after, Action refreshAction)
            {
                _drawLayer = drawLayer;
                _prevSnapshot = prev;
                _afterSnapshot = after;
                _refreshAction = refreshAction;
            }

            public void Execute()
            {
                ReplaceLayer(_afterSnapshot);
            }

            public void UnExecute()
            {
                ReplaceLayer(_prevSnapshot);
            }

            private void ReplaceLayer(Bitmap source)
            {
                if (source == null || _drawLayer == null) return;

                using (Graphics g = Graphics.FromImage(_drawLayer))
                {
                    g.CompositingMode = System.Drawing.Drawing2D.CompositingMode.SourceCopy;
                    g.DrawImage(source, new Rectangle(0, 0, _drawLayer.Width, _drawLayer.Height));
                }
                _refreshAction?.Invoke();
            }
    }
}
