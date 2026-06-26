using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


// -----------------------------------------------------------------------------
// 파일 역할: 텍스트 입력, 오버레이, 이동, 저장 전 임시 상태 관리를 담당한다.
// 병합 작업 때 빠르게 구조를 파악할 수 있도록 주석을 보강한 버전이다.
// -----------------------------------------------------------------------------

namespace Photo
{
    // 메인 편집 창과 공통 편집 상태를 관리하는 핵심 폼 클래스.
    public partial class Main
    {
        // 저장 전 텍스트 오버레이 한 개의 속성을 보관하는 데이터 클래스.
        public class TextItem
        {
            public string Text { get; set; } = string.Empty;
            public Point Position { get; set; }          // 이미지 좌표
            public Size TextSize { get; set; }           // 이미지 좌표 기준 크기
            public string SelectedFont { get; set; } = "맑은 고딕";
            public float FontSize { get; set; } = 12f;
            public Color TextColor { get; set; } = Color.Black;

            /// <summary>
            /// TextItem을 깊은 복사한다.
            /// Undo/Redo나 임시 상태 보존 시 원본 객체가 함께 바뀌는 것을 막기 위해 사용한다.
            /// </summary>
            public TextItem Clone()
            {
                return new TextItem
                {
                    Text = Text,
                    Position = Position,
                    TextSize = TextSize,
                    SelectedFont = SelectedFont,
                    FontSize = FontSize,
                    TextColor = TextColor
                };
            }
        }
        //텍스트 기능을 위한 멤버들
        private string currentFontFamily = "맑은 고딕";
        // 현재 선택/진행 중인 상태값을 저장한다.
        private float currentFontSize = 12f;
        // 현재 선택/진행 중인 상태값을 저장한다.
        private Color currentTextColor = Color.Black;
        private TextItem selectedText = null;
        // 관련 요소들을 순서대로 보관하는 컬렉션이다.
        private List<TextItem> textList = new List<TextItem>();
        // 현재 선택/진행 중인 상태값을 저장한다.
        private Point currentClickLocation = Point.Empty;
        // 현재 동작 상태를 나타내는 플래그이다.
        private bool isTextMode = false;
        // 현재 동작 상태를 나타내는 플래그이다.
        private bool isDrawingNewText = false;
        // 현재 동작 상태를 나타내는 플래그이다.
        private bool isResizingText = false;
        // 연산 또는 그리기에 사용할 사각형 범위를 저장한다.
        private Rectangle dragRect;
        private Point dragStartPos = Point.Empty;
        private Point originalDraggedTextPosition = Point.Empty;

        public IReadOnlyList<TextItem> PendingTexts => textList;


        /// <summary>
        /// 저장 전 텍스트 오버레이 상태를 모두 초기화한다.
        /// 새 이미지를 열거나 작업을 취소할 때 임시 텍스트, 선택 상태, 입력창을 정리한다.
        /// </summary>
        private void ClearTextOverlayState()
        {
            selectedText = null;
            isDraggingText = false;
            isDrawingNewText = false;
            isResizingText = false;
            dragRect = Rectangle.Empty;

            if (tempInputBox != null)
            {
                tempInputBox.Visible = false;
                tempInputBox.Text = "";
                tempInputBox.Tag = null;
            }
            textList.Clear();

            if (pnlTextRemote != null)
                pnlTextRemote.Visible = false;

            MainPic.Invalidate();
        }

