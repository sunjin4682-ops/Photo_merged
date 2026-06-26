using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


// -----------------------------------------------------------------------------
// 파일 역할: LensFlareFilter 필터의 실제 픽셀 처리 알고리즘을 구현한다.
// 병합 작업 때 빠르게 구조를 파악할 수 있도록 주석을 보강한 버전이다.
// -----------------------------------------------------------------------------

namespace Photo
{
    // 메인 편집 창과 공통 편집 상태를 관리하는 핵심 폼 클래스.
    public partial class Main
    {
        /// <summary>
        /// 렌즈 플레어의 실제 픽셀 연산을 담당하는 필터 클래스.
        /// Command 계층에서 호출되며 여기서는 순수하게 계산만 수행한다.
        /// </summary>
        internal static class SoftLensFlareFilter
        {
            unsafe public static void ApplySoftLensFlare32bpp_BGRA(
                byte* src, int width, int height, int stride,
                byte[]? mask,
                double intensity = 1.0,
                double bloomRadius = 0.20,
                double haloRadius = 0.18,
                double haloWidth = 0.035,
                double veilStrength = 0.24,
                double ghostStrength = 0.16,
                double streakStrength = 0.06,
                double pinkHaloStrength = 0.42,
                double blueGhostStrength = 0.18,
                int rayCount = 6,
                double rayStrength = 0.30,
                double raySharpness = 20.0,
                double rayLength = 0.42)
            {
                if (src == null || width <= 0 || height <= 0) return;

                int pixelCount = width * height;
                if (mask == null || mask.Length < pixelCount)
                    return;

                bool hasMask = false;
                for (int i = 0; i < pixelCount; i++)
                {
                    if (mask[i] != 0)
                    {
                        hasMask = true;
                        break;
                    }
                }
                if (!hasMask) return;

                if (!TryGetMaskCentroid(mask, width, height, out double lightX, out double lightY, out double maskStrength))
                    return;

                if (intensity < 0.0) intensity = 0.0;
                if (bloomRadius < 0.01) bloomRadius = 0.01;
                if (haloRadius < 0.01) haloRadius = 0.01;
                if (haloWidth < 0.005) haloWidth = 0.005;
                if (veilStrength < 0.0) veilStrength = 0.0;
                if (ghostStrength < 0.0) ghostStrength = 0.0;
                if (streakStrength < 0.0) streakStrength = 0.0;
                if (pinkHaloStrength < 0.0) pinkHaloStrength = 0.0;
                if (blueGhostStrength < 0.0) blueGhostStrength = 0.0;
                if (rayCount < 2) rayCount = 2;
                if (rayCount > 16) rayCount = 16;
                if (rayStrength < 0.0) rayStrength = 0.0;
                if (raySharpness < 1.0) raySharpness = 1.0;
                if (rayLength < 0.02) rayLength = 0.02;

                double cx = (width - 1) * 0.5;
                double cy = (height - 1) * 0.5;

                double axisX = cx - lightX;
                double axisY = cy - lightY;

                double diag = Math.Sqrt(width * width + height * height);
                if (diag < 1.0) diag = 1.0;

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
                            double srcB = s[0];
                            double srcG = s[1];
                            double srcR = s[2];
                            byte srcA = s[3];

                            double dx = x - lightX;
                            double dy = y - lightY;
                            double r = Math.Sqrt(dx * dx + dy * dy) / diag;

                            // 1) 중심 bloom
                            double bloom =
                                Math.Exp(-(r * r) / Math.Max(1e-6, bloomRadius * bloomRadius) * 10.5) * 1.15 +
                                Math.Exp(-(r * r) / Math.Max(1e-6, (bloomRadius * 0.42) * (bloomRadius * 0.42)) * 14.5) * 1.40;

                            // 2) veiling glare
                            double veil = Math.Exp(-r * 3.1) * veilStrength;

                            // 3) halo ring
                            double haloDist = Math.Abs(r - haloRadius);
                            double halo = Math.Exp(-(haloDist * haloDist) / Math.Max(1e-6, haloWidth * haloWidth) * 1.9);

                            // 4) 아주 약한 streak
                            double angle = Math.Atan2(dy, dx);
                            double streak =
                                Math.Pow(Math.Abs(Math.Cos(angle * 2.0)), 18.0) * 0.60 +
                                Math.Pow(Math.Abs(Math.Cos((angle - 0.12) * 4.0)), 28.0) * 0.40;
                            streak *= Math.Exp(-r * 6.0) * streakStrength;

                            // 5) 광원 중심에서 퍼지는 ray burst
                            double wobble =
                                0.035 * Math.Sin(angle * 3.0 + r * 40.0) +
                                0.018 * Math.Sin(angle * 7.0 - r * 22.0);

                            double rayPattern = Math.Pow(
                                Math.Abs(Math.Cos((angle + wobble) * rayCount * 0.5)),
                                raySharpness);

                            double rayEnvelope =
                                (1.0 - Math.Exp(-r * 28.0)) *
                                Math.Exp(-r / Math.Max(1e-6, rayLength));

                            double rayBurst = rayPattern * rayEnvelope * rayStrength;

                            // 6) small blue/cyan ghosts
                            double ghost = 0.0;
                            ghost += SampleGhost(x, y, lightX + axisX * 0.38, lightY + axisY * 0.38, diag, 0.030, 0.85);
                            ghost += SampleGhost(x, y, lightX + axisX * 0.58, lightY + axisY * 0.58, diag, 0.022, 0.75);
                            ghost += SampleGhost(x, y, lightX + axisX * 0.84, lightY + axisY * 0.84, diag, 0.016, 0.65);
                            ghost *= ghostStrength;

                            // 전체 flare 강도
                            double flare = (bloom + veil + halo * 0.65 + streak + ghost + rayBurst) * intensity;
                            flare *= (0.55 + 0.45 * maskStrength);

                            if (flare <= 0.0001)
                                continue;

                            // 색 구성
                            double warmCore = bloom * 1.0 + veil * 0.25 + rayBurst * 0.55;
                            double pinkRing = halo * pinkHaloStrength + rayBurst * 0.10;
                            double blueGhost = ghost * blueGhostStrength;

                            double flareR =
                                warmCore * 255.0 +
                                pinkRing * 255.0 +
                                blueGhost * 120.0;

                            double flareG =
                                warmCore * 246.0 +
                                pinkRing * 190.0 +
                                blueGhost * 215.0;

                            double flareB =
                                warmCore * 236.0 +
                                pinkRing * 225.0 +
                                blueGhost * 255.0;

                            // 중심 백색화
                            double coreWhiten = Math.Exp(
                                -(r * r) /
                                Math.Max(1e-6, (bloomRadius * 0.28) * (bloomRadius * 0.28)) * 18.0);

                            flareR += coreWhiten * 255.0 * 0.55;
                            flareG += coreWhiten * 255.0 * 0.52;
                            flareB += coreWhiten * 255.0 * 0.50;

                            // Screen + 약한 Add 혼합
                            double screenB = 255.0 - (255.0 - srcB) * (255.0 - Clamp255(flareB)) / 255.0;
                            double screenG = 255.0 - (255.0 - srcG) * (255.0 - Clamp255(flareG)) / 255.0;
                            double screenR = 255.0 - (255.0 - srcR) * (255.0 - Clamp255(flareR)) / 255.0;

                            double addB = Clamp255(srcB + flareB * 0.18);
                            double addG = Clamp255(srcG + flareG * 0.18);
                            double addR = Clamp255(srcR + flareR * 0.18);

                            double outB = Lerp(screenB, addB, 0.22);
                            double outG = Lerp(screenG, addG, 0.22);
                            double outR = Lerp(screenR, addR, 0.22);

                            src[idx + 0] = ToByte(outB);
                            src[idx + 1] = ToByte(outG);
                            src[idx + 2] = ToByte(outR);
                            src[idx + 3] = srcA;
                        }
                    }
                }
            }

            private static double SampleGhost(
                double x, double y,
                double gx, double gy,
                double diag,
                double radius,
                double amp)
            {
                double dx = x - gx;
                double dy = y - gy;
                double r = Math.Sqrt(dx * dx + dy * dy) / diag;

                double disc = Math.Exp(-(r * r) / Math.Max(1e-6, radius * radius) * 22.0);
                double ring = Math.Exp(-(Math.Abs(r - radius * 0.72) * Math.Abs(r - radius * 0.72)) / 0.00002);
                return (disc * 0.72 + ring * 0.38) * amp;
            }

            // 이 파일의 핵심 동작을 수행하는 메서드.
            private static bool TryGetMaskCentroid(byte[] mask, int width, int height, out double cx, out double cy, out double avgStrength)
            {
                double sumX = 0.0;
                double sumY = 0.0;
                double sumW = 0.0;

                int pixelCount = width * height;
                for (int i = 0; i < pixelCount; i++)
                {
                    byte m = mask[i];
                    if (m == 0) continue;

                    double w = m / 255.0;
                    int y = i / width;
                    int x = i - y * width;

                    sumX += x * w;
                    sumY += y * w;
                    sumW += w;
                }

                if (sumW <= 0.0)
                {
                    cx = cy = avgStrength = 0.0;
                    return false;
                }

                cx = sumX / sumW;
                cy = sumY / sumW;
                avgStrength = sumW / Math.Max(1.0, pixelCount);
                avgStrength = Math.Min(1.0, avgStrength * 14.0);
                return true;
            }

            // 이 파일의 핵심 동작을 수행하는 메서드.
            private static double Clamp255(double v)
            {
                if (v < 0.0) return 0.0;
                if (v > 255.0) return 255.0;
                return v;
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
                if (v <= 0.0) return 0;
                if (v >= 255.0) return 255;
                return (byte)(v + 0.5);
            }
        }
    }
}
