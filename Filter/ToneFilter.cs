using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


// -----------------------------------------------------------------------------
// 파일 역할: ToneFilter 필터의 실제 픽셀 처리 알고리즘을 구현한다.
// 병합 작업 때 빠르게 구조를 파악할 수 있도록 주석을 보강한 버전이다.
// -----------------------------------------------------------------------------

namespace Photo
{
    // 메인 편집 창과 공통 편집 상태를 관리하는 핵심 폼 클래스.
    public partial class Main {
        // ToneMode 관련 역할을 담당하는 타입이다.
        internal enum ToneMode
        {
            Warm,
            Cool
        }

        /// <summary>
        /// 톤 보정의 실제 픽셀 연산을 담당하는 필터 클래스.
        /// Command 계층에서 호출되며 여기서는 순수하게 계산만 수행한다.
        /// </summary>
        internal class ToneFilter
        {
            unsafe public static void ApplyTone32bpp_BGRA(
                byte* src, int width, int height, int stride,
                ToneMode mode, double strength = 0.18)
            {
                if (strength < 0.0) strength = 0.0;
                if (strength > 1.0) strength = 1.0;

                for (int y = 0; y < height; y++)
                {
                    byte* row = src + y * stride;

                    for (int x = 0; x < width; x++)
                    {
                        byte* p = row + x * 4;

                        double b = p[0];
                        double g = p[1];
                        double r = p[2];

                        double targetB = b;
                        double targetG = g;
                        double targetR = r;

                        if (mode == ToneMode.Warm)
                        {
                            targetR = Clamp255(r + 40.0);
                            targetG = Clamp255(g + 12.0);
                            targetB = Clamp255(b - 28.0);
                        }
                        else // Cool
                        {
                            targetR = Clamp255(r - 28.0);
                            targetG = Clamp255(g + 8.0);
                            targetB = Clamp255(b + 50.0);
                        }

                        p[0] = ToByte(b * (1.0 - strength) + targetB * strength); // B
                        p[1] = ToByte(g * (1.0 - strength) + targetG * strength); // G
                        p[2] = ToByte(r * (1.0 - strength) + targetR * strength); // R
                                                                                  // p[3] 알파 유지
                    }
                }
            }

            // 이 파일의 핵심 동작을 수행하는 메서드.
            private static double Clamp255(double v)
            {
                if (v < 0.0) return 0.0;
                if (v > 255.0) return 255.0;
                return v;
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
