using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;


// -----------------------------------------------------------------------------
// 파일 역할: PinchFilter 필터의 실제 픽셀 처리 알고리즘을 구현한다.
// 병합 작업 때 빠르게 구조를 파악할 수 있도록 주석을 보강한 버전이다.
// -----------------------------------------------------------------------------

namespace Photo
{
    // 메인 편집 창과 공통 편집 상태를 관리하는 핵심 폼 클래스.
    public partial class Main
    {
        /// <summary>
        /// 핀치 왜곡의 실제 픽셀 연산을 담당하는 필터 클래스.
        /// Command 계층에서 호출되며 여기서는 순수하게 계산만 수행한다.
        /// </summary>
        internal class PinchFilter
        {
            unsafe public static void PinchMasked32bpp_BGRA(
                byte* dst, int width, int height, int stride,
                byte[] mask,
                double strength = 0.42,
                double radiusScale = 1.15,
                double aspectX = 1.00,
                double aspectY = 0.85)
            {
                if (dst == null || mask == null) return;
                if (mask.Length < width * height) return;

                // -----------------------------
                // 1) 마스크 중심 / 범위 계산
                // -----------------------------
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
                    int rowIdx = y * width;
                    for (int x = 0; x < width; x++)
                    {
                        byte m = mask[rowIdx + x];
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

                // 전체 마스크 범위를 받되, 실제 왜곡은 안쪽 위주로 사용
                double radiusX = Math.Max(8.0, halfW * radiusScale);
                double radiusY = Math.Max(8.0, halfH * radiusScale);

                int workMinX = Math.Max(0, (int)Math.Floor(cx - radiusX));
                int workMaxX = Math.Min(width - 1, (int)Math.Ceiling(cx + radiusX));
                int workMinY = Math.Max(0, (int)Math.Floor(cy - radiusY));
                int workMaxY = Math.Min(height - 1, (int)Math.Ceiling(cy + radiusY));

                // -----------------------------
                // 2) 원본 백업
                // -----------------------------
                int byteCount = stride * height;
                byte[] srcCopy = new byte[byteCount];
                Marshal.Copy((IntPtr)dst, srcCopy, 0, byteCount);

                fixed (byte* src = srcCopy)
                {
                    // -----------------------------
                    // 3) Pinch 적용
                    // -----------------------------
                    for (int y = workMinY; y <= workMaxY; y++)
                    {
                        int maskRow = y * width;
                        byte* dstRow = dst + y * stride;

                        for (int x = workMinX; x <= workMaxX; x++)
                        {
                            byte m = mask[maskRow + x];
                            if (m == 0) continue;

                            double dx = x - cx;
                            double dy = y - cy;

                            // 타원 기준 정규화 거리
                            double nx = dx / radiusX;
                            double ny = dy / radiusY;
                            double dist = Math.Sqrt(nx * nx + ny * ny);

                            if (dist >= 1.0)
                                continue;

                            double alpha = m / 255.0;

                            // -----------------------------
                            // inner / outer 분리
                            // 바깥쪽은 완충 구간으로만 사용
                            // -----------------------------
                            const double innerRatio = 0.8;

                            
                            double ringAlpha;

                            double localT;
                            if (dist <= innerRatio)
                            {
                                // 실제 핀치 구간
                                localT = 1.0 - (dist / innerRatio);
                                ringAlpha = 1.0;
                            }
                            else
                            {
                                // 완충 링 구간
                                double edgeT = (1.0 - dist) / (1.0 - innerRatio);
                                if (edgeT < 0.0) edgeT = 0.0;
                                if (edgeT > 1.0) edgeT = 1.0;

                                // 왜곡은 약하게 남김
                                localT = edgeT * 0.20;
                                
                                
                                // 최종 합성은 더 빠르게 사라지게
                                ringAlpha = edgeT * edgeT;
                                ringAlpha = ringAlpha * ringAlpha;

                                
                            }

                            if (localT < 0.0) localT = 0.0;
                            if (localT > 1.0) localT = 1.0;

                            // 부드러운 감쇠
                            double falloff = localT * localT * (3.0 - 2.0 * localT);

                            // 중심부 과도한 접힘 방지
                            double centerGuard = dist / (dist + 0.10);

                            // 최대 이동량 제한
                            double maxShiftX = radiusX * strength * 0.22 * aspectX;
                            double maxShiftY = radiusY * strength * 0.22 * aspectY;

                            // 역매핑:
                            // 원본의 바깥쪽을 읽어와 중심으로 모이는 효과
                            double sx = x + dx * falloff * centerGuard * strength * 0.42 * aspectX;
                            double sy = y + dy * falloff * centerGuard * strength * 0.42 * aspectY;

                            double shiftX = sx - x;
                            double shiftY = sy - y;

                            if (shiftX > maxShiftX) shiftX = maxShiftX;
                            if (shiftX < -maxShiftX) shiftX = -maxShiftX;
                            if (shiftY > maxShiftY) shiftY = maxShiftY;
                            if (shiftY < -maxShiftY) shiftY = -maxShiftY;

                            sx = x + shiftX;
                            sy = y + shiftY;

                            SampleBilinearBGRA(
                                src, width, height, stride,
                                sx, sy,
                                out double sb, out double sg, out double sr, out double sa);

                            byte* p = dstRow + x * 4;

                            double ob = p[0];
                            double og = p[1];
                            double orr = p[2];
                            double oa = p[3];

                            // 마스크는 결과 blend가 아니라
                            // 왜곡 강도 보정 느낌으로만 사용
                            double blendStrength;

                            if (dist <= innerRatio)
                            {
                                // 중심부는 원본을 남기지 않고 왜곡 결과를 그대로 사용
                                blendStrength = 1.0;
                            }
                            else
                            {
                                // 바깥 feather 링만 부드럽게 섞음
                                blendStrength = alpha * ringAlpha;
                            }

                            p[0] = ClampToByte(ob + (sb - ob) * blendStrength);
                            p[1] = ClampToByte(og + (sg - og) * blendStrength);
                            p[2] = ClampToByte(orr + (sr - orr) * blendStrength);
                            p[3] = ClampToByte(oa + (sa - oa) * blendStrength);
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
