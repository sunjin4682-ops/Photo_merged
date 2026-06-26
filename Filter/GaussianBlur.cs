using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


// -----------------------------------------------------------------------------
// 파일 역할: GaussianBlur 필터의 실제 픽셀 처리 알고리즘을 구현한다.
// 병합 작업 때 빠르게 구조를 파악할 수 있도록 주석을 보강한 버전이다.
// -----------------------------------------------------------------------------

namespace Photo
{

    /// <summary>
        /// 가우시안 블러의 실제 픽셀 연산을 담당하는 필터 클래스.
        /// Command 계층에서 호출되며 여기서는 순수하게 계산만 수행한다.
        /// </summary>
        internal class GaussianBlur
    {
        unsafe public static void GaussianBlurMasked32bpp_BGRA(
        byte* src, int width, int height, int stride,
        byte[] mask, int kernelSize, double sigma)
        {
            if (mask == null || mask.Length != width * height)
                throw new ArgumentException("mask size must be width * height");
            if (kernelSize < 3 || kernelSize % 2 == 0)
                throw new ArgumentException("kernelSize must be odd and >= 3");
            if (sigma <= 0)
                throw new ArgumentException("sigma must be > 0");

            double[] kernel = CreateGaussianKernel1D(kernelSize, sigma);
            int r = kernelSize / 2;
            int bytes = height * stride;

            byte[] original = new byte[bytes];
            byte[] tmp = new byte[bytes];
            byte[] blurred = new byte[bytes];

            fixed (byte* pOriginal = original)
            fixed (byte* pTmp = tmp)
            fixed (byte* pBlurred = blurred)
            fixed (byte* pMask = mask)
            {
                // 원본 백업
                Buffer.MemoryCopy(src, pOriginal, bytes, bytes);

                // ---------- Horizontal pass ----------
                for (int y = 0; y < height; y++)
                {
                    byte* row = pOriginal + y * stride;
                    byte* outRow = pTmp + y * stride;

                    for (int x = 0; x < width; x++)
                    {
                        double accB = 0, accG = 0, accR = 0, accA = 0;

                        for (int k = -r; k <= r; k++)
                        {
                            int xx = x + k;
                            if (xx < 0) xx = 0;
                            else if (xx >= width) xx = width - 1;

                            byte* p = row + xx * 4;
                            double w = kernel[k + r];

                            accB += w * p[0];
                            accG += w * p[1];
                            accR += w * p[2];
                            accA += w * p[3];
                        }

                        byte* o = outRow + x * 4;
                        o[0] = ToByte(accB);
                        o[1] = ToByte(accG);
                        o[2] = ToByte(accR);
                        o[3] = ToByte(accA);
                    }
                }

                // ---------- Vertical pass ----------
                for (int y = 0; y < height; y++)
                {
                    byte* outRow = pBlurred + y * stride;

                    for (int x = 0; x < width; x++)
                    {
                        double accB = 0, accG = 0, accR = 0, accA = 0;

                        for (int k = -r; k <= r; k++)
                        {
                            int yy = y + k;
                            if (yy < 0) yy = 0;
                            else if (yy >= height) yy = height - 1;

                            byte* p = pTmp + yy * stride + x * 4;
                            double w = kernel[k + r];

                            accB += w * p[0];
                            accG += w * p[1];
                            accR += w * p[2];
                            accA += w * p[3];
                        }

                        byte* o = outRow + x * 4;
                        o[0] = ToByte(accB);
                        o[1] = ToByte(accG);
                        o[2] = ToByte(accR);
                        o[3] = ToByte(accA);
                    }
                }

                // ---------- 원본과 블러 결과를 soft mask로 합성 ----------
                for (int y = 0; y < height; y++)
                {
                    for (int x = 0; x < width; x++)
                    {
                        int mi = y * width + x;
                        double a = pMask[mi] / 255.0;  // 0.0 ~ 1.0

                        double strength = a * 2.0;   // 효과 보정 (1.5~2.5 추천)

                        byte* o = src + y * stride + x * 4;
                        byte* orig = pOriginal + y * stride + x * 4;
                        byte* blur = pBlurred + y * stride + x * 4;

                        o[0] = ToByte(orig[0] + (blur[0] - orig[0]) * strength);
                        o[1] = ToByte(orig[1] + (blur[1] - orig[1]) * strength);
                        o[2] = ToByte(orig[2] + (blur[2] - orig[2]) * strength);
                        o[3] = ToByte(orig[3] + (blur[3] - orig[3]) * strength);
                    }
                }
            }

            // 이 파일의 핵심 동작을 수행하는 메서드.
            static double[] CreateGaussianKernel1D(int kernelSize, double sigma)
            {
                int r = kernelSize / 2;
                double[] k = new double[kernelSize];

                double sum = 0.0;
                double twoSigma2 = 2.0 * sigma * sigma;

                // G(x) = exp(-(x^2)/(2*sigma^2))  (μ=0)
                for (int i = 0; i < kernelSize; i++)
                {
                    int x = i - r;
                    double v = Math.Exp(-(x * x) / twoSigma2);
                    k[i] = v;
                    sum += v;
                }

                // 정규화(합=1)
                for (int i = 0; i < kernelSize; i++)
                    k[i] /= sum;

                return k;
            }

            // 이 파일의 핵심 동작을 수행하는 메서드.
            static byte ToByte(double v)
            {
                int iv = (int)Math.Round(v);
                if (iv < 0) return 0;
                if (iv > 255) return 255;
                return (byte)iv;
            }
        }


    }
}
