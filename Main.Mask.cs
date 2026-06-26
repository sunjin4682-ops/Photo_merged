using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


// -----------------------------------------------------------------------------
// 파일 역할: 브러시 마스크 생성/초기화/좌표 보정 로직을 담당한다.
// 병합 작업 때 빠르게 구조를 파악할 수 있도록 주석을 보강한 버전이다.
// -----------------------------------------------------------------------------

namespace Photo
{
    // 메인 편집 창과 공통 편집 상태를 관리하는 핵심 폼 클래스.
    public partial class Main
    {
        private void ResetMaskForCurrentImage(bool clearContents = true)
        {//마스크 초기화용 함수
            if (MainPic.Image == null)
            {
                mask = null;
                return;
            }

            int w = MainPic.Image.Width;
            int h = MainPic.Image.Height;

            if (mask == null || mask.Length != w * h)
            {
                mask = new byte[w * h];
                return;
            }

            if (clearContents)
            {
                Array.Clear(mask, 0, mask.Length);
            }
        }

        private Rectangle GetDisplayedImageRect()
        {
            if (MainPic.Image == null || MainPic.ClientSize.Width <= 0 || MainPic.ClientSize.Height <= 0)
                return Rectangle.Empty;

            Image img = MainPic.Image;

            float imageAspect = (float)img.Width / img.Height;
            float controlAspect = (float)MainPic.ClientSize.Width / MainPic.ClientSize.Height;

            int drawWidth, drawHeight, offsetX, offsetY;

            if (imageAspect > controlAspect)
            {
                drawWidth = MainPic.ClientSize.Width;
                drawHeight = (int)(drawWidth / imageAspect);
                offsetX = 0;
                offsetY = (MainPic.ClientSize.Height - drawHeight) / 2;
            }
            else
            {
                drawHeight = MainPic.ClientSize.Height;
                drawWidth = (int)(drawHeight * imageAspect);
                offsetX = (MainPic.ClientSize.Width - drawWidth) / 2;
                offsetY = 0;
            }

            return new Rectangle(offsetX, offsetY, drawWidth, drawHeight);
        }

        private bool TryPictureBoxToImage(Point pbPoint, out Point imagePoint)
        {
            imagePoint = Point.Empty;

            if (MainPic.Image == null)
                return false;

            Rectangle imgRect = GetDisplayedImageRect();
            if (imgRect == Rectangle.Empty || !imgRect.Contains(pbPoint))
                return false;

            float scaleX = (float)MainPic.Image.Width / imgRect.Width;
            float scaleY = (float)MainPic.Image.Height / imgRect.Height;

            int imgX = (int)((pbPoint.X - imgRect.X) * scaleX);
            int imgY = (int)((pbPoint.Y - imgRect.Y) * scaleY);

            imgX = Math.Max(0, Math.Min(MainPic.Image.Width - 1, imgX));
            imgY = Math.Max(0, Math.Min(MainPic.Image.Height - 1, imgY));

            imagePoint = new Point(imgX, imgY);
            return true;
        }



        /// <summary>
        /// PictureBox 좌표를 실제 이미지 좌표로 변환한다.
        /// 사용자가 화면에서 클릭한 위치를 실제 픽셀 편집 위치로 바꾸기 위해 사용한다.
        /// </summary>
        private Point PictureBoxToImage(Point pbPoint)
        {
            return TryPictureBoxToImage(pbPoint, out Point imagePoint) ? imagePoint : Point.Empty;
        }

        private Point ImageToPictureBoxPoint(Point imagePoint)
        {
            if (MainPic.Image == null)
                return Point.Empty;

            Rectangle imgRect = GetDisplayedImageRect();
            if (imgRect == Rectangle.Empty)
                return Point.Empty;

            float scaleX = (float)imgRect.Width / MainPic.Image.Width;
            float scaleY = (float)imgRect.Height / MainPic.Image.Height;

            int pbX = imgRect.X + (int)Math.Round(imagePoint.X * scaleX);
            int pbY = imgRect.Y + (int)Math.Round(imagePoint.Y * scaleY);

            return new Point(pbX, pbY);
        }


