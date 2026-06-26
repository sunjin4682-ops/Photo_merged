using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


// -----------------------------------------------------------------------------
// 파일 역할: SmartSharpenFilter 필터의 실제 픽셀 처리 알고리즘을 구현한다.
// 병합 작업 때 빠르게 구조를 파악할 수 있도록 주석을 보강한 버전이다.
// -----------------------------------------------------------------------------

namespace Photo
{
    // 메인 편집 창과 공통 편집 상태를 관리하는 핵심 폼 클래스.
    public partial class Main
    {
        /// <summary>
        /// 스마트 샤픈의 실제 픽셀 연산을 담당하는 필터 클래스.
        /// Command 계층에서 호출되며 여기서는 순수하게 계산만 수행한다.
        /// </summary>
        internal static class SmartSharpenFilter
        {
            unsafe public static byte[] CreateSmartSharpenImage32bpp_BGRA(
                byte* src,
                int width,
                int height,
                int stride,
                double radius = 1.8,
                double amount = 1.35,
                double threshold = 4.0,
                double edgeBoost = 1.0,
                double noiseSuppression = 0.65,
                double highlightFade = 0.25,
                double shadowFade = 0.18,
                double antiHalo = 1.0)
            {
                if (radius < 0.3) radius = 0.3;
                if (amount < 0.0) amount = 0.0;
                if (threshold < 0.0) threshold = 0.0;
                if (edgeBoost < 0.0) edgeBoost = 0.0;
                if (noiseSuppression < 0.0) noiseSuppression = 0.0;
                if (noiseSuppression > 1.0) noiseSuppression = 1.0;
                if (highlightFade < 0.0) highlightFade = 0.0;
                if (highlightFade > 1.0) highlightFade = 1.0;
                if (shadowFade < 0.0) shadowFade = 0.0;
                if (shadowFade > 1.0) shadowFade = 1.0;
                if (antiHalo < 0.1) antiHalo = 0.1;
                if (antiHalo > 3.0) antiHalo = 3.0;

                int pixelCount = width * height;
                int bytes = height * stride;

                byte[] original = new byte[bytes];
                byte[] result = new byte[bytes];

                double[] lum = new double[pixelCount];
                double[] blurLum = new double[pixelCount];
                double[] edgeMap = new double[pixelCount];

                fixed (byte* pOriginal = original)
                fixed (byte* pResult = result)
                {
                    Buffer.MemoryCopy(src, pOriginal, bytes, bytes);

                    // 1) 원본 명도 추출
                    for (int y = 0; y < height; y++)
                    {
                        for (int x = 0; x < width; x++)
                        {
                            int idx = y * width + x;
                            byte* p = pOriginal + y * stride + x * 4;

                            double b = p[0];
                            double g = p[1];
                            double r = p[2];

                            lum[idx] = 0.114 * b + 0.587 * g + 0.299 * r;
                        }
                    }

                    // 2) 명도 가우시안 블러
                    GaussianBlurGray(lum, blurLum, width, height, radius);

                    // 3) 엣지 맵 계산 (blur된 명도 기반)
                    double maxEdge = 1e-8;

                    for (int y = 0; y < height; y++)
                    {
                        int y0 = (y > 0) ? y - 1 : 0;
                        int y2 = (y < height - 1) ? y + 1 : height - 1;

                        for (int x = 0; x < width; x++)
                        {
                            int x0 = (x > 0) ? x - 1 : 0;
                            int x2 = (x < width - 1) ? x + 1 : width - 1;

                            double gx = blurLum[y * width + x2] - blurLum[y * width + x0];
                            double gy = blurLum[y2 * width + x] - blurLum[y0 * width + x];

                            double e = Math.Sqrt(gx * gx + gy * gy);
                            edgeMap[y * width + x] = e;

                            if (e > maxEdge) maxEdge = e;
                        }
                    }

                    for (int i = 0; i < pixelCount; i++)
                        edgeMap[i] /= maxEdge;

                    // 4) 스마트 샤픈 적용
                    for (int y = 0; y < height; y++)
                    {
                        for (int x = 0; x < width; x++)
                        {
                            int idx = y * width + x;

                            byte* o = pOriginal + y * stride + x * 4;
                            byte* d = pResult + y * stride + x * 4;

                            double origB = o[0];
                            double origG = o[1];
                            double origR = o[2];
                            double origA = o[3];

                            double origL = lum[idx];
                            double blurL = blurLum[idx];
                            double detailL = origL - blurL;
                            double absDetail = Math.Abs(detailL);

                            // threshold 이하 디테일은 거의 억제
                            double detailGate = SmoothStep(threshold, threshold + 12.0, absDetail);

                            // 엣지일수록 더 강화, 평탄부는 덜 강화
                            double edge = edgeMap[idx];
                            double edgeFactor = Lerp(0.35, 1.0 + edgeBoost * 0.55, edge);

                            // 노이즈 억제: 평탄부일수록 덜 sharpen
                            double noiseFactor = Lerp(1.0, edge, noiseSuppression);

                            double deltaL = detailL * amount * detailGate * edgeFactor * noiseFactor;

                            // anti-halo: 과도한 halo 방지
                            double limit = antiHalo * (8.0 + 28.0 * edge);
                            if (deltaL > limit) deltaL = limit;
                            if (deltaL < -limit) deltaL = -limit;

                            // highlight / shadow 보호
                            if (deltaL > 0.0)
                            {
                                // 밝은 하이라이트 쪽은 과하게 번쩍이지 않게
                                double hp = 1.0 - highlightFade * SmoothStep(170.0, 255.0, origL);
                                deltaL *= hp;
                            }
                            else if (deltaL < 0.0)
                            {
                                // 깊은 그림자는 과하게 먹지 않게
                                double t = 1.0 - SmoothStep(35.0, 95.0, origL);
                                double sp = 1.0 - shadowFade * t;
                                deltaL *= sp;
                            }

                            // 명도 델타를 RGB에 동일 적용 -> 컬러 halo 억제
                            double outB = origB + deltaL;
                            double outG = origG + deltaL;
                            double outR = origR + deltaL;

                            d[0] = ToByte(outB);
                            d[1] = ToByte(outG);
                            d[2] = ToByte(outR);
                            d[3] = (byte)origA;
                        }
                    }
                }

                return result;
            }

