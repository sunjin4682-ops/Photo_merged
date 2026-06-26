using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Photo.Commands
{
    internal class JabCommand :ICommand  
    {//잡티제거!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!
        //잡티제거 명령
        private Bitmap _workingImage;
            private Point _center;
            private int _radius;
            private Bitmap _prevSnapshot; // Undo를 위한 이전 상태 보관
            private Bitmap _afterSnapshot; // Redo를 위한 결과 상태 보관
            private Action _refreshAction;

            public JabCommand(Bitmap workingImage, Point center, int brushSize, Action refreshAction)
            {
                _workingImage = workingImage;
                _center = center;
                _radius = brushSize / 2;
                _refreshAction = refreshAction;

                // 실행 전, 해당 영역의 이미지를 백업함 (Undo용)
                _prevSnapshot = GetAreaSnapshot();
            }

            private Bitmap GetAreaSnapshot()
            {
                Rectangle area = GetTargetRect();
                return _workingImage.Clone(area, _workingImage.PixelFormat);
            }

            private Rectangle GetTargetRect()
            {
                int startX = Math.Max(_center.X - _radius, 0);
                int startY = Math.Max(_center.Y - _radius, 0);
                int endX = Math.Min(_center.X + _radius, _workingImage.Width - 1);
                int endY = Math.Min(_center.Y + _radius, _workingImage.Height - 1);
                return new Rectangle(startX, startY, endX - startX + 1, endY - startY + 1);
            }

            public void Execute()
            {
                if (_afterSnapshot != null)
                {
                    // 이미 실행된 적이 있다면(Redo 상황), 저장된 결과물로 덮어쓰기
                    RestoreSnapshot(_afterSnapshot);
                }
                else
                {
                    // 처음 실행 시 (RemoveSpot 로직 실행)
                    ApplyJabEffect();
                    // 결과물을 저장해둠 (나중에 Redo할 때 다시 계산 안 하도록)
                    _afterSnapshot = GetAreaSnapshot();
                }
                _refreshAction?.Invoke();
            }

            public void UnExecute()
            {
                // 백업해둔 이전 상태로 복구
                RestoreSnapshot(_prevSnapshot);
                _refreshAction?.Invoke();
            }

            private void RestoreSnapshot(Bitmap snapshot)
            {
                Rectangle area = GetTargetRect();
                using (Graphics g = Graphics.FromImage(_workingImage))
                {
                    g.DrawImage(snapshot, area.Location);
                }
            }

            private void ApplyJabEffect()
            {
                Rectangle area = GetTargetRect();
                int r = 0, g = 0, b = 0, count = 0;

                // 1. 평균 색상 계산
                for (int x = area.Left; x <= area.Right; x++)
                {
                    for (int y = area.Top; y <= area.Bottom; y++)
                    {
                        if (x < 0 || x >= _workingImage.Width || y < 0 || y >= _workingImage.Height) continue;

                        double dist = Math.Sqrt(Math.Pow(x - _center.X, 2) + Math.Pow(y - _center.Y, 2));
                        if (dist <= _radius)
                        {
                            Color c = _workingImage.GetPixel(x, y);
                            r += c.R; g += c.G; b += c.B; count++;
                        }
                    }
                }

                if (count == 0) return;
                Color avgColor = Color.FromArgb(r / count, g / count, b / count);

                // 2. 픽셀 믹싱 적용
                for (int x = area.Left; x <= area.Right; x++)
                {
                    for (int y = area.Top; y <= area.Bottom; y++)
                    {
                        double dist = Math.Sqrt(Math.Pow(x - _center.X, 2) + Math.Pow(y - _center.Y, 2));
                        if (dist <= _radius)
                        {
                            Color original = _workingImage.GetPixel(x, y);
                            int mixR = (original.R + avgColor.R) / 2;
                            int mixG = (original.G + avgColor.G) / 2;
                            int mixB = (original.B + avgColor.B) / 2;
                            _workingImage.SetPixel(x, y, Color.FromArgb(mixR, mixG, mixB));
                        }
                    }
                }
            }
    }
}
