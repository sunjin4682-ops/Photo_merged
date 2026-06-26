using Photo.Commands;
using Photo.Models;
using Photo.Service;
using Photo.Services;
using System;
using System.Collections.Generic;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


// -----------------------------------------------------------------------------
// 파일 역할: MainPic 마우스 입력을 받아 현재 편집 도구로 분기한다.
// 병합 작업 때 빠르게 구조를 파악할 수 있도록 주석을 보강한 버전이다.
// -----------------------------------------------------------------------------

namespace Photo
{
    // 메인 편집 창과 공통 편집 상태를 관리하는 핵심 폼 클래스.
    public partial class Main
    {
        /// <summary>
        /// PictureBox 위에 편집용 오버레이를 그린다.
        /// 현재 도구가 마스크면 칠한 영역을, 텍스트가 있으면 텍스트 미리보기를 함께 표시한다.
        /// </summary>
        private void MainPic_Paint(object? sender, PaintEventArgs e)
        {
            // 1. 콜라주 오버레이는 이미지 없어도 그릴 수 있음
            if (_isChoosingImage && collageFrames != null)
            {
                DrawCollageOverlay(e);
            }

            // 2. 실제 이미지가 있을 때만 이미지 기반 그리기
            if (MainPic.Image == null && FaceMode.workingImage == null) return;
            
            DrawFaceAndDrawLayer(e); 
            DrawStickerOverlay(e);
            DrawSelectionOverlay(e);

            if (currentTool == EditorTool.MaskBrush)
                DrawMaskOverlay(e);

            DrawTextOverlay(e);
        }

        /// <summary>
        /// 마우스 입력 종료를 현재 도구에 맞게 전달한다.
        /// 드래그 중이던 마스크/텍스트 작업을 마무리하는 진입점이다.
        /// </summary>
        private void MainPic_MouseUp(object? sender, MouseEventArgs e)
        {
            currentMousePoint = e.Location;
            showBrushCursor = true;

            // 1. 스티커 이동 / 크기 조절 종료
            if (HandleStickerMouseUp(e))
            {
                MainPic.Invalidate();
                return;
            }

            // 2. 자르기 / 모자이크 선택 종료
            if (HandleSelectionMouseUp(e))
            {
                MainPic.Invalidate();
                return;
            }

            //3.주름 제거 종료
            if (HandleJuMouseUp(e))
            {
                MainPic.Invalidate();
                return;
            }

            // 4. 그리기 / 지우개 종료
            if (HandleDrawMouseUp(e))
            {
                MainPic.Invalidate();
                return;
            }

            // 5. 기본 도구 분기
            switch (currentTool)
            {
                case EditorTool.MaskBrush:
                    HandleMaskMouseUp(e);
                    break;

                case EditorTool.Text:
                    HandleTextMouseUp(e);
                    break;
            }

            MainPic.Invalidate();
        }

