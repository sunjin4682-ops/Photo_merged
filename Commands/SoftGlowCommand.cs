using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


// -----------------------------------------------------------------------------
// 파일 역할: SoftGlowCommand 기능을 명령 패턴으로 실행하고 Undo/Redo와 연결한다.
// 병합 작업 때 빠르게 구조를 파악할 수 있도록 주석을 보강한 버전이다.
// -----------------------------------------------------------------------------

namespace Photo
{
    // 메인 편집 창과 공통 편집 상태를 관리하는 핵심 폼 클래스.
    public partial class Main
    {
        /// <summary>
        /// 부드러운 발광 효과를 추가하는 명령 클래스.
        /// 메뉴/버튼 이벤트에서는 이 객체만 만들고, 실제 이미지 변경은 Execute-ApplyEffect 흐름에서 처리한다.
        /// </summary>
        internal sealed class SoftGlowCommand : ImageCommandBase
        {
            private readonly int radius;
            private readonly double blurBlend;
            private readonly double brightnessBoost;
            private readonly double contrastSoften;
            private readonly double glowThreshold;
            private readonly double glowStrength;

            public SoftGlowCommand(
                Main mainForm,
                int radius = 5,
                double blurBlend = 0.48,
                double brightnessBoost = 14.0,
                double contrastSoften = 0.90,
                double glowThreshold = 170.0,
                double glowStrength = 0.35)
                : base(mainForm)
            {
                this.radius = radius;
                this.blurBlend = blurBlend;
                this.brightnessBoost = brightnessBoost;
                this.contrastSoften = contrastSoften;
                this.glowThreshold = glowThreshold;
                this.glowStrength = glowStrength;
            }

            /// <summary>
            /// 부드러운 발광 효과를 추가하기 위한 실제 이미지 처리 루틴이다.
            /// Command 패턴 구조상 이 메서드 안에서만 MainPic의 비트맵을 변경한다.
            /// </summary>
            protected override void ApplyEffect()
            {
                // 편집할 원본 이미지가 없으면 아무 작업도 하지 않는다.
                if (mainForm.MainPic.Image == null)
                    return;

                using ReadAsBmp glow = new ReadAsBmp();
                glow.ImageProcessing((Bitmap)mainForm.MainPic.Image);

                unsafe
                {
                    byte* pPixel = (byte*)glow.photo_bmpdata.Scan0;
                    int w = glow.photo_bmp.Width;
                    int h = glow.photo_bmp.Height;
                    int stride = glow.photo_bmpdata.Stride;

                    SoftGlowFilter.SoftGlow32bpp_BGRA(
                        pPixel, w, h, stride,
                        radius,
                        blurBlend,
                        brightnessBoost,
                        contrastSoften,
                        glowThreshold,
                        glowStrength);
                }

                glow.ProcessingEnd();

                Bitmap result = glow.photo_bmp;
                glow.photo_bmp = null;

                // 계산이 끝난 결과 이미지를 메인 화면에 교체한다.
                mainForm.ReplaceMainImage(result);

                if (mainForm.mask != null)
                    // 한 번 적용이 끝난 마스크는 다음 작업을 위해 초기화한다.
                    Array.Clear(mainForm.mask, 0, mainForm.mask.Length);

                mainForm.MainPic.Invalidate();
            }
        }
    }
}
