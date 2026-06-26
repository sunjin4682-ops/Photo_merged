using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Photo
{
    public partial class Sticker_property : Form
    {
        public Sticker_property()
        {
            InitializeComponent();
            LoadStickers();

            //모든 픽처박스에 이벤트 연결
            foreach (Control c in this.Controls)
            {
                if (c is PictureBox pb)
                    pb.MouseDown += Sticker_MouseDown;
            }
        }

        private void LoadStickers()
        {
            // 1. 스티커 폴더 경로 설정
            string stickerPath = Path.Combine(Application.StartupPath, "Stickers");

            // 폴더가 없으면 생성
            if (!Directory.Exists(stickerPath))
            {
                Directory.CreateDirectory(stickerPath);
                return;
            }

            // 2. 폴더 내 이미지 파일들 가져오기
            string[] files = Directory.GetFiles(stickerPath, "*.*")
                .Where(f => f.EndsWith(".png") || f.EndsWith(".jpg") || f.EndsWith(".bmp"))
                .ToArray();
            flowStickers.Controls.Clear();

            foreach (string file in files)
            {
                // 3. 픽처박스 동적 생성
                PictureBox pb = new PictureBox();
                pb.Image = Image.FromFile(file);
                pb.SizeMode = PictureBoxSizeMode.Zoom;
                pb.Width = 80;  // 스티커 목록 크기
                pb.Height = 80;
                pb.Margin = new Padding(5);
                pb.Cursor = Cursors.Hand;

                // 4. 드래그 이벤트 연결
                pb.MouseDown += (s, e) =>
                {
                    if (e.Button == MouseButtons.Left)
                    {
                        // 클릭한 픽처박스의 이미지를 드래그 데이터로 넘김
                        pb.DoDragDrop(pb.Image, DragDropEffects.Copy);
                    }
                };

                flowStickers.Controls.Add(pb);
            }
        }

        private void Sticker_MouseDown(object sender, MouseEventArgs e)
        {
            if (sender is PictureBox pb && pb.Image != null)
            {
                // 드래그 시작(보낼 데이터, 허용할 효과)
                pb.DoDragDrop(pb.Image, DragDropEffects.Copy);
            }
        }

    }
}
