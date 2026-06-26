using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


// -----------------------------------------------------------------------------
// 파일 역할: SoftGlowFilter 필터의 실제 픽셀 처리 알고리즘을 구현한다.
// 병합 작업 때 빠르게 구조를 파악할 수 있도록 주석을 보강한 버전이다.
// -----------------------------------------------------------------------------

namespace Photo
{
    // 메인 편집 창과 공통 편집 상태를 관리하는 핵심 폼 클래스.
    public partial class Main
    {
        /// <summary>
        /// 소프트 글로우의 실제 픽셀 연산을 담당하는 필터 클래스.
        /// Command 계층에서 호출되며 여기서는 순수하게 계산만 수행한다.
        /// </summary>
        internal class SoftGlowFilter
        {
            unsafe public static void SoftGlow32bpp_BGRA(
            byte* src, int width, int height, int stride,
            int radius = 5,
            double blurBlend = 0.48,
            double brightnessBoost = 14.0,
            double contrastSoften = 0.90,
            double glowThreshold = 170.0,
            double glowStrength = 0.35)
            {
                if (radius < 1) return;

                int bytes = height * stride;
                byte[] original = new byte[bytes];
                byte[] blurred = new byte[bytes];
                byte[] glowMap = new byte[bytes];

                int diameter = radius * 2 + 1;
                int maxKernelCount = diameter * diameter;

                int[] offsetX = new int[maxKernelCount];
                int[] offsetY = new int[maxKernelCount];
                int kernelCount = 0;

                int r2 = radius * radius;

                for (int ky = -radius; ky <= radius; ky++)
                {
                    for (int kx = -radius; kx <= radius; kx++)
                    {
                        if ((kx * kx) + (ky * ky) <= r2)
                        {
                            offsetX[kernelCount] = kx;
                            offsetY[kernelCount] = ky;
                            kernelCount++;
                        }
                    }
                }

                fixed (byte* pOriginal = original)
                fixed (byte* pBlurred = blurred)
                fixed (byte* pGlow = glowMap)
                {
                    Buffer.MemoryCopy(src, pOriginal, bytes, bytes);

                    // 1) 전체 이미지 블러 생성
                    ApplyDiskBlur(
                        pOriginal, pBlurred,
                        width, height, stride,
                        offsetX, offsetY, kernelCount);

                    // 2) 밝은 부분만 glow map으로 추출
                    for (int y = 0; y < height; y++)
                    {
                        for (int x = 0; x < width; x++)
                        {
                            byte* orig = pOriginal + y * stride + x * 4;
                            byte* glow = pGlow + y * stride + x * 4;

                            double b = orig[0];
                            double g = orig[1];
                            double r = orig[2];

                            // 밝기 계산
                            double lum = 0.114 * b + 0.587 * g + 0.299 * r;

                            if (lum >= glowThreshold)
                            {
                                double factor = (lum - glowThreshold) / (255.0 - glowThreshold);
                                if (factor < 0.0) factor = 0.0;
                                if (factor > 1.0) factor = 1.0;

                                glow[0] = ToByte(b * factor);
                                glow[1] = ToByte(g * factor);
                                glow[2] = ToByte(r * factor);
                                glow[3] = orig[3];
                            }
                            else
                            {
                                glow[0] = 0;
                                glow[1] = 0;
                                glow[2] = 0;
                                glow[3] = orig[3];
                            }
                        }
                    }

                    // 3) glow map도 한 번 더 블러해서 퍼지게 함
                    byte[] blurredGlow = new byte[bytes];
                    fixed (byte* pBlurredGlow = blurredGlow)
                    {
                        ApplyDiskBlur(
                            pGlow, pBlurredGlow,
                            width, height, stride,
                            offsetX, offsetY, kernelCount);

                        // 4) 원본 + 블러 + glow 합성
                        for (int y = 0; y < height; y++)
                        {
                            for (int x = 0; x < width; x++)
                            {
                                byte* dst = src + y * stride + x * 4;
                                byte* orig = pOriginal + y * stride + x * 4;
                                byte* blur = pBlurred + y * stride + x * 4;
                                byte* glow = pBlurredGlow + y * stride + x * 4;

                                double b = orig[0] * (1.0 - blurBlend) + blur[0] * blurBlend;
                                double g = orig[1] * (1.0 - blurBlend) + blur[1] * blurBlend;
                                double r = orig[2] * (1.0 - blurBlend) + blur[2] * blurBlend;

                                // glow 추가
                                b += glow[0] * glowStrength;
                                g += glow[1] * glowStrength;
                                r += glow[2] * glowStrength;

                                // 전체 밝기 상승
                                b += brightnessBoost;
                                g += brightnessBoost;
                                r += brightnessBoost;

                                // 대비 약화
                                b = ((b - 128.0) * contrastSoften) + 128.0;
                                g = ((g - 128.0) * contrastSoften) + 128.0;
                                r = ((r - 128.0) * contrastSoften) + 128.0;

                                dst[0] = ToByte(b);
                                dst[1] = ToByte(g);
                                dst[2] = ToByte(r);
                                dst[3] = orig[3];
                            }
                        }
                    }
                }
            }

            unsafe private static void ApplyDiskBlur(
                byte* src,
                byte* dst,
                int width,
                int height,
                int stride,
                int[] offsetX,
                int[] offsetY,
                int kernelCount)
            {
                for (int y = 0; y < height; y++)
                {
                    byte* outRow = dst + y * stride;

                    for (int x = 0; x < width; x++)
                    {
                        int sumB = 0;
                        int sumG = 0;
                        int sumR = 0;
                        int sumA = 0;
                        int count = 0;

                        for (int i = 0; i < kernelCount; i++)
                        {
                            int xx = x + offsetX[i];
                            int yy = y + offsetY[i];

                            if (xx < 0) xx = 0;
                            else if (xx >= width) xx = width - 1;

                            if (yy < 0) yy = 0;
                            else if (yy >= height) yy = height - 1;

                            byte* p = src + yy * stride + xx * 4;

                            sumB += p[0];
                            sumG += p[1];
                            sumR += p[2];
                            sumA += p[3];
                            count++;
                        }

                        byte* o = outRow + x * 4;

                        o[0] = (byte)(sumB / count);
                        o[1] = (byte)(sumG / count);
                        o[2] = (byte)(sumR / count);
                        o[3] = (byte)(sumA / count);
                    }
                }
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
