using Photo.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Photo.Services
{
    public static class CollageService
    {
        // 콜라주 프레임을 만드는 메서드
        public static List<CollageFrame> CreateLayout(int type, int width, int height)
        {
            var frames = new List<CollageFrame>();
            switch (type)
            {
                case 1: // 좌우 2분할
                    frames.Add(new CollageFrame(new RectangleF(0, 0, width / 2, height)));
                    frames.Add(new CollageFrame(new RectangleF(width / 2, 0, width / 2, height)));
                    break;
                case 2: // 상하 2분할
                    frames.Add(new CollageFrame(new RectangleF(0, 0, width, height / 2)));
                    frames.Add(new CollageFrame(new RectangleF(0, height / 2, width, height / 2)));
                    break;
                case 3: // 위 1, 아래 2분할
                    frames.Add(new CollageFrame(new RectangleF(0, 0, width, height / 2)));
                    frames.Add(new CollageFrame(new RectangleF(0, height / 2, width / 2, height / 2)));
                    frames.Add(new CollageFrame(new RectangleF(width / 2, height / 2, width / 2, height / 2)));
                    break;
                case 4: // 2x2 격자
                    float w2 = width / 2f; float h2 = height / 2f;
                    frames.Add(new CollageFrame(new RectangleF(0, 0, w2, h2)));
                    frames.Add(new CollageFrame(new RectangleF(w2, 0, w2, h2)));
                    frames.Add(new CollageFrame(new RectangleF(0, h2, w2, h2)));
                    frames.Add(new CollageFrame(new RectangleF(w2, h2, w2, h2)));
                    break;
            }
            return frames;
        }

        // 콜라주에 추가되는 이미지의 비율을 유지하기 위한 메서드
        public static RectangleF GetAspectFillRect(RectangleF frameArea, Image img)
        {
            float imgWidth = img.Width;
            float imgHeight = img.Height;
            float frameWidth = frameArea.Width;
            float frameHeight = frameArea.Height;

            // 1. 비율 계산
            float ratioX = frameWidth / imgWidth;
            float ratioY = frameHeight / imgHeight;

            // 2. 더 큰 비율을 선택 (칸을 꽉 채우기 위해)
            float scale = Math.Max(ratioX, ratioY);

            // 3. 스케일링된 크기 계산
            float newWidth = imgWidth * scale;
            float newHeight = imgHeight * scale;

            // 4. 중앙 정렬을 위한 위치 계산
            float x = frameArea.X + (frameWidth - newWidth) / 2;
            float y = frameArea.Y + (frameHeight - newHeight) / 2;

            return new RectangleF(x, y, newWidth, newHeight);
        }

        

    }
}
