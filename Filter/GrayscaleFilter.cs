using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


// -----------------------------------------------------------------------------
// 파일 역할: GrayscaleFilter 필터의 실제 픽셀 처리 알고리즘을 구현한다.
// 병합 작업 때 빠르게 구조를 파악할 수 있도록 주석을 보강한 버전이다.
// -----------------------------------------------------------------------------

namespace Photo
{
     // 메인 편집 창과 공통 편집 상태를 관리하는 핵심 폼 클래스.
     public partial class Main
    {
        /// <summary>
        /// 흑백 변환의 실제 픽셀 연산을 담당하는 필터 클래스.
        /// Command 계층에서 호출되며 여기서는 순수하게 계산만 수행한다.
        /// </summary>
        internal class GrayscaleFilter
        {
            unsafe public static void Grayscale32bpp_BGRA(
            byte* src, int width, int height, int stride)
            {
                for (int y = 0; y < height; y++)
                {
                    byte* row = src + y * stride;

                    for (int x = 0; x < width; x++)
                    {
                        byte* p = row + x * 4;

                        byte b = p[0];
                        byte g = p[1];
                        byte r = p[2];

                        byte gray = ToByte(0.114 * b + 0.587 * g + 0.299 * r);

                        p[0] = gray; // B
                        p[1] = gray; // G
                        p[2] = gray; // R
                                     // p[3] = alpha 유지
                    }
                }
            }

            /// <summary>
            /// 계산 결과를 0~255 범위의 byte로 안전하게 변환한다.
            /// </summary>
            private static byte ToByte(double v)
            {
                int iv = (int)Math.Round(v);
                if (iv < 0) return 0;
                if (iv > 255) return 255;
                return (byte)iv;
            }
        }
    }
}
