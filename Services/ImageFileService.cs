using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Photo.Service
{
    public static class ImageFileService
    {
        public static Bitmap LoadBitmap(string path)
        {
            return new Bitmap(path);
        }
    }
}
