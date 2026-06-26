using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


// -----------------------------------------------------------------------------
// 파일 역할: CrystallizeFilter 필터의 실제 픽셀 처리 알고리즘을 구현한다.
// 병합 작업 때 빠르게 구조를 파악할 수 있도록 주석을 보강한 버전이다.
// -----------------------------------------------------------------------------

namespace Photo
{
    // 메인 편집 창과 공통 편집 상태를 관리하는 핵심 폼 클래스.
    public partial class Main
    {
        /// <summary>
        /// 수정화의 실제 픽셀 연산을 담당하는 필터 클래스.
        /// Command 계층에서 호출되며 여기서는 순수하게 계산만 수행한다.
        /// </summary>
        internal static class CrystallizeFilter
        {
            private struct Seed
            {
                public float X;
                public float Y;
            }

            unsafe public static void ApplyCrystallize32bpp_BGRA(
                byte* src, int width, int height, int stride,
                int cellSize = 18,
                double jitter = 0.85,
                double borderSoftness = 2.2,
                double borderDarkness = 0.12,
                double detailPreserve = 0.22,
                double colorBoost = 1.03)
            {
                if (cellSize < 4) cellSize = 4;
                if (jitter < 0.0) jitter = 0.0;
                if (jitter > 1.0) jitter = 1.0;
                if (borderSoftness < 0.3) borderSoftness = 0.3;
                if (detailPreserve < 0.0) detailPreserve = 0.0;
                if (detailPreserve > 1.0) detailPreserve = 1.0;
                if (colorBoost < 0.0) colorBoost = 0.0;

                int cols = (width + cellSize - 1) / cellSize;
                int rows = (height + cellSize - 1) / cellSize;
                int seedCount = cols * rows;

                Seed[] seeds = new Seed[seedCount];
                int[] ownerMap = new int[width * height];

                double[] sumB = new double[seedCount];
                double[] sumG = new double[seedCount];
                double[] sumR = new double[seedCount];
                int[] counts = new int[seedCount];

                BuildSeeds(seeds, cols, rows, cellSize, width, height, jitter);

                // 1차 패스: 각 픽셀의 소유 셀 찾기 + 셀 평균색 누적
                for (int y = 0; y < height; y++)
                {
                    for (int x = 0; x < width; x++)
                    {
                        int owner = FindNearestSeedIndex(x, y, seeds, cols, rows, cellSize);
                        ownerMap[y * width + x] = owner;

                        byte* p = src + y * stride + x * 4;
                        sumB[owner] += p[0];
                        sumG[owner] += p[1];
                        sumR[owner] += p[2];
                        counts[owner]++;
                    }
                }

                byte[] avgB = new byte[seedCount];
                byte[] avgG = new byte[seedCount];
                byte[] avgR = new byte[seedCount];

                for (int i = 0; i < seedCount; i++)
                {
                    if (counts[i] <= 0)
                    {
                        avgB[i] = 0;
                        avgG[i] = 0;
                        avgR[i] = 0;
                    }
                    else
                    {
                        avgB[i] = ToByte(sumB[i] / counts[i]);
                        avgG[i] = ToByte(sumG[i] / counts[i]);
                        avgR[i] = ToByte(sumR[i] / counts[i]);
                    }
                }

                int totalBytes = height * stride;
                byte[] original = new byte[totalBytes];

                fixed (byte* pOriginal = original)
                {
                    Buffer.MemoryCopy(src, pOriginal, totalBytes, totalBytes);

                    // 2차 패스: 경계 부드럽게 + 내부 디테일 보존 + 결정 느낌
                    for (int y = 0; y < height; y++)
                    {
                        for (int x = 0; x < width; x++)
                        {
                            int owner = ownerMap[y * width + x];
                            FindNearestTwoSeeds(
                                x, y,
                                seeds, cols, rows, cellSize,
                                out int nearestIdx,
                                out double nearestDistSq,
                                out int secondIdx,
                                out double secondDistSq);

                            byte* srcP = pOriginal + y * stride + x * 4;
                            double srcB = srcP[0];
                            double srcG = srcP[1];
                            double srcR = srcP[2];
                            byte alpha = srcP[3];

                            double cellB = avgB[owner];
                            double cellG = avgG[owner];
                            double cellR = avgR[owner];

                            // 셀 내부 원본 명암을 조금 살려서 너무 납작해지지 않게
                            double srcLum = 0.114 * srcB + 0.587 * srcG + 0.299 * srcR;
                            double cellLum = 0.114 * cellB + 0.587 * cellG + 0.299 * cellR;

                            double lumRatio = 1.0;
                            if (cellLum > 1.0)
                                lumRatio = srcLum / cellLum;

                            double detailMix = detailPreserve;
                            double detB = cellB * Lerp(1.0, lumRatio, detailMix);
                            double detG = cellG * Lerp(1.0, lumRatio, detailMix);
                            double detR = cellR * Lerp(1.0, lumRatio, detailMix);

                            detB *= colorBoost;
                            detG *= colorBoost;
                            detR *= colorBoost;

                            // 경계 판정: nearest와 second nearest 거리 차이가 작을수록 경계에 가까움
                            double d1 = Math.Sqrt(nearestDistSq);
                            double d2 = Math.Sqrt(secondDistSq);
                            double edgeGap = d2 - d1;

                            // 경계 근처에서 두 셀 색을 살짝 블렌딩
                            double blendT = SmoothStep(0.0, borderSoftness, edgeGap);

                            double secondB = avgB[secondIdx];
                            double secondG = avgG[secondIdx];
                            double secondR = avgR[secondIdx];

                            double mixB = Lerp(secondB, detB, blendT);
                            double mixG = Lerp(secondG, detG, blendT);
                            double mixR = Lerp(secondR, detR, blendT);

                            // 경계선 약간 어둡게 해서 결정 조각 느낌 강화
                            double edgeDark = 1.0 - borderDarkness * (1.0 - blendT);

                            mixB *= edgeDark;
                            mixG *= edgeDark;
                            mixR *= edgeDark;

                            byte* dst = src + y * stride + x * 4;
                            dst[0] = ToByte(mixB);
                            dst[1] = ToByte(mixG);
                            dst[2] = ToByte(mixR);
                            dst[3] = alpha;
                        }
                    }
                }
            }