        /// <summary>
        /// 텍스트 도구에서 사용할 입력창과 서식 UI를 초기화한다.
        /// 폰트, 크기, 색상 컨트롤의 변경 이벤트도 여기서 연결한다.
        /// </summary>
        public void InitTextEntry() 
        {
            // 실제 이미지를 바로 수정하지 않고 임시 입력을 받기 위한 TextBox를 만든다.
            tempInputBox = new TextBox { Visible = false, BorderStyle = BorderStyle.FixedSingle };
            tempInputBox.KeyDown += TempInputBox_KeyDown;
            this.Controls.Add(tempInputBox);
            tempInputBox.BringToFront();

            if (pnlTextRemote != null)
            {
                pnlTextRemote.Visible = false;
                pnlTextRemote.BringToFront();
            }

            if (cbFont != null)
            {
                cbFont.DropDownStyle = ComboBoxStyle.DropDownList;
                cbFont.Items.Clear();
                cbFont.Items.AddRange(new object[] { "맑은 고딕", "궁서", "돋움", "Arial", "Verdana", "Times New Roman" });
                cbFont.SelectedIndex = 0;
                // 폰트가 바뀌면 현재 선택된 텍스트나 앞으로 입력할 텍스트의 서식을 즉시 갱신한다.
                cbFont.SelectedIndexChanged += (s, e) => {
                    if (cbFont.SelectedItem != null)
                    {
                        currentFontFamily = cbFont.SelectedItem.ToString();
                        UpdateSelectedTextFormat();
                    }
                };
            }



            if (numFontSize != null)
            {
                numFontSize.Value = 12;
                // 글자 크기 변경도 같은 방식으로 현재 선택 텍스트에 반영한다.
                numFontSize.ValueChanged += (s, e) => {
                    currentFontSize = (float)numFontSize.Value;
                    UpdateSelectedTextFormat();
                };
            }

            if (btnTextColor != null)
            {
                // 색상 선택 버튼은 ColorDialog를 띄워 텍스트 색을 바꾼다.
                btnTextColor.Click += (s, e) => {
                    if (colorDialog1.ShowDialog() == DialogResult.OK)
                    {
                        currentTextColor = colorDialog1.Color;
                        btnTextColor.BackColor = currentTextColor;
                        if (selectedText != null) { selectedText.TextColor = currentTextColor; MainPic.Invalidate(); }
                    }
                };
            }

            

            if (ToolStrip_text != null) ToolStrip_text.Click += ToolStrip_text_Click;
        }


       

        

        /// <summary>
        /// 드래그 시작점과 현재 점으로부터 항상 정상적인 사각형을 만든다.
        /// 드래그 방향이 어느 쪽이든 너비/높이가 음수가 되지 않도록 보정한다.
        /// </summary>
        private Rectangle NormalizeRect(Point p1, Point p2)
        {//드래그 사각형 정규화 함수
            int x = Math.Min(p1.X, p2.X);
            int y = Math.Min(p1.Y, p2.Y);
            int w = Math.Abs(p1.X - p2.X);
            int h = Math.Abs(p1.Y - p2.Y);

            return new Rectangle(x, y, w, h);
        }


        /// <summary>
        /// 텍스트 도구 모드를 토글한다.
        /// 버튼 한 번으로 마스크 모드와 텍스트 모드 사이를 전환할 수 있게 한다.
        /// </summary>
        private void Btntext_Click(object? sender, EventArgs e)
        {
            SetCurrentTool(EditorTool.Text);
            selectedText = null;
            MainPic.Invalidate();
        }

        /// <summary>
        /// 현재 마우스 입력을 어떤 편집 기능으로 해석할지 변경한다.
        /// 도구를 바꿀 때 이전 도구의 드래그 상태도 함께 정리한다.
        /// </summary>
        private void SetCurrentTool(EditorTool tool)
        {
            currentTool = tool;
            isTextMode = (tool == EditorTool.Text);

            // 공통 상태 정리
            isPaintingMask = false;
            isDraggingText = false;
            isDrawingNewText = false;
            isResizingText = false;
            dragRect = Rectangle.Empty;
            showBrushCursor = false;

            if (tool != EditorTool.Text)
            {
                if (tempInputBox != null)
                {
                    tempInputBox.Visible = false;
                    tempInputBox.Text = string.Empty;
                    tempInputBox.Tag = null;
                }

                selectedText = null;
            }

            if (pnlTextRemote != null)
                pnlTextRemote.Visible = (tool == EditorTool.Text);

            MainPic.Cursor = tool switch
            {
                EditorTool.MaskBrush => Cursors.Cross,
                EditorTool.Text => Cursors.Cross,
                _ => Cursors.Default
            };

            UpdateToolModeButton();
            MainPic.Invalidate();
        }

