using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Photo.Commands
{
    internal class FaceEdit
    {
        public bool isShowingOriginal { get; set; } = false;
        public bool isBrushMode { get; set; } = false;   // 펜 모드
        public bool isEraserMode { get; set; } = false;  // 지우개 모드
        public bool isJuMode { get; set; } = false; // 주름 제거 모드 활성화 여부
        public bool isDrawingJu { get; set; } = false; // 현재 마우스 드래그 중인지
        public bool isJabMode = false;
        public Main.EditorState? juStartState { get; set; }
        public Main.EditorState? drawStartState { get; set; }


        public Bitmap drawLayer { get; set; }            // 투명한 그림 레이어
        public Color currentDrawColor { get; set; } = Color.Red; // 기본 펜 색상
        public Point lastDrawPoint { get; set; }         // 선을 잇기 위한 이전 좌표 저장

        public Bitmap juStartSnapshot { get; set; } // 드래그 시작 시점의 이미지 전체 백업
        public Bitmap drawStartSnapshot { get; set; }  //그리기/지우개용 스냅샷

        public float zoomFactor { get; set; } = 1.0f; // 1.0 = 100%, 2.0 = 200%
        public  float zoomStep  { get; set; }= 0.1f;

        public Bitmap originalImage { get; set; }
        public Bitmap workingImage { get; set; }
        public Bitmap ResultImage { get; set; }

        public int brushSize { get; set; } = 40;

    }
}