            private static void GaussianBlurGray(
                double[] src,
                double[] dst,
                int width,
                int height,
                double sigma)
            {
                int radius = Math.Max(1, (int)Math.Ceiling(sigma * 3.0));
                int size = radius * 2 + 1;

                double[] kernel = new double[size];
                double sum = 0.0;

                for (int i = -radius; i <= radius; i++)
                {
                    double v = Math.Exp(-(i * i) / (2.0 * sigma * sigma));
                    kernel[i + radius] = v;
                    sum += v;
                }

                for (int i = 0; i < size; i++)
                    kernel[i] /= sum;

                double[] temp = new double[width * height];

                // Horizontal
                for (int y = 0; y < height; y++)
                {
                    int row = y * width;

                    for (int x = 0; x < width; x++)
                    {
                        double acc = 0.0;

                        for (int k = -radius; k <= radius; k++)
                        {
                            int sx = x + k;
                            if (sx < 0) sx = 0;
                            if (sx >= width) sx = width - 1;

                            acc += src[row + sx] * kernel[k + radius];
                        }

                        temp[row + x] = acc;
                    }
                }

                // Vertical
                for (int y = 0; y < height; y++)
                {
                    for (int x = 0; x < width; x++)
                    {
                        double acc = 0.0;

                        for (int k = -radius; k <= radius; k++)
                        {
                            int sy = y + k;
                            if (sy < 0) sy = 0;
                            if (sy >= height) sy = height - 1;

                            acc += temp[sy * width + x] * kernel[k + radius];
                        }

                        dst[y * width + x] = acc;
                    }
                }
            }

            unsafe public static void BlendWithMask(
                byte* original,
                byte[] effect,
                byte[] mask,
                int width,
                int height,
                int stride,
                double maskStrength = 1.0,
                double falloffPower = 1.0)
            {
                if (mask == null || effect == null) return;
                if (maskStrength < 0.0) maskStrength = 0.0;
                if (maskStrength > 3.0) maskStrength = 3.0;
                if (falloffPower < 0.1) falloffPower = 0.1;
                if (falloffPower > 4.0) falloffPower = 4.0;

                fixed (byte* pEffect = effect)
                {
                    for (int y = 0; y < height; y++)
                    {
                        for (int x = 0; x < width; x++)
                        {
                            int idx = y * width + x;
                            byte m = mask[idx];
                            if (m == 0) continue;

                            double alpha = (m / 255.0) * maskStrength;
                            if (alpha > 1.0) alpha = 1.0;
                            alpha = Math.Pow(alpha, falloffPower);

                            byte* dst = original + y * stride + x * 4;
                            byte* eff = pEffect + y * stride + x * 4;

                            dst[0] = ToByte(Lerp(dst[0], eff[0], alpha));
                            dst[1] = ToByte(Lerp(dst[1], eff[1], alpha));
                            dst[2] = ToByte(Lerp(dst[2], eff[2], alpha));
                            dst[3] = ToByte(Lerp(dst[3], eff[3], alpha));
                        }
                    }
                }
            }

            /// <summary>
            /// 두 값 사이를 선형 보간한다.
            /// 원본과 효과 이미지를 마스크 강도만큼 섞을 때 사용한다.
            /// </summary>
            private static double Lerp(double a, double b, double t)
            {
                return a + (b - a) * t;
            }

            // 이 파일의 핵심 동작을 수행하는 메서드.
            private static double SmoothStep(double edge0, double edge1, double x)
            {
                if (edge1 <= edge0)
                    return x >= edge1 ? 1.0 : 0.0;

                double t = (x - edge0) / (edge1 - edge0);
                if (t < 0.0) t = 0.0;
                if (t > 1.0) t = 1.0;

                return t * t * (3.0 - 2.0 * t);
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