        /// <summary>
        /// 현재 선택된 도구를 버튼 텍스트에 표시한다.
        /// 팀원이 디버깅할 때 지금 어떤 모드인지 즉시 알 수 있게 해 준다.
        /// </summary>
        private void UpdateToolModeButton()
        {
            if (btntoolMode == null) return;

            btntoolMode.Text = currentTool switch
            {
                EditorTool.MaskBrush => "모드 선택 (현재: 마스크)",
                EditorTool.Text => "모드 선택 (현재: 텍스트)",
                _ => "모드 선택"
            };
        }

        /// <summary>
        /// 텍스트 도구의 MouseDown 처리.
        /// 기존 텍스트를 클릭하면 선택/이동을 시작하고,
        /// 빈 공간을 드래그하면 새 텍스트 입력 영역을 만든다.
        /// </summary>
        private void HandleTextMouseDown(MouseEventArgs e)
        {
            if (e.Button != MouseButtons.Left || MainPic.Image == null) return;

            Point imagePoint = PictureBoxToImage(e.Location);
            // 클릭 위치에 이미 존재하는 텍스트가 있는지 검사해
            // "기존 텍스트 선택/이동"인지 "새 텍스트 생성"인지 판단한다.
            TextItem? hitText = FindTopMostTextAt(imagePoint);

            if (hitText != null)
            {
                selectedText = hitText;
                currentFontFamily = hitText.SelectedFont;
                currentFontSize = hitText.FontSize;
                currentTextColor = hitText.TextColor;

                if (cbFont != null) cbFont.SelectedItem = currentFontFamily;

                if (numFontSize != null)
                {
                    decimal safeSize = Math.Max(numFontSize.Minimum, Math.Min(numFontSize.Maximum, (decimal)currentFontSize));
                    numFontSize.Value = safeSize;
                }

                if (btnTextColor != null) btnTextColor.BackColor = currentTextColor;

                // 드래그 시작 시 원래 위치를 기억해 두어 이후 Undo/Redo에서 사용할 수 있게 한다.
                isDraggingText = true;
                originalDraggedTextPosition = hitText.Position;
                textDragOffset = new Point(imagePoint.X - hitText.Position.X, imagePoint.Y - hitText.Position.Y);

                dragRect = Rectangle.Empty;
                isDrawingNewText = false;
                MainPic.Invalidate();
                return;
            }

            selectedText = null;
            dragStartPos = e.Location;
            dragRect = new Rectangle(e.Location, Size.Empty);
            isDrawingNewText = true;
            MainPic.Invalidate();
        }

        /// <summary>
        /// 텍스트 이동 또는 새 입력 영역 드래그를 계속 진행한다.
        /// 아직 실제 비트맵을 수정하지 않고 오버레이 상태만 갱신한다.
        /// </summary>
        private void HandleTextMouseMove(MouseEventArgs e)
        {
            if (MainPic.Image == null) return;

            if (isDraggingText && selectedText != null)
            {
                Point imagePoint = PictureBoxToImage(e.Location);
                // 현재 마우스 위치에서 처음 눌렀을 때의 오프셋을 빼
                // 텍스트가 마우스 아래에서 자연스럽게 따라오도록 한다.
                selectedText.Position = new Point(
                    Math.Max(0, imagePoint.X - textDragOffset.X),
                    Math.Max(0, imagePoint.Y - textDragOffset.Y));

                MainPic.Invalidate();
                return;
            }

            if (isDrawingNewText)
            {
                dragRect = NormalizeRect(dragStartPos, e.Location);
                MainPic.Invalidate();
            }
        }

