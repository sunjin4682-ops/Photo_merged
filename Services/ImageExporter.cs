using Photo.Models;
using System;
using System.Collections.Generic;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Photo.Services
{
    public static class ImageExporter
    {


        // 콜라주 프레임들을 하나의 합쳐진 비트맵으로 만들어 반환
        public static Bitmap FlattenCollage(List<CollageFrame> frames, int width, int height)
        {
            // 주의: 이 비트맵은 반환되어 MainPic에서 사용되므로 using 문을 쓰지 않습니다.
            Bitmap canvas = new Bitmap(width, height);

            try
            {
                using (Graphics g = Graphics.FromImage(canvas))
                {
                    // 1. 고화질 설정
                    g.SmoothingMode = SmoothingMode.AntiAlias;
                    g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                    g.PixelOffsetMode = PixelOffsetMode.HighQuality;
                    g.Clear(Color.White); // 배경을 흰색으로 초기화

                    // 2. 각 프레임 그리기
                    foreach (var frame in frames)
                    {
                        if (frame.Photo != null)
                        {
                            // 칸 밖으로 나가지 않게 영역 제한
                            g.SetClip(frame.Area);

                            // 비율 유지 좌표 계산 (이전 답변의 함수 활용)
                            RectangleF drawRect = GetAspectFillRect(frame.Area, frame.Photo);

                            g.DrawImage(frame.Photo, drawRect);

                            g.ResetClip();
                        }

                        // 칸 구분선 그리기 (합쳐진 이미지에 경계선을 남기고 싶을 때)
                        g.DrawRectangle(Pens.White, Rectangle.Round(frame.Area));
                    }
                }

                return canvas; // 합쳐진 비트맵 반환
            }
            catch
            {
                // 에러 발생 시 메모리 해제 후 throw
                canvas.Dispose();
                throw;
            }
        }

        // 비율 유지 계산 헬퍼 함수 (내부 사용)
        private static RectangleF GetAspectFillRect(RectangleF frameArea, Image img)
        {
            float scale = Math.Max(frameArea.Width / img.Width, frameArea.Height / img.Height);
            float newWidth = img.Width * scale;
            float newHeight = img.Height * scale;
            float x = frameArea.X + (frameArea.Width - newWidth) / 2;
            float y = frameArea.Y + (frameArea.Height - newHeight) / 2;
            return new RectangleF(x, y, newWidth, newHeight);
        }
    }
}
