using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


// -----------------------------------------------------------------------------
// 파일 역할: 이미지 드래그 앤 드롭 관련 이벤트를 담당한다.
// 병합 작업 때 빠르게 구조를 파악할 수 있도록 주석을 보강한 버전이다.
// -----------------------------------------------------------------------------

namespace Photo
{
    // 메인 편집 창과 공통 편집 상태를 관리하는 핵심 폼 클래스.
    public partial class Main
    {
        // 드래그된 데이터가 들어올 때 허용 여부를 결정한다.
        private void Main_DragEnter(object? sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                string[] files = (string[])e.Data.GetData(DataFormats.FileDrop); if (files.Length > 0)
                {
                    string ext = System.IO.Path.GetExtension(files[0]).ToLower(); if (ext == ".jpg" || ext == ".png" || ext == ".bmp")
                    {
                        e.Effect = DragDropEffects.Copy; // 허용 } } }
                    }
                }
            }
        }

        // 드롭된 파일/데이터를 실제로 불러온다.
        private void Main_DragDrop(object? sender, DragEventArgs e)
        {
            string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
            if (files.Length > 0)
            {
                string filePath = files[0];
                ReadAsBmp.photo_filePath = filePath;
                MainPic.Image = Image.FromFile(filePath);

                Bitmap bmp = new Bitmap(filePath);
                MainPic.Image = new Bitmap(bmp);

                mask = new byte[bmp.Width * bmp.Height];
            }
        }
    }
}
