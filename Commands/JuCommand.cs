using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Photo.Commands
{
    public class JuCommand : ICommand
    {//주름제거!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!
        private Bitmap _workingImage;
            private Point _center;
            private int _radius;
            private Bitmap _prevSnapshot; // Undo를 위한 이전 상태 보관
            private Bitmap _afterSnapshot; // Redo를 위한 결과 상태 보관
            private Action _refreshAction;

            public JuCommand(Bitmap workingImage, Point center, int brushSize, Action refreshAction)
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
                    ApplyJuEffect();
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

            private void ApplyJuEffect()
            {
                Rectangle area = GetTargetRect();

                int r = 0, g = 0, b = 0, count = 0;

                // 1. 평균 색 계산
                for (int x = area.Left; x <= area.Right; x++)
                {
                    for (int y = area.Top; y <= area.Bottom; y++)
                    {
                        double dist = Math.Sqrt((x - _center.X) * (x - _center.X) + (y - _center.Y) * (y - _center.Y));

                        if (dist <= _radius)
                        {
                            Color c = _workingImage.GetPixel(x, y);
                            r += c.R;
                            g += c.G;
                            b += c.B;
                            count++;
                        }
                    }
                }

                if (count == 0) return;

                Color avg = Color.FromArgb(r / count, g / count, b / count);

                // 밝기 약간 올리기
                int bright = 3;

                int rBright = Math.Min(avg.R + bright, 255);
                int gBright = Math.Min(avg.G + bright, 255);
                int bBright = Math.Min(avg.B + bright, 255);

                avg = Color.FromArgb(rBright, gBright, bBright);

                // 2. 블렌딩 적용
                for (int x = area.Left; x <= area.Right; x++)
                {
                    for (int y = area.Top; y <= area.Bottom; y++)
                    {
                        double dist = Math.Sqrt((x - _center.X) * (x - _center.X) + (y - _center.Y) * (y - _center.Y));

                        if (dist <= _radius)
                        {
                            Color original = _workingImage.GetPixel(x, y);

                            // ⭐ 중심은 강하게, 가장자리는 약하게
                            double weight = 1.0 - (dist / _radius);

                            int rMix = (int)(original.R * (1 - weight) + avg.R * weight);
                            int gMix = (int)(original.G * (1 - weight) + avg.G * weight);
                            int bMix = (int)(original.B * (1 - weight) + avg.B * weight);

                            _workingImage.SetPixel(x, y, Color.FromArgb(rMix, gMix, bMix));
                        }
                    }
                }
            }
    }
}
