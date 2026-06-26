using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


// -----------------------------------------------------------------------------
// 파일 역할: BrightnessContrastFilter 필터의 실제 픽셀 처리 알고리즘을 구현한다.
// 병합 작업 때 빠르게 구조를 파악할 수 있도록 주석을 보강한 버전이다.
// -----------------------------------------------------------------------------

namespace Photo
{
    // 메인 편집 창과 공통 편집 상태를 관리하는 핵심 폼 클래스.
    public partial class Main
    {
        /// <summary>
        /// 밝기/대비의 실제 픽셀 연산을 담당하는 필터 클래스.
        /// Command 계층에서 호출되며 여기서는 순수하게 계산만 수행한다.
        /// </summary>
        internal static class BrightnessContrastFilter
        {
            unsafe public static void ApplyBrightnessContrast32bpp_BGRA(
                byte* src, int width, int height, int stride,
                int brightness,      // -255 ~ 255
                double contrast)     // 0.0 ~ 3.0, 1.0 = 원본
            {
                if (src == null || width <= 0 || height <= 0) return;

                if (brightness < -255) brightness = -255;
                if (brightness > 255) brightness = 255;
                if (contrast < 0.0) contrast = 0.0;
                if (contrast > 5.0) contrast = 5.0;

                int bytes = stride * height;
                byte[] original = new byte[bytes];

                fixed (byte* pOriginal = original)
                {
                    Buffer.MemoryCopy(src, pOriginal, bytes, bytes);

                    for (int y = 0; y < height; y++)
                    {
                        int row = y * stride;

                        for (int x = 0; x < width; x++)
                        {
                            int idx = row + x * 4;

                            byte* s = pOriginal + idx;

                            double b = s[0];
                            double g = s[1];
                            double r = s[2];
                            byte a = s[3];

                            // 밝기 적용
                            b += brightness;
                            g += brightness;
                            r += brightness;

                            // 대비 적용 (128 기준)
                            b = (b - 128.0) * contrast + 128.0;
                            g = (g - 128.0) * contrast + 128.0;
                            r = (r - 128.0) * contrast + 128.0;

                            src[idx + 0] = ToByte(b);
                            src[idx + 1] = ToByte(g);
                            src[idx + 2] = ToByte(r);
                            src[idx + 3] = a;
                        }
                    }
                }
            }

            /// <summary>
            /// 계산 결과를 0~255 범위의 byte로 안전하게 변환한다.
            /// </summary>
            private static byte ToByte(double v)
            {
                if (v <= 0.0) return 0;
                if (v >= 255.0) return 255;
                return (byte)(v + 0.5);
            }
        }
    }
}