        /// <summary>
        /// 텍스트 도구의 MouseUp 처리.
        /// 이동이 끝났다면 MoveTextCommand를 기록하고,
        /// 새 영역 드래그가 끝났다면 입력 TextBox를 표시한다.
        /// </summary>
        private void HandleTextMouseUp(MouseEventArgs e)
        {
            if (e.Button != MouseButtons.Left || MainPic.Image == null) return;

            if (isDraggingText && selectedText != null)
            {
                isDraggingText = false;

                Point newPos = selectedText.Position;
                if (newPos != originalDraggedTextPosition)
                {
                    commandManager.ExecuteCommand(
                        new MoveTextCommand(this, selectedText, originalDraggedTextPosition, newPos));
                    UpdateUndoRedoButtons();
                }

                MainPic.Invalidate();
                return;
            }

            if (isDrawingNewText)
            {
                isDrawingNewText = false;

                if (dragRect.Width >= 8 && dragRect.Height >= 8)
                {
                    currentClickLocation = PictureBoxToImage(dragRect.Location);

                    Point screenPoint = MainPic.PointToScreen(dragRect.Location);
                    Point formPoint = this.PointToClient(screenPoint);

                    tempInputBox.Text = "";
                    tempInputBox.Font = new Font(currentFontFamily, currentFontSize, FontStyle.Bold);
                    tempInputBox.ForeColor = currentTextColor;
                    tempInputBox.Location = formPoint;
                    tempInputBox.Size = new Size(
                        Math.Max(60, dragRect.Width),
                        Math.Max(28, dragRect.Height)
                    );
                    tempInputBox.Visible = true;
                    tempInputBox.BringToFront();
                    tempInputBox.Focus();
                }

                MainPic.Invalidate();
            }
        }

        // 오버레이나 시각적 요소를 화면에 그린다.
        private void DrawTextOverlay(PaintEventArgs e)
        {
            if (MainPic.Image == null) return;

            float scaleX = (float)MainPic.ClientSize.Width / MainPic.Image.Width;
            float scaleY = (float)MainPic.ClientSize.Height / MainPic.Image.Height;
            float fontScale = Math.Min(scaleX, scaleY);

            foreach (TextItem item in textList)
            {
                Point pbPoint = new Point((int)(item.Position.X * scaleX), (int)(item.Position.Y * scaleY));
                float drawFontSize = Math.Max(1f, item.FontSize * fontScale);

                using Font font = new Font(item.SelectedFont, drawFontSize, FontStyle.Bold);
                using Brush brush = new SolidBrush(item.TextColor);

                e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                e.Graphics.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAlias;
                e.Graphics.DrawString(item.Text, font, brush, pbPoint);

                if (selectedText == item)
                {
                    Rectangle pbBounds = GetTextBoundsInPictureBox(item);
                    using Pen selectPen = new Pen(Color.DeepSkyBlue, 1)
                    {
                        DashStyle = System.Drawing.Drawing2D.DashStyle.Dash
                    };
                    e.Graphics.DrawRectangle(selectPen, pbBounds);
                }
            }

            if (isDrawingNewText && !dragRect.IsEmpty)
            {
                using Pen pen = new Pen(Color.DeepSkyBlue, 1)
                {
                    DashStyle = System.Drawing.Drawing2D.DashStyle.Dash
                };
                e.Graphics.DrawRectangle(pen, dragRect);
            }
        }

        // 이 파일의 핵심 동작을 수행하는 메서드.
        private Size ImageSizeToPictureBoxSize(Size imgSize)
        {
            if (MainPic.Image == null) return Size.Empty;

            int imgW = MainPic.Image.Width;
            int imgH = MainPic.Image.Height;
            int boxW = MainPic.ClientSize.Width;
            int boxH = MainPic.ClientSize.Height;

            float imageAspect = (float)imgW / imgH;
            float boxAspect = (float)boxW / boxH;

            int drawW, drawH;

            if (imageAspect > boxAspect)
            {
                drawW = boxW;
                drawH = (int)(boxW / imageAspect);
            }
            else
            {
                drawH = boxH;
                drawW = (int)(boxH * imageAspect);
            }

            float scaleX = (float)drawW / imgW;
            float scaleY = (float)drawH / imgH;

            return new Size(
                (int)(imgSize.Width * scaleX),
                (int)(imgSize.Height * scaleY)
            );
        }