        /// <summary>
        /// 마우스 이동을 현재 도구에 맞게 해석한다.
        /// 마스크 칠하기나 텍스트 이동처럼 드래그 중인 작업이 여기서 계속 진행된다.
        /// </summary>
        private void MainPic_MouseMove(object? sender, MouseEventArgs e)
        {
            currentMousePoint = e.Location;
            showBrushCursor = true;

            // 1. 스티커 이동 / 리사이즈
            if (HandleStickerMouseMove(e))
            {
                MainPic.Invalidate();
                return;
            }

            // 2. 자르기 / 모자이크 드래그
            if (HandleSelectionMouseMove(e))
            {
                MainPic.Invalidate();
                return;
            }

            // 3. 주름 제거 연속 적용
            if (HandleJuMouseMove(e))
            {
                MainPic.Invalidate();
                return;
            }

            // 4. 그리기 / 지우개 드래그
            if (HandleDrawMouseMove(e))
            {
                MainPic.Invalidate();
                return;
            }

            // 5. 기본 도구 분기
            switch (currentTool)
            {
                case EditorTool.MaskBrush:
                    HandleMaskMouseMove(e);
                    break;

                case EditorTool.Text:
                    HandleTextMouseMove(e);
                    break;
            }

            MainPic.Invalidate();
        }
        // 마우스가 컨트롤을 벗어났을 때 표시 상태를 정리한다.
        private void MainPic_MouseLeave(object? sender, EventArgs e)
        {
            showBrushCursor = false;
            MainPic.Invalidate();
        }
        /// <summary>
        /// 마우스 입력 시작을 현재 도구에 맞게 전달한다.
        /// 어떤 편집 기능을 시작할지 분기하는 공용 진입점이다.
        /// </summary>
        private void MainPic_MouseDown(object? sender, MouseEventArgs e)
        {
             currentMousePoint = e.Location;
            showBrushCursor = true;
            
            // 1. 콜라주 프레임 클릭
            if (HandleCollageMouseDown(e))
            {
                MainPic.Invalidate();
                return;
            }

            if (MainPic.Image == null) return;

            if (e.Button == MouseButtons.Right)
            {
                StickerObject rightClickedSticker = FindStickerAt(e.Location);
                if (rightClickedSticker != null)
                {
                    stickersList.ForEach(s => s.IsSelected = false);

                    _targetSticker = rightClickedSticker;
                    _targetSticker.IsSelected = true;
                    _originalBounds = _targetSticker.Bounds;
                    _lastMousePos = e.Location;
                    _isResizing = false;

                    trbOpacity.Value = (int)(_targetSticker.Opacity * 100);
                    trbOpacity.Enabled = true;
                    trbOpacity.Visible = true;
                    label1.Visible = true;

                    isPaintingMask = false;
                    showBrushCursor = false;

                    MainPic.Invalidate();
                    return;
                }
            }


            // 2. 스티커 선택 / 리사이즈 시작
            if (HandleStickerMouseDown(e))
            {
                MainPic.Invalidate();
                return;
            }

            // 3. 자르기 / 모자이크 영역 선택 시작
            if (HandleSelectionMouseDown(e))
            {
                MainPic.Invalidate();
                return;
            }

            // 4. 주름 제거 시작
            if (HandleJuMouseDown(e))
            {
                MainPic.Invalidate();
                return;
            }

            // 5. 그리기 / 지우개 시작
            if (HandleDrawMouseDown(e))
            {
                MainPic.Invalidate();
                return;
            }

            // 6. 기본 도구 분기 (민준 구조 유지)
            switch (currentTool)
            {
                case EditorTool.MaskBrush:
                    HandleMaskMouseDown(e);
                    break;

                case EditorTool.Text:
                    HandleTextMouseDown(e);
                    break;

                default:
                    ClearStickerSelection();
                    break;
            }

            MainPic.Invalidate();
        }


        // ---------------------------------------------------------------------
        // 콜라주
        // ---------------------------------------------------------------------
        private bool HandleCollageMouseDown(MouseEventArgs e)
        {
            var clickedFrame = collageFrames.FirstOrDefault(f => f.Contains(e.Location));
            if (clickedFrame == null || !_isChoosingImage)
                return false;

            try
            {
                using (OpenFileDialog ofd = new OpenFileDialog())
                {
                    if (ofd.ShowDialog() == DialogResult.OK)
                    {
                        clickedFrame.Photo = Image.FromFile(ofd.FileName);
                    }
                }
            }
            catch
            {
                MessageBox.Show("이미지를 가져오는데 실패했습니다.\n이미지 확장자명을 확인하세요.");
            }

            return true;
        }

        private void DrawCollageOverlay(PaintEventArgs e)
        {
            if (!_isChoosingImage) return;

            foreach (var frame in collageFrames)
            {
                if (frame.Photo != null)
                {
                    e.Graphics.SetClip(frame.Area);
                    RectangleF drawRect = CollageService.GetAspectFillRect(frame.Area, frame.Photo);
                    e.Graphics.DrawImage(frame.Photo, drawRect);
                    e.Graphics.ResetClip();
                }
                else
                {
                    e.Graphics.FillRectangle(Brushes.LightGray, frame.Area);
                }

                e.Graphics.DrawRectangle(Pens.White, Rectangle.Round(frame.Area));
            }
        }

        // ---------------------------------------------------------------------
        // 스티커
        // ---------------------------------------------------------------------

