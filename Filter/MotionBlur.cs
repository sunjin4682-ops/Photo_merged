using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


// -----------------------------------------------------------------------------
// 파일 역할: MotionBlur 필터의 실제 픽셀 처리 알고리즘을 구현한다.
// 병합 작업 때 빠르게 구조를 파악할 수 있도록 주석을 보강한 버전이다.
// -----------------------------------------------------------------------------

namespace Photo
{
    // 메인 편집 창과 공통 편집 상태를 관리하는 핵심 폼 클래스.
    public partial class Main
    {
        /// <summary>
        /// 모션 블러의 실제 픽셀 연산을 담당하는 필터 클래스.
        /// Command 계층에서 호출되며 여기서는 순수하게 계산만 수행한다.
        /// </summary>
        internal static class MotionBlurFilter
        {
            /// <summary>
            /// 모션 블러가 적용된 별도의 효과 이미지를 만든다.
            /// 원본 픽셀을 직접 덮어쓰지 않고 결과 버퍼를 따로 만든 뒤 마지막에 블렌딩한다.
            /// </summary>
            unsafe public static byte[] CreateMotionBlurImage32bpp_BGRA(
                byte* src, int width, int height, int stride,
                double angleDeg = 0.0,
                double length = 24.0,
                int samples = 33,
                double sigmaScale = 0.35,
                double centerBias = 1.15,
                bool bidirectional = false)
            {
                // 지나치게 작은 값이나 비정상 파라미터를 보정해 계산이 불안정해지는 것을 막는다.
                if (length < 0.5) length = 0.5;
                if (samples < 3) samples = 3;
                if ((samples & 1) == 0) samples += 1;
                if (samples > 255) samples = 255;
                if (sigmaScale < 0.05) sigmaScale = 0.05;
                if (sigmaScale > 2.0) sigmaScale = 2.0;
                if (centerBias < 0.1) centerBias = 0.1;
                if (centerBias > 6.0) centerBias = 6.0;

                int bytes = height * stride;
                byte[] original = new byte[bytes];
                byte[] output = new byte[bytes];

                fixed (byte* pOriginal = original)
                fixed (byte* pOutput = output)
                {
                    // 원본 픽셀을 별도 버퍼에 복사해 샘플링 중 자기 자신을 덮어쓰지 않도록 한다.
                    Buffer.MemoryCopy(src, pOriginal, bytes, bytes);

                    // 각도를 라디안과 방향 벡터로 바꿔 샘플링 진행 방향을 계산한다.
                    double rad = angleDeg * Math.PI / 180.0;
                    double dirX = Math.Cos(rad);
                    double dirY = Math.Sin(rad);

                    double halfLen = bidirectional ? length * 0.5 : length;
                    double sigma = Math.Max(0.001, length * sigmaScale);

                    int halfSamples = samples / 2;

                    for (int y = 0; y < height; y++)
                    {
                        for (int x = 0; x < width; x++)
                        {
                            double sumB = 0.0;
                            double sumG = 0.0;
                            double sumR = 0.0;
                            double sumA = 0.0;
                            double sumW = 0.0;

                            if (bidirectional)
                            {
                                for (int i = -halfSamples; i <= halfSamples; i++)
                                {
                                    double t = (halfSamples == 0) ? 0.0 : (double)i / halfSamples;
                                    double dist = t * halfLen;

                                    double fx = x + dirX * dist;
                                    double fy = y + dirY * dist;

                                    double w = Gaussian(dist, sigma);
                                    if (i == 0) w *= centerBias;

                                    SampleBilinear_BGRA(
                                        pOriginal, width, height, stride,
                                        fx, fy,
                                        out double b, out double g, out double r, out double a);

                                    sumB += b * w;
                                    sumG += g * w;
                                    sumR += r * w;
                                    sumA += a * w;
                                    sumW += w;
                                }
                            }
                            else
                            {
                                for (int i = 0; i < samples; i++)
                                {
                                    double t = (samples == 1) ? 0.0 : (double)i / (samples - 1);
                                    double dist = t * length;

                                    // 진행 방향 반대쪽으로 잔상이 생기게
                                    double fx = x - dirX * dist;
                                    double fy = y - dirY * dist;

                                    double w = Gaussian(dist, sigma);
                                    if (i == 0) w *= centerBias;

                                    SampleBilinear_BGRA(
                                        pOriginal, width, height, stride,
                                        fx, fy,
                                        out double b, out double g, out double r, out double a);

                                    sumB += b * w;
                                    sumG += g * w;
                                    sumR += r * w;
                                    sumA += a * w;
                                    sumW += w;
                                }
                            }

                            if (sumW < 1e-12) sumW = 1.0;

                            byte* dst = pOutput + y * stride + x * 4;
                            dst[0] = ToByte(sumB / sumW);
                            dst[1] = ToByte(sumG / sumW);
                            dst[2] = ToByte(sumR / sumW);
                            dst[3] = ToByte(sumA / sumW);
                        }
                    }
                }

                return output;
            }

