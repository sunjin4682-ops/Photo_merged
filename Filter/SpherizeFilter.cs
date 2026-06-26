using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;


// -----------------------------------------------------------------------------
// 파일 역할: SpherizeFilter 필터의 실제 픽셀 처리 알고리즘을 구현한다.
// 병합 작업 때 빠르게 구조를 파악할 수 있도록 주석을 보강한 버전이다.
// -----------------------------------------------------------------------------

namespace Photo
{
    // 메인 편집 창과 공통 편집 상태를 관리하는 핵심 폼 클래스.
    public partial class Main
    {
        /// <summary>
        /// 구형화의 실제 픽셀 연산을 담당하는 필터 클래스.
        /// Command 계층에서 호출되며 여기서는 순수하게 계산만 수행한다.
        /// </summary>
        internal class SpherizeFilter
        {
            unsafe public static void SpherizeMasked32bpp_BGRA(
                byte* dst, int width, int height, int stride,
                byte[] mask,
                double amount = 0.55,
                double radiusScale = 1.10,
                double aspectX = 1.00,
                double aspectY = 1.00)
            {
                if (dst == null || mask == null) return;
                if (mask.Length < width * height) return;

                // 1) 마스크 중심 / 범위 계산
                double sumW = 0.0;
                double sumX = 0.0;
                double sumY = 0.0;

                int minX = width - 1;
                int minY = height - 1;
                int maxX = 0;
                int maxY = 0;

                bool hasMask = false;

                for (int y = 0; y < height; y++)
                {
                    int row = y * width;
                    for (int x = 0; x < width; x++)
                    {
                        byte m = mask[row + x];
                        if (m == 0) continue;

                        hasMask = true;

                        double w = m / 255.0;
                        sumW += w;
                        sumX += x * w;
                        sumY += y * w;

                        if (x < minX) minX = x;
                        if (x > maxX) maxX = x;
                        if (y < minY) minY = y;
                        if (y > maxY) maxY = y;
                    }
                }

                if (!hasMask || sumW <= 0.0) return;

                double cx = sumX / sumW;
                double cy = sumY / sumW;

                double halfW = Math.Max(1.0, (maxX - minX + 1) * 0.5);
                double halfH = Math.Max(1.0, (maxY - minY + 1) * 0.5);

                double radiusX = Math.Max(8.0, halfW * radiusScale);
                double radiusY = Math.Max(8.0, halfH * radiusScale);

                int workMinX = Math.Max(0, (int)Math.Floor(cx - radiusX));
                int workMaxX = Math.Min(width - 1, (int)Math.Ceiling(cx + radiusX));
                int workMinY = Math.Max(0, (int)Math.Floor(cy - radiusY));
                int workMaxY = Math.Min(height - 1, (int)Math.Ceiling(cy + radiusY));

                // 2) 원본 백업
                int byteCount = stride * height;
                byte[] srcCopy = new byte[byteCount];
                Marshal.Copy((IntPtr)dst, srcCopy, 0, byteCount);

                fixed (byte* src = srcCopy)
                {
                    // 중심부는 완전 적용, 바깥쪽은 feather
                    const double innerRatio = 0.90;

                    for (int y = workMinY; y <= workMaxY; y++)
                    {
                        int maskRow = y * width;
                        byte* dstRow = dst + y * stride;

                        for (int x = workMinX; x <= workMaxX; x++)
                        {
                            byte m = mask[maskRow + x];
                            if (m == 0) continue;

                            double alpha = m / 255.0;

                            double dx = x - cx;
                            double dy = y - cy;

                            // 타원 기준 정규화 거리
                            double nx = dx / radiusX;
                            double ny = dy / radiusY;
                            double dist = Math.Sqrt(nx * nx + ny * ny);

                            if (dist >= 1.0)
                                continue;

                            // core / feather 분리
                            double localT;
                            double ringAlpha;

                            if (dist <= innerRatio)
                            {
                                localT = 1.0 - (dist / innerRatio);
                                ringAlpha = 1.0;
                            }
                            else
                            {
                                double edgeT = (1.0 - dist) / (1.0 - innerRatio);
                                if (edgeT < 0.0) edgeT = 0.0;
                                if (edgeT > 1.0) edgeT = 1.0;

                                // 왜곡은 아주 약하게만 남김
                                localT = edgeT * 0.20;

                                // feather blend는 더 빨리 사라지게
                                ringAlpha = edgeT * edgeT;
                                ringAlpha *= ringAlpha; // ^4
                            }

                            double falloff = localT * localT * (3.0 - 2.0 * localT);

                            // aspect 보정 공간으로 이동
                            double ex = dx / aspectX;
                            double ey = dy / aspectY;

                            double r = Math.Sqrt(ex * ex + ey * ey);

                            if (r < 1e-8)
                            {
                                // 중심점은 그대로 복사
                                continue;
                            }

                            double refRadius = Math.Min(radiusX / aspectX, radiusY / aspectY);
                            if (refRadius < 1.0) refRadius = 1.0;

                            double rn = r / refRadius;
                            if (rn > 1.0) rn = 1.0;

                            // -----------------------------
                            // 진짜 구슬/이슬 느낌용 반구 확대 매핑
                            // -----------------------------
                            double warpedRn;

                            if (amount >= 0.0)
                            {
                                // 반구 높이: 중심 1, 가장자리 0
                                double z = Math.Sqrt(Math.Max(0.0, 1.0 - rn * rn));

                                // 중심 확대
                                double magnify = 1.0 + amount * 1.75 * z * falloff;

                                // 출력 픽셀에 대해 원본의 더 안쪽을 읽어오므로
                                // 화면에서는 확대되어 보임
                                warpedRn = rn / magnify;
                            }
                            else
                            {
                                // 오목 렌즈 느낌
                                double a = -amount;
                                double z = Math.Sqrt(Math.Max(0.0, 1.0 - rn * rn));

                                double shrink = 1.0 + a * 1.35 * z * falloff;
                                warpedRn = rn * shrink;
                            }

                            if (warpedRn < 0.0) warpedRn = 0.0;
                            if (warpedRn > 1.0) warpedRn = 1.0;

                            // 원래 방향 유지, 반경만 재설정
                            double scale = warpedRn / rn;

                            double srcEx = ex * scale;
                            double srcEy = ey * scale;

                            double sx = cx + srcEx * aspectX;
                            double sy = cy + srcEy * aspectY;

                            SampleBilinearBGRA(
                                src, width, height, stride,
                                sx, sy,
                                out double sb, out double sg, out double sr, out double sa);

                            byte* p = dstRow + x * 4;

                            double ob = p[0];
                            double og = p[1];
                            double orr = p[2];
                            double oa = p[3];

                            double blendAlpha;

                            if (dist <= innerRatio)
                            {
                                // 중심부는 원본 잔상 없이 완전 대체
                                blendAlpha = 1.0;
                            }
                            else
                            {
                                // feather ring만 부드럽게 혼합
                                blendAlpha = alpha * ringAlpha;
                            }

                            p[0] = ClampToByte(ob + (sb - ob) * blendAlpha);
                            p[1] = ClampToByte(og + (sg - og) * blendAlpha);
                            p[2] = ClampToByte(orr + (sr - orr) * blendAlpha);
                            p[3] = ClampToByte(oa + (sa - oa) * blendAlpha);
                        }
                    }
                }
            }

