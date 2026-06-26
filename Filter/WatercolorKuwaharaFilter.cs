using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


// -----------------------------------------------------------------------------
// 파일 역할: WatercolorKuwaharaFilter 필터의 실제 픽셀 처리 알고리즘을 구현한다.
// 병합 작업 때 빠르게 구조를 파악할 수 있도록 주석을 보강한 버전이다.
// -----------------------------------------------------------------------------

namespace Photo
{
    // 메인 편집 창과 공통 편집 상태를 관리하는 핵심 폼 클래스.
    public partial class Main
    {
        /// <summary>
        /// 수채화의 실제 픽셀 연산을 담당하는 필터 클래스.
        /// Command 계층에서 호출되며 여기서는 순수하게 계산만 수행한다.
        /// </summary>
        internal class WatercolorKuwaharaFilter
        {
            unsafe public static void WatercolorKuwahara32bpp_BGRA(
                byte* src, int width, int height, int stride,
                int radius = 4,
                int colorLevels = 10,
                double saturationBoost = 1.08)
            {
                if (radius < 1) radius = 1;
                if (colorLevels < 2) colorLevels = 2;
                if (saturationBoost < 0.0) saturationBoost = 0.0;

                int bytes = height * stride;
                byte[] original = new byte[bytes];

                fixed (byte* pOriginal = original)
                {
                    Buffer.MemoryCopy(src, pOriginal, bytes, bytes);

                    for (int y = 0; y < height; y++)
                    {
                        for (int x = 0; x < width; x++)
                        {
                            RegionStat s1 = ComputeRegion(pOriginal, width, height, stride,
                                x - radius, y - radius, x, y);

                            RegionStat s2 = ComputeRegion(pOriginal, width, height, stride,
                                x, y - radius, x + radius, y);

                            RegionStat s3 = ComputeRegion(pOriginal, width, height, stride,
                                x - radius, y, x, y + radius);

                            RegionStat s4 = ComputeRegion(pOriginal, width, height, stride,
                                x, y, x + radius, y + radius);

                            RegionStat best = s1;
                            if (s2.Variance < best.Variance) best = s2;
                            if (s3.Variance < best.Variance) best = s3;
                            if (s4.Variance < best.Variance) best = s4;

                            double b = Quantize(best.MeanB, colorLevels);
                            double g = Quantize(best.MeanG, colorLevels);
                            double r = Quantize(best.MeanR, colorLevels);

                            ApplySaturation(ref b, ref g, ref r, saturationBoost);

                            byte* dst = src + y * stride + x * 4;
                            byte* orig = pOriginal + y * stride + x * 4;

                            dst[0] = ToByte(b);
                            dst[1] = ToByte(g);
                            dst[2] = ToByte(r);
                            dst[3] = orig[3];
                        }
                    }
                }
            }

            private struct RegionStat
            {
                public double MeanB;
                public double MeanG;
                public double MeanR;
                public double Variance;
            }

            unsafe private static RegionStat ComputeRegion(
                byte* src, int width, int height, int stride,
                int x0, int y0, int x1, int y1)
            {
                if (x0 < 0) x0 = 0;
                if (y0 < 0) y0 = 0;
                if (x1 >= width) x1 = width - 1;
                if (y1 >= height) y1 = height - 1;

                double sumB = 0.0, sumG = 0.0, sumR = 0.0;
                double sumGray = 0.0, sumGray2 = 0.0;
                int count = 0;

                for (int y = y0; y <= y1; y++)
                {
                    for (int x = x0; x <= x1; x++)
                    {
                        byte* p = src + y * stride + x * 4;

                        double b = p[0];
                        double g = p[1];
                        double r = p[2];
                        double gray = 0.114 * b + 0.587 * g + 0.299 * r;

                        sumB += b;
                        sumG += g;
                        sumR += r;
                        sumGray += gray;
                        sumGray2 += gray * gray;
                        count++;
                    }
                }

                if (count == 0)
                {
                    return new RegionStat
                    {
                        MeanB = 0,
                        MeanG = 0,
                        MeanR = 0,
                        Variance = double.MaxValue
                    };
                }

                double meanGray = sumGray / count;
                double variance = (sumGray2 / count) - (meanGray * meanGray);
                if (variance < 0.0) variance = 0.0;

                return new RegionStat
                {
                    MeanB = sumB / count,
                    MeanG = sumG / count,
                    MeanR = sumR / count,
                    Variance = variance
                };
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

            // 이 파일의 핵심 동작을 수행하는 메서드.
            private static double Quantize(double value, int levels)
            {
                double step = 255.0 / (levels - 1);
                return Math.Round(value / step) * step;
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
