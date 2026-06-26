using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;
using Photo.Models;

namespace Photo.Models
{
    //스티커 객체 클래스
    public class StickerObject
    {
        public Bitmap StickerImage { get; set; }   // 스티커 원본 이미지
        public RectangleF Bounds { get; set; }     // 스티커 위치와 크기
        public bool IsSelected { get; set; }       // 현재 선택되었는지 여부
        public float Opacity { get; set; }         //= 0.0f; // 투명도(0.0 불투명 ~ 1.0 완전투명)
        public const float HandleSize = 10f;       // 크기 조절 핸들 크기

        public StickerObject(Bitmap image, float x, float y, float width, float height, float opa = 0.0f)
        {
            StickerImage = image;
            Bounds = new RectangleF(x, y, width, height);
            //스티커를 추가하면 선택된 상태로 저장.
            //다른 스티커 선택시 그 스티커 외 다른 스티커 
            IsSelected = false;
            Opacity = opa;
        }

        //주어진 마우스 좌표가 스티커 영역 안에 있는지 확인하는 메서드
        public bool Contains(PointF point)
        {
            return Bounds.Contains(point);
        }

        // 크기 조절 핸들(오른쪽 하단) 영역 반환
        public RectangleF GetResizeHandleRect()
        {
            return new RectangleF(Bounds.Right - HandleSize, Bounds.Bottom - HandleSize, HandleSize, HandleSize);
        }

        // 마우스가 핸들 위에 있는지 확인
        public bool IsOnResizeHandle(PointF point)
        {
            return GetResizeHandleRect().Contains(point);
        }
    };
}
