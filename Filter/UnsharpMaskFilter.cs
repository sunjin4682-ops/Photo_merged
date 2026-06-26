using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


// -----------------------------------------------------------------------------
// 파일 역할: UnsharpMaskFilter 필터의 실제 픽셀 처리 알고리즘을 구현한다.
// 병합 작업 때 빠르게 구조를 파악할 수 있도록 주석을 보강한 버전이다.
// -----------------------------------------------------------------------------

namespace Photo
{
    // 메인 편집 창과 공통 편집 상태를 관리하는 핵심 폼 클래스.
    public partial class Main
    {
        /// <summary>
        /// 언샵 마스크의 실제 픽셀 연산을 담당하는 필터 클래스.
        /// Command 계층에서 호출되며 여기서는 순수하게 계산만 수행한다.
        /// </summary>
        internal static class UnsharpMaskFilter
        {
            unsafe public static byte[] CreateUnsharpImage32bpp_BGRA(
                byte* src,
                int width,
                int height,
                int stride,
                double radius,
                double amount,
                double threshold)
            {
                int bytes = height * stride;

                byte[] original = new byte[bytes];
                byte[] blur = new byte[bytes];
                byte[] result = new byte[bytes];

                fixed (byte* pOriginal = original)
                fixed (byte* pBlur = blur)
                fixed (byte* pResult = result)
                {
                    Buffer.MemoryCopy(src, pOriginal, bytes, bytes);

                    GaussianBlur(pOriginal, pBlur, width, height, stride, radius);

                    for (int y = 0; y < height; y++)
                    {
                        for (int x = 0; x < width; x++)
                        {
                            byte* o = pOriginal + y * stride + x * 4;
                            byte* b = pBlur + y * stride + x * 4;
                            byte* r = pResult + y * stride + x * 4;

                            for (int c = 0; c < 3; c++)
                            {
                                double diff = o[c] - b[c];

                                if (Math.Abs(diff) < threshold * 255)
                                {
                                    r[c] = o[c];
                                }
                                else
                                {
                                    double val = o[c] + diff * amount;

                                    if (val < 0) val = 0;
                                    if (val > 255) val = 255;

                                    r[c] = (byte)val;
                                }
                            }

                            r[3] = o[3];
                        }
                    }
                }

                return result;
            }

            unsafe static void GaussianBlur(
                byte* src,
                byte* dst,
                int width,
                int height,
                int stride,
                double sigma)
            {
                int radius = (int)Math.Ceiling(sigma * 3);
                int size = radius * 2 + 1;

                double[] kernel = new double[size];
                double sum = 0;

                for (int i = -radius; i <= radius; i++)
                {
                    double v = Math.Exp(-(i * i) / (2 * sigma * sigma));
                    kernel[i + radius] = v;
                    sum += v;
                }

                for (int i = 0; i < size; i++)
                    kernel[i] /= sum;

                byte[] temp = new byte[height * stride];

                fixed (byte* pTemp = temp)
                {
                    for (int y = 0; y < height; y++)
                    {
                        for (int x = 0; x < width; x++)
                        {
                            double b = 0, g = 0, r = 0;

                            for (int k = -radius; k <= radius; k++)
                            {
                                int sx = x + k;
                                if (sx < 0) sx = 0;
                                if (sx >= width) sx = width - 1;

                                byte* p = src + y * stride + sx * 4;
                                double w = kernel[k + radius];

                                b += p[0] * w;
                                g += p[1] * w;
                                r += p[2] * w;
                            }

                            byte* t = pTemp + y * stride + x * 4;

                            t[0] = (byte)b;
                            t[1] = (byte)g;
                            t[2] = (byte)r;
                            t[3] = 255;
                        }
                    }

                    for (int y = 0; y < height; y++)
                    {
                        for (int x = 0; x < width; x++)
                        {
                            double b = 0, g = 0, r = 0;

                            for (int k = -radius; k <= radius; k++)
                            {
                                int sy = y + k;
                                if (sy < 0) sy = 0;
                                if (sy >= height) sy = height - 1;

                                byte* p = pTemp + sy * stride + x * 4;
                                double w = kernel[k + radius];

                                b += p[0] * w;
                                g += p[1] * w;
                                r += p[2] * w;
                            }

                            byte* d = dst + y * stride + x * 4;

                            d[0] = (byte)b;
                            d[1] = (byte)g;
                            d[2] = (byte)r;
                            d[3] = 255;
                        }
                    }
                }
            }

            // 이 파일의 핵심 동작을 수행하는 메서드.
            static byte Lerp(double a, double b, double t)
            {
                double v = a + (b - a) * t;

                if (v < 0) v = 0;
                if (v > 255) v = 255;

                return (byte)v;
            }

            unsafe public static void BlendWithMask(
                byte* original,
                byte[] effect,
                byte[] mask,
                int width,
                int height,
                int stride)
            {
                fixed (byte* pEffect = effect)
                {
                    for (int y = 0; y < height; y++)
                    {
                        for (int x = 0; x < width; x++)
                        {
                            int idx = y * width + x;
                            byte m = mask[idx];

                            if (m == 0) continue;

                            double alpha = m / 255.0;

                            byte* dst = original + y * stride + x * 4;
                            byte* eff = pEffect + y * stride + x * 4;

                            dst[0] = Lerp(dst[0], eff[0], alpha);
                            dst[1] = Lerp(dst[1], eff[1], alpha);
                            dst[2] = Lerp(dst[2], eff[2], alpha);
                        }
                    }
                }
            }
        }
    }
}