        //스티커 찾기 헬퍼
        private StickerObject FindStickerAt(Point location)
        {
            return stickersList.LastOrDefault(s =>
                s.Contains(location) || s.IsOnResizeHandle(location));
        }

        private bool HandleStickerMouseDown(MouseEventArgs e)
        {
            StickerObject clickedSticker =
                stickersList.LastOrDefault(s => s.Contains(e.Location) || s.IsOnResizeHandle(e.Location));

            if (clickedSticker == null)
            {
                trbOpacity.Enabled = false;
                trbOpacity.Visible = false;
                label1.Visible = false;
                return false;
            }

            stickersList.ForEach(s => s.IsSelected = false);

            _targetSticker = clickedSticker;
            _targetSticker.IsSelected = true;
            _originalBounds = _targetSticker.Bounds;
            _lastMousePos = e.Location;
            trbOpacity.Value = (int)(_targetSticker.Opacity * 100);
            trbOpacity.Enabled = true;
            trbOpacity.Visible = true;
            label1.Visible = true;
            _isResizing = _targetSticker.IsOnResizeHandle(e.Location);

            if (stickersList.IndexOf(_targetSticker) != stickersList.Count - 1)
            {
                ICommand frontCmd = new BringToFrontCommand(
                    stickersList,
                    _targetSticker,
                    () => MainPic.Invalidate());

                commandManager.ExecuteCommand(frontCmd);
                UpdateUndoRedoButtons();
                MainPic.Invalidate();
            }

            return true;
        }

        private bool HandleStickerMouseMove(MouseEventArgs e)
        {
            if (_targetSticker == null || e.Button != MouseButtons.Left)
                return false;

            float dx = e.X - _lastMousePos.X;
            float dy = e.Y - _lastMousePos.Y;

            if (_isResizing)
            {
                _targetSticker.Bounds = new RectangleF(
                    _targetSticker.Bounds.X,
                    _targetSticker.Bounds.Y,
                    Math.Max(20, _targetSticker.Bounds.Width + dx),
                    Math.Max(20, _targetSticker.Bounds.Height + dy));
            }
            else
            {
                _targetSticker.Bounds = new RectangleF(
                    _targetSticker.Bounds.X + dx,
                    _targetSticker.Bounds.Y + dy,
                    _targetSticker.Bounds.Width,
                    _targetSticker.Bounds.Height);
            }

            _lastMousePos = e.Location;
            return true;
        }

        private bool HandleStickerMouseUp(MouseEventArgs e)
        {
            if (_targetSticker == null)
                return false;

            if (_originalBounds != _targetSticker.Bounds)
            {
                ICommand cmd;

                if (_isResizing)
                {
                    cmd = new ResizeStickerCommand(
                        _targetSticker,
                        _originalBounds,
                        _targetSticker.Bounds,
                        () => MainPic.Invalidate());
                }
                else
                {
                    cmd = new MoveStickerCommand(
                        _targetSticker,
                        _originalBounds,
                        _targetSticker.Bounds,
                        () => MainPic.Invalidate());
                }

                commandManager.ExecuteCommand(cmd);
                UpdateUndoRedoButtons();
                MainPic.Invalidate();
            }

            _isResizing = false;
            return true;
        }

        private void DrawStickerOverlay(PaintEventArgs e)
        {
            foreach (var sticker in stickersList)
            {
                float[][] matrixItems =
                {
                    new float[] {1, 0, 0, 0, 0},
                    new float[] {0, 1, 0, 0, 0},
                    new float[] {0, 0, 1, 0, 0},
                    new float[] {0, 0, 0, 1 - sticker.Opacity, 0},
                    new float[] {0, 0, 0, 0, 1},
                };

                ColorMatrix colorMatrix = new ColorMatrix(matrixItems);

                using (ImageAttributes attributes = new ImageAttributes())
                {
                    attributes.SetColorMatrix(colorMatrix, ColorMatrixFlag.Default, ColorAdjustType.Bitmap);
                    Rectangle destRect = Rectangle.Round(sticker.Bounds);

                    e.Graphics.DrawImage(
                        sticker.StickerImage,
                        destRect,
                        0, 0,
                        sticker.StickerImage.Width,
                        sticker.StickerImage.Height,
                        GraphicsUnit.Pixel,
                        attributes);
                }

                if (sticker.IsSelected)
                {
                    using (Pen p = new Pen(Color.Blue, 2))
                    {
                        e.Graphics.DrawRectangle(p, Rectangle.Round(sticker.Bounds));
                        e.Graphics.FillRectangle(Brushes.White, sticker.GetResizeHandleRect());
                        e.Graphics.DrawRectangle(Pens.Blue, Rectangle.Round(sticker.GetResizeHandleRect()));
                    }
                }
            }
        }

