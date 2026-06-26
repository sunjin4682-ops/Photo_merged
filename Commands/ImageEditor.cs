using System;
using System.Collections.Generic;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Photo.Main;

namespace Photo
{
    public class ImageEditor
    {
        
        public Bitmap MainBIT { get;  set; } //이미지 비트맵
        public Bitmap OriginalBIT { get;  set; } // 초기화 기준 이미지

        public Point StartPNT { get; set; } //마우스 클릭 시작지점
        public Point EndPNT { get; set; } //마우스 클릭 종료지점

        public bool Dragging { get; set; } // ㅁ,ㅇ 그리기 드래그
        public bool isDrawing { get; set; } //자유곡선 그리기 드래그
        public bool ReverseSet { get;  set; } // 반전 자르기 
        public List<Point> FreePNT { get;  set; } = new List<Point>(); // 자유곡선 그리기 좌표
        public GraphicsPath path { get;  set; } = new GraphicsPath(); // 자유 곡선 그린 경로

        public EditMode CurrentEdit { get; set; } = EditMode.None; // 폼 시작시 None모드
        public CutMode Mode { get;  set; } = CutMode.None; // 자르기 시작시 None모드
        
        public Bitmap CloneBTM(Bitmap s)//Undo/Redo용 비트맵
        {
            return s.Clone(new Rectangle(0, 0, s.Width, s.Height), s.PixelFormat);        }                                                        // 
        public Stack<Bitmap> UnStack = new Stack<Bitmap>(); //Undo 비트맵
        public Stack<Bitmap> ReStack = new Stack<Bitmap>(); //Redo 비트맵

        public void SaveUndo()//이전 이미지 저장
        {
            if (MainBIT == null) return;
            UnStack.Push(CloneBTM(MainBIT));
            while (ReStack.Count > 0) ReStack.Pop().Dispose();


        }
        public void ClearRedo()//이전 이미지 편집데이터 제거
        {
            while (ReStack.Count > 0)
            {
                ReStack.Pop().Dispose();
            }
        }
        public void ClearUndo()//이전 이미지 편집데이터 제거
        {
            while (UnStack.Count > 0)
            {
                UnStack.Pop().Dispose();
            }
        }

        public Rectangle SetRect()// ㅁ,ㅇ 영역(사각형)
        {
            return new Rectangle(
                Math.Min(StartPNT.X, EndPNT.X),
                Math.Min(StartPNT.Y, EndPNT.Y),
                Math.Abs(StartPNT.X - EndPNT.X),
                Math.Abs(StartPNT.Y - EndPNT.Y)
                );
        }
        public enum CutMode// 자르기 모드 분류
        {
            Square, Circle, Free, None
        }
        public enum EditMode// 편집모드 분류
        {
            None, Cut, Mosaic
        }
        
        public void SetMode(CutMode Mode)//편집모드 기초
        {
            this.Mode = Mode;
            StartPNT = Point.Empty;
            EndPNT = Point.Empty;
            FreePNT.Clear();
            path.Reset();
            Dragging = false;
            isDrawing = false;
        }
        public void CancelSelect()// 영역선택 후 X누를시
        {
            this.Mode = Mode;
            StartPNT = Point.Empty;
            EndPNT = Point.Empty;
            FreePNT.Clear();
            path.Reset();
            Dragging = false;
            isDrawing = false;
        }

