using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


// -----------------------------------------------------------------------------
// 파일 역할: WindFilter 필터의 실제 픽셀 처리 알고리즘을 구현한다.
// 병합 작업 때 빠르게 구조를 파악할 수 있도록 주석을 보강한 버전이다.
// -----------------------------------------------------------------------------

namespace Photo
{
    // 메인 편집 창과 공통 편집 상태를 관리하는 핵심 폼 클래스.
    public partial class Main
    {
        /// <summary>
        /// 바람 효과의 실제 픽셀 연산을 담당하는 필터 클래스.
        /// Command 계층에서 호출되며 여기서는 순수하게 계산만 수행한다.
        /// </summary>
        internal static class WindFilter
        {
            // WindMode 관련 역할을 담당하는 타입이다.
            internal enum WindMode
            {
                Wind = 0,
                Blast = 1,
                Stagger = 2
            }

            unsafe public static void ApplyWind32bpp_BGRA(
                byte* src, int width, int height, int stride,
                bool toRight = true,
                int strength = 18,
                double threshold = 0.16,
                double edgeSensitivity = 0.75,
                double blendOpacity = 0.85,
                double scatter = 0.30,
                WindMode mode = WindMode.Wind)
            {
                if (strength < 1) strength = 1;
                if (strength > 200) strength = 200;
                if (threshold < 0.0) threshold = 0.0;
                if (threshold > 1.0) threshold = 1.0;
                if (edgeSensitivity < 0.0) edgeSensitivity = 0.0;
                if (edgeSensitivity > 3.0) edgeSensitivity = 3.0;
                if (blendOpacity < 0.0) blendOpacity = 0.0;
                if (blendOpacity > 1.0) blendOpacity = 1.0;
                if (scatter < 0.0) scatter = 0.0;
                if (scatter > 1.0) scatter = 1.0;

                int bytes = height * stride;
                int pixelCount = width * height;

                byte[] original = new byte[bytes];
                double[] lum = new double[pixelCount];
                double[] edgeMap = new double[pixelCount];

                fixed (byte* pOriginal = original)
                {
                    Buffer.MemoryCopy(src, pOriginal, bytes, bytes);

                    // 1) luminance 추출
                    for (int y = 0; y < height; y++)
                    {
                        for (int x = 0; x < width; x++)
                        {
                            int idx = y * width + x;
                            byte* p = pOriginal + y * stride + x * 4;

                            double b = p[0] / 255.0;
                            double g = p[1] / 255.0;
                            double r = p[2] / 255.0;

                            lum[idx] = 0.114 * b + 0.587 * g + 0.299 * r;
                        }
                    }

                    // 2) 간단한 엣지 강도 계산 (좌우 차이 + 상하 차이)
                    double maxEdge = 1e-8;

                    for (int y = 0; y < height; y++)
                    {
                        int y0 = (y > 0) ? y - 1 : 0;
                        int y2 = (y < height - 1) ? y + 1 : height - 1;

                        for (int x = 0; x < width; x++)
                        {
                            int x0 = (x > 0) ? x - 1 : 0;
                            int x2 = (x < width - 1) ? x + 1 : width - 1;

                            double gx = lum[y * width + x2] - lum[y * width + x0];
                            double gy = lum[y2 * width + x] - lum[y0 * width + x];
                            double grad = Math.Sqrt(gx * gx + gy * gy);

                            edgeMap[y * width + x] = grad;
                            if (grad > maxEdge) maxEdge = grad;
                        }
                    }

                    for (int i = 0; i < pixelCount; i++)
                        edgeMap[i] /= maxEdge;

                    // 3) 출력 누적 버퍼
                    double[] accB = new double[pixelCount];
                    double[] accG = new double[pixelCount];
                    double[] accR = new double[pixelCount];
                    double[] accW = new double[pixelCount];

                    // 4) 원본 기본층 먼저 깔기
                    for (int y = 0; y < height; y++)
                    {
                        for (int x = 0; x < width; x++)
                        {
                            int idx = y * width + x;
                            byte* p = pOriginal + y * stride + x * 4;

                            accB[idx] = p[0];
                            accG[idx] = p[1];
                            accR[idx] = p[2];
                            accW[idx] = 1.0;
                        }
                    }

                    // 5) streak 생성
                    for (int y = 0; y < height; y++)
                    {
                        double rowJitter = 0.82 + 0.36 * Hash01(y, 0, 17);

                        for (int x = 0; x < width; x++)
                        {
                            int idx = y * width + x;
                            byte* p = pOriginal + y * stride + x * 4;

                            double srcB = p[0];
                            double srcG = p[1];
                            double srcR = p[2];

                            double brightness = lum[idx];
                            double edge = edgeMap[idx];

                            // 바람에 날릴 소스 마스크
                            double seed = Math.Max(
                                (brightness - threshold) / Math.Max(1e-8, 1.0 - threshold),
                                (edge - threshold) / Math.Max(1e-8, 1.0 - threshold) * edgeSensitivity);

                            if (seed <= 0.001)
                                continue;

                            if (seed > 1.0) seed = 1.0;

                            int localStrength = (int)Math.Round(strength * rowJitter);
                            if (localStrength < 1) localStrength = 1;

                            // 모드별 길이/분포 차이
                            int streakLength = localStrength;
                            switch (mode)
                            {
                                case WindMode.Wind:
                                    streakLength = localStrength;
                                    break;
                                case WindMode.Blast:
                                    streakLength = (int)Math.Round(localStrength * 1.6);
                                    break;
                                case WindMode.Stagger:
                                    streakLength = (int)Math.Round(localStrength * (0.7 + 0.8 * Hash01(x, y, 91)));
                                    break;
                            }

                            if (streakLength < 1) streakLength = 1;

                            for (int s = 1; s <= streakLength; s++)
                            {
                                int dx = toRight ? (x + s) : (x - s);
                                if (dx < 0 || dx >= width)
                                    break;

                                int dy = y;

                                // scatter: 약간 위아래로 흔들기
                                if (scatter > 0.0)
                                {
                                    double rnd = Hash01(x, y, 101 + s * 13);
                                    double offset = (rnd * 2.0 - 1.0);

                                    double amp = 0.0;
                                    switch (mode)
                                    {
                                        case WindMode.Wind:
                                            amp = scatter * 0.6;
                                            break;
                                        case WindMode.Blast:
                                            amp = scatter * 1.2;
                                            break;
                                        case WindMode.Stagger:
                                            amp = scatter * 0.9;
                                            break;
                                    }

                                    int oy = (int)Math.Round(offset * amp * Math.Sqrt(s));
                                    dy += oy;

                                    if (dy < 0 || dy >= height)
                                        continue;
                                }

                                int dstIdx = dy * width + dx;

                                // 거리 감쇠
                                double decay;
                                switch (mode)
                                {
                                    default:
                                    case WindMode.Wind:
                                        decay = Math.Exp(-s / (double)Math.Max(1, streakLength) * 2.2);
                                        break;

                                    case WindMode.Blast:
                                        decay = Math.Exp(-s / (double)Math.Max(1, streakLength) * 1.3);
                                        break;

                                    case WindMode.Stagger:
                                        decay = Math.Exp(-s / (double)Math.Max(1, streakLength) * 1.8);
                                        break;
                                }

                                // 끊김 효과
                                double gapMul = 1.0;
                                if (mode == WindMode.Stagger)
                                {
                                    double g = Hash01(x, y, 201 + s * 29);
                                    gapMul = (g > 0.22) ? 1.0 : 0.0;
                                }
                                else if (mode == WindMode.Blast)
                                {
                                    double g = Hash01(x, y, 301 + s * 7);
                                    gapMul = 0.65 + 0.35 * g;
                                }

                                double w = seed * decay * gapMul * blendOpacity;

                                if (w <= 0.0001)
                                    continue;

                                accB[dstIdx] += srcB * w;
                                accG[dstIdx] += srcG * w;
                                accR[dstIdx] += srcR * w;
                                accW[dstIdx] += w;
                            }
                        }
                    }

                    // 6) 정규화 + 출력
                    for (int y = 0; y < height; y++)
                    {
                        for (int x = 0; x < width; x++)
                        {
                            int idx = y * width + x;
                            byte* dst = src + y * stride + x * 4;
                            byte* ori = pOriginal + y * stride + x * 4;

                            double w = accW[idx];
                            if (w < 1e-8) w = 1.0;

                            double b = accB[idx] / w;
                            double g = accG[idx] / w;
                            double r = accR[idx] / w;

                            // 너무 뿌옇게 되지 않게 원본 약간 보정
                            b = Lerp(b, ori[0], 0.06);
                            g = Lerp(g, ori[1], 0.06);
                            r = Lerp(r, ori[2], 0.06);

                            dst[0] = ToByte(b);
                            dst[1] = ToByte(g);
                            dst[2] = ToByte(r);
                            dst[3] = ori[3];
                        }
                    }
                }
            }

            // 이 파일의 핵심 동작을 수행하는 메서드.
            private static double Hash01(int x, int y, int seed)
            {
                unchecked
                {
                    int h = x * 374761393 + y * 668265263 + seed * 1442695041;
                    h = (h ^ (h >> 13)) * 1274126177;
                    h ^= (h >> 16);
                    uint uh = (uint)h;
                    return uh / 4294967295.0;
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
