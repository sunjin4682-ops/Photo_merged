using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


// -----------------------------------------------------------------------------
// 파일 역할: FindEdgesFilter 필터의 실제 픽셀 처리 알고리즘을 구현한다.
// 병합 작업 때 빠르게 구조를 파악할 수 있도록 주석을 보강한 버전이다.
// -----------------------------------------------------------------------------

namespace Photo
{
    // 메인 편집 창과 공통 편집 상태를 관리하는 핵심 폼 클래스.
    public partial class Main
    {
        /// <summary>
        /// 윤곽선 검출의 실제 픽셀 연산을 담당하는 필터 클래스.
        /// Command 계층에서 호출되며 여기서는 순수하게 계산만 수행한다.
        /// </summary>
        internal static class FindEdgesFilter
        {
            unsafe public static void ApplyFindEdges32bpp_BGRA(
                byte* src, int width, int height, int stride,
                double edgeStrength = 1.25,
                double threshold = 0.10,
                double gamma = 0.85,
                double colorSensitivity = 0.55,
                double lineOpacity = 1.0,
                bool invertToBlackLines = true)
            {
                if (edgeStrength < 0.0) edgeStrength = 0.0;
                if (threshold < 0.0) threshold = 0.0;
                if (threshold > 1.0) threshold = 1.0;
                if (gamma < 0.1) gamma = 0.1;
                if (gamma > 3.0) gamma = 3.0;
                if (colorSensitivity < 0.0) colorSensitivity = 0.0;
                if (colorSensitivity > 2.0) colorSensitivity = 2.0;
                if (lineOpacity < 0.0) lineOpacity = 0.0;
                if (lineOpacity > 1.0) lineOpacity = 1.0;

                int pixelCount = width * height;
                int bytes = height * stride;

                byte[] original = new byte[bytes];
                double[] lum = new double[pixelCount];
                double[] cb = new double[pixelCount];
                double[] cr = new double[pixelCount];

                fixed (byte* pOriginal = original)
                {
                    Buffer.MemoryCopy(src, pOriginal, bytes, bytes);

                    // 1) YCbCr 계열 분해
                    for (int y = 0; y < height; y++)
                    {
                        for (int x = 0; x < width; x++)
                        {
                            int idx = y * width + x;
                            byte* p = pOriginal + y * stride + x * 4;

                            double b = p[0] / 255.0;
                            double g = p[1] / 255.0;
                            double r = p[2] / 255.0;

                            double Y = 0.299 * r + 0.587 * g + 0.114 * b;
                            double Cb = b - Y;
                            double Cr = r - Y;

                            lum[idx] = Y;
                            cb[idx] = Cb;
                            cr[idx] = Cr;
                        }
                    }

                    // 2) 3x3 Gaussian blur
                    double[] blurLum = new double[pixelCount];
                    double[] blurCb = new double[pixelCount];
                    double[] blurCr = new double[pixelCount];

                    Gaussian3x3(lum, blurLum, width, height);
                    Gaussian3x3(cb, blurCb, width, height);
                    Gaussian3x3(cr, blurCr, width, height);

                    // 3) Sobel
                    double[] edgeMap = new double[pixelCount];
                    double maxEdge = 1e-8;

                    for (int y = 0; y < height; y++)
                    {
                        int y0 = (y > 0) ? y - 1 : 0;
                        int y1 = y;
                        int y2 = (y < height - 1) ? y + 1 : height - 1;

                        for (int x = 0; x < width; x++)
                        {
                            int x0 = (x > 0) ? x - 1 : 0;
                            int x1 = x;
                            int x2 = (x < width - 1) ? x + 1 : width - 1;

                            // Luminance Sobel
                            double tl = blurLum[y0 * width + x0];
                            double tc = blurLum[y0 * width + x1];
                            double tr = blurLum[y0 * width + x2];
                            double ml = blurLum[y1 * width + x0];
                            double mr = blurLum[y1 * width + x2];
                            double bl = blurLum[y2 * width + x0];
                            double bc = blurLum[y2 * width + x1];
                            double br = blurLum[y2 * width + x2];

                            double gxL = -tl + tr - 2.0 * ml + 2.0 * mr - bl + br;
                            double gyL = -tl - 2.0 * tc - tr + bl + 2.0 * bc + br;
                            double gradL = Math.Sqrt(gxL * gxL + gyL * gyL);

                            // Chroma Sobel (Cb)
                            double tlCb = blurCb[y0 * width + x0];
                            double tcCb = blurCb[y0 * width + x1];
                            double trCb = blurCb[y0 * width + x2];
                            double mlCb = blurCb[y1 * width + x0];
                            double mrCb = blurCb[y1 * width + x2];
                            double blCb = blurCb[y2 * width + x0];
                            double bcCb = blurCb[y2 * width + x1];
                            double brCb = blurCb[y2 * width + x2];

                            double gxCb = -tlCb + trCb - 2.0 * mlCb + 2.0 * mrCb - blCb + brCb;
                            double gyCb = -tlCb - 2.0 * tcCb - trCb + blCb + 2.0 * bcCb + brCb;
                            double gradCb = Math.Sqrt(gxCb * gxCb + gyCb * gyCb);

                            // Chroma Sobel (Cr)
                            double tlCr = blurCr[y0 * width + x0];
                            double tcCr = blurCr[y0 * width + x1];
                            double trCr = blurCr[y0 * width + x2];
                            double mlCr = blurCr[y1 * width + x0];
                            double mrCr = blurCr[y1 * width + x2];
                            double blCr = blurCr[y2 * width + x0];
                            double bcCr = blurCr[y2 * width + x1];
                            double brCr = blurCr[y2 * width + x2];

                            double gxCr = -tlCr + trCr - 2.0 * mlCr + 2.0 * mrCr - blCr + brCr;
                            double gyCr = -tlCr - 2.0 * tcCr - trCr + blCr + 2.0 * bcCr + brCr;
                            double gradCr = Math.Sqrt(gxCr * gxCr + gyCr * gyCr);

                            double chromaGrad = Math.Sqrt(gradCb * gradCb + gradCr * gradCr);

                            double edge = gradL + chromaGrad * colorSensitivity;
                            edge *= edgeStrength;

                            edgeMap[y * width + x] = edge;
                            if (edge > maxEdge) maxEdge = edge;
                        }
                    }

                    // 4) Normalize + threshold + gamma
                    for (int y = 0; y < height; y++)
                    {
                        for (int x = 0; x < width; x++)
                        {
                            int idx = y * width + x;
                            double e = edgeMap[idx] / maxEdge;

                            // 약한 잡선 제거
                            e = (e - threshold) / (1.0 - threshold);
                            if (e < 0.0) e = 0.0;
                            if (e > 1.0) e = 1.0;

                            // 중간 영역을 더 자연스럽게
                            e = Math.Pow(e, gamma);

                            // 아주 약간 추가 소프트닝
                            e = SmoothStep01(e);

                            double v;
                            if (invertToBlackLines)
                            {
                                // 흰 배경 + 검은 윤곽선
                                v = 1.0 - e * lineOpacity;
                            }
                            else
                            {
                                // 검은 배경 + 흰 윤곽선
                                v = e * lineOpacity;
                            }

                            byte outV = ToByte(v * 255.0);

                            byte* dst = src + y * stride + x * 4;
                            byte alpha = pOriginal[y * stride + x * 4 + 3];

                            dst[0] = outV;
                            dst[1] = outV;
                            dst[2] = outV;
                            dst[3] = alpha;
                        }
                    }
                }
            }