        private void ClearStickerSelection()
        {
            _targetSticker = null;
            stickersList.ForEach(s => s.IsSelected = false);
        }

        // ---------------------------------------------------------------------
        // 자르기 / 모자이크
        // ---------------------------------------------------------------------

        private bool HandleSelectionMouseDown(MouseEventArgs e)
        {
            if (editor.Mode == ImageEditor.CutMode.None)
                return false;

            if (!TryPictureBoxToImage(e.Location, out Point imagePt))
                return false;

            ClearStickerSelection();

            editor.StartPNT = imagePt;
            editor.EndPNT = imagePt;
            editor.Dragging = true;

            if (editor.Mode == ImageEditor.CutMode.Free)
            {
                editor.FreePNT.Clear();
                editor.FreePNT.Add(imagePt);
                editor.isDrawing = true;
            }

            MainPic.Invalidate();
            return true;
        }

        private bool HandleSelectionMouseMove(MouseEventArgs e)
        {
            if (editor.Mode == ImageEditor.CutMode.None)
                return false;

            if (!TryPictureBoxToImage(e.Location, out Point imagePt))
                return true;

            if (editor.Dragging && editor.Mode != ImageEditor.CutMode.Free)
            {
                editor.EndPNT = imagePt;
            }

            if (editor.isDrawing && editor.Mode == ImageEditor.CutMode.Free)
            {
                editor.FreePNT.Add(imagePt);
            }

            MainPic.Invalidate();
            return true;
        }

        private bool HandleSelectionMouseUp(MouseEventArgs e)
        {
            if (editor.Mode == ImageEditor.CutMode.None)
                return false;

            if (TryPictureBoxToImage(e.Location, out Point imagePt))
            {
                editor.EndPNT = imagePt;

                if (editor.Mode == ImageEditor.CutMode.Free && editor.FreePNT.Count > 2)
                {
                    editor.path.Reset();
                    editor.path.AddLines(editor.FreePNT.ToArray());
                    editor.path.CloseFigure();
                }
            }

            editor.Dragging = false;
            editor.isDrawing = false;

            if (editor.CurrentEdit == ImageEditor.EditMode.Cut)
            {
                if (ModeForm != null && !ModeForm.IsDisposed)
                    ModeForm.SetSelectBtnOn();
            }
            else if (editor.CurrentEdit == ImageEditor.EditMode.Mosaic)
            {
                if (MosaicForm != null && !MosaicForm.IsDisposed)
                    MosaicForm.SetSelectBtnOn();
            }

            MainPic.Invalidate();
            return true;
        }

        private void DrawSelectionOverlay(PaintEventArgs e)
        {
            if (MainPic.Image == null)
                return;

            Rectangle rect = ImageToPictureBoxRect(editor.SetRect());

            if (editor.Mode == ImageEditor.CutMode.Square)
            {
                e.Graphics.DrawRectangle(Pens.Red, rect);

                if (!editor.Dragging)
                {
                    using (SolidBrush br = new SolidBrush(Color.FromArgb(120, Color.Red)))
                        e.Graphics.FillRectangle(br, rect);
                }
            }
            else if (editor.Mode == ImageEditor.CutMode.Circle)
            {
                e.Graphics.DrawEllipse(Pens.Blue, rect);

                if (!editor.Dragging)
                {
                    using (SolidBrush br = new SolidBrush(Color.FromArgb(120, Color.Blue)))
                        e.Graphics.FillEllipse(br, rect);
                }
            }
            else if (editor.Mode == ImageEditor.CutMode.Free && editor.FreePNT.Count > 1)
            {
                Point[] pbPoints = editor.FreePNT
                    .Select(p => ImageToPictureBoxPoint(p))
                    .ToArray();

                if (pbPoints.Length > 1)
                    e.Graphics.DrawLines(Pens.Green, pbPoints);

                if (!editor.isDrawing && editor.path.PointCount > 2)
                {
                    using (GraphicsPath pbPath = new GraphicsPath())
                    {
                        pbPath.AddLines(pbPoints);
                        pbPath.CloseFigure();

                        using (SolidBrush br = new SolidBrush(Color.FromArgb(120, Color.Green)))
                            e.Graphics.FillPath(br, pbPath);

                        e.Graphics.DrawPath(Pens.Green, pbPath);
                    }
                }
            }
        }