        // 현재 상태를 반영해 UI 또는 내부 값을 갱신한다.
        private void UpdateSelectedTextFormat()
        {
            if (selectedText != null)
            {
                selectedText.SelectedFont = currentFontFamily;
                selectedText.FontSize = currentFontSize;
                RecalculateTextSize(selectedText);
                MainPic.Invalidate();
            }
        }

        

        // 이 파일의 핵심 동작을 수행하는 메서드.
        private void TempInputBox_KeyDown(object? sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                e.SuppressKeyPress = true;

                string text = tempInputBox.Text.Trim();
                if (!string.IsNullOrWhiteSpace(text))
                {
                    TextItem item = new TextItem
                    {
                        Text = text,
                        Position = currentClickLocation,
                        SelectedFont = currentFontFamily,
                        FontSize = currentFontSize,
                        TextColor = currentTextColor
                    };

                    RecalculateTextSize(item);

                    commandManager.ExecuteCommand(new TextCommand(this, item));
                    UpdateUndoRedoButtons();

                    if (textList.Count > 0)
                        selectedText = textList[^1];
                }

                CloseInput();
            }
            else if (e.KeyCode == Keys.Escape)
            {
                e.SuppressKeyPress = true;
                CloseInput();
            }
        }

        
           // 이 파일의 핵심 동작을 수행하는 메서드.
           private void CloseInput()
        {
            tempInputBox.Visible = false;
            tempInputBox.Text = string.Empty;
            tempInputBox.Tag = null;
            dragRect = Rectangle.Empty;
            MainPic.Invalidate();
        }
        // 이 파일의 핵심 동작을 수행하는 메서드.
        private void RecalculateTextSize(TextItem item)
        {
            using Bitmap measureBmp = new Bitmap(1, 1);
            using Graphics g = Graphics.FromImage(measureBmp);
            using Font f = new Font(item.SelectedFont, item.FontSize, FontStyle.Bold);

            SizeF measured = g.MeasureString(item.Text, f);
            item.TextSize = new Size(
                (int)Math.Ceiling(measured.Width),
                (int)Math.Ceiling(measured.Height));
        }

        // 계산 결과나 내부 값을 반환한다.
        private Rectangle GetTextBoundsInImage(TextItem item)
        {
            return new Rectangle(item.Position, item.TextSize);
        }

        // 계산 결과나 내부 값을 반환한다.
        private Rectangle GetTextBoundsInPictureBox(TextItem item)
        {
            if (MainPic.Image == null) return Rectangle.Empty;

            float scaleX = (float)MainPic.ClientSize.Width / MainPic.Image.Width;
            float scaleY = (float)MainPic.ClientSize.Height / MainPic.Image.Height;

            return new Rectangle(
                (int)(item.Position.X * scaleX),
                (int)(item.Position.Y * scaleY),
                Math.Max(1, (int)Math.Ceiling(item.TextSize.Width * scaleX)),
                Math.Max(1, (int)Math.Ceiling(item.TextSize.Height * scaleY)));
        }

        // 이 파일의 핵심 동작을 수행하는 메서드.
        private TextItem? FindTopMostTextAt(Point imagePoint)
        {
            for (int i = textList.Count - 1; i >= 0; i--)
            {
                if (GetTextBoundsInImage(textList[i]).Contains(imagePoint))
                    return textList[i];
            }
            return null;
        }

        // 목록이나 상태에 새 요소를 추가한다.
        internal void AddPendingText(TextItem item)
        {
            textList.Add(item);
            selectedText = item;
            MainPic.Invalidate();
        }

        // 목록이나 상태에서 요소를 제거한다.
        internal void RemovePendingText(TextItem item)
        {
            if (selectedText == item)
                selectedText = null;

            textList.Remove(item);
            MainPic.Invalidate();
        }

        // 선택된 요소의 위치를 변경한다.
        internal void MovePendingText(TextItem item, Point newPosition)
        {
            item.Position = newPosition;
            MainPic.Invalidate();
        }
    }
    
}