            // 이 파일의 핵심 동작을 수행하는 메서드.
            private static void Gaussian3x3(double[] src, double[] dst, int width, int height)
            {
                // 1 2 1
                // 2 4 2   / 16
                // 1 2 1
                for (int y = 0; y < height; y++)
                {
                    int y0 = (y > 0) ? y - 1 : 0;
                    int y1 = y;
                    int y2 = (y < height - 1) ? y + 1 : height - 1;

                    for (int x = 0; x < width; x++)
                    {
                        int x0 = (x > 0) ? x - 1 : 0;
                        int x1 = x;
                        int x2 = (x < width - 1) ? x + 1 : width - 1;

                        double sum =
                            src[y0 * width + x0] * 1.0 +
                            src[y0 * width + x1] * 2.0 +
                            src[y0 * width + x2] * 1.0 +
                            src[y1 * width + x0] * 2.0 +
                            src[y1 * width + x1] * 4.0 +
                            src[y1 * width + x2] * 2.0 +
                            src[y2 * width + x0] * 1.0 +
                            src[y2 * width + x1] * 2.0 +
                            src[y2 * width + x2] * 1.0;

                        dst[y * width + x] = sum / 16.0;
                    }
                }
            }

            // 이 파일의 핵심 동작을 수행하는 메서드.
            private static double SmoothStep01(double x)
            {
                if (x <= 0.0) return 0.0;
                if (x >= 1.0) return 1.0;
                return x * x * (3.0 - 2.0 * x);
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
