using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


// -----------------------------------------------------------------------------
// 파일 역할: MotionBlurCommand 기능을 명령 패턴으로 실행하고 Undo/Redo와 연결한다.
// 병합 작업 때 빠르게 구조를 파악할 수 있도록 주석을 보강한 버전이다.
// -----------------------------------------------------------------------------

namespace Photo
{
    // 메인 편집 창과 공통 편집 상태를 관리하는 핵심 폼 클래스.
    public partial class Main
    {
        /// <summary>
        /// 이동 방향 잔상 형태의 모션 블러를 적용하는 명령 클래스.
        /// 메뉴/버튼 이벤트에서는 이 객체만 만들고, 실제 이미지 변경은 Execute-ApplyEffect 흐름에서 처리한다.
        /// </summary>
        internal sealed class MotionBlurCommand : ImageCommandBase
        {
            private readonly double angleDeg;
            private readonly double length;
            private readonly int samples;
            private readonly double sigmaScale;
            private readonly double centerBias;
            private readonly bool bidirectional;
            private readonly double maskStrength;
            private readonly double falloffPower;

            public MotionBlurCommand(
                Main mainForm,
                double angleDeg = 0.0,
                double length = 24.0,
                int samples = 33,
                double sigmaScale = 0.35,
                double centerBias = 1.35,
                bool bidirectional = false,
                double maskStrength = 1.0,
                double falloffPower = 1.0)
                : base(mainForm)
            {
                this.angleDeg = angleDeg;
                this.length = length;
                this.samples = samples;
                this.sigmaScale = sigmaScale;
                this.centerBias = centerBias;
                this.bidirectional = bidirectional;
                this.maskStrength = maskStrength;
                this.falloffPower = falloffPower;
            }

            /// <summary>
            /// 이동 방향 잔상 형태의 모션 블러를 적용하기 위한 실제 이미지 처리 루틴이다.
            /// Command 패턴 구조상 이 메서드 안에서만 MainPic의 비트맵을 변경한다.
            /// </summary>
            protected override void ApplyEffect()
            {
                // 편집할 원본 이미지가 없으면 아무 작업도 하지 않는다.
                if (mainForm.MainPic.Image == null)
                    return;

                using ReadAsBmp motion = new ReadAsBmp();
                motion.ImageProcessing((Bitmap)mainForm.MainPic.Image);

                unsafe
                {
                    byte* pPixel = (byte*)motion.photo_bmpdata.Scan0;
                    int w = motion.photo_bmp.Width;
                    int h = motion.photo_bmp.Height;
                    int stride = motion.photo_bmpdata.Stride;

                    // 마스크가 없거나 전부 0이면 적용 안 함
                    if (mainForm.mask == null || mainForm.mask.Length != w * h)
                    {
                        motion.ProcessingEnd();
                        return;
                    }

                    bool hasMask = false;
                    for (int i = 0; i < mainForm.mask.Length; i++)
                    {
                        if (mainForm.mask[i] != 0)
                        {
                            hasMask = true;
                            break;
                        }
                    }

                    if (!hasMask)
                    {
                        motion.ProcessingEnd();
                        return;
                    }

                    byte[] effectImage = MotionBlurFilter.CreateMotionBlurImage32bpp_BGRA(
                        pPixel, w, h, stride,
                        angleDeg,
                        length,
                        samples,
                        sigmaScale,
                        centerBias,
                        bidirectional);

                    MotionBlurFilter.BlendWithMask32bpp_BGRA(
                        pPixel,
                        effectImage,
                        mainForm.mask,
                        w,
                        h,
                        stride,
                        maskStrength,
                        falloffPower);
                }

                motion.ProcessingEnd();

                Bitmap result = motion.photo_bmp;
                motion.photo_bmp = null;

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
