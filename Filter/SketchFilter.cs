using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


// -----------------------------------------------------------------------------
// 파일 역할: SketchFilter 필터의 실제 픽셀 처리 알고리즘을 구현한다.
// 병합 작업 때 빠르게 구조를 파악할 수 있도록 주석을 보강한 버전이다.
// -----------------------------------------------------------------------------

namespace Photo
{
    // 메인 편집 창과 공통 편집 상태를 관리하는 핵심 폼 클래스.
    public partial class Main
    {
        /// <summary>
        /// 스케치의 실제 픽셀 연산을 담당하는 필터 클래스.
        /// Command 계층에서 호출되며 여기서는 순수하게 계산만 수행한다.
        /// </summary>
        internal class SketchFilter
        {
            unsafe public static void PencilSketch32bpp_BGRA(
                byte* src, int width, int height, int stride)
            {
                int bytes = height * stride;

                byte[] gray = new byte[bytes];
                byte[] inverted = new byte[bytes];
                byte[] blurred = new byte[bytes];

                fixed (byte* pGray = gray)
                fixed (byte* pInv = inverted)
                fixed (byte* pBlur = blurred)
                {
                    // 1️⃣ grayscale
                    for (int y = 0; y < height; y++)
                    {
                        for (int x = 0; x < width; x++)
                        {
                            byte* p = src + y * stride + x * 4;

                            byte b = p[0];
                            byte g = p[1];
                            byte red = p[2];

                            double gv = (byte)(0.114 * b + 0.587 * g + 0.299 * red);

                            // contrast 1.2 정도
                            gv = (gv - 128.0) * 1.2 + 128.0;

                            if (gv < 0) gv = 0;
                            if (gv > 255) gv = 255;

                            byte gval = (byte)gv;

                            pGray[y * stride + x * 4 + 0] = gval;
                            pGray[y * stride + x * 4 + 1] = gval;
                            pGray[y * stride + x * 4 + 2] = gval;
                            pGray[y * stride + x * 4 + 3] = p[3];
                        }
                    }

                    // 2️⃣ invert
                    for (int i = 0; i < bytes; i += 4)
                    {
                        pInv[i + 0] = (byte)(255 - pGray[i + 0]);
                        pInv[i + 1] = (byte)(255 - pGray[i + 1]);
                        pInv[i + 2] = (byte)(255 - pGray[i + 2]);
                        pInv[i + 3] = pGray[i + 3];
                    }

                    // 3️⃣ simple blur (radius 2)
                    int r = 2;

                    for (int y = 0; y < height; y++)
                    {
                        for (int x = 0; x < width; x++)
                        {
                            int sum = 0;
                            int count = 0;

                            for (int ky = -r; ky <= r; ky++)
                            {
                                for (int kx = -r; kx <= r; kx++)
                                {
                                    int xx = x + kx;
                                    int yy = y + ky;

                                    if (xx < 0) xx = 0;
                                    if (xx >= width) xx = width - 1;
                                    if (yy < 0) yy = 0;
                                    if (yy >= height) yy = height - 1;

                                    sum += pInv[yy * stride + xx * 4];
                                    count++;
                                }
                            }

                            byte v = (byte)(sum / count);

                            pBlur[y * stride + x * 4 + 0] = v;
                            pBlur[y * stride + x * 4 + 1] = v;
                            pBlur[y * stride + x * 4 + 2] = v;
                            pBlur[y * stride + x * 4 + 3] = 255;
                        }
                    }

                    // 4️⃣ color dodge blend
                    for (int y = 0; y < height; y++)
                    {
                        for (int x = 0; x < width; x++)
                        {
                            int i = y * stride + x * 4;

                            double baseVal = pGray[i];
                            double blend = pBlur[i];

                            double result = baseVal * 255.0 / (255.0 - blend + 1);

                            if (result > 255) result = 255;

                            byte v = (byte)result;

                            byte* dst = src + i;

                            dst[0] = v;
                            dst[1] = v;
                            dst[2] = v;
                        }
                    }
                }
            }
        }
    }
}