        // ---------------------------------------------------------------------
        // 주름 제거
        // ---------------------------------------------------------------------

        private bool HandleJuMouseDown(MouseEventArgs e)
        {
            if (!FaceMode.isJuMode || FaceMode.workingImage == null)
                return false;

            FaceMode.isDrawingJu = true;

            // ⭐ 여기서 항상 새 상태 캡처
            FaceMode.juStartState = CaptureState();

            return true;
        }

        private bool HandleJuMouseMove(MouseEventArgs e)
        {
            if (!FaceMode.isJuMode || !FaceMode.isDrawingJu)
                return false;

            ApplyJuEffectContinuous(e.Location);

            return true;
        }

        private bool HandleJuMouseUp(MouseEventArgs e)
        {
            if (!FaceMode.isJuMode || !FaceMode.isDrawingJu)
                return false;

            FaceMode.isDrawingJu = false;

            if (FaceMode.juStartState == null)
                return true;

            var beforeState = FaceMode.juStartState;
            var afterState = CaptureState();

            commandManager.ExecuteCommand(
                new StateSnapshotCommand(this, beforeState, afterState));

            UpdateUndoRedoButtons();

            // ⭐ 중요
            FaceMode.juStartState = null;

            return true;
        }

        // ---------------------------------------------------------------------
        // 브러시 / 지우개
        // ---------------------------------------------------------------------

        private bool HandleDrawMouseDown(MouseEventArgs e)
        {
            if (!(FaceMode.isBrushMode || FaceMode.isEraserMode) || FaceMode.drawLayer == null)
                return false;

            FaceMode.isDrawingJu = true;
            FaceMode.drawStartState?.Dispose();
            FaceMode.drawStartState = CaptureState();

            Point startPoint = ZoomService.TranslateZoomMousePosition(MainPic, FaceMode.workingImage, e.Location);
            if (startPoint == Point.Empty)
                return true;

            FaceMode.lastDrawPoint = startPoint;
            return true;
        }

        private bool HandleDrawMouseMove(MouseEventArgs e)
        {
            if (!(FaceMode.isBrushMode || FaceMode.isEraserMode) ||
                !FaceMode.isDrawingJu ||
                FaceMode.drawLayer == null)
                return false;

            Point currentPoint = ZoomService.TranslateZoomMousePosition(MainPic, FaceMode.workingImage, e.Location);
            if (currentPoint == Point.Empty)
                return true;

            if (FaceMode.isBrushMode)
            {
                DrawService.DrawLine(
                    FaceMode.drawLayer,
                    FaceMode.lastDrawPoint,
                    currentPoint,
                    FaceMode.currentDrawColor,
                    FaceMode.brushSize);
            }
            else
            {
                DrawService.EraseLine(
                    FaceMode.drawLayer,
                    FaceMode.lastDrawPoint,
                    currentPoint,
                    FaceMode.brushSize);
            }

            FaceMode.lastDrawPoint = currentPoint;
            return true;
        }

        private bool HandleDrawMouseUp(MouseEventArgs e)
        {
            if (!(FaceMode.isBrushMode || FaceMode.isEraserMode) ||
         !FaceMode.isDrawingJu ||
         FaceMode.drawLayer == null)
                return false;

            if (FaceMode.drawStartState != null)
            {
                var beforeState = FaceMode.drawStartState;
                var afterState = CaptureState();

                commandManager.ExecuteCommand(new StateSnapshotCommand(this, beforeState, afterState));
                UpdateUndoRedoButtons();

                FaceMode.drawStartState = null;
            }

            FaceMode.isDrawingJu = false;
            return true;
        }

