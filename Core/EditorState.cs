using System;
using System.Collections.Generic;
using System.Drawing.Imaging;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


// -----------------------------------------------------------------------------
// 파일 역할: 이미지와 마스크를 함께 보관하는 편집 상태 스냅샷을 정의한다.
// 병합 작업 때 빠르게 구조를 파악할 수 있도록 주석을 보강한 버전이다.
// -----------------------------------------------------------------------------

namespace Photo
{
    // 메인 편집 창과 공통 편집 상태를 관리하는 핵심 폼 클래스.
    public partial class Main
    {
        //공통 상태 구조(이미지 + 마스크 한 세트를 저장함)
        internal sealed class EditorState : IDisposable
        {
            public Bitmap Image { get; }
            public byte[] Mask { get; }
            public Bitmap? DrawLayer { get; }

            public EditorState(Bitmap image, byte[] mask, Bitmap? drawLayer)
            {
                Image = CloneBitmap(image);
                Mask = (byte[])mask.Clone();
                DrawLayer = drawLayer != null ? CloneBitmap(drawLayer) : null;
            }

            private static Bitmap CloneBitmap(Bitmap source)
            {
                return source.Clone(
                    new Rectangle(0, 0, source.Width, source.Height),
                    PixelFormat.Format32bppArgb);
            }

            public void Dispose()
            {
                Image.Dispose();
                DrawLayer?.Dispose();
            }
        }
    }
}