        public void ApplyCut()//자르기 적용(6가지)
        {
            switch (Mode)
            {
                case CutMode.Square:
                    if (ReverseSet) ApplySquareReverse();
                    else ApplySquare();
                    break;

                case CutMode.Circle:
                    if (ReverseSet) ApplyCircleReverse();
                    else ApplyCircle();
                    break;

                case CutMode.Free:
                    if (ReverseSet) ApplyFreeReverse();
                    else ApplyFree();
                    break;
            }
        }
        public void ApplyMosaic()//모자이크 적용(3가지)
        {
            switch (Mode)
            {
                case CutMode.Square:
                    ApplyMosaicSquare();
                    break;

                case CutMode.Circle:
                    ApplyMosaicCircle();
                    break;

                case CutMode.Free:
                    ApplyMosaicFree();
                    break;
            }
        }
        public void ApplyMosaicSquare()//모자이크 ㅁ 적용
        {
            if (MainBIT == null) return;

            Rectangle rect = SetRect();
            rect.Intersect(new Rectangle(0, 0, MainBIT.Width, MainBIT.Height));

            if (rect.Width <= 0 || rect.Height <= 0) return;

            int block = 12;   // 모자이크 강도

            for (int y = rect.Top; y < rect.Bottom; y += block){
                for (int x = rect.Left; x < rect.Right; x += block){
                    int offsetX = Math.Min(block, rect.Right - x);
                    int offsetY = Math.Min(block, rect.Bottom - y);

                    Color pixel = MainBIT.GetPixel(x, y);

                    for (int yy = 0; yy < offsetY; yy++){
                        for (int xx = 0; xx < offsetX; xx++){
                            MainBIT.SetPixel(x + xx, y + yy, pixel);
                        }
                    }
                }
            }
        }
        public void ApplyMosaicCircle()//모자이크 ㅇ 적용
        {
            Rectangle rect = SetRect();

            GraphicsPath gp = new GraphicsPath();
            gp.AddEllipse(rect);

            Region region = new Region(gp);

            ApplyMosaicRegion(region);
        }
        public void ApplyMosaicFree()//모자이크 자유지정 적용
        {
            if (path == null || path.PointCount < 3) return;

            Region region = new Region(path);

            ApplyMosaicRegion(region);
        }
        public void ApplyMosaicRegion(Region region)//모자이크 처리
        {
            int block = 12;

            Rectangle bounds = Rectangle.Round(region.GetBounds(Graphics.FromImage(MainBIT)));
            bounds.Intersect(new Rectangle(0, 0, MainBIT.Width, MainBIT.Height));
            for (int y = bounds.Top; y < bounds.Bottom; y += block){
                for (int x = bounds.Left; x < bounds.Right; x += block){
                    if (!region.IsVisible(x, y)) continue;

                    int offsetX = Math.Min(block, bounds.Right - x);
                    int offsetY = Math.Min(block, bounds.Bottom - y);

                    Color pixel = MainBIT.GetPixel(x, y);

                    for (int yy = 0; yy < offsetY; yy++){
                        for (int xx = 0; xx < offsetX; xx++){
                            if (region.IsVisible(x + xx, y + yy))
                                MainBIT.SetPixel(x + xx, y + yy, pixel);
                        }
                    }
                }
            }
        }

