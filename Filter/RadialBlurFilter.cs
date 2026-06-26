using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


// -----------------------------------------------------------------------------
// 파일 역할: RadialBlurFilter 필터의 실제 픽셀 처리 알고리즘을 구현한다.
// 병합 작업 때 빠르게 구조를 파악할 수 있도록 주석을 보강한 버전이다.
// -----------------------------------------------------------------------------

namespace Photo
{
    // 메인 편집 창과 공통 편집 상태를 관리하는 핵심 폼 클래스.
    public partial class Main
    {
        /// <summary>
        /// 방사형 블러의 실제 픽셀 연산을 담당하는 필터 클래스.
        /// Command 계층에서 호출되며 여기서는 순수하게 계산만 수행한다.
        /// </summary>
        internal class RadialBlurFilter
        {
            unsafe public static void RadialBlurMasked32bpp_BGRA(
                byte* src, int width, int height, int stride,
                byte[] mask, int sampleCount, double strength)
            {
                if (mask == null || mask.Length != width * height)
                    throw new ArgumentException("mask size must be width * height");

                if (sampleCount < 3) sampleCount = 3;
                if ((sampleCount & 1) == 0) sampleCount += 1; // 홀수 권장
                if (strength <= 0.0) return;

                int bytes = height * stride;

                byte[] original = new byte[bytes];
                byte[] blurred = new byte[bytes];

                // -----------------------------
                // 1) 마스크의 가중 중심 계산
                // -----------------------------
                double sumW = 0.0;
                double sumX = 0.0;
                double sumY = 0.0;

                for (int y = 0; y < height; y++)
                {
                    int rowIndex = y * width;

                    for (int x = 0; x < width; x++)
                    {
                        byte m = mask[rowIndex + x];
                        if (m == 0) continue;

                        double w = m / 255.0;
                        sumW += w;
                        sumX += x * w;
                        sumY += y * w;
                    }
                }

                if (sumW <= 0.0)
                    return;

                double cx = sumX / sumW;
                double cy = sumY / sumW;

                fixed (byte* pOriginal = original)
                fixed (byte* pBlurred = blurred)
                fixed (byte* pMask = mask)
                {
                    Buffer.MemoryCopy(src, pOriginal, bytes, bytes);

                    // -----------------------------------
                    // 2) 마스크 중심 기준 줌형 방사형 흐림 생성
                    //    - bilinear sampling
                    //    - 대칭 샘플링
                    //    - 가중 평균
                    // -----------------------------------
                    for (int y = 0; y < height; y++)
                    {
                        byte* outRow = pBlurred + y * stride;

                        for (int x = 0; x < width; x++)
                        {
                            double vx = x - cx;
                            double vy = y - cy;

                            double sumB = 0.0;
                            double sumG = 0.0;
                            double sumR = 0.0;
                            double sumA = 0.0;
                            double weightSum = 0.0;

                            for (int i = 0; i < sampleCount; i++)
                            {
                                double t = (double)i / (sampleCount - 1); // 0~1
                                double centered = t - 0.5;                // -0.5~+0.5

                                // 가운데 샘플에 더 높은 가중치
                                double w = 1.0 - Math.Abs(centered) * 2.0;
                                if (w < 0.001) w = 0.001;

                                // 대칭 줌 샘플링
                                double scale = 1.0 + centered * strength;

                                double sx = cx + vx * scale;
                                double sy = cy + vy * scale;

                                BilinearSample(
                                    pOriginal, width, height, stride,
                                    sx, sy,
                                    out double b, out double g, out double r, out double a);

                                sumB += b * w;
                                sumG += g * w;
                                sumR += r * w;
                                sumA += a * w;
                                weightSum += w;
                            }

                            byte* o = outRow + x * 4;

                            if (weightSum > 0.0)
                            {
                                o[0] = ToByte(sumB / weightSum);
                                o[1] = ToByte(sumG / weightSum);
                                o[2] = ToByte(sumR / weightSum);
                                o[3] = ToByte(sumA / weightSum);
                            }
                            else
                            {
                                byte* p = pOriginal + y * stride + x * 4;
                                o[0] = p[0];
                                o[1] = p[1];
                                o[2] = p[2];
                                o[3] = p[3];
                            }
                        }
                    }

                    // -----------------------------------
                    // 3) soft mask로 원본과 blurred 합성
                    // -----------------------------------
                    for (int y = 0; y < height; y++)
                    {
                        for (int x = 0; x < width; x++)
                        {
                            int mi = y * width + x;
                            double a = pMask[mi] / 255.0;

                            double strength1 = a * 2.0;   // 효과 보정 (1.5~2.5 추천)

                            byte* dst = src + y * stride + x * 4;
                            byte* orig = pOriginal + y * stride + x * 4;
                            byte* blur = pBlurred + y * stride + x * 4;

                            dst[0] = ToByte(orig[0] + (blur[0] - orig[0]) * strength1);
                            dst[1] = ToByte(orig[1] + (blur[1] - orig[1]) * strength1);
                            dst[2] = ToByte(orig[2] + (blur[2] - orig[2]) * strength1);
                            dst[3] = ToByte(orig[3] + (blur[3] - orig[3]) * strength1);
                        }
                    }
                }
            }

            unsafe private static void BilinearSample(
                byte* src, int width, int height, int stride,
                double x, double y,
                out double b, out double g, out double r, out double a)
            {
                if (x < 0) x = 0;
                if (y < 0) y = 0;
                if (x > width - 1) x = width - 1;
                if (y > height - 1) y = height - 1;

                int x0 = (int)Math.Floor(x);
                int y0 = (int)Math.Floor(y);
                int x1 = x0 + 1;
                int y1 = y0 + 1;

                if (x1 >= width) x1 = width - 1;
                if (y1 >= height) y1 = height - 1;

                double fx = x - x0;
                double fy = y - y0;

                byte* p00 = src + y0 * stride + x0 * 4;
                byte* p10 = src + y0 * stride + x1 * 4;
                byte* p01 = src + y1 * stride + x0 * 4;
                byte* p11 = src + y1 * stride + x1 * 4;

                double w00 = (1.0 - fx) * (1.0 - fy);
                double w10 = fx * (1.0 - fy);
                double w01 = (1.0 - fx) * fy;
                double w11 = fx * fy;

                b = p00[0] * w00 + p10[0] * w10 + p01[0] * w01 + p11[0] * w11;
                g = p00[1] * w00 + p10[1] * w10 + p01[1] * w01 + p11[1] * w11;
                r = p00[2] * w00 + p10[2] * w10 + p01[2] * w01 + p11[2] * w11;
                a = p00[3] * w00 + p10[3] * w10 + p01[3] * w01 + p11[3] * w11;
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
