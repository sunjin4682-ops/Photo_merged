using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;


// -----------------------------------------------------------------------------
// 파일 역할: PinchCommand 기능을 명령 패턴으로 실행하고 Undo/Redo와 연결한다.
// 병합 작업 때 빠르게 구조를 파악할 수 있도록 주석을 보강한 버전이다.
// -----------------------------------------------------------------------------

namespace Photo
{
    // 메인 편집 창과 공통 편집 상태를 관리하는 핵심 폼 클래스.
    public partial class Main
    {
        /// <summary>
        /// 핀치 왜곡을 적용하는 명령 클래스.
        /// 메뉴/버튼 이벤트에서는 이 객체만 만들고, 실제 이미지 변경은 Execute-ApplyEffect 흐름에서 처리한다.
        /// </summary>
        internal sealed class PinchCommand : ImageCommandBase
        {
            private readonly double strength;
            private readonly double radiusScale;
            private readonly double aspectX;
            private readonly double aspectY;

            public PinchCommand(
                Main mainForm,
                double strength = 0.42,
                double radiusScale = 1.15,
                double aspectX = 1.00,
                double aspectY = 0.85)
                : base(mainForm)
            {
                this.strength = strength;
                this.radiusScale = radiusScale;
                this.aspectX = aspectX;
                this.aspectY = aspectY;
            }

            /// <summary>
            /// 핀치 왜곡을 적용하기 위한 실제 이미지 처리 루틴이다.
            /// Command 패턴 구조상 이 메서드 안에서만 MainPic의 비트맵을 변경한다.
            /// </summary>
            protected override void ApplyEffect()
            {
                if (mainForm.MainPic.Image == null || mainForm.mask == null)
                    return;

                using ReadAsBmp pinch = new ReadAsBmp();
                pinch.ImageProcessing((Bitmap)mainForm.MainPic.Image);

                unsafe
                {
                    byte* pPixel = (byte*)pinch.photo_bmpdata.Scan0;
                    int w = pinch.photo_bmp.Width;
                    int h = pinch.photo_bmp.Height;
                    int stride = pinch.photo_bmpdata.Stride;

                    PinchFilter.PinchMasked32bpp_BGRA(
                        pPixel, w, h, stride,
                        mainForm.mask,
                        strength,
                        radiusScale,
                        aspectX,
                        aspectY);
                }

                pinch.ProcessingEnd();

                Bitmap result = pinch.photo_bmp;
                pinch.photo_bmp = null;

                // 계산이 끝난 결과 이미지를 메인 화면에 교체한다.
                mainForm.ReplaceMainImage(result);

                // 한 번 적용이 끝난 마스크는 다음 작업을 위해 초기화한다.
                    Array.Clear(mainForm.mask, 0, mainForm.mask.Length);
                mainForm.MainPic.Invalidate();
            }
        }
    }
}
