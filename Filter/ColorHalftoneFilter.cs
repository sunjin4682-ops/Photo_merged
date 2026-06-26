using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


// -----------------------------------------------------------------------------
// 파일 역할: ColorHalftoneFilter 필터의 실제 픽셀 처리 알고리즘을 구현한다.
// 병합 작업 때 빠르게 구조를 파악할 수 있도록 주석을 보강한 버전이다.
// -----------------------------------------------------------------------------

namespace Photo
{
    // 메인 편집 창과 공통 편집 상태를 관리하는 핵심 폼 클래스.
    public partial class Main
    {
        /// <summary>
        /// 컬러 하프톤의 실제 픽셀 연산을 담당하는 필터 클래스.
        /// Command 계층에서 호출되며 여기서는 순수하게 계산만 수행한다.
        /// </summary>
        internal static class ColorHalftoneFilter
        {
            private const double PI = Math.PI;

            unsafe public static void ApplyColorHalftone32bpp_BGRA(
                byte* src, int width, int height, int stride,
                int cellSize = 8,
                double dotScale = 0.92,
                double softness = 1.10,
                int supersample = 3,
                double inkStrength = 0.98,
                double blackStrength = 1.08,
                double preserveLight = 0.18)
            {
                if (cellSize < 4) cellSize = 4;
                if (dotScale < 0.30) dotScale = 0.30;
                if (dotScale > 1.60) dotScale = 1.60;
                if (softness < 0.30) softness = 0.30;
                if (softness > 3.50) softness = 3.50;
                if (supersample < 1) supersample = 1;
                if (supersample > 5) supersample = 5;
                if (inkStrength < 0.0) inkStrength = 0.0;
                if (blackStrength < 0.0) blackStrength = 0.0;
                if (preserveLight < 0.0) preserveLight = 0.0;
                if (preserveLight > 1.0) preserveLight = 1.0;

                int bytes = height * stride;
                byte[] original = new byte[bytes];

                fixed (byte* pOriginal = original)
                {
                    Buffer.MemoryCopy(src, pOriginal, bytes, bytes);

                    double cAngle = DegToRad(15.0);
                    double mAngle = DegToRad(75.0);
                    double yAngle = DegToRad(0.0);
                    double kAngle = DegToRad(45.0);

                    double baseRadius = (cellSize * 0.5) * dotScale;
                    double aaWidth = Math.Max(0.75, softness);

                    for (int y = 0; y < height; y++)
                    {
                        for (int x = 0; x < width; x++)
                        {
                            byte* p = pOriginal + y * stride + x * 4;

                            double b = p[0] / 255.0;
                            double g = p[1] / 255.0;
                            double r = p[2] / 255.0;
                            byte a = p[3];

                            RGBToCMYK(r, g, b, out double c, out double m, out double yy, out double k);

                            c = Clamp01(c * inkStrength);
                            m = Clamp01(m * inkStrength);
                            yy = Clamp01(yy * inkStrength);
                            k = Clamp01(k * blackStrength);

                            double covC = SampleDotCoverage(
                                x, y, cellSize, baseRadius, aaWidth, supersample, c, cAngle);

                            double covM = SampleDotCoverage(
                                x, y, cellSize, baseRadius, aaWidth, supersample, m, mAngle);

                            double covY = SampleDotCoverage(
                                x, y, cellSize, baseRadius, aaWidth, supersample, yy, yAngle);

                            double covK = SampleDotCoverage(
                                x, y, cellSize, baseRadius, aaWidth, supersample, k, kAngle);

                            // 종이(흰 바탕) 위에 C/M/Y/K 잉크가 올라가는 방식
                            double outR = 1.0;
                            double outG = 1.0;
                            double outB = 1.0;

                            // C 잉크: R 감소
                            outR *= (1.0 - covC);

                            // M 잉크: G 감소
                            outG *= (1.0 - covM);

                            // Y 잉크: B 감소
                            outB *= (1.0 - covY);

                            // K 잉크: 전체 감소
                            double kMul = (1.0 - covK);
                            outR *= kMul;
                            outG *= kMul;
                            outB *= kMul;

                            // 너무 탁해지지 않도록 원본 밝기 일부 보존
                            outR = Lerp(outR, r, preserveLight);
                            outG = Lerp(outG, g, preserveLight);
                            outB = Lerp(outB, b, preserveLight);

                            byte* dst = src + y * stride + x * 4;
                            dst[0] = ToByte(outB * 255.0);
                            dst[1] = ToByte(outG * 255.0);
                            dst[2] = ToByte(outR * 255.0);
                            dst[3] = a;
                        }
                    }
                }
            }

