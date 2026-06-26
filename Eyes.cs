using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using DlibDotNet;
using OpenCvSharp;
using OpenCvSharp.Extensions;


// -----------------------------------------------------------------------------
// 파일 역할: 얼굴/눈 검출과 눈 확대 미리보기 및 적용 흐름을 담당한다.
// 병합 작업 때 빠르게 구조를 파악할 수 있도록 주석을 보강한 버전이다.
// -----------------------------------------------------------------------------

namespace Photo
{
    // 메인 편집 창과 공통 편집 상태를 관리하는 핵심 폼 클래스.
    public partial class Main
    {
        private ShapePredictor? shapePredictor;
        private FrontalFaceDetector? faceDetector;

        // 비트맵 임시 결과 또는 미리보기 이미지를 저장한다.
        private Bitmap? previewBaseBitmap;   // 눈 키우기 미리보기 시작 시점 원본
        // 현재 선택/진행 중인 상태값을 저장한다.
        private float currentEyeStrength = 0f;

        private List<System.Drawing.Point> leftEyePoints = new();
        private List<System.Drawing.Point> rightEyePoints = new();
        private bool faceDetected = false;
        // 트랙바 미리보기용 현재 비트맵
        private Bitmap? eyePreviewBitmap;  

        // 초기화 작업을 수행한다.
        private void InitializeDlib()
        {
            try
            {
                faceDetector = Dlib.GetFrontalFaceDetector();
                string modelPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "shape_predictor_68_face_landmarks.dat");
                if (File.Exists(modelPath))
                    shapePredictor = ShapePredictor.Deserialize(modelPath);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Dlib 로드 실패: {ex.Message}");
            }
        }

        ////눈키우기 클릭
        // private void Btneyes_Click(object? sender, EventArgs e)
        //{
            
        //}

        // 트랙바 스크롤 값을 반영한다.
        private void TrackBar1_Scroll(object? sender, EventArgs e)
        {
            if (!faceDetected || previewBaseBitmap == null)
                return;

            float t = trackBar1.Value / (float)trackBar1.Maximum;
            float eased = t * t;
            float maxStrength = 0.65f;
            currentEyeStrength = eased * maxStrength;

            eyePreviewBitmap?.Dispose();
            eyePreviewBitmap = ApplyEyeEnlarge(previewBaseBitmap, currentEyeStrength);

            MainPic.Image = eyePreviewBitmap;
            MainPic.Invalidate();
        }

        // 버튼/메뉴 클릭 이벤트를 처리한다.
        
        private void btnApplyEyes_Click(object sender, EventArgs e)
        {
            if (!faceDetected || previewBaseBitmap == null)
                return;

            if (currentEyeStrength <= 0.0001f)
                return;

            commandManager.ExecuteCommand(new EyeEnlargeCommand(this, currentEyeStrength, previewBaseBitmap));
            UpdateUndoRedoButtons();

            eyePreviewBitmap?.Dispose();
            eyePreviewBitmap = null;

            previewBaseBitmap?.Dispose();
            previewBaseBitmap = null;

            faceDetected = false;
            currentEyeStrength = 0f;
            trackBar1.Value = 0;

            MainPic.Invalidate();

            btnApplyEyes.Enabled = false;
            btnApplyEyes.Visible = false;
            label3.Visible = false;
            trackBar1.Enabled = false;
            trackBar1.Visible = false;
        }
        

        // 현재 효과나 상태를 실제 이미지/편집 상태에 반영한다.
        public Bitmap ApplyEyeEnlarge(Bitmap source, float strength)
        {
            if (strength <= 0.0001f)
                return (Bitmap)source.Clone();

            using (Mat src = BitmapConverter.ToMat(source))
            using (Mat dst = src.Clone())
            {
                var lCenter = GetCenter(leftEyePoints);
                var rCenter = GetCenter(rightEyePoints);

                int leftEyeWidth = Math.Abs(leftEyePoints[3].X - leftEyePoints[0].X);
                int rightEyeWidth = Math.Abs(rightEyePoints[3].X - rightEyePoints[0].X);

                int leftRadius = Math.Max(8, (int)(leftEyeWidth * 0.95));
                int rightRadius = Math.Max(8, (int)(rightEyeWidth * 0.95));

                ProcessBulge(src, dst, lCenter, leftRadius, strength);
                ProcessBulge(src, dst, rCenter, rightRadius, strength);

                return BitmapConverter.ToBitmap(dst);
            }
        }

        // 이 파일의 핵심 동작을 수행하는 메서드.
        private void ProcessBulge(Mat src, Mat dst, System.Drawing.Point center, int radius, float strength)
        {
            int xStart = Math.Max(0, center.X - radius);
            int xEnd = Math.Min(src.Cols - 1, center.X + radius);
            int yStart = Math.Max(0, center.Y - radius);
            int yEnd = Math.Min(src.Rows - 1, center.Y + radius);

            for (int y = yStart; y <= yEnd; y++)
            {
                for (int x = xStart; x <= xEnd; x++)
                {
                    double dx = x - center.X;
                    double dy = y - center.Y;
                    double dist = Math.Sqrt(dx * dx + dy * dy);

                    if (dist >= radius)
                        continue;

                    double ratio = dist / radius;
                    double falloff = 1.0 - ratio * ratio;
                    double localAmount = strength * falloff;

                    double sx = center.X + dx * (1.0 - localAmount);
                    double sy = center.Y + dy * (1.0 - localAmount);

                    dst.Set(y, x, GetSafePixel(src, sx, sy));
                }
            }
        }

        // 계산 결과나 내부 값을 반환한다.
        private Vec3b GetSafePixel(Mat img, double x, double y)
        {
            x = Math.Max(0, Math.Min(x, img.Cols - 1.001));
            y = Math.Max(0, Math.Min(y, img.Rows - 1.001));

            int x1 = (int)x;
            int y1 = (int)y;
            int x2 = Math.Min(x1 + 1, img.Cols - 1);
            int y2 = Math.Min(y1 + 1, img.Rows - 1);

            double dx = x - x1;
            double dy = y - y1;

            Vec3b p1 = img.At<Vec3b>(y1, x1);
            Vec3b p2 = img.At<Vec3b>(y1, x2);
            Vec3b p3 = img.At<Vec3b>(y2, x1);
            Vec3b p4 = img.At<Vec3b>(y2, x2);

            byte b = (byte)(
                (1 - dx) * (1 - dy) * p1.Item0 +
                dx * (1 - dy) * p2.Item0 +
                (1 - dx) * dy * p3.Item0 +
                dx * dy * p4.Item0);

            byte g = (byte)(
                (1 - dx) * (1 - dy) * p1.Item1 +
                dx * (1 - dy) * p2.Item1 +
                (1 - dx) * dy * p3.Item1 +
                dx * dy * p4.Item1);

            byte r = (byte)(
                (1 - dx) * (1 - dy) * p1.Item2 +
                dx * (1 - dy) * p2.Item2 +
                (1 - dx) * dy * p3.Item2 +
                dx * dy * p4.Item2);

            return new Vec3b(b, g, r);
        }

        // 계산 결과나 내부 값을 반환한다.
        private System.Drawing.Point GetCenter(List<System.Drawing.Point> pts)
        {
            int x = 0, y = 0;
            foreach (var p in pts)
            {
                x += p.X;
                y += p.Y;
            }
            return new System.Drawing.Point(x / pts.Count, y / pts.Count);
        }
    }
}