            /// <summary>
            /// 효과 이미지와 원본 이미지를 마스크 강도에 따라 섞는다.
            /// 마스크가 0인 곳은 원본을 유지하고, 255에 가까울수록 효과 비중이 커진다.
            /// </summary>
            unsafe public static void BlendWithMask32bpp_BGRA(
                byte* dstOriginal,
                byte[] effectImage,
                byte[] mask,
                int width,
                int height,
                int stride,
                double maskStrength = 1.0,
                double falloffPower = 1.0)
            {
                if (mask == null || effectImage == null) return;
                if (maskStrength < 0.0) maskStrength = 0.0;
                if (maskStrength > 3.0) maskStrength = 3.0;
                if (falloffPower < 0.1) falloffPower = 0.1;
                if (falloffPower > 4.0) falloffPower = 4.0;

                fixed (byte* pEffect = effectImage)
                {
                    for (int y = 0; y < height; y++)
                    {
                        for (int x = 0; x < width; x++)
                        {
                            int idx = y * width + x;
                            byte m = mask[idx];
                            if (m == 0) continue;

                            // 브러시 마스크 값(0~255)을 실제 블렌딩 계수(0~1)로 변환한다.
                            double alpha = (m / 255.0) * maskStrength;
                            if (alpha > 1.0) alpha = 1.0;

                            alpha = Math.Pow(alpha, falloffPower);

                            byte* dst = dstOriginal + y * stride + x * 4;
                            byte* eff = pEffect + y * stride + x * 4;

                            dst[0] = ToByte(Lerp(dst[0], eff[0], alpha));
                            dst[1] = ToByte(Lerp(dst[1], eff[1], alpha));
                            dst[2] = ToByte(Lerp(dst[2], eff[2], alpha));
                            dst[3] = ToByte(Lerp(dst[3], eff[3], alpha));
                        }
                    }
                }
            }

            private unsafe static void SampleBilinear_BGRA(
                byte* src, int width, int height, int stride,
                double x, double y,
                out double b, out double g, out double r, out double a)
            {
                if (x < 0.0) x = 0.0;
                if (y < 0.0) y = 0.0;
                if (x > width - 1) x = width - 1;
                if (y > height - 1) y = height - 1;

                int x0 = (int)Math.Floor(x);
                int y0 = (int)Math.Floor(y);
                int x1 = x0 + 1;
                int y1 = y0 + 1;

                if (x1 >= width) x1 = width - 1;
                if (y1 >= height) y1 = height - 1;

                double tx = x - x0;
                double ty = y - y0;

                byte* p00 = src + y0 * stride + x0 * 4;
                byte* p10 = src + y0 * stride + x1 * 4;
                byte* p01 = src + y1 * stride + x0 * 4;
                byte* p11 = src + y1 * stride + x1 * 4;

                double w00 = (1.0 - tx) * (1.0 - ty);
                double w10 = tx * (1.0 - ty);
                double w01 = (1.0 - tx) * ty;
                double w11 = tx * ty;

                b = p00[0] * w00 + p10[0] * w10 + p01[0] * w01 + p11[0] * w11;
                g = p00[1] * w00 + p10[1] * w10 + p01[1] * w01 + p11[1] * w11;
                r = p00[2] * w00 + p10[2] * w10 + p01[2] * w01 + p11[2] * w11;
                a = p00[3] * w00 + p10[3] * w10 + p01[3] * w01 + p11[3] * w11;
            }

            /// <summary>
            /// 거리 기반 가중치를 계산하는 가우시안 함수.
            /// 블러에서 중심에 가까운 샘플에 더 큰 비중을 주기 위해 사용한다.
            /// </summary>
            private static double Gaussian(double x, double sigma)
            {
                double s2 = sigma * sigma;
                return Math.Exp(-(x * x) / (2.0 * s2));
            }

            /// <summary>
            /// 두 값 사이를 선형 보간한다.
            /// 원본과 효과 이미지를 마스크 강도만큼 섞을 때 사용한다.
            /// </summary>
            private static double Lerp(double a, double b, double t)
            {
                return a + (b - a) * t;
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
