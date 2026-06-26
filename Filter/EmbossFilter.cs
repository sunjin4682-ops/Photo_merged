using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


// -----------------------------------------------------------------------------
// 파일 역할: EmbossFilter 필터의 실제 픽셀 처리 알고리즘을 구현한다.
// 병합 작업 때 빠르게 구조를 파악할 수 있도록 주석을 보강한 버전이다.
// -----------------------------------------------------------------------------

namespace Photo
{
    // 메인 편집 창과 공통 편집 상태를 관리하는 핵심 폼 클래스.
    public partial class Main
    {
        /// <summary>
        /// 엠보스의 실제 픽셀 연산을 담당하는 필터 클래스.
        /// Command 계층에서 호출되며 여기서는 순수하게 계산만 수행한다.
        /// </summary>
        internal static class MetalEmbossFilter
        {
            unsafe public static void ApplyMetalEmboss32bpp_BGRA(
                byte* src, int width, int height, int stride,
                double angleDeg = 135.0,
                double elevationDeg = 42.0,
                double depth = 2.4,
                double blurSigma = 1.0,
                double contrast = 1.28,
                double specularStrength = 0.75,
                double specularPower = 18.0,
                bool preserveColor = false,
                double colorBlend = 0.22)
            {
                if (depth < 0.1) depth = 0.1;
                if (blurSigma < 0.2) blurSigma = 0.2;
                if (contrast < 0.1) contrast = 0.1;
                if (specularStrength < 0.0) specularStrength = 0.0;
                if (specularPower < 1.0) specularPower = 1.0;
                if (colorBlend < 0.0) colorBlend = 0.0;
                if (colorBlend > 1.0) colorBlend = 1.0;

                int pixelCount = width * height;
                int bytes = height * stride;

                byte[] original = new byte[bytes];
                double[] heightMap = new double[pixelCount];
                double[] blurMap = new double[pixelCount];

                fixed (byte* pOriginal = original)
                {
                    Buffer.MemoryCopy(src, pOriginal, bytes, bytes);

                    // 1) height map 생성 (명도 기반)
                    for (int y = 0; y < height; y++)
                    {
                        for (int x = 0; x < width; x++)
                        {
                            int idx = y * width + x;
                            byte* p = pOriginal + y * stride + x * 4;

                            double b = p[0];
                            double g = p[1];
                            double r = p[2];

                            // 밝기를 높이맵으로 사용
                            heightMap[idx] = (0.114 * b + 0.587 * g + 0.299 * r) / 255.0;
                        }
                    }

                    // 2) Gaussian blur
                    GaussianBlur(heightMap, blurMap, width, height, blurSigma);

                    // 3) 광원 벡터
                    double az = angleDeg * Math.PI / 180.0;
                    double el = elevationDeg * Math.PI / 180.0;

                    double lx = Math.Cos(el) * Math.Cos(az);
                    double ly = Math.Cos(el) * Math.Sin(az);
                    double lz = Math.Sin(el);

                    // 시선 방향은 정면
                    double vx = 0.0;
                    double vy = 0.0;
                    double vz = 1.0;

                    for (int y = 0; y < height; y++)
                    {
                        int y0 = (y > 0) ? y - 1 : 0;
                        int y2 = (y < height - 1) ? y + 1 : height - 1;

                        for (int x = 0; x < width; x++)
                        {
                            int x0 = (x > 0) ? x - 1 : 0;
                            int x2 = (x < width - 1) ? x + 1 : width - 1;

                            int idx = y * width + x;

                            // 4) surface normal 추정
                            double dx = (blurMap[y * width + x2] - blurMap[y * width + x0]) * depth;
                            double dy = (blurMap[y2 * width + x] - blurMap[y0 * width + x]) * depth;

                            // 높이맵 기울기 -> 법선
                            double nx = -dx;
                            double ny = -dy;
                            double nz = 1.0;

                            Normalize(ref nx, ref ny, ref nz);

                            // 5) diffuse
                            double diffuse = nx * lx + ny * ly + nz * lz;
                            if (diffuse < 0.0) diffuse = 0.0;

                            // 6) half-vector 기반 specular
                            double hx = lx + vx;
                            double hy = ly + vy;
                            double hz = lz + vz;
                            Normalize(ref hx, ref hy, ref hz);

                            double ndoth = nx * hx + ny * hy + nz * hz;
                            if (ndoth < 0.0) ndoth = 0.0;

                            double spec = Math.Pow(ndoth, specularPower) * specularStrength;

                            // 7) 금속 재질 느낌용 base shading
                            // 128 기준 relief + contrast 강화
                            double relief = (diffuse - 0.5) * 2.0;   // -1 ~ +1
                            double metalShade = 128.0 + relief * 92.0 * contrast + spec * 255.0;

                            // 금속 질감이 너무 흐리지 않게 약간 S-curve 느낌
                            double v = metalShade / 255.0;
                            v = ApplyMetalCurve(v);
                            metalShade = v * 255.0;

                            byte* dst = src + y * stride + x * 4;
                            byte* ori = pOriginal + y * stride + x * 4;

                            if (preserveColor)
                            {
                                // 원본 색을 약간 남기되, 금속 음영으로 조정
                                double shadeMul = metalShade / 128.0;

                                double b = ori[0] * shadeMul;
                                double g = ori[1] * shadeMul;
                                double r = ori[2] * shadeMul;

                                // 금속 느낌이 너무 사진 그대로 되지 않게 회색 금속톤과 블렌딩
                                double gray = metalShade;

                                b = Lerp(gray, b, colorBlend);
                                g = Lerp(gray, g, colorBlend);
                                r = Lerp(gray, r, colorBlend);

                                dst[0] = ClampByte(b);
                                dst[1] = ClampByte(g);
                                dst[2] = ClampByte(r);
                            }
                            else
                            {
                                byte mv = ClampByte(metalShade);
                                dst[0] = mv;
                                dst[1] = mv;
                                dst[2] = mv;
                            }

                            dst[3] = ori[3];
                        }
                    }
                }
            }

            // 현재 효과나 상태를 실제 이미지/편집 상태에 반영한다.
            private static double ApplyMetalCurve(double v)
            {
                if (v < 0.0) v = 0.0;
                if (v > 1.0) v = 1.0;

                // 금속광택처럼 중간 회색을 살짝 밀고 하이라이트/그림자 대비를 키움
                // 부드러운 S-curve
                double t = v * v * (3.0 - 2.0 * v);
                double mix = 0.65 * v + 0.35 * t;

                // 약한 하이라이트 강조
                mix = Math.Pow(mix, 0.92);

                if (mix < 0.0) mix = 0.0;
                if (mix > 1.0) mix = 1.0;
                return mix;
            }

            // 이 파일의 핵심 동작을 수행하는 메서드.
            private static void GaussianBlur(double[] src, double[] dst, int width, int height, double sigma)
            {
                int radius = Math.Max(1, (int)Math.Ceiling(3.0 * sigma));
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

                // horizontal
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

                // vertical
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

            // 이 파일의 핵심 동작을 수행하는 메서드.
            private static void Normalize(ref double x, ref double y, ref double z)
            {
                double len = Math.Sqrt(x * x + y * y + z * z);
                if (len < 1e-12)
                {
                    x = 0.0;
                    y = 0.0;
                    z = 1.0;
                    return;
                }

                x /= len;
                y /= len;
                z /= len;
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
            private static byte ClampByte(double v)
            {
                int iv = (int)Math.Round(v);
                if (iv < 0) return 0;
                if (iv > 255) return 255;
                return (byte)iv;
            }
        }
    }
}