        private void DrawFaceAndDrawLayer(PaintEventArgs e)
        {
            Bitmap? imageToDraw = null;

            // 1. 눈 키우기 미리보기처럼 MainPic.Image에 임시 결과가 들어간 경우 우선 사용
            if (MainPic.Image is Bitmap mainBitmap)
                imageToDraw = mainBitmap;

            // 2. 없으면 FaceMode 쪽 작업 이미지 사용
            if (imageToDraw == null)
                imageToDraw = FaceMode.isShowingOriginal ? FaceMode.originalImage : FaceMode.workingImage;

            if (imageToDraw == null)
                return;

            Rectangle baseRect = ZoomService.GetImageRectangle(MainPic, imageToDraw);
            e.Graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
            e.Graphics.DrawImage(imageToDraw, baseRect);

            // drawLayer는 실제 얼굴 편집 레이어일 때만 덧그림
            // 눈 키우기 미리보기 중에는 보통 preview 이미지 자체에 결과가 포함되므로
            // 필요 없으면 아래 부분은 조건을 더 좁혀도 됨.
            if (!FaceMode.isShowingOriginal && FaceMode.drawLayer != null)
            {
                e.Graphics.DrawImage(FaceMode.drawLayer, baseRect);
            }
        }




        /// <summary>
        /// 마스크 브러시 칠하기를 시작한다.
        /// 클릭한 위치를 이미지 좌표로 바꾼 뒤 즉시 한 번 칠해 첫 스트로크를 만든다.
        /// </summary>
        private void HandleMaskMouseDown(MouseEventArgs e)
        {
            if (mask == null) return;

            isPaintingMask = true;
            Point imagePt = PictureBoxToImage(e.Location);
            PaintMaskAt(imagePt, e.Button == MouseButtons.Right);
        }

        /// <summary>
        /// 마스크 브러시 드래그 중 계속 칠한다.
        /// 사용자가 이동한 경로를 따라 적용 영역을 누적한다.
        /// </summary>
        private void HandleMaskMouseMove(MouseEventArgs e)
        {
            if (!isPaintingMask || mask == null || MainPic.Image == null) return;

            Point imagePt = PictureBoxToImage(e.Location);
            PaintMaskAt(imagePt, e.Button == MouseButtons.Right);
        }

        /// <summary>
        /// 마스크 브러시 입력을 종료한다.
        /// 이후 MouseMove가 와도 더 이상 칠하지 않도록 상태 플래그를 내린다.
        /// </summary>
        private void HandleMaskMouseUp(MouseEventArgs e)
        {
            isPaintingMask = false;
        }

        /// <summary>
        /// 현재 mask 배열을 반투명 빨간색 오버레이로 그린다.
        /// 사용자가 어느 영역에 효과가 들어갈지 직관적으로 확인할 수 있게 한다.
        /// </summary>
        private void DrawMaskOverlay(PaintEventArgs e)
        {
            if (MainPic.Image == null || mask == null) return;

            int w = MainPic.Image.Width;
            int h = MainPic.Image.Height;

            // 마스크가 칠해진 픽셀은 반투명 빨간색으로 덮어 사용자가 적용 범위를 확인할 수 있게 한다.
            using (Brush brush = new SolidBrush(Color.FromArgb(80, Color.Red)))
            {
                for (int y = 0; y < h; y++)
                {
                    for (int x = 0; x < w; x++)
                    {
                        byte m = mask[y * w + x];
                        if (m > 20)
                        {
                            // 원본 이미지의 한 픽셀 위치를 화면 좌표로 바꿔 해당 칸을 채운다.
                            Rectangle r = ImageToPictureBoxRect(x, y);
                            e.Graphics.FillRectangle(brush, r);
                        }
                    }
                }
            }

            if (showBrushCursor && currentTool == EditorTool.MaskBrush)
            {
                int d = brushRadius * 2;
                Rectangle rect = new Rectangle(
                    currentMousePoint.X - brushRadius,
                    currentMousePoint.Y - brushRadius,
                    d,
                    d);

                using (Pen pen = new Pen(Color.Lime, 1))
                {
                    e.Graphics.DrawEllipse(pen, rect);
                }
            }
        }

    }
}