            private static void BuildSeeds(
                Seed[] seeds, int cols, int rows, int cellSize,
                int width, int height, double jitter)
            {
                for (int gy = 0; gy < rows; gy++)
                {
                    for (int gx = 0; gx < cols; gx++)
                    {
                        int idx = gy * cols + gx;

                        double baseX = gx * cellSize + cellSize * 0.5;
                        double baseY = gy * cellSize + cellSize * 0.5;

                        double jx = (Hash01(gx, gy, 17) * 2.0 - 1.0) * cellSize * 0.5 * jitter;
                        double jy = (Hash01(gx, gy, 53) * 2.0 - 1.0) * cellSize * 0.5 * jitter;

                        double sx = baseX + jx;
                        double sy = baseY + jy;

                        if (sx < 0) sx = 0;
                        if (sx > width - 1) sx = width - 1;
                        if (sy < 0) sy = 0;
                        if (sy > height - 1) sy = height - 1;

                        seeds[idx].X = (float)sx;
                        seeds[idx].Y = (float)sy;
                    }
                }
            }

            private static int FindNearestSeedIndex(
                int x, int y,
                Seed[] seeds, int cols, int rows, int cellSize)
            {
                int gx = x / cellSize;
                int gy = y / cellSize;

                double bestDist = double.MaxValue;
                int bestIdx = 0;

                for (int yy = gy - 1; yy <= gy + 1; yy++)
                {
                    if (yy < 0 || yy >= rows) continue;

                    for (int xx = gx - 1; xx <= gx + 1; xx++)
                    {
                        if (xx < 0 || xx >= cols) continue;

                        int idx = yy * cols + xx;
                        double dx = x - seeds[idx].X;
                        double dy = y - seeds[idx].Y;
                        double dist = dx * dx + dy * dy;

                        if (dist < bestDist)
                        {
                            bestDist = dist;
                            bestIdx = idx;
                        }
                    }
                }

                return bestIdx;
            }

            private static void FindNearestTwoSeeds(
                int x, int y,
                Seed[] seeds, int cols, int rows, int cellSize,
                out int nearestIdx,
                out double nearestDistSq,
                out int secondIdx,
                out double secondDistSq)
            {
                int gx = x / cellSize;
                int gy = y / cellSize;

                nearestIdx = 0;
                secondIdx = 0;
                nearestDistSq = double.MaxValue;
                secondDistSq = double.MaxValue;

                for (int yy = gy - 1; yy <= gy + 1; yy++)
                {
                    if (yy < 0 || yy >= rows) continue;

                    for (int xx = gx - 1; xx <= gx + 1; xx++)
                    {
                        if (xx < 0 || xx >= cols) continue;

                        int idx = yy * cols + xx;
                        double dx = x - seeds[idx].X;
                        double dy = y - seeds[idx].Y;
                        double dist = dx * dx + dy * dy;

                        if (dist < nearestDistSq)
                        {
                            secondDistSq = nearestDistSq;
                            secondIdx = nearestIdx;

                            nearestDistSq = dist;
                            nearestIdx = idx;
                        }
                        else if (dist < secondDistSq)
                        {
                            secondDistSq = dist;
                            secondIdx = idx;
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

            // 이 파일의 핵심 동작을 수행하는 메서드.
            private static double Clamp01(double v)
            {
                if (v < 0.0) return 0.0;
                if (v > 1.0) return 1.0;
                return v;
            }

            // 이 파일의 핵심 동작을 수행하는 메서드.
            private static double SmoothStep(double edge0, double edge1, double x)
            {
                if (edge1 <= edge0) return x >= edge1 ? 1.0 : 0.0;
                double t = Clamp01((x - edge0) / (edge1 - edge0));
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
