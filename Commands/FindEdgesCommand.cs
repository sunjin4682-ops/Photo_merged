using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


// -----------------------------------------------------------------------------
// 파일 역할: FindEdgesCommand 기능을 명령 패턴으로 실행하고 Undo/Redo와 연결한다.
// 병합 작업 때 빠르게 구조를 파악할 수 있도록 주석을 보강한 버전이다.
// -----------------------------------------------------------------------------

namespace Photo
{
    // 메인 편집 창과 공통 편집 상태를 관리하는 핵심 폼 클래스.
    public partial class Main
    {
        /// <summary>
        /// 윤곽선을 검출하는 명령 클래스.
        /// 메뉴/버튼 이벤트에서는 이 객체만 만들고, 실제 이미지 변경은 Execute-ApplyEffect 흐름에서 처리한다.
        /// </summary>
        internal sealed class FindEdgesCommand : ImageCommandBase
        {
            private readonly double edgeStrength;
            private readonly double threshold;
            private readonly double gamma;
            private readonly double colorSensitivity;
            private readonly double lineOpacity;
            private readonly bool invertToBlackLines;

            public FindEdgesCommand(
                Main mainForm,
                double edgeStrength = 1.25,
                double threshold = 0.10,
                double gamma = 0.85,
                double colorSensitivity = 0.55,
                double lineOpacity = 1.0,
                bool invertToBlackLines = true)
                : base(mainForm)
            {
                this.edgeStrength = edgeStrength;
                this.threshold = threshold;
                this.gamma = gamma;
                this.colorSensitivity = colorSensitivity;
                this.lineOpacity = lineOpacity;
                this.invertToBlackLines = invertToBlackLines;
            }

            /// <summary>
            /// 윤곽선을 검출하기 위한 실제 이미지 처리 루틴이다.
            /// Command 패턴 구조상 이 메서드 안에서만 MainPic의 비트맵을 변경한다.
            /// </summary>
            protected override void ApplyEffect()
            {
                // 편집할 원본 이미지가 없으면 아무 작업도 하지 않는다.
                if (mainForm.MainPic.Image == null)
                    return;

                using ReadAsBmp edge = new ReadAsBmp();
                edge.ImageProcessing((Bitmap)mainForm.MainPic.Image);

                unsafe
                {
                    byte* pPixel = (byte*)edge.photo_bmpdata.Scan0;
                    int w = edge.photo_bmp.Width;
                    int h = edge.photo_bmp.Height;
                    int stride = edge.photo_bmpdata.Stride;

                    FindEdgesFilter.ApplyFindEdges32bpp_BGRA(
                        pPixel, w, h, stride,
                        edgeStrength,
                        threshold,
                        gamma,
                        colorSensitivity,
                        lineOpacity,
                        invertToBlackLines);
                }

                edge.ProcessingEnd();

                Bitmap result = edge.photo_bmp;
                edge.photo_bmp = null;

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
