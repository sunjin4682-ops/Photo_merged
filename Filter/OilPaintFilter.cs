using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


// -----------------------------------------------------------------------------
// 파일 역할: OilPaintFilter 필터의 실제 픽셀 처리 알고리즘을 구현한다.
// 병합 작업 때 빠르게 구조를 파악할 수 있도록 주석을 보강한 버전이다.
// -----------------------------------------------------------------------------

namespace Photo
{
    // 메인 편집 창과 공통 편집 상태를 관리하는 핵심 폼 클래스.
    public partial class Main
    {
        /// <summary>
        /// 유화의 실제 픽셀 연산을 담당하는 필터 클래스.
        /// Command 계층에서 호출되며 여기서는 순수하게 계산만 수행한다.
        /// </summary>
        internal class OilPaintFilter
        {
            unsafe public static void OilPaint32bpp_BGRA(
                byte* src, int width, int height, int stride,
                int radius = 4,
                int intensityLevels = 24,
                double saturationBoost = 1.10,
                double contrastBoost = 1.04)
            {
                if (radius < 1) radius = 1;
                if (intensityLevels < 4) intensityLevels = 4;
                if (saturationBoost < 0.0) saturationBoost = 0.0;
                if (contrastBoost < 0.0) contrastBoost = 0.0;

                int bytes = height * stride;
                byte[] original = new byte[bytes];

                fixed (byte* pOriginal = original)
                {
                    Buffer.MemoryCopy(src, pOriginal, bytes, bytes);

                    int[] count = new int[intensityLevels];
                    int[] sumB = new int[intensityLevels];
                    int[] sumG = new int[intensityLevels];
                    int[] sumR = new int[intensityLevels];

                    for (int y = 0; y < height; y++)
                    {
                        for (int x = 0; x < width; x++)
                        {
                            Array.Clear(count, 0, intensityLevels);
                            Array.Clear(sumB, 0, intensityLevels);
                            Array.Clear(sumG, 0, intensityLevels);
                            Array.Clear(sumR, 0, intensityLevels);

                            int y0 = y - radius;
                            int y1 = y + radius;
                            int x0 = x - radius;
                            int x1 = x + radius;

                            if (x0 < 0) x0 = 0;
                            if (y0 < 0) y0 = 0;
                            if (x1 >= width) x1 = width - 1;
                            if (y1 >= height) y1 = height - 1;

                            for (int yy = y0; yy <= y1; yy++)
                            {
                                for (int xx = x0; xx <= x1; xx++)
                                {
                                    byte* p = pOriginal + yy * stride + xx * 4;

                                    int b = p[0];
                                    int g = p[1];
                                    int r = p[2];

                                    // 밝기/intensity를 bin으로 나눔
                                    double lum = 0.114 * b + 0.587 * g + 0.299 * r;
                                    int idx = (int)(lum * intensityLevels / 256.0);
                                    if (idx < 0) idx = 0;
                                    if (idx >= intensityLevels) idx = intensityLevels - 1;

                                    count[idx]++;
                                    sumB[idx] += b;
                                    sumG[idx] += g;
                                    sumR[idx] += r;
                                }
                            }

                            // 가장 많이 나타난 intensity 구간 선택
                            int bestIdx = 0;
                            int bestCount = count[0];

                            for (int i = 1; i < intensityLevels; i++)
                            {
                                if (count[i] > bestCount)
                                {
                                    bestCount = count[i];
                                    bestIdx = i;
                                }
                            }

                            double outB, outG, outR;

                            if (bestCount > 0)
                            {
                                outB = (double)sumB[bestIdx] / bestCount;
                                outG = (double)sumG[bestIdx] / bestCount;
                                outR = (double)sumR[bestIdx] / bestCount;
                            }
                            else
                            {
                                byte* p = pOriginal + y * stride + x * 4;
                                outB = p[0];
                                outG = p[1];
                                outR = p[2];
                            }

                            // 유채화 느낌을 위해 채도/대비를 살짝 올림
                            ApplySaturation(ref outB, ref outG, ref outR, saturationBoost);
                            ApplyContrast(ref outB, ref outG, ref outR, contrastBoost);

                            byte* dst = src + y * stride + x * 4;
                            byte* orig = pOriginal + y * stride + x * 4;

                            dst[0] = ToByte(outB);
                            dst[1] = ToByte(outG);
                            dst[2] = ToByte(outR);
                            dst[3] = orig[3];
                        }
                    }
                }
            }

            // 현재 효과나 상태를 실제 이미지/편집 상태에 반영한다.
            private static void ApplySaturation(ref double b, ref double g, ref double r, double saturation)
            {
                double gray = 0.114 * b + 0.587 * g + 0.299 * r;

                b = gray + (b - gray) * saturation;
                g = gray + (g - gray) * saturation;
                r = gray + (r - gray) * saturation;

                b = Clamp255(b);
                g = Clamp255(g);
                r = Clamp255(r);
            }

            // 현재 효과나 상태를 실제 이미지/편집 상태에 반영한다.
            private static void ApplyContrast(ref double b, ref double g, ref double r, double contrast)
            {
                b = ((b - 128.0) * contrast) + 128.0;
                g = ((g - 128.0) * contrast) + 128.0;
                r = ((r - 128.0) * contrast) + 128.0;

                b = Clamp255(b);
                g = Clamp255(g);
                r = Clamp255(r);
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