            unsafe private static void SampleBilinearBGRA(
                byte* src, int width, int height, int stride,
                double x, double y,
                out double b, out double g, out double r, out double a)
            {
                if (x < 0) x = 0;
                if (y < 0) y = 0;
                if (x > width - 1) x = width - 1;
                if (y > height - 1) y = height - 1;

                int x0 = (int)Math.Floor(x);
                int y0 = (int)Math.Floor(y);
                int x1 = x0 + 1;
                int y1 = y0 + 1;

                if (x1 >= width) x1 = width - 1;
                if (y1 >= height) y1 = height - 1;

                double fx = x - x0;
                double fy = y - y0;

                byte* p00 = src + y0 * stride + x0 * 4;
                byte* p10 = src + y0 * stride + x1 * 4;
                byte* p01 = src + y1 * stride + x0 * 4;
                byte* p11 = src + y1 * stride + x1 * 4;

                double w00 = (1.0 - fx) * (1.0 - fy);
                double w10 = fx * (1.0 - fy);
                double w01 = (1.0 - fx) * fy;
                double w11 = fx * fy;

                b = p00[0] * w00 + p10[0] * w10 + p01[0] * w01 + p11[0] * w11;
                g = p00[1] * w00 + p10[1] * w10 + p01[1] * w01 + p11[1] * w11;
                r = p00[2] * w00 + p10[2] * w10 + p01[2] * w01 + p11[2] * w11;
                a = p00[3] * w00 + p10[3] * w10 + p01[3] * w01 + p11[3] * w11;
            }

            // 이 파일의 핵심 동작을 수행하는 메서드.
            private static byte ClampToByte(double v)
            {
                if (v < 0.0) return 0;
                if (v > 255.0) return 255;
                return (byte)(v + 0.5);
            }
        }
    }
}
