using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Photo.Service
{
    public static class ZoomService
    {
        public static Point TranslateZoomMousePosition(PictureBox MainPic, Bitmap workingImage, Point mousePos)
        {
            if (MainPic.Image == null || workingImage == null) return Point.Empty;

            float imageAspect = (float)MainPic.Image.Width / MainPic.Image.Height;
            float controlAspect = (float)MainPic.Width / MainPic.Height;

            float scale, xOffset = 0, yOffset = 0;

            if (imageAspect > controlAspect)
            {
                scale = (float)MainPic.Width / MainPic.Image.Width;
                yOffset = (MainPic.Height - (MainPic.Image.Height * scale)) / 2;
            }
            else
            {
                scale = (float)MainPic.Height / MainPic.Image.Height;
                xOffset = (MainPic.Width - (MainPic.Image.Width * scale)) / 2;
            }

            int imgX = (int)((mousePos.X - xOffset) / scale);
            int imgY = (int)((mousePos.Y - yOffset) / scale);

            if (imgX >= 0 && imgX < workingImage.Width && imgY >= 0 && imgY < workingImage.Height)
                return new Point(imgX, imgY);

            return Point.Empty;
        }

        public static Rectangle GetImageRectangle(PictureBox pbox, Bitmap workingImage)
        {
            if (pbox == null || workingImage == null)
                return Rectangle.Empty;

            if (pbox.Width <= 0 || pbox.Height <= 0)
                return Rectangle.Empty;

            if (workingImage.Width <= 0 || workingImage.Height <= 0)
                return Rectangle.Empty;

            float imgAspect = (float)workingImage.Width / workingImage.Height;
            float boxAspect = (float)pbox.Width / pbox.Height;
            int w, h, x, y;

            if (imgAspect > boxAspect)
            {
                w = pbox.Width;
                h = (int)(pbox.Width / imgAspect);
                x = 0;
                y = (pbox.Height - h) / 2;
            }
            else
            {
                h = pbox.Height;
                w = (int)(pbox.Height * imgAspect);
                x = (pbox.Width - w) / 2;
                y = 0;
            }

            return new Rectangle(x, y, w, h);
        }
    }
}
