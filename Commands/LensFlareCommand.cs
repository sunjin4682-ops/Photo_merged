using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


// -----------------------------------------------------------------------------
// 파일 역할: LensFlareCommand 기능을 명령 패턴으로 실행하고 Undo/Redo와 연결한다.
// 병합 작업 때 빠르게 구조를 파악할 수 있도록 주석을 보강한 버전이다.
// -----------------------------------------------------------------------------

namespace Photo
{
    // 메인 편집 창과 공통 편집 상태를 관리하는 핵심 폼 클래스.
    public partial class Main
    {
        /// <summary>
        /// 렌즈 플레어 조명을 추가하는 명령 클래스.
        /// 메뉴/버튼 이벤트에서는 이 객체만 만들고, 실제 이미지 변경은 Execute-ApplyEffect 흐름에서 처리한다.
        /// </summary>
        internal sealed class SoftLensFlareCommand : ImageCommandBase
        {
            private readonly double intensity;
            private readonly double bloomRadius;
            private readonly double haloRadius;
            private readonly double haloWidth;
            private readonly double veilStrength;
            private readonly double ghostStrength;
            private readonly double streakStrength;
            private readonly double pinkHaloStrength;
            private readonly double blueGhostStrength;
            private readonly int rayCount;
            private readonly double rayStrength;
            private readonly double raySharpness;
            private readonly double rayLength;

            public SoftLensFlareCommand(
                Main mainForm,
                double intensity = 1.0,
                double bloomRadius = 0.20,
                double haloRadius = 0.18,
                double haloWidth = 0.035,
                double veilStrength = 0.24,
                double ghostStrength = 0.16,
                double streakStrength = 0.06,
                double pinkHaloStrength = 0.42,
                double blueGhostStrength = 0.18,
                int rayCount = 6,
                double rayStrength = 0.30,
                double raySharpness = 20.0,
                double rayLength = 0.42)
                : base(mainForm)
            {
                this.intensity = intensity;
                this.bloomRadius = bloomRadius;
                this.haloRadius = haloRadius;
                this.haloWidth = haloWidth;
                this.veilStrength = veilStrength;
                this.ghostStrength = ghostStrength;
                this.streakStrength = streakStrength;
                this.pinkHaloStrength = pinkHaloStrength;
                this.blueGhostStrength = blueGhostStrength;
                this.rayCount = rayCount;
                this.rayStrength = rayStrength;
                this.raySharpness = raySharpness;
                this.rayLength = rayLength;
            }

            /// <summary>
            /// 렌즈 플레어 조명을 추가하기 위한 실제 이미지 처리 루틴이다.
            /// Command 패턴 구조상 이 메서드 안에서만 MainPic의 비트맵을 변경한다.
            /// </summary>
            protected override void ApplyEffect()
            {
                // 편집할 원본 이미지가 없으면 아무 작업도 하지 않는다.
                if (mainForm.MainPic.Image == null)
                    return;

                using ReadAsBmp bmp = new ReadAsBmp();
                bmp.ImageProcessing((Bitmap)mainForm.MainPic.Image);

                unsafe
                {
                    byte* pPixel = (byte*)bmp.photo_bmpdata.Scan0;
                    int w = bmp.photo_bmp.Width;
                    int h = bmp.photo_bmp.Height;
                    int stride = bmp.photo_bmpdata.Stride;

                    SoftLensFlareFilter.ApplySoftLensFlare32bpp_BGRA(
                        pPixel,
                        w,
                        h,
                        stride,
                        mainForm.mask,
                        intensity,
                        bloomRadius,
                        haloRadius,
                        haloWidth,
                        veilStrength,
                        ghostStrength,
                        streakStrength,
                        pinkHaloStrength,
                        blueGhostStrength,
                        rayCount,
                        rayStrength,
                        raySharpness,
                        rayLength);
                }

                bmp.ProcessingEnd();

                Bitmap result = bmp.photo_bmp;
                bmp.photo_bmp = null;

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