        /// <summary>
        /// 실제 이미지의 한 픽셀/영역을 PictureBox 표시 좌표로 바꾼다.
        /// 마스크 오버레이를 화면에 다시 그릴 때 사용한다.
        /// </summary>
        private Rectangle ImageToPictureBoxRect(int imgX, int imgY)
        {
            if (MainPic.Image == null)
                return Rectangle.Empty;

            Rectangle imgRect = GetDisplayedImageRect();
            if (imgRect == Rectangle.Empty)
                return Rectangle.Empty;

            float scaleX = (float)imgRect.Width / MainPic.Image.Width;
            float scaleY = (float)imgRect.Height / MainPic.Image.Height;

            int pbX = imgRect.X + (int)(imgX * scaleX);
            int pbY = imgRect.Y + (int)(imgY * scaleY);

            int pbW = Math.Max(1, (int)Math.Ceiling(scaleX));
            int pbH = Math.Max(1, (int)Math.Ceiling(scaleY));

            return new Rectangle(pbX, pbY, pbW, pbH);
        }


        private Rectangle ImageToPictureBoxRect(Rectangle imgRectSrc)
        {
            if (MainPic.Image == null)
                return Rectangle.Empty;

            Point tl = ImageToPictureBoxPoint(new Point(imgRectSrc.Left, imgRectSrc.Top));
            Point br = ImageToPictureBoxPoint(new Point(imgRectSrc.Right, imgRectSrc.Bottom));

            return Rectangle.FromLTRB(tl.X, tl.Y, br.X, br.Y);
        }

        /// <summary>
        /// 지정한 이미지 좌표를 중심으로 마스크 값을 칠한다.
        /// 좌클릭은 마스크를 추가하고, 우클릭은 기존 마스크를 지운다.
        /// </summary>
        private void PaintMaskAt(Point imagePt, bool erase)
        {
            if (MainPic.Image == null || mask == null) return;

            int w = MainPic.Image.Width;
            int h = MainPic.Image.Height;

            int cx = imagePt.X;
            int cy = imagePt.Y;

            // StretchImage에서는 화면상 원형 브러시가
            // 원본 이미지 좌표계에서는 타원처럼 대응되어야
            // 실제 칠한 위치와 적용 위치가 더 잘 맞음
            float radiusX = brushRadius * ((float)MainPic.Image.Width / MainPic.ClientSize.Width);
            float radiusY = brushRadius * ((float)MainPic.Image.Height / MainPic.ClientSize.Height);

            // 브러시가 영향을 줄 수 있는 사각 범위만 순회해 불필요한 연산을 줄인다.
            int minX = Math.Max(0, (int)Math.Floor(cx - radiusX));
            int maxX = Math.Min(w - 1, (int)Math.Ceiling(cx + radiusX));
            int minY = Math.Max(0, (int)Math.Floor(cy - radiusY));
            int maxY = Math.Min(h - 1, (int)Math.Ceiling(cy + radiusY));

            for (int y = minY; y <= maxY; y++)
            {
                for (int x = minX; x <= maxX; x++)
                {
                    double nx = (x - cx) / radiusX;
                    double ny = (y - cy) / radiusY;
                    double dist = Math.Sqrt(nx * nx + ny * ny);

                    if (dist > 1.0) continue;

                    // 중심에 가까울수록 강하고 가장자리로 갈수록 약한 브러시 곡선을 만든다.
                    double t = 1.0 - dist;

                    // falloffPower가 클수록 가장자리가 더 빠르게 약해진다.
                    t = Math.Pow(t, maskFalloffPower);


                    byte value = (byte)(t * maskMaxValue);
                    int idx = y * w + x;

                    if (!erase)
                    {
                        // 더 강한 브러시 값이 들어왔을 때만 갱신해 중앙부가 자연스럽게 누적되게 한다.
                        if (value > mask[idx])
                            mask[idx] = value;
                    }
                    else
                    {
                        // 지우개 모드에서는 현재 값에서 브러시 세기만큼 차감한다.
                        int newValue = mask[idx] - value;
                        if (newValue < 0) newValue = 0;
                        mask[idx] = (byte)newValue;
                    }
                }
            }

            MainPic.Invalidate();
        }

    }
}
