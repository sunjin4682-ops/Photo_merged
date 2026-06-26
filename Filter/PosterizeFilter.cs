using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


// -----------------------------------------------------------------------------
// 파일 역할: PosterizeFilter 필터의 실제 픽셀 처리 알고리즘을 구현한다.
// 병합 작업 때 빠르게 구조를 파악할 수 있도록 주석을 보강한 버전이다.
// -----------------------------------------------------------------------------

namespace Photo
{
    // 메인 편집 창과 공통 편집 상태를 관리하는 핵심 폼 클래스.
    public partial class Main
    {
        /// <summary>
        /// 포스터라이즈의 실제 픽셀 연산을 담당하는 필터 클래스.
        /// Command 계층에서 호출되며 여기서는 순수하게 계산만 수행한다.
        /// </summary>
        internal class PosterEffectFilter
        {
            unsafe public static void PosterEffect32bpp_BGRA(
                byte* src, int width, int height, int stride,
                int levels = 5,
                double contrast = 1.12,
                int saturationBoost = 12)
            {
                if (levels < 2) levels = 2;
                if (levels > 256) levels = 256;

                double step = 255.0 / (levels - 1);

                for (int y = 0; y < height; y++)
                {
                    byte* row = src + y * stride;

                    for (int x = 0; x < width; x++)
                    {
                        byte* p = row + x * 4;

                        double b = p[0];
                        double g = p[1];
                        double r = p[2];

                        // 1) 약한 contrast boost
                        r = (r - 128.0) * contrast + 128.0;
                        g = (g - 128.0) * contrast + 128.0;
                        b = (b - 128.0) * contrast + 128.0;

                        // clamp
                        if (r < 0) r = 0; if (r > 255) r = 255;
                        if (g < 0) g = 0; if (g > 255) g = 255;
                        if (b < 0) b = 0; if (b > 255) b = 255;

                        // 2) posterize
                        r = Math.Round(r / step) * step;
                        g = Math.Round(g / step) * step;
                        b = Math.Round(b / step) * step;

                        // clamp
                        if (r < 0) r = 0; if (r > 255) r = 255;
                        if (g < 0) g = 0; if (g > 255) g = 255;
                        if (b < 0) b = 0; if (b > 255) b = 255;

                        // 3) 살짝 채도 올리기
                        // 평균을 기준으로 각 채널을 조금 벌려서 포스터 느낌 강화
                        double avg = (r + g + b) / 3.0;

                        r = avg + (r - avg) * (1.0 + saturationBoost / 100.0);
                        g = avg + (g - avg) * (1.0 + saturationBoost / 100.0);
                        b = avg + (b - avg) * (1.0 + saturationBoost / 100.0);

                        // 최종 clamp
                        if (r < 0) r = 0; if (r > 255) r = 255;
                        if (g < 0) g = 0; if (g > 255) g = 255;
                        if (b < 0) b = 0; if (b > 255) b = 255;

                        p[0] = (byte)b;
                        p[1] = (byte)g;
                        p[2] = (byte)r;
                        // alpha 유지
                    }
                }
            }
        }
    }
    
   
}
