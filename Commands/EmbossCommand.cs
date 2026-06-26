using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


// -----------------------------------------------------------------------------
// 파일 역할: EmbossCommand 기능을 명령 패턴으로 실행하고 Undo/Redo와 연결한다.
// 병합 작업 때 빠르게 구조를 파악할 수 있도록 주석을 보강한 버전이다.
// -----------------------------------------------------------------------------

namespace Photo
{
    // 메인 편집 창과 공통 편집 상태를 관리하는 핵심 폼 클래스.
    public partial class Main
    {
        /// <summary>
        /// 입체감 있는 엠보스 질감을 생성하는 명령 클래스.
        /// 메뉴/버튼 이벤트에서는 이 객체만 만들고, 실제 이미지 변경은 Execute-ApplyEffect 흐름에서 처리한다.
        /// </summary>
        internal sealed class MetalEmbossCommand : ImageCommandBase
        {
            private readonly double angleDeg;
            private readonly double elevationDeg;
            private readonly double depth;
            private readonly double blurSigma;
            private readonly double contrast;
            private readonly double specularStrength;
            private readonly double specularPower;
            private readonly bool preserveColor;
            private readonly double colorBlend;

            public MetalEmbossCommand(
                Main mainForm,
                double angleDeg = 135.0,
                double elevationDeg = 42.0,
                double depth = 2.4,
                double blurSigma = 1.0,
                double contrast = 1.28,
                double specularStrength = 0.75,
                double specularPower = 18.0,
                bool preserveColor = false,
                double colorBlend = 0.22)
                : base(mainForm)
            {
                this.angleDeg = angleDeg;
                this.elevationDeg = elevationDeg;
                this.depth = depth;
                this.blurSigma = blurSigma;
                this.contrast = contrast;
                this.specularStrength = specularStrength;
                this.specularPower = specularPower;
                this.preserveColor = preserveColor;
                this.colorBlend = colorBlend;
            }

            /// <summary>
            /// 입체감 있는 엠보스 질감을 생성하기 위한 실제 이미지 처리 루틴이다.
            /// Command 패턴 구조상 이 메서드 안에서만 MainPic의 비트맵을 변경한다.
            /// </summary>
            protected override void ApplyEffect()
            {
                // 편집할 원본 이미지가 없으면 아무 작업도 하지 않는다.
                if (mainForm.MainPic.Image == null)
                    return;

                using ReadAsBmp emboss = new ReadAsBmp();
                emboss.ImageProcessing((Bitmap)mainForm.MainPic.Image);

                unsafe
                {
                    byte* pPixel = (byte*)emboss.photo_bmpdata.Scan0;
                    int w = emboss.photo_bmp.Width;
                    int h = emboss.photo_bmp.Height;
                    int stride = emboss.photo_bmpdata.Stride;

                    MetalEmbossFilter.ApplyMetalEmboss32bpp_BGRA(
                        pPixel, w, h, stride,
                        angleDeg,
                        elevationDeg,
                        depth,
                        blurSigma,
                        contrast,
                        specularStrength,
                        specularPower,
                        preserveColor,
                        colorBlend);
                }

                emboss.ProcessingEnd();

                Bitmap result = emboss.photo_bmp;
                emboss.photo_bmp = null;

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