        public void ApplySquare()//자르기 ㅁ 처리
        {
            if (MainBIT == null) return;
            Rectangle rect = SetRect();
            rect.Intersect(new Rectangle(0, 0, MainBIT.Width, MainBIT.Height));
            if (rect.Width <= 0 || rect.Height <= 0) return;
            Bitmap newbit = new Bitmap(rect.Width, rect.Height);
            using (Graphics g = Graphics.FromImage(newbit))
            {
                g.DrawImage(MainBIT, new Rectangle(0, 0, rect.Width, rect.Height), rect, GraphicsUnit.Pixel);
            }
            MainBIT.Dispose();
            MainBIT = newbit;
            
        }
        public void ApplySquareReverse()//자르기 ㅁ 반전 처리
        {
            if (MainBIT == null) return;

            Rectangle rect = SetRect();
            rect.Intersect(new Rectangle(0, 0, MainBIT.Width, MainBIT.Height));
            if (rect.Width <= 0 || rect.Height <= 0) return;

            Bitmap newbit = new Bitmap(MainBIT.Width, MainBIT.Height, System.Drawing.Imaging.PixelFormat.Format32bppArgb);

            using (Graphics g = Graphics.FromImage(newbit))
            {
                g.DrawImage(MainBIT, 0, 0);

                using (SolidBrush transparentBrush = new SolidBrush(Color.Transparent))
                {
                    g.CompositingMode = CompositingMode.SourceCopy;
                    g.FillRectangle(transparentBrush, rect);
                }
            }
            MainBIT.Dispose();
            MainBIT = newbit;
        }
        public void ApplyCircle()//자르기 ㅇ 처리
        {
            if (MainBIT == null) return;
            Rectangle rect = SetRect();
            rect.Intersect(new Rectangle(0, 0, MainBIT.Width, MainBIT.Height));
            if (rect.Width <= 0 || rect.Height <= 0) return;
            Bitmap newbit = new Bitmap(rect.Width, rect.Height, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
            using (Graphics g = Graphics.FromImage(newbit))
            {
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.Clear(Color.Transparent);
                using (GraphicsPath gp = new GraphicsPath())
                {
                    gp.AddEllipse(0, 0, rect.Width, rect.Height);
                    g.SetClip(gp);
                    g.DrawImage(MainBIT, new Rectangle(0, 0, rect.Width, rect.Height), rect, GraphicsUnit.Pixel);
                }
            }
            MainBIT.Dispose();
            MainBIT = newbit;
        }
        public void ApplyCircleReverse()//자르기 ㅇ 반전 처리
        {
            if (MainBIT == null) return;

            Rectangle rect = SetRect();
            rect.Intersect(new Rectangle(0, 0, MainBIT.Width, MainBIT.Height));
            if (rect.Width <= 0 || rect.Height <= 0) return;

            Bitmap newbit = new Bitmap(MainBIT.Width, MainBIT.Height, System.Drawing.Imaging.PixelFormat.Format32bppArgb);

            using (Graphics g = Graphics.FromImage(newbit))
            {
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.DrawImage(MainBIT, 0, 0);

                using (GraphicsPath gp = new GraphicsPath())
                {
                    gp.AddEllipse(rect);

                    g.CompositingMode = CompositingMode.SourceCopy;
                    using (SolidBrush transparentBrush = new SolidBrush(Color.Transparent))
                    {
                        g.FillPath(transparentBrush, gp);
                    }
                }
            }
            MainBIT.Dispose();
            MainBIT = newbit;
        }
        public void ApplyFree()//자르기 자유곡선 처리
        {
            if (MainBIT == null) return;
            if (path == null || path.PointCount < 3) return;

            Rectangle bounds = Rectangle.Round(path.GetBounds());
            bounds.Intersect(new Rectangle(0, 0, MainBIT.Width, MainBIT.Height));

            bounds.Intersect(new Rectangle(0, 0, MainBIT.Width, MainBIT.Height));
            if (bounds.Width <= 0 || bounds.Height <= 0) return;
            Bitmap newbit = new Bitmap(bounds.Width, bounds.Height, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
            using (Graphics g = Graphics.FromImage(newbit))
            {
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.Clear(Color.Transparent);
                using (GraphicsPath localPath = (GraphicsPath)path.Clone())
                using (Matrix m = new Matrix())
                {
                    m.Translate(-bounds.X, -bounds.Y);
                    localPath.Transform(m);

                    g.SetClip(localPath);
                    g.DrawImage(MainBIT,
                        new Rectangle(0, 0, bounds.Width, bounds.Height),
                        bounds,
                        GraphicsUnit.Pixel);
                }
            }
            MainBIT.Dispose();
            MainBIT = newbit;
        }
        public void ApplyFreeReverse()//자르기 자유곡선 반전 처리
        {
            if (MainBIT == null) return;
            if (path == null || path.PointCount < 3) return;

            Bitmap newbit = new Bitmap(MainBIT.Width, MainBIT.Height, System.Drawing.Imaging.PixelFormat.Format32bppArgb);

            using (Graphics g = Graphics.FromImage(newbit))
            {
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.DrawImage(MainBIT, 0, 0);

                g.CompositingMode = CompositingMode.SourceCopy;
                using (SolidBrush transparentBrush = new SolidBrush(Color.Transparent))
                {
                    g.FillPath(transparentBrush, path);
                }
            }

            MainBIT.Dispose();
            MainBIT = newbit;
        }

        public void Undo()//실행 취소
        {
            if (UnStack.Count == 0 || MainBIT == null) return;
            ReStack.Push(CloneBTM(MainBIT));
            if (MainBIT != null) MainBIT.Dispose();
            MainBIT = UnStack.Pop();

            CancelSelect();
        }
        public void Redo()//다시 실행
        {
            if (ReStack.Count == 0 || MainBIT == null) return;
            UnStack.Push(CloneBTM(MainBIT));
            if (MainBIT != null) MainBIT.Dispose();
            MainBIT = ReStack.Pop();
            CancelSelect();
        }

        public void ResetToOriginal()//초기화
        {
            if (OriginalBIT == null || MainBIT == null) return;

            SaveUndo();   // 초기화 전 상태를 undo에 저장

            MainBIT.Dispose();
            MainBIT = CloneBTM(OriginalBIT);
            CancelSelect();
        }
        public void CommitCurrent()//확정(되돌리기 불가)
        {
            OriginalBIT = CloneBTM(MainBIT);
            ClearUndo();
            ClearRedo();
            
        }
        public void LoadImage(Bitmap bmp)
        {
            ClearRedo();
            ClearUndo();

            if (OriginalBIT != null)
            {
                OriginalBIT.Dispose();
                OriginalBIT = null;
            }

            if (MainBIT != null)
            {
                MainBIT.Dispose();
                MainBIT = null;
            }

            OriginalBIT = new Bitmap(bmp);
            MainBIT = new Bitmap(bmp);
        }
        public void LoadImage(string filePath)
        {
            using (FileStream fs = new FileStream(filePath, FileMode.Open, FileAccess.Read))
            using (Image img = Image.FromStream(fs))
            {
                LoadImage(new Bitmap(img));
            }
        }
    }
}