            private static double SampleDotCoverage(
                int px, int py,
                int cellSize,
                double baseRadius,
                double aaWidth,
                int supersample,
                double amount,
                double angleRad)
            {
                if (amount <= 0.0001) return 0.0;
                if (amount >= 0.9999) return 1.0;

                double radius = baseRadius * Math.Sqrt(amount);

                double total = 0.0;
                int count = supersample * supersample;

                double inv = 1.0 / supersample;

                for (int sy = 0; sy < supersample; sy++)
                {
                    for (int sx = 0; sx < supersample; sx++)
                    {
                        double fx = px + (sx + 0.5) * inv;
                        double fy = py + (sy + 0.5) * inv;

                        Rotate(fx, fy, angleRad, out double rx, out double ry);

                        double cellX = Mod(rx, cellSize);
                        double cellY = Mod(ry, cellSize);

                        double cx = cellSize * 0.5;
                        double cy = cellSize * 0.5;

                        double dx = cellX - cx;
                        double dy = cellY - cy;

                        double dist = Math.Sqrt(dx * dx + dy * dy);

                        double sample = SmoothDisk(dist, radius, aaWidth);
                        total += sample;
                    }
                }

                return Clamp01(total / count);
            }

            // 이 파일의 핵심 동작을 수행하는 메서드.
            private static double SmoothDisk(double dist, double radius, double aaWidth)
            {
                double inner = radius - aaWidth * 0.5;
                double outer = radius + aaWidth * 0.5;

                if (dist <= inner) return 1.0;
                if (dist >= outer) return 0.0;

                double t = (dist - inner) / (outer - inner);
                t = Clamp01(t);

                // 부드러운 에지
                return 1.0 - (t * t * (3.0 - 2.0 * t));
            }

            private static void RGBToCMYK(
                double r, double g, double b,
                out double c, out double m, out double y, out double k)
            {
                double maxRGB = Math.Max(r, Math.Max(g, b));
                k = 1.0 - maxRGB;

                if (k >= 0.999999)
                {
                    c = 0.0;
                    m = 0.0;
                    y = 0.0;
                    k = 1.0;
                    return;
                }

                double denom = 1.0 - k;
                c = (1.0 - r - k) / denom;
                m = (1.0 - g - k) / denom;
                y = (1.0 - b - k) / denom;

                c = Clamp01(c);
                m = Clamp01(m);
                y = Clamp01(y);
                k = Clamp01(k);
            }

            // 이 파일의 핵심 동작을 수행하는 메서드.
            private static void Rotate(double x, double y, double angleRad, out double rx, out double ry)
            {
                double ca = Math.Cos(angleRad);
                double sa = Math.Sin(angleRad);

                rx = x * ca - y * sa;
                ry = x * sa + y * ca;
            }

            // 이 파일의 핵심 동작을 수행하는 메서드.
            private static double DegToRad(double deg)
            {
                return deg * PI / 180.0;
            }

            // 이 파일의 핵심 동작을 수행하는 메서드.
            private static double Mod(double x, double m)
            {
                double r = x % m;
                if (r < 0.0) r += m;
                return r;
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
            private static double Clamp01(double v)
            {
                if (v < 0.0) return 0.0;
                if (v > 1.0) return 1.0;
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
