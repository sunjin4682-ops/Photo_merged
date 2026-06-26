using System;
using System.Collections.Generic;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Photo.Service
{
    public static class DrawService
    {
        public static void DrawLine(Bitmap drawLayer, Point from, Point to, Color color, int brushSize)
        {
            using (Graphics g = Graphics.FromImage(drawLayer))
            using (Pen p = new Pen(color, brushSize))
            {
                g.SmoothingMode = SmoothingMode.AntiAlias;
                p.StartCap = p.EndCap = LineCap.Round;
                g.DrawLine(p, from, to);
            }
        }

        public static void EraseLine(Bitmap drawLayer, Point from, Point to, int brushSize)
        {
            using (Graphics g = Graphics.FromImage(drawLayer))
            using (Pen p = new Pen(Color.Transparent, brushSize))
            {
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.CompositingMode = CompositingMode.SourceCopy;
                p.StartCap = p.EndCap = LineCap.Round;
                g.DrawLine(p, from, to);
            }
        }
    }
}
