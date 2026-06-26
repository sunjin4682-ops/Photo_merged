using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Photo.Models
{
    //콜라주 모델
    // 정해진 구역(템플릿)에 사진을 끼워 넣는 방식
    // 이 클래스는 콜라주 안에 있는 한 칸의 객체를 만드는 클래스
    public class CollageFrame
    {
        public RectangleF Area {  get; set; }  // 칸의 영역
        public Image Photo { get; set; }       // 채워진 사진(null이면 빈칸)

        public CollageFrame(RectangleF area)
        {
            Area = area;
            Photo = null;
        }

        // 마우스 클릭 시 이 칸이 선택되었는지 확인
        public bool Contains(Point pt) => Area.Contains(pt);
    }
}
