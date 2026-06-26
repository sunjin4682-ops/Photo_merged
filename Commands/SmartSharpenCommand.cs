using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


// -----------------------------------------------------------------------------
// 파일 역할: SmartSharpenCommand 기능을 명령 패턴으로 실행하고 Undo/Redo와 연결한다.
// 병합 작업 때 빠르게 구조를 파악할 수 있도록 주석을 보강한 버전이다.
// -----------------------------------------------------------------------------

namespace Photo
{
    // 메인 편집 창과 공통 편집 상태를 관리하는 핵심 폼 클래스.
    public partial class Main
    {
        /// <summary>
        /// 스마트 샤픈으로 선명도를 높임하는 명령 클래스.
        /// 메뉴/버튼 이벤트에서는 이 객체만 만들고, 실제 이미지 변경은 Execute-ApplyEffect 흐름에서 처리한다.
        /// </summary>
        internal sealed class SmartSharpenCommand : ImageCommandBase
        {
            private readonly double radius;
            private readonly double amount;
            private readonly double threshold;
            private readonly double edgeBoost;
            private readonly double noiseSuppression;
            private readonly double highlightFade;
            private readonly double shadowFade;
            private readonly double antiHalo;

            public SmartSharpenCommand(
                Main mainForm,
                double radius = 1.8,
                double amount = 1.35,
                double threshold = 4.0,
                double edgeBoost = 1.0,
                double noiseSuppression = 0.65,
                double highlightFade = 0.25,
                double shadowFade = 0.18,
                double antiHalo = 1.0)
                : base(mainForm)
            {
                this.radius = radius;
                this.amount = amount;
                this.threshold = threshold;
                this.edgeBoost = edgeBoost;
                this.noiseSuppression = noiseSuppression;
                this.highlightFade = highlightFade;
                this.shadowFade = shadowFade;
                this.antiHalo = antiHalo;
            }

            /// <summary>
            /// 스마트 샤픈으로 선명도를 높임하기 위한 실제 이미지 처리 루틴이다.
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

                    byte[] effect = SmartSharpenFilter.CreateSmartSharpenImage32bpp_BGRA(
                        pPixel,
                        w,
                        h,
                        stride,
                        radius,
                        amount,
                        threshold,
                        edgeBoost,
                        noiseSuppression,
                        highlightFade,
                        shadowFade,
                        antiHalo);

                    fixed (byte* pEffect = effect)
                    {
                        Buffer.MemoryCopy(pEffect, pPixel, h * stride, h * stride);
                    }
                }

                bmp.ProcessingEnd();

                Bitmap result = bmp.photo_bmp;
                bmp.photo_bmp = null;

                // 계산이 끝난 결과 이미지를 메인 화면에 교체한다.
                mainForm.ReplaceMainImage(result);
                mainForm.MainPic.Invalidate();
            }
        }
    }
}
