using DlibDotNet;
using Photo.Commands;
using Photo.Models;
using Photo.Service;
using Photo.Services;
using System.Drawing.Design;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Net;
using System.Reflection.Emit;
using System.Runtime.InteropServices;
using System.Security.AccessControl;
using System.Security.Cryptography.X509Certificates;
using System.Windows.Forms;
using static Photo.Main;
using static System.Windows.Forms.VisualStyles.VisualStyleElement;

//메인 코드
namespace Photo
{
    public partial class Main : Form
    {
        class CustomMenuRenderer : ToolStripProfessionalRenderer
        {
            protected override void OnRenderMenuItemBackground(ToolStripItemRenderEventArgs e)
            {
                System.Drawing.Rectangle rect = new System.Drawing.Rectangle(System.Drawing.Point.Empty, e.Item.Size);
                if (Main._isLightMode)
                {
                    if (e.Item.Selected)
                    {
                        e.Graphics.FillRectangle(
                            new SolidBrush(Color.FromArgb(235, 235, 235)), rect);
                    }
                    else
                    {
                        e.Graphics.FillRectangle(
                            new SolidBrush(Color.FromArgb(245, 245, 245)), rect);
                    }
                }
                else
                {
                    if (e.Item.Selected)
                    {
                        e.Graphics.FillRectangle(
                            new SolidBrush(Color.FromArgb(37, 37, 38)), rect);
                    }
                    else
                    {
                        e.Graphics.FillRectangle(
                            new SolidBrush(Color.FromArgb(47, 47, 48)), rect);
                    }
                }

            }
            protected override void OnRenderItemText(ToolStripItemTextRenderEventArgs e)
            {
                Font font;

                if (e.Item.Selected)
                {
                    font = new Font(e.TextFont, FontStyle.Bold); // Hover시 Bold
                }
                else
                {
                    font = new Font(e.TextFont, FontStyle.Regular); // 기본
                }

                e.TextFont = font;

                base.OnRenderItemText(e);
            }
        }
        // 마우스 입력을 어떤 편집 도구로 해석할지 구분하는 열거형.
        private enum EditorTool  //브러시의 모드 선택하는 enum
        {
            None,
            MaskBrush,
            Text
        }
        private FaceEdit FaceMode = new FaceEdit();

        private readonly CommandManager commandManager = new CommandManager();          //명령 관리
        //라이트모드/다크모드
        static bool _isLightMode = true;
        string LightDarkPath = Application.StartupPath;

        // 스티커 관련 변수
        List<StickerObject> stickersList = new List<StickerObject>();  //스티커 리스트
        private StickerObject _targetSticker;  // 선택한 스티커
        private RectangleF _originalBounds;    // 선택한 스티커의 원래 위치
        private PointF _lastMousePos;          // 마우스로 클릭한 위치
        private bool _isResizing = false;      // 현재 크기 조절 중인가?
        private StickerObject _clipboardSticker = null;     //복사된 스티커 임시 저장
        private float _startOpacity;           // 조작 시작 시점의 투명도 값 보관
        Sticker_property _stickerForm;         // 스티커 선택폼

        // 콜라주 관련 변수
        private List<CollageFrame> collageFrames = new List<CollageFrame>();
        private bool _isChoosingImage = false; // 이미지 선택 중인가?

        //자르기/ 모자이크 편집 클래스
        private ImageEditor editor;
        private CutMain ModeForm; // 자르기 편집 폼
        private ChangeMosaic MosaicForm; // 모자이크 편집 폼

        //적용 영역을 만들기 위한 마스크 필드 
        private bool isPaintingMask = false;
        // 브러시로 칠한 적용 영역을 0~255 강도로 저장하는 마스크 배열.
        private byte[] mask = null;
        // 마스크 브러시의 반경을 저장한다.
        private int brushRadius = 20;
        // 브러시 중심부의 최대 마스크 강도를 저장한다.
        private int maskMaxValue = 255;          // 중심 최대 마스크 세기
        // 브러시 가장자리 감쇠 곡선을 제어한다.
        private double maskFalloffPower = 1.3;   // 가장자리 감쇠 정도
        //칠할 브러시 위치를 표시 
        private System.Drawing.Point currentMousePoint = System.Drawing.Point.Empty;
        // 브러시 원형 커서를 화면에 표시할지 결정한다.
        private bool showBrushCursor = false;
        private Image originalImage = null;
        // 현재 선택된 프레임 이름을 보관한다.
        private string currentFrameName = "";

        // 텍스트 입력용 임시 TextBox 컨트롤을 가리킨다.
        private System.Windows.Forms.TextBox tempInputBox;

        // 현재 선택된 편집 도구를 저장한다.
        private EditorTool currentTool = EditorTool.None;

        // 텍스트 드래그/선택 상태
        private bool isDraggingText = false;
        // 텍스트 드래그 시 기준점 오프셋을 저장한다.
        private System.Drawing.Point textDragOffset = System.Drawing.Point.Empty;

        private void SwitchEditorTool(EditorTool tool)
        {//도구 전환 래퍼 함수
            DeactivateAllInteractiveModes();
            SetCurrentTool(tool);
        }


        // 삭제 로직
        private void DeleteSelectedSticker()
        {
            if (_targetSticker != null)
            {
                var deleteCmd = new DeleteStickerCommand(
                    stickersList,
                    _targetSticker,
                    () => MainPic.Invalidate());

                commandManager.ExecuteCommand(deleteCmd);

                _targetSticker = null;
                UpdateUndoRedoButtons();
                MainPic.Invalidate();
            }
        }
        // 복사 로직
        private void CopySelectedSticker()
        {
            if (_targetSticker != null)
            {
                // 이미지와 정보를 복제하여 보관
                _clipboardSticker = new StickerObject(
                    new Bitmap(_targetSticker.StickerImage),
                    _targetSticker.Bounds.X + 20,  // 붙여넣을 때 겹치지 않게 살짝 옆으로
                    _targetSticker.Bounds.Y + 20,
                    _targetSticker.Bounds.Width,
                    _targetSticker.Bounds.Height,
                    _targetSticker.Opacity
                    );
            }
        }
        // 붙여넣기 로직
        private void PasteSticker()
        {
            if (_clipboardSticker != null)
            {
                // 클립보드에 있는 정보를 바탕으로 새 객체 생성
                StickerObject newSticker = new StickerObject(
                    new Bitmap(_clipboardSticker.StickerImage),
                    _clipboardSticker.Bounds.X,
                    _clipboardSticker.Bounds.Y,
                    _clipboardSticker.Bounds.Width,
                    _clipboardSticker.Bounds.Height,
                    _clipboardSticker.Opacity
                    );
                newSticker.IsSelected = false;

                var addCmd = new AddStickerCommand(stickersList, newSticker, () => MainPic.Invalidate());
                commandManager.ExecuteCommand(addCmd);


                //다음 붙여넣기를 위해 클립보드 위치 이동
                _clipboardSticker.Bounds = new RectangleF(
                    _clipboardSticker.Bounds.X + 20,
                    _clipboardSticker.Bounds.Y + 20,
                    _clipboardSticker.Bounds.Width,
                    _clipboardSticker.Bounds.Height
                    );

                isPaintingMask = false;
                showBrushCursor = false;
                UpdateUndoRedoButtons();
                MainPic.Invalidate();

            }
        }

        // 스티커창 위치를 업데이트하는 메서드
        private void UpdateStickerFormLocation()
        {
            if (_stickerForm != null && !_stickerForm.IsDisposed && _stickerForm.Visible)
            {
                // 메인창 오른쪽에 붙임
                _stickerForm.Location = new System.Drawing.Point(this.Right - 248, this.Top + 30);
            }
        }
        private void RefreshMainPic() // 편집 후 화면 업데이트
        {
            SyncEditorToFace();
        }
        public void Undo()
        {
            if (!commandManager.CanUndo)
            {
                UpdateUndoRedoButtons();
                return;
            }

            try
            {
                commandManager.Undo();
            }
            catch
            {
                // 현재는 크래시 방지가 우선
            }
            UpdateUndoRedoButtons();
            MainPic.Invalidate();
        }//실행 취소
        public void Redo()
        {
            if (!commandManager.CanRedo)
            {
                UpdateUndoRedoButtons();
                return;
            }

            try
            {
                commandManager.Redo();
            }
            catch
            {
                // 현재는 크래시 방지가 우선
            }

            UpdateUndoRedoButtons();
            MainPic.Invalidate();
        }//다시 실행
        public void ResetToOriginal()
        {
            if (MainPic.Image == null) return;
            commandManager.ExecuteCommand(new ResetEditorImageCommand(this));
            UpdateUndoRedoButtons();
        }//초기화(되돌리기 가능)

        class ReadAsBmp : IDisposable  //사진 데이터를 받기 위한 클래스(시작)
        {
            // 파일 경로나 리소스 위치를 저장한다.
            public static string photo_filePath;
            // 비트맵 잠금 처리에 사용하는 내부 상태를 저장한다.
            public Bitmap photo_bmp;
            // 연산 또는 그리기에 사용할 사각형 범위를 저장한다.
            public System.Drawing.Rectangle rect;
            // 비트맵 잠금 처리에 사용하는 내부 상태를 저장한다.
            public BitmapData photo_bmpdata;

            // 이 파일의 핵심 동작을 수행하는 메서드.
            public void ImageProcessing(Bitmap source)
            {
                photo_bmp = source.Clone(
                new System.Drawing.Rectangle(0, 0, source.Width, source.Height),
                PixelFormat.Format32bppArgb);


                rect = new System.Drawing.Rectangle(0, 0, photo_bmp.Width, photo_bmp.Height);
                photo_bmpdata = photo_bmp.LockBits(rect, ImageLockMode.ReadWrite, PixelFormat.Format32bppArgb);


            }

            // 이 파일의 핵심 동작을 수행하는 메서드.
            public void ProcessingEnd()
            {
                if (photo_bmpdata != null)
                {
                    photo_bmp.UnlockBits(photo_bmpdata);
                    photo_bmpdata = null;
                }
            }

            // 사용한 리소스를 해제한다.
            public void Dispose()
            {
                ProcessingEnd();
            }
        }//사진 데이터를 받기 위한 클래스(끝)

        //라이트 모드
        public void TurntoLightMode()
        {
            lb_screenMode.Image = Image.FromFile(LightDarkPath + "Icon_light.png");

            lb_screenMode.BackColor = Color.FromArgb(235, 235, 235);
            MainPic.BackColor = Color.FromArgb(235, 235, 235);
            menuStrip1.BackColor = Color.FromArgb(235, 235, 235);
            panel1.BackColor = Color.FromArgb(235, 235, 235);
            trbOpacity.BackColor = Color.FromArgb(235, 235, 235);

            파일ToolStripMenuItem.BackColor = Color.WhiteSmoke;
            ToolStrip_image.BackColor = Color.WhiteSmoke;
            필터ToolStripMenuItem.BackColor = Color.WhiteSmoke;
            추가ToolStripMenuItem.BackColor = Color.WhiteSmoke;
            그리기ToolStripMenuItem.BackColor = Color.WhiteSmoke;
            민지ToolStripMenuItem.BackColor = Color.WhiteSmoke;
            파일ToolStripMenuItem.ForeColor = Color.FromArgb(37, 37, 38);
            ToolStrip_image.ForeColor = Color.FromArgb(37, 37, 38);
            필터ToolStripMenuItem.ForeColor = Color.FromArgb(37, 37, 38);
            추가ToolStripMenuItem.ForeColor = Color.FromArgb(37, 37, 38);
            그리기ToolStripMenuItem.ForeColor = Color.FromArgb(37, 37, 38);
            민지ToolStripMenuItem.ForeColor = Color.FromArgb(37, 37, 38);

            label1.ForeColor = Color.FromArgb(37, 37, 37);
            label3.ForeColor = Color.FromArgb(37, 37, 37);

            btn_Undo.BackColor = SystemColors.ScrollBar;
            btn_redo.BackColor = SystemColors.ScrollBar;
            btn_collageModeEnd.BackColor = SystemColors.ScrollBar;
            btntoolMode.BackColor = SystemColors.ScrollBar;
            btnApplyEyes.BackColor = SystemColors.ScrollBar;
            btn_Undo.ForeColor = Color.FromArgb(37, 37, 38);
            btn_redo.ForeColor = Color.FromArgb(37, 37, 38);
            btn_collageModeEnd.ForeColor = Color.FromArgb(37, 37, 38);
            btntoolMode.ForeColor = Color.FromArgb(37, 37, 38);
            btnApplyEyes.ForeColor = Color.FromArgb(37, 37, 38);

            btnSave.ForeColor = Color.FromArgb(37, 37, 38);
            btnPost.ForeColor = Color.FromArgb(37, 37, 38);
            toolStrip_collage.ForeColor = Color.FromArgb(37, 37, 38);
            toolStrip_collage1.ForeColor = Color.FromArgb(37, 37, 38);
            toolStrip_collage2.ForeColor = Color.FromArgb(37, 37, 38);
            toolStrip_collage3.ForeColor = Color.FromArgb(37, 37, 38);
            toolStrip_collage4.ForeColor = Color.FromArgb(37, 37, 38);
            Cut.ForeColor = Color.FromArgb(37, 37, 38);
            BrightnessContrast.ForeColor = Color.FromArgb(37, 37, 38);
            필터변경ToolStripMenuItem.ForeColor = Color.FromArgb(37, 37, 38);
            흐림ToolStripMenuItem.ForeColor = Color.FromArgb(37, 37, 38);
            왜곡ToolStripMenuItem.ForeColor = Color.FromArgb(37, 37, 38);
            픽셀화ToolStripMenuItem.ForeColor = Color.FromArgb(37, 37, 38);
            스타일화ToolStripMenuItem.ForeColor = Color.FromArgb(37, 37, 38);
            예술효과ToolStripMenuItem.ForeColor = Color.FromArgb(37, 37, 38);
            선명효과ToolStripMenuItem.ForeColor = Color.FromArgb(37, 37, 38);
            렌더링ToolStripMenuItem.ForeColor = Color.FromArgb(37, 37, 38);
            GrayToolStripMenuItem.ForeColor = Color.FromArgb(37, 37, 38);
            Mosaic.ForeColor = Color.FromArgb(37, 37, 38);
            ToolStrip_sticker.ForeColor = Color.FromArgb(37, 37, 38);
            ToolStrip_frame.ForeColor = Color.FromArgb(37, 37, 38);
            ToolStrip_text.ForeColor = Color.FromArgb(37, 37, 38);
            펜ToolStripMenuItem.ForeColor = Color.FromArgb(37, 37, 38);
            지우개ToolStripMenuItem.ForeColor = Color.FromArgb(37, 37, 38);
            Jap.ForeColor = Color.FromArgb(37, 37, 38);
            Ju.ForeColor = Color.FromArgb(37, 37, 38);
            ToolStrip_eyes.ForeColor = Color.FromArgb(37, 37, 38);

            cbFont.BackColor = Color.WhiteSmoke;
            cbFont.ForeColor = Color.FromArgb(37, 37, 38);
            numFontSize.BackColor = Color.WhiteSmoke;
            numFontSize.ForeColor = Color.FromArgb(37, 37, 38);
            btnTextColor.BackColor = Color.WhiteSmoke;
            btnTextColor.ForeColor = Color.FromArgb(37, 37, 38);
            foreach (Control c in this.Controls)
            {
                if (c is System.Windows.Forms.TrackBar tb)
                {
                    tb.BackColor = Color.WhiteSmoke;
                    tb.ForeColor = Color.FromArgb(37, 37, 38);
                }
            }
            splitContainer1.Panel1.BackColor = Color.FromArgb(235, 235, 235);
            splitContainer1.Panel2.BackColor = Color.FromArgb(235, 235, 235);
            splitContainer1.BackColor = Color.FromArgb(235, 235, 235);

            cmsToolMode.BackColor = Color.WhiteSmoke;
            cmsToolMode.ForeColor = Color.FromArgb(37, 37, 38);
        }
        //다크 모드
        public void TurntoDarkMode()
        {
            lb_screenMode.Image = Image.FromFile(LightDarkPath + "Icon_dark.png");

            lb_screenMode.BackColor = Color.FromArgb(37, 37, 38);
            MainPic.BackColor = Color.FromArgb(37, 37, 38);
            menuStrip1.BackColor = Color.FromArgb(37, 37, 38);
            panel1.BackColor = Color.FromArgb(37, 37, 38);
            trbOpacity.BackColor = Color.FromArgb(37, 37, 38);

            파일ToolStripMenuItem.BackColor = Color.FromArgb(63, 63, 70);
            ToolStrip_image.BackColor = Color.FromArgb(63, 63, 70);
            필터ToolStripMenuItem.BackColor = Color.FromArgb(63, 63, 70);
            추가ToolStripMenuItem.BackColor = Color.FromArgb(63, 63, 70);
            그리기ToolStripMenuItem.BackColor = Color.FromArgb(63, 63, 70);
            민지ToolStripMenuItem.BackColor = Color.FromArgb(63, 63, 70);
            파일ToolStripMenuItem.ForeColor = Color.FromArgb(230, 230, 230);
            ToolStrip_image.ForeColor = Color.FromArgb(230, 230, 230);
            필터ToolStripMenuItem.ForeColor = Color.FromArgb(230, 230, 230);
            추가ToolStripMenuItem.ForeColor = Color.FromArgb(230, 230, 230);
            그리기ToolStripMenuItem.ForeColor = Color.FromArgb(230, 230, 230);
            민지ToolStripMenuItem.ForeColor = Color.FromArgb(230, 230, 230);

            label1.ForeColor = Color.FromArgb(224, 224, 224);
            label3.ForeColor = Color.FromArgb(224, 224, 224);

            btn_Undo.BackColor = Color.FromArgb(63, 63, 70);
            btn_redo.BackColor = Color.FromArgb(63, 63, 70);
            btn_collageModeEnd.BackColor = Color.FromArgb(63, 63, 70);
            btntoolMode.BackColor = Color.FromArgb(63, 63, 70);
            btnApplyEyes.BackColor = Color.FromArgb(63, 63, 70);
            btn_Undo.ForeColor = SystemColors.ControlLight;
            btn_redo.ForeColor = SystemColors.ControlLight;
            btn_collageModeEnd.ForeColor = SystemColors.ControlLight;
            btntoolMode.ForeColor = SystemColors.ControlLight;

            btnApplyEyes.ForeColor = SystemColors.ControlLight;


            btnSave.ForeColor = SystemColors.ControlLightLight;
            btnPost.ForeColor = SystemColors.ControlLightLight;
            toolStrip_collage.ForeColor = SystemColors.ControlLightLight;
            toolStrip_collage1.ForeColor = SystemColors.ControlLightLight;
            toolStrip_collage2.ForeColor = SystemColors.ControlLightLight;
            toolStrip_collage3.ForeColor = SystemColors.ControlLightLight;
            toolStrip_collage4.ForeColor = SystemColors.ControlLightLight;
            Cut.ForeColor = SystemColors.ControlLightLight;
            BrightnessContrast.ForeColor = SystemColors.ControlLightLight;
            필터변경ToolStripMenuItem.ForeColor = SystemColors.ControlLightLight;
            흐림ToolStripMenuItem.ForeColor = SystemColors.ControlLightLight;
            왜곡ToolStripMenuItem.ForeColor = SystemColors.ControlLightLight;
            픽셀화ToolStripMenuItem.ForeColor = SystemColors.ControlLightLight;
            스타일화ToolStripMenuItem.ForeColor = SystemColors.ControlLightLight;
            예술효과ToolStripMenuItem.ForeColor = SystemColors.ControlLightLight;
            선명효과ToolStripMenuItem.ForeColor = SystemColors.ControlLightLight;
            렌더링ToolStripMenuItem.ForeColor = SystemColors.ControlLightLight;
            GrayToolStripMenuItem.ForeColor = SystemColors.ControlLightLight;
            Mosaic.ForeColor = SystemColors.ControlLightLight;
            ToolStrip_sticker.ForeColor = SystemColors.ControlLightLight;
            ToolStrip_frame.ForeColor = SystemColors.ControlLightLight;
            ToolStrip_text.ForeColor = SystemColors.ControlLightLight;
            펜ToolStripMenuItem.ForeColor = SystemColors.ControlLightLight;
            지우개ToolStripMenuItem.ForeColor = SystemColors.ControlLightLight;
            Jap.ForeColor = SystemColors.ControlLightLight;
            Ju.ForeColor = SystemColors.ControlLightLight;
            ToolStrip_eyes.ForeColor = SystemColors.ControlLightLight;

            cbFont.BackColor = Color.FromArgb(63, 63, 70);
            cbFont.ForeColor = Color.FromArgb(230, 230, 230);
            numFontSize.BackColor = Color.FromArgb(63, 63, 70);
            numFontSize.ForeColor = Color.FromArgb(230, 230, 230);
            btnTextColor.BackColor = Color.FromArgb(63, 63, 70);
            btnTextColor.ForeColor = Color.FromArgb(230, 230, 230);
            splitContainer1.Panel1.BackColor = Color.FromArgb(37, 37, 38);
            splitContainer1.Panel2.BackColor = Color.FromArgb(37, 37, 38);
            foreach (Control c in this.Controls)
            {
                if (c is System.Windows.Forms.TrackBar tb)
                {
                    tb.BackColor = Color.FromArgb(63, 63, 70);
                    tb.ForeColor = Color.FromArgb(230, 230, 230);
                }
            }
            splitContainer1.BackColor = Color.FromArgb(37, 37, 38);

            cmsToolMode.BackColor = Color.FromArgb(63, 63, 70);
            cmsToolMode.ForeColor = Color.FromArgb(230, 230, 230);

        }

        public Main()
        {
            InitializeComponent();

            TurntoLightMode();
            //Main창에 드롭 허용하지 않음
            this.AllowDrop = false;
            //픽쳐박스에 드롭 허용
            MainPic.AllowDrop = true;
            MainPic.DragEnter += MainPic_DragEnter;
            MainPic.DragDrop += MainPic_DragDrop;

            //================================================

            this.AllowDrop = false;
            this.pnJab.Visible = false;
            this.pnJu.Visible = false;
            this.pnDraw.Visible = false;

            if (FaceMode.workingImage != null)
                MainPic.Image = FaceMode.workingImage;

            tbJab.Minimum = 1;
            tbJab.Maximum = 100;
            tbJab.Value = 20;

            tbJu.Minimum = 1;
            tbJu.Maximum = 100;
            tbJu.Value = 20;

            tbDraw.Minimum = 10;
            tbDraw.Maximum = 150;
            tbDraw.Value = 20;

            panel1.AutoScroll = true;
            MainPic.SizeMode = PictureBoxSizeMode.Normal;
            MainPic.Location = new System.Drawing.Point(0, 0);

            this.MainPic.MouseWheel += MainPic_MouseWheel; ;

            //===================================================
            InitializeDlib();

            // 트랙바 설정: 0(원본) ~ 100(최대치)
            trackBar1.Minimum = 0;
            trackBar1.Maximum = 100;
            trackBar1.Value = 0;

            MainPic.MouseDown += MainPic_MouseDown;
            MainPic.MouseMove += MainPic_MouseMove;
            MainPic.MouseUp += MainPic_MouseUp;
            MainPic.MouseLeave += MainPic_MouseLeave;
            MainPic.Paint += MainPic_Paint;

            this.trackBar1.Scroll += TrackBar1_Scroll;

            // [핵심 추가] text.cs 파일에 작성된 텍스트 기능을 초기화합니다.
            this.InitTextEntry();

            this.Load += (s, e) =>
            {
                if (pnlFrames != null) pnlFrames.Visible = false;
                if (MainPic != null) MainPic.SizeMode = PictureBoxSizeMode.Zoom;
                RegisterFrameEvents();
            };

            // 키보드 이벤트 설정을 위해 KeyPreview 활성화
            this.KeyPreview = true;
            this.KeyDown += Main_KeyDown;

            // 우클릭 컨텍스트 메뉴 생성
            ContextMenuStrip menu = new ContextMenuStrip();
            menu.Items.Add("복사", null, (s, e) => CopySelectedSticker());
            menu.Items.Add("붙여넣기", null, (s, e) => PasteSticker());
            menu.Items.Add("-"); // 구분선
            menu.Items.Add("삭제", null, (s, e) => DeleteSelectedSticker());

            //MainPic에 메뉴 연결
            MainPic.ContextMenuStrip = menu;

            //투명도 바 이벤트
            trbOpacity.MouseDown += TrbOpacity_MouseDown;
            trbOpacity.MouseUp += TrbOpacity_MouseUp;
            trbOpacity.Scroll += trbOpacity_Scroll;
            //스티커(또는 투명도 조절 가능한 객체)를 선택했을 때만 활성화
            trbOpacity.Enabled = false;

            // 메인폼 이동 이벤트(이동할 때마다 스티커폼 위치 변경)
            this.LocationChanged += Main_LocationChanged;

            // 콜라주 이미지 선택 끝 버튼 (콜라주 기능 선택했을 때만 나타나게)
            btn_collageModeEnd.Enabled = false;
            btn_collageModeEnd.Visible = false;
            btn_collageModeEnd.Click += btn_collageModeEnd_Click;

            editor = new ImageEditor();

            Mosaic.Click += Mosaic_Click;
            Cut.Click += Cut_Click;
        }


        private void ReplaceMainImage(Bitmap newBitmap)
        {

            // 교체 전 이미지를 잠시 보관해 두었다가 새 이미지로 바꾼 뒤 메모리를 정리한다.
            Image old = MainPic.Image;
            MainPic.Image = newBitmap;
            old?.Dispose();

            // 이미지 크기가 바뀌면 마스크도 동일한 해상도로 다시 생성해야 좌표가 어긋나지 않는다.
            if (mask == null || mask.Length != newBitmap.Width * newBitmap.Height)
                mask = new byte[newBitmap.Width * newBitmap.Height];

            MainPic.Invalidate();
        }


        internal EditorState CaptureState()
        {
            if (MainPic.Image == null)
                throw new InvalidOperationException("현재 편집 상태가 없습니다.");

            byte[] safeMask = mask != null
                ? (byte[])mask.Clone()
                : new byte[MainPic.Image.Width * MainPic.Image.Height];

            Bitmap? safeDrawLayer = null;
            if (FaceMode.drawLayer != null)
            {
                safeDrawLayer = FaceMode.drawLayer.Clone(
                    new System.Drawing.Rectangle(0, 0, FaceMode.drawLayer.Width, FaceMode.drawLayer.Height),
                    PixelFormat.Format32bppArgb);
            }

            return new EditorState((Bitmap)MainPic.Image.Clone(), safeMask, safeDrawLayer);
        }

        // 현재 상태를 반영해 UI 또는 내부 값을 갱신한다.
        public void UpdateMainImage(Image newImage, string frameName)
        {
            if (MainPic.Image != null && MainPic.Image != originalImage)
                MainPic.Image.Dispose();

            MainPic.Image = newImage;
            currentFrameName = frameName;

            if (newImage != null)
            {
                if (mask == null || mask.Length != newImage.Width * newImage.Height)
                    mask = new byte[newImage.Width * newImage.Height];

                if (FaceMode.workingImage != null && !ReferenceEquals(FaceMode.workingImage, newImage))
                {
                    FaceMode.workingImage.Dispose();
                }

                FaceMode.workingImage = new Bitmap(newImage);
            }

            MainPic.Invalidate();
        }

        internal void ApplyState(EditorState state)
        {
            if (state == null) return;

            Bitmap restored = state.Image.Clone(
                new System.Drawing.Rectangle(0, 0, state.Image.Width, state.Image.Height),
                PixelFormat.Format32bppArgb);

            ReplaceMainImage(restored);
            mask = (byte[])state.Mask.Clone();

            if (FaceMode.workingImage != null && !ReferenceEquals(FaceMode.workingImage, MainPic.Image))
                FaceMode.workingImage.Dispose();

            FaceMode.workingImage = restored.Clone(
                new System.Drawing.Rectangle(0, 0, restored.Width, restored.Height),
                PixelFormat.Format32bppArgb);

            if (FaceMode.drawLayer != null)
            {
                FaceMode.drawLayer.Dispose();
                FaceMode.drawLayer = null;
            }

            if (state.DrawLayer != null)
            {
                FaceMode.drawLayer = state.DrawLayer.Clone(
                    new System.Drawing.Rectangle(0, 0, state.DrawLayer.Width, state.DrawLayer.Height),
                    PixelFormat.Format32bppArgb);
            }

            editor.LoadImage(FaceMode.workingImage);

            MainPic.Invalidate();
            UpdateUndoRedoButtons();
        }

        private void UpdateUndoRedoButtons()
        {
            if (btn_Undo != null) btn_Undo.Enabled = commandManager.CanUndo;
            if (btn_redo != null) btn_redo.Enabled = commandManager.CanRedo;

        }

        private void SyncMainImageToFaceAndEditor(bool syncOriginal = false, bool clearHistory = false)
        {
            if (MainPic.Image is not Bitmap current) return;

            Bitmap faceBitmap = current.Clone(
                new System.Drawing.Rectangle(0, 0, current.Width, current.Height),
                PixelFormat.Format32bppArgb);

            if (FaceMode.workingImage != null && !ReferenceEquals(FaceMode.workingImage, MainPic.Image))
                FaceMode.workingImage.Dispose();

            FaceMode.workingImage = faceBitmap;

            if (syncOriginal)
            {
                FaceMode.originalImage?.Dispose();
                FaceMode.originalImage = faceBitmap.Clone(
                    new System.Drawing.Rectangle(0, 0, faceBitmap.Width, faceBitmap.Height),
                    PixelFormat.Format32bppArgb);
            }

            //FaceMode.drawLayer?.Dispose();
            //FaceMode.drawLayer = null;

            editor.LoadImage(faceBitmap);

            if (clearHistory)
                commandManager.Clear();

            MainPic.Image = faceBitmap;
            MainPic.Invalidate();
        }

        
        private void MainPic_MouseWheel(object? sender, MouseEventArgs e)
        {
            // 1. 현재 마우스가 가리키는 '이미지 상의 실제 좌표'를 먼저 구함
            System.Drawing.Point imgPoint = ZoomService.TranslateZoomMousePosition(MainPic, FaceMode.workingImage, e.Location);
            if (imgPoint == System.Drawing.Point.Empty) return;

            // 2. 확대/축소 비율 결정
            float oldZoom = FaceMode.zoomFactor;
            if (e.Delta > 0) FaceMode.zoomFactor += FaceMode.zoomStep;
            else if (FaceMode.zoomFactor > 0.5f) FaceMode.zoomFactor -= FaceMode.zoomStep;

            // 3. PictureBox 크기 조절
            // (기존 Form 크기나 Panel 크기 대비 확대 비율 적용)
            // 여기서는 간단하게 원본 이미지 크기에 zoomFactor를 곱합니다.
            MainPic.Width = (int)(panel1.Width * FaceMode.zoomFactor);
            MainPic.Height = (int)(panel1.Height * FaceMode.zoomFactor);

            // 4. 스크롤 위치 조정 (마우스 포인터 중심 유지)
            // 새로운 확대 비율에서의 마우스 위치 계산
            int newMouseX = (int)(imgPoint.X * ((float)MainPic.Width / FaceMode.workingImage.Width));
            int newMouseY = (int)(imgPoint.Y * ((float)MainPic.Height / FaceMode.workingImage.Height));

            // Panel의 스크롤을 이동시켜 마우스 지점이 유지되게 함
            int scrollX = newMouseX - e.X + Math.Abs(panel1.AutoScrollPosition.X);
            int scrollY = newMouseY - e.Y + Math.Abs(panel1.AutoScrollPosition.Y);

            panel1.AutoScrollPosition = new System.Drawing.Point(scrollX, scrollY);

        }
        private void MainPic_DragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop) || e.Data.GetDataPresent(typeof(Bitmap)))
            {
                e.Effect = DragDropEffects.Copy;
            }
        }
        private void MainPic_DragDrop(object sender, DragEventArgs e)
        {
            Bitmap droppedImage = null;

            //1. 외부 파일에서 끌어온 경우
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
                droppedImage = new Bitmap(files[0]);
            }
            // 2. 스티커 창(Sticker_property)에서 이미지를 끌어온 경우
            else if (e.Data.GetDataPresent(typeof(Bitmap)))
            {
                droppedImage = (Bitmap)e.Data.GetData(typeof(Bitmap));
            }

            if (droppedImage != null)
            {
                // 드롭된 좌표 계산 (화면 좌표를 폼 내부 좌표로 변환)
                System.Drawing.Point clientPoint = MainPic.PointToClient(new System.Drawing.Point(e.X, e.Y));

                // 스티커 객체 생성 (비율 유지)
                float defaultWidth = 100;
                float defaultHeight = (float)droppedImage.Height * (defaultWidth / droppedImage.Width);

                StickerObject newSticker = new StickerObject(
                    new Bitmap(droppedImage),            // 원본 보호를 위해 복제본 생성
                    clientPoint.X - (defaultWidth / 2),  // 마우스 위치가 중심이 되도록
                    clientPoint.Y - (defaultHeight / 2),
                    defaultWidth,
                    defaultHeight
                );

                // 명령 실행 및 추가
                ICommand addCommand = new AddStickerCommand(
                    this.stickersList,
                    newSticker,
                    () => MainPic.Invalidate()  // 람다식을 _refreshAction으로 전달
                    );

                this.commandManager.ExecuteCommand(addCommand);
                UpdateUndoRedoButtons();
                MainPic.Invalidate();
            }
        }


        private void Main_LocationChanged(object sender, EventArgs e)
        {
            UpdateStickerFormLocation();
            if (ModeForm != null && !ModeForm.IsDisposed)//동호
                ModeForm.Location = new System.Drawing.Point(this.Right - 15, this.Top);
        }
        private void btn_Undo_Click(object sender, EventArgs e)
        {
            Undo();
        }
        private void btn_redo_Click(object sender, EventArgs e)
        {
            Redo();
        }
        private void Main_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Delete)
            {
                DeleteSelectedSticker();
                _targetSticker = null;
            }
            // 복사 붙여넣기 단축키 추가
            if (e.Control && e.KeyCode == Keys.C) CopySelectedSticker();
            if (e.Control && e.KeyCode == Keys.V) PasteSticker();
            // 이전 다음 단축키 추가
            if (e.Control && e.KeyCode == Keys.Z)
            {
                Undo();
                e.SuppressKeyPress = true;
            }

            if (e.Control && e.KeyCode == Keys.Y)
            {
                Redo();
                e.SuppressKeyPress = true;
            }
        }
        // 투명도 슬라이더를 움직일 때 실시간 반영
        private void trbOpacity_Scroll(object sender, EventArgs e)
        {
            if (_targetSticker.Opacity != null)
            {
                _targetSticker.Opacity = trbOpacity.Value / 100f;
                MainPic.Invalidate();
            }
        }
        //슬라이더 조작이 끝났을 때(MouseUp) 최종 값을 Undo스택에 저장
        private void TrbOpacity_MouseDown(object sender, MouseEventArgs e)
        {
            if (_targetSticker != null)
                _startOpacity = _targetSticker.Opacity;
        }
        private void TrbOpacity_MouseUp(object sender, MouseEventArgs e)
        {
            if (_targetSticker != null && _startOpacity != _targetSticker.Opacity)
            {
                var cmd = new OpacityCommand(_targetSticker, _startOpacity, _targetSticker.Opacity,
                    () => MainPic.Invalidate());
                commandManager.ExecuteCommand(cmd);
                UpdateUndoRedoButtons();
                MainPic.Invalidate();
            }
        }

        private void ResetAll()
        {
            MainPic.Image = null;

            FaceMode.workingImage = null;
            FaceMode.originalImage = null;

            previewBaseBitmap = null;

            faceDetected = false;
            currentEyeStrength = 0f;

            mask = null;

            trackBar1.Value = 0;

            MainPic.Invalidate();
        }


        private void toolStrip_collage1_Click(object sender, EventArgs e)
        {
            ResetAll();
            MainPic.Invalidate();
            collageFrames = CollageService.CreateLayout(1, MainPic.Width, MainPic.Height);

            _isChoosingImage = true;
            btn_collageModeEnd.Enabled = true;
            btn_collageModeEnd.Visible = true;
            MainPic.Invalidate();
        }
        private void toolStrip_collage2_Click(object sender, EventArgs e)
        {
            ResetAll();
            MainPic.Invalidate();
            collageFrames = CollageService.CreateLayout(2, MainPic.Width, MainPic.Height);

            _isChoosingImage = true;
            btn_collageModeEnd.Enabled = true;
            btn_collageModeEnd.Visible = true;
            MainPic.Invalidate();
        }
        private void toolStrip_collage3_Click(object sender, EventArgs e)
        {
            ResetAll();
            MainPic.Invalidate();
            collageFrames = CollageService.CreateLayout(3, MainPic.Width, MainPic.Height);

            _isChoosingImage = true;
            btn_collageModeEnd.Enabled = true;
            btn_collageModeEnd.Visible = true;
            MainPic.Invalidate();
        }
        private void toolStrip_collage4_Click(object sender, EventArgs e)
        {
            ResetAll();
            MainPic.Invalidate();
            collageFrames = CollageService.CreateLayout(4, MainPic.Width, MainPic.Height);

            _isChoosingImage = true;
            btn_collageModeEnd.Enabled = true;
            btn_collageModeEnd.Visible = true;
            MainPic.Invalidate();
        }
        public void btn_collageModeEnd_Click(object sender, EventArgs e)
        {
            _isChoosingImage = false;
            btn_collageModeEnd.Enabled = false;
            btn_collageModeEnd.Visible = false;

            Bitmap flattenedBitmap = ImageExporter.FlattenCollage(
                collageFrames,
                MainPic.Width,
                MainPic.Height
            );

            if (flattenedBitmap != null)
            {
                commandManager.Clear();
                editor.LoadImage(flattenedBitmap);

                // FaceMode도 같이 갱신
                if (FaceMode.originalImage != null)
                {
                    FaceMode.originalImage.Dispose();
                    FaceMode.originalImage = null;
                }

                if (FaceMode.workingImage != null)
                {
                    FaceMode.workingImage.Dispose();
                    FaceMode.workingImage = null;
                }

                if (FaceMode.drawLayer != null)
                {
                    FaceMode.drawLayer.Dispose();
                    FaceMode.drawLayer = null;
                }

                if (originalImage != null)
                {
                    originalImage.Dispose();
                    originalImage = null;
                }

                originalImage = new Bitmap(flattenedBitmap);
                FaceMode.originalImage = new Bitmap(flattenedBitmap);
                FaceMode.workingImage = new Bitmap(flattenedBitmap);

                MainPic.Image = FaceMode.workingImage;

                collageFrames.Clear();
                MainPic.Invalidate();

                flattenedBitmap.Dispose();
            }
        }



        private void Cut_Click(object sender, EventArgs e)// 자르기 편집 시작 버튼
        {
            SyncFaceToEditor();   // 추가
            DeactivateAllInteractiveModes();
            HideAllToolPanels();

            editor.CurrentEdit = ImageEditor.EditMode.Cut;
            editor.Mode = ImageEditor.CutMode.None;

            if (ModeForm == null || ModeForm.IsDisposed)
                ModeForm = new CutMain(this);

            ModeForm.Show();
            ModeForm.BringToFront();
        }
        private void Mosaic_Click(object sender, EventArgs e)// 모자이크 편집 시작 버튼
        {
            SyncFaceToEditor();   // 추가
            DeactivateAllInteractiveModes();
            HideAllToolPanels();

            editor.CurrentEdit = ImageEditor.EditMode.Mosaic;
            editor.Mode = ImageEditor.CutMode.None;

            if (MosaicForm == null || MosaicForm.IsDisposed)
                MosaicForm = new ChangeMosaic(this);

            MosaicForm.Show();
            MosaicForm.BringToFront();
        }
        private void Main_SizeChanged(object sender, EventArgs e)//메인폼 크기 바꿔도 자르기폼 붙여두기
        {
            if (ModeForm != null && !ModeForm.IsDisposed)
                ModeForm.Location = new System.Drawing.Point(this.Right - 15, this.Top);
        }
        public void SetMode(ImageEditor.CutMode Mode)//편집모드 기초
        {
            editor.SetMode(Mode);
            MainPic.Invalidate();
            if (editor.CurrentEdit == ImageEditor.EditMode.Cut)
            {
                if (ModeForm != null && !ModeForm.IsDisposed)
                {
                    ModeForm.SetSelectBtnOff();
                    ModeForm.ToggleReClickOff();
                }
            }
            else if (editor.CurrentEdit == ImageEditor.EditMode.Mosaic)
            {
                if (MosaicForm != null && !MosaicForm.IsDisposed)
                {
                    MosaicForm.SetSelectBtnOff();
                }
            }
        }
        public void CancelSelect()// 영역선택 후 X누를시
        {
            editor.CancelSelect();

            MainPic.Invalidate();

            if (ModeForm != null && !ModeForm.IsDisposed)
            {
                ModeForm.SetSelectBtnOff();
                ModeForm.ToggleReClickOff();
            }

        }
        public bool ToggleReClickOn()//반전 자르기 on/off
        {
            editor.ReverseSet = !editor.ReverseSet;
            return editor.ReverseSet;
        }
        public void Confirm() // 확인(저장)
        {
            if (MainPic.Image == null) return;
            if (editor.MainBIT == null) return;
            if (editor.Mode == ImageEditor.CutMode.None) return;

            commandManager.ExecuteCommand(new SelectionEditCommand(this));
            CancelSelect();
            UpdateUndoRedoButtons();

            if (editor.CurrentEdit == ImageEditor.EditMode.Cut)
            {
                if (ModeForm != null && !ModeForm.IsDisposed)
                {
                    ModeForm.SetSelectBtnOff();
                    ModeForm.ToggleReClickOff();
                }
            }
            else if (editor.CurrentEdit == ImageEditor.EditMode.Mosaic)
            {
                if (MosaicForm != null && !MosaicForm.IsDisposed)
                {
                    MosaicForm.SetSelectBtnOff();
                }
            }
        }
        public void SaveWithConfirm()
        {
            if (editor.MainBIT == null) return;

            DialogResult result = MessageBox.Show(
                "저장하시겠습니까?",
                "저장 확인",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);

            if (result == DialogResult.Yes)
            {
                editor.CommitCurrent();
                commandManager.Clear();
                UpdateUndoRedoButtons();
                MessageBox.Show("현재 상태가 저장되었습니다.");
            }
        }// 저장(확정:되돌리기 불가)
        private void btnPost_Click(object sender, EventArgs e) // 사진 넣기
        {
            OpenFileDialog OpnFile = new OpenFileDialog();
            OpnFile.Title = "이미지 선택";
            OpnFile.Filter = "이미지 파일|*.jpg;*.jpeg;*.png;*.bmp;*.gif";

            if (OpnFile.ShowDialog() == DialogResult.OK)
            {
                commandManager.Clear();
                UpdateUndoRedoButtons();
                // editor 쪽
                editor.LoadImage(OpnFile.FileName);

                // FaceMode 쪽
                if (FaceMode.originalImage != null)
                {
                    FaceMode.originalImage.Dispose();
                    FaceMode.originalImage = null;
                }

                if (FaceMode.workingImage != null)
                {
                    FaceMode.workingImage.Dispose();
                    FaceMode.workingImage = null;
                }

                if (FaceMode.drawLayer != null)
                {
                    FaceMode.drawLayer.Dispose();
                    FaceMode.drawLayer = null;
                }

                if (originalImage != null)
                {
                    originalImage.Dispose();
                    originalImage = null;
                }

                originalImage = new Bitmap(OpnFile.FileName);
                FaceMode.originalImage = new Bitmap(OpnFile.FileName);
                FaceMode.workingImage = new Bitmap(OpnFile.FileName);

                MainPic.Image = FaceMode.workingImage;

                // 새 이미지 기준으로 마스크 완전 초기화
                ResetMaskForCurrentImage(clearContents: true);

                // 텍스트 오버레이/입력창/선택 상태 정리
                ClearTextOverlayState();

                // 스티커 선택 상태 등 인터랙션 상태 정리
                DeactivateAllInteractiveModes();
                ResetToolModes();

                MainPic.SizeMode = PictureBoxSizeMode.Zoom;
                MainPic.Invalidate();
            }
        }

        private void SyncFaceToEditor()
        {
            if (FaceMode.workingImage == null) return;

            editor.LoadImage(FaceMode.workingImage);
        }
        private void SyncEditorToFace(bool syncOriginal = false)
        {
            if (MainPic.Image is not Bitmap current) return;

            Bitmap synced = current.Clone(
                new System.Drawing.Rectangle(0, 0, current.Width, current.Height),
                PixelFormat.Format32bppArgb);

            ReplaceMainImage(synced);
            SyncMainImageToFaceAndEditor(syncOriginal: syncOriginal, clearHistory: false);
        }

        private void DeactivateAllInteractiveModes()
        {
            ResetToolModes();

            SetCurrentTool(EditorTool.None);

            editor.CurrentEdit = ImageEditor.EditMode.None;
            editor.Mode = ImageEditor.CutMode.None;
            editor.Dragging = false;
            editor.isDrawing = false;

            editor.FreePNT.Clear();
            editor.path.Reset();

            _isChoosingImage = false;

            isPaintingMask = false;
            isDraggingText = false;
            showBrushCursor = false;

            ClearStickerSelection();
            MainPic.Invalidate();
        }

        private void ResetToolModes()
        {
            FaceMode.isBrushMode = false;
            FaceMode.isEraserMode = false;
            FaceMode.isJuMode = false;
            FaceMode.isJabMode = false;
            FaceMode.isDrawingJu = false;
        }
        private void HideAllToolPanels()
        {
            pnJab.Visible = false;
            pnJu.Visible = false;
            pnDraw.Visible = false;
        }
        private void ApplyJuEffectContinuous(System.Drawing.Point mousePos)
        {
            System.Drawing.Point imgPoint = ZoomService.TranslateZoomMousePosition(MainPic, FaceMode.workingImage, mousePos);
            if (imgPoint != System.Drawing.Point.Empty && FaceMode.workingImage != null)
            {
                var tempCmd = new JuCommand(FaceMode.workingImage, imgPoint, FaceMode.brushSize, null);
                tempCmd.Execute();

                MainPic.Image = FaceMode.workingImage;
                MainPic.Invalidate();
            }

            //System.Drawing.Point imgPoint = ZoomService.TranslateZoomMousePosition(MainPic, FaceMode.workingImage, mousePos);
            //if (imgPoint != System.Drawing.Point.Empty && FaceMode.workingImage != null)
            //{
            //    // ⭐ 명령 객체를 생성하는 오버헤드를 줄이기 위해 내부 로직(ApplyJuEffect)과 유사한 코드를 직접 실행하거나
            //    // 아래처럼 가볍게 호출합니다.
            //    int radius = FaceMode.brushSize / 2;

            //    // 1. 영역 계산
            //    int startX = Math.Max(imgPoint.X - radius, 0);
            //    int startY = Math.Max(imgPoint.Y - radius, 0);
            //    int endX = Math.Min(imgPoint.X + radius, FaceMode.workingImage.Width - 1);
            //    int endY = Math.Min(imgPoint.Y + radius, FaceMode.workingImage.Height - 1);

            //    // 2. 간단한 블러(주름제거) 효과 직접 적용 (성능을 위해 GetPixel 사용은 최소화)
            //    // (기존 JuCommand의 ApplyJabEffect 로직을 가져오되 Refresh 없이 수행)
            //    var tempCmd = new JuCommand(FaceMode.workingImage, imgPoint, FaceMode.brushSize, null);
            //    tempCmd.Execute();

            //    // 3. 화면 갱신 (Invalidate는 전체 Refresh보다 훨씬 가볍습니다)
            //    MainPic.Invalidate();
            //}
        }
        private void Ju_Click(object sender, EventArgs e)
        {
            SyncEditorToFace();
            DeactivateAllInteractiveModes();
            HideAllToolPanels();

            pnJu.Visible = true;
            FaceMode.isJuMode = true;

        }
        private void Jap_Click(object sender, EventArgs e)
        {
            SyncEditorToFace();
            DeactivateAllInteractiveModes();
            HideAllToolPanels();

            pnJab.Visible = true;
            FaceMode.isJabMode = true;
        }

        private void tbDraw_Scroll(object sender, EventArgs e)
        {
            FaceMode.brushSize = tbDraw.Value;
        }
        private void btnColor_Click(object sender, EventArgs e)
        {
            ColorDialog cd = new ColorDialog();
            if (cd.ShowDialog() == DialogResult.OK)
            {
                FaceMode.currentDrawColor = cd.Color;
                btnColor.BackColor = cd.Color; // 버튼 색상 변경
                FaceMode.isBrushMode = true;
                FaceMode.isEraserMode = false;
            }
        }
        private void btnWonJu_MouseDown(object sender, MouseEventArgs e)
        {
            FaceMode.isShowingOriginal = true;
            MainPic.Invalidate();
        }
        private void btnWonJu_MouseUp(object sender, MouseEventArgs e)
        {
            FaceMode.isShowingOriginal = false;
            MainPic.Invalidate();
        }
        private void tbJu_Scroll(object sender, EventArgs e)
        {
            FaceMode.brushSize = tbJu.Value;
        }
        private void tbJab1_Scroll(object sender, EventArgs e)
        {
            FaceMode.brushSize = tbJab.Value;
        }


        private void MainPic_MouseClick(object sender, MouseEventArgs e)
        {

            if (!FaceMode.isJabMode || FaceMode.workingImage == null)
                return;

            System.Drawing.Point imagePoint = ZoomService.TranslateZoomMousePosition(MainPic, FaceMode.workingImage, e.Location);
            if (imagePoint == System.Drawing.Point.Empty)
                return;

            var beforeState = CaptureState();
            ApplyJabEffectAt(imagePoint, FaceMode.brushSize);
            var afterState = CaptureState();

            commandManager.ExecuteCommand(new StateSnapshotCommand(this, beforeState, afterState));
            UpdateUndoRedoButtons();
        }

        private void ApplyJabEffectAt(System.Drawing.Point imagePoint, int brushSize)
        {
            int radius = brushSize / 2;

            int startX = Math.Max(imagePoint.X - radius, 0);
            int startY = Math.Max(imagePoint.Y - radius, 0);
            int endX = Math.Min(imagePoint.X + radius, FaceMode.workingImage.Width - 1);
            int endY = Math.Min(imagePoint.Y + radius, FaceMode.workingImage.Height - 1);

            System.Drawing.Rectangle area = new System.Drawing.Rectangle(startX, startY, endX - startX + 1, endY - startY + 1);

            int r = 0, g = 0, b = 0, count = 0;

            for (int x = area.Left; x <= area.Right; x++)
            {
                for (int y = area.Top; y <= area.Bottom; y++)
                {
                    double dist = Math.Sqrt(Math.Pow(x - imagePoint.X, 2) + Math.Pow(y - imagePoint.Y, 2));
                    if (dist <= radius)
                    {
                        Color c = FaceMode.workingImage.GetPixel(x, y);
                        r += c.R;
                        g += c.G;
                        b += c.B;
                        count++;
                    }
                }
            }

            if (count == 0) return;

            Color avgColor = Color.FromArgb(r / count, g / count, b / count);

            for (int x = area.Left; x <= area.Right; x++)
            {
                for (int y = area.Top; y <= area.Bottom; y++)
                {
                    double dist = Math.Sqrt(Math.Pow(x - imagePoint.X, 2) + Math.Pow(y - imagePoint.Y, 2));
                    if (dist <= radius)
                    {
                        Color original = FaceMode.workingImage.GetPixel(x, y);
                        int mixR = (original.R + avgColor.R) / 2;
                        int mixG = (original.G + avgColor.G) / 2;
                        int mixB = (original.B + avgColor.B) / 2;
                        FaceMode.workingImage.SetPixel(x, y, Color.FromArgb(mixR, mixG, mixB));
                    }
                }
            }

            MainPic.Image = FaceMode.workingImage;
            MainPic.Invalidate();
        }


        private void tsmiMaskBrush_Click(object sender, EventArgs e)
        {// 마스크 모드
            SwitchEditorTool(EditorTool.MaskBrush);
        }

        private void btntoolMode_Click(object sender, EventArgs e)
        {
            // 마우스 모드 선택
            cmsToolMode.Show(btntoolMode, new System.Drawing.Point(0, btntoolMode.Height));
        }

        private void GausianToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (MainPic.Image == null || mask == null) return;

            SetBrushMaskProfile(maxValue: 250, falloffPower: 1.5);


            // 실제 픽셀 연산은 Command 내부에서 수행되며, 여기서는 필터 요청만 생성한다.
            ICommand command = new GaussianBlurCommand(this, kernelSize: 49, sigma: 13.0);
            commandManager.ExecuteCommand(command);

            UpdateUndoRedoButtons();
        }
        /// <summary>
        /// 마스크 브러시의 최대 세기와 가장자리 감쇠 특성을 조정한다.
        /// 필터마다 자연스러운 경계를 만들기 위해 적용 전에 호출한다.
        /// </summary>
        private void SetBrushMaskProfile(int maxValue, double falloffPower)
        {
            if (maxValue < 1) maxValue = 1;
            if (maxValue > 255) maxValue = 255;

            if (falloffPower < 0.1) falloffPower = 0.1;
            if (falloffPower > 8.0) falloffPower = 8.0;

            maskMaxValue = maxValue;
            maskFalloffPower = falloffPower;
        }

        private void MotionToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (MainPic.Image == null) return;

            // 잔상 길이, 샘플 수, 중심 선명도 등의 파라미터를 묶어 모션 블러 명령을 생성한다.
            ICommand command = new MotionBlurCommand(
                 this,
             angleDeg: 0.0,
             length: 82.0,
             samples: 41,
             sigmaScale: 0.36,
             centerBias: 1.35,
             bidirectional: false,
             maskStrength: 1.85,
             falloffPower: 1.2);

            commandManager.ExecuteCommand(command);
            UpdateUndoRedoButtons();
        }

        private void RadialToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (MainPic.Image == null) return;

            SetBrushMaskProfile(maxValue: 210, falloffPower: 2.2);

            ICommand command = new RadialBlurCommand(this, sampleCount: 25, strength: 1.0);


            commandManager.ExecuteCommand(command);
            UpdateUndoRedoButtons();
        }

        private void PinchToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (MainPic.Image == null || mask == null) return;

            SetBrushMaskProfile(maxValue: 255, falloffPower: 1.20);

            ICommand command = new PinchCommand(
                this,
                strength: 2.0,
                radiusScale: 1.15,
                aspectX: 1.00,
                aspectY: 0.85);

            commandManager.ExecuteCommand(command);
            UpdateUndoRedoButtons();
        }

        private void ShareToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (MainPic.Image == null || mask == null) return;

            SetBrushMaskProfile(maxValue: 255, falloffPower: 1.05);



            ICommand command = new ShearCommand(
                this,
                shearX: 0.35,
                shearY: 0.00,
                radiusScale: 1.10,
                aspectX: 1.00,
                aspectY: 1.00);
            commandManager.ExecuteCommand(command);
            UpdateUndoRedoButtons();
        }

        private void SwirlToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (MainPic.Image == null || mask == null) return;

            SetBrushMaskProfile(maxValue: 255, falloffPower: 1.05);

            ICommand command = new SwirlCommand(
                this,
                angleDeg: 55.0,
                radiusScale: 1.15,
                aspectX: 1.00,
                aspectY: 1.00);

            commandManager.ExecuteCommand(command);
            UpdateUndoRedoButtons();
        }

        private void SpherizeToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (MainPic.Image == null || mask == null) return;

            SetBrushMaskProfile(maxValue: 255, falloffPower: 1.00);

            ICommand command = new SpherizeCommand(
                this,
                amount: 0.85,
                radiusScale: 1.12,
                aspectX: 1.00,
                aspectY: 1.00);

            commandManager.ExecuteCommand(command);
            UpdateUndoRedoButtons();
        }

        private void ColorHalftoneToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (MainPic.Image == null) return;

            ICommand command = new ColorHalftoneCommand(

             this,
             cellSize: 8,
             dotScale: 0.92,
             softness: 1.10,
             supersample: 3,
             inkStrength: 0.98,
             blackStrength: 1.08,
             preserveLight: 0.18);
            commandManager.ExecuteCommand(command);
            UpdateUndoRedoButtons();
        }

        private void CrystallizeToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (MainPic.Image == null) return;

            ICommand command = new CrystallizeCommand(
                 this,
                 cellSize: 18,
                 jitter: 0.85,
                 borderSoftness: 2.2,
                 borderDarkness: 0.12,
                 detailPreserve: 0.22,
                 colorBoost: 1.03);
            commandManager.ExecuteCommand(command);
            UpdateUndoRedoButtons();
        }

        private void FindedgeToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (MainPic.Image == null) return;

            ICommand command = new FindEdgesCommand(

                 this,
                  edgeStrength: 1.45,
                  threshold: 0.16,
                  gamma: 1.05,
                  colorSensitivity: 0.40,
                  lineOpacity: 1.0,
                  invertToBlackLines: true);
            commandManager.ExecuteCommand(command);
            UpdateUndoRedoButtons();
        }

        private void EmbossToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (MainPic.Image == null) return;

            ICommand command = new MetalEmbossCommand(
                this,
            angleDeg: 135.0,
            elevationDeg: 35.0,
            depth: 3.2,
            blurSigma: 0.9,
            contrast: 1.45,
            specularStrength: 0.95,
            specularPower: 22.0,
            preserveColor: false,
            colorBlend: 0.18);
            commandManager.ExecuteCommand(command);
            UpdateUndoRedoButtons();
        }

        private void WindEffToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (MainPic.Image == null) return;

            ICommand command = new WindCommand(
                this,
            toRight: true,
            strength: 28,
            threshold: 0.14,
            edgeSensitivity: 0.95,
            blendOpacity: 0.92,
            scatter: 0.48,
            mode: WindFilter.WindMode.Blast);
            commandManager.ExecuteCommand(command);
            UpdateUndoRedoButtons();
        }

        private void DrawToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (MainPic.Image == null) return;

            ICommand command = new SketchCommand(this);
            commandManager.ExecuteCommand(command);

            UpdateUndoRedoButtons();
        }

        private void WatercolorToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (MainPic.Image == null) return;
            //blur radius     //색 양자화 수준     //경계의 강도
            ICommand command = new WatercolorKuwaharaCommand(this, radius: 4, colorLevels: 10, saturationBoost: 1.08);

            commandManager.ExecuteCommand(command);

            UpdateUndoRedoButtons();
        }

        private void PosterToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (MainPic.Image == null) return;

            ICommand command = new PosterEffectCommand(this, levels: 4, contrast: 1.18, saturationBoost: 18);

            commandManager.ExecuteCommand(command);
            UpdateUndoRedoButtons();
        }

        private void WarmtoneToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (MainPic.Image == null) return;

            ICommand command = new ToneCommand(this, ToneMode.Warm, 0.55);
            commandManager.ExecuteCommand(command);

            UpdateUndoRedoButtons();
        }

        private void CooltoneToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (MainPic.Image == null) return;

            ICommand command = new ToneCommand(this, ToneMode.Cool, 0.55);
            commandManager.ExecuteCommand(command);

            UpdateUndoRedoButtons();
        }

        private void SoftGlowToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (MainPic.Image == null) return;

            ICommand command = new SoftGlowCommand(
                this,
                radius: 5,
                blurBlend: 0.50,
                brightnessBoost: 16.0,
                contrastSoften: 0.88,
                glowThreshold: 165.0,
                glowStrength: 0.42);
            commandManager.ExecuteCommand(command);
            UpdateUndoRedoButtons();
        }

        private void OilpaintToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (MainPic.Image == null) return;
            //                                    주변을 묶는 정도,   밝기 구간 개수,     채도 보정,            대비 보정   
            ICommand command = new OilPaintCommand(this, radius: 5, intensityLevels: 20, saturationBoost: 1.14, contrastBoost: 1.06);
            /*                             자연스러운 유채화    3                   28                  1.06                1.02   
                                           확실한 그림 느낌     4                   24                  1.10                1.04
                                           강한 유채화          5                   20                  1.14                1.06
                                           두꺼운 회화풍        6                   16                  1.18                1.08             


             */
            commandManager.ExecuteCommand(command);
            UpdateUndoRedoButtons();
        }

        private void UnsharpToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (MainPic.Image == null) return;

            ICommand command = new UnsharpMaskCommand(
                this,
                radius: 2.0,
                amount: 1.3,
                threshold: 0.02);
            commandManager.ExecuteCommand(command);
            UpdateUndoRedoButtons();
        }

        private void SmartToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (MainPic.Image == null) return;

            ICommand command = new SmartSharpenCommand(
                this,
                radius: 1.8,
                amount: 1.35,
                threshold: 4.0,
                edgeBoost: 1.0,
                noiseSuppression: 0.65,
                highlightFade: 0.25,
                shadowFade: 0.18,
                antiHalo: 1.0);
            commandManager.ExecuteCommand(command);
            UpdateUndoRedoButtons();
        }

        private void FlareToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (MainPic.Image == null) return;

            // 광원 위치를 작게 찍듯이 지정
            SetBrushMaskProfile(maxValue: 255, falloffPower: 1.15);

            ICommand command = new SoftLensFlareCommand(
                this,
                intensity: 1.08,
                bloomRadius: 0.20,
                haloRadius: 0.16,
                haloWidth: 0.028,
                veilStrength: 0.22,
                ghostStrength: 0.08,
                streakStrength: 0.02,
                pinkHaloStrength: 0.44,
                blueGhostStrength: 0.10,
                rayCount: 6,
                rayStrength: 0.34,
                raySharpness: 22.0,
                rayLength: 0.36
            );

            commandManager.ExecuteCommand(command);
            UpdateUndoRedoButtons();
        }

        private void GrayToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (MainPic.Image == null) return;

            ICommand command = new GrayscaleCommand(this);
            commandManager.ExecuteCommand(command);

            UpdateUndoRedoButtons();
        }

        private void btnSave_Click(object sender, EventArgs e)
        {
            if (MainPic.Image == null)
            {
                MessageBox.Show("저장할 이미지가 없습니다.");
                return;
            }

            using SaveFileDialog sfd = new SaveFileDialog();
            sfd.Title = "이미지 저장";
            sfd.Filter = "PNG 이미지|*.png|JPEG 이미지|*.jpg;*.jpeg|BMP 이미지|*.bmp";
            sfd.DefaultExt = "png";
            sfd.AddExtension = true;
            sfd.FileName = GetDefaultSaveFileName();

            if (sfd.ShowDialog() != DialogResult.OK)
                return;

            try
            {
                using Bitmap baseSaveBmp = new Bitmap(MainPic.Image);
                // drawLayer 합치기
                if (FaceMode.drawLayer != null)
                {
                    using (Graphics g = Graphics.FromImage(baseSaveBmp))
                    {
                        g.DrawImage(FaceMode.drawLayer, 0, 0);
                    }
                }

                using Bitmap stickerMergedBmp = RenderPendingStickersToBitmap(baseSaveBmp);
                using Bitmap saveBmp = RenderPendingTextsToBitmap(stickerMergedBmp);

                ImageFormat format = GetImageFormatFromExtension(sfd.FileName);
                saveBmp.Save(sfd.FileName, format);

                MessageBox.Show("이미지가 저장되었습니다.");
            }
            catch (Exception ex)
            {
                MessageBox.Show("저장 중 오류가 발생했습니다.\n" + ex.Message);
            }
        }

        internal Bitmap RenderPendingStickersToBitmap(Bitmap source)
        {
            Bitmap result = new Bitmap(source);

            if (MainPic.Image == null)
                return result;

            System.Drawing.Rectangle displayedRect = GetDisplayedImageRect();
            if (displayedRect == System.Drawing.Rectangle.Empty)
                return result;

            float scaleX = (float)MainPic.Image.Width / displayedRect.Width;
            float scaleY = (float)MainPic.Image.Height / displayedRect.Height;

            using (Graphics g = Graphics.FromImage(result))
            {
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                g.PixelOffsetMode = PixelOffsetMode.HighQuality;

                foreach (var sticker in stickersList)
                {
                    RectangleF pbBounds = sticker.Bounds;

                    System.Drawing.Rectangle destRect = new System.Drawing.Rectangle(
                    (int)Math.Round((pbBounds.X - displayedRect.X) * scaleX),
                    (int)Math.Round((pbBounds.Y - displayedRect.Y) * scaleY),
                    Math.Max(1, (int)Math.Round(pbBounds.Width * scaleX)),
                    Math.Max(1, (int)Math.Round(pbBounds.Height * scaleY))
                    );

                    if (destRect.Width <= 0 || destRect.Height <= 0)
                        continue;

                    float[][] matrixItems =
                    {
                    new float[] {1, 0, 0, 0, 0},
                    new float[] {0, 1, 0, 0, 0},
                    new float[] {0, 0, 1, 0, 0},
                    new float[] {0, 0, 0, 1 - sticker.Opacity, 0},
                    new float[] {0, 0, 0, 0, 1},
                    };

                    using ImageAttributes attr = new ImageAttributes();
                    attr.SetColorMatrix(new ColorMatrix(matrixItems));

                    g.DrawImage(
                    sticker.StickerImage,
                    destRect,
                    0,
                    0,
                    sticker.StickerImage.Width,
                    sticker.StickerImage.Height,
                    GraphicsUnit.Pixel,
                    attr);
                }
            }

            return result;
        }

        // 계산 결과나 내부 값을 반환한다.
        private string GetDefaultSaveFileName()
        {
            string time = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            return $"edited_{time}.png";
        }

        // 계산 결과나 내부 값을 반환한다.
        private ImageFormat GetImageFormatFromExtension(string path)
        {
            string ext = Path.GetExtension(path).ToLowerInvariant();

            return ext switch
            {
                ".jpg" => ImageFormat.Jpeg,
                ".jpeg" => ImageFormat.Jpeg,
                ".bmp" => ImageFormat.Bmp,
                _ => ImageFormat.Png,
            };
        }

        private void BrightnessContrast_Click(object sender, EventArgs e)
        {//밝기 및 대비 조절 코드
            if (MainPic.Image == null) return;

            Bitmap originalPreview = new Bitmap((Bitmap)MainPic.Image);

            using BrightnessContrastDialog dlg = new BrightnessContrastDialog();

            dlg.ValuesChanged += (brightness, contrast) =>
            {
                // 미리보기는 원본 기준으로 매번 다시 생성
                Bitmap previewSource = new Bitmap(originalPreview);

                using ReadAsBmp bmp = new ReadAsBmp();
                bmp.ImageProcessing(previewSource);

                unsafe
                {
                    byte* pPixel = (byte*)bmp.photo_bmpdata.Scan0;
                    int w = bmp.photo_bmp.Width;
                    int h = bmp.photo_bmp.Height;
                    int stride = bmp.photo_bmpdata.Stride;

                    BrightnessContrastFilter.ApplyBrightnessContrast32bpp_BGRA(
                        pPixel, w, h, stride,
                        brightness, contrast);
                }

                bmp.ProcessingEnd();

                Bitmap previewResult = bmp.photo_bmp;
                bmp.photo_bmp = null;

                // 기존 화면 이미지 교체
                if (!ReferenceEquals(MainPic.Image, originalPreview))
                {
                    Image? old = MainPic.Image;
                    ReplaceMainImage(previewResult);

                    if (old != null && !ReferenceEquals(old, originalPreview))
                        old.Dispose();
                }
                else
                {
                    ReplaceMainImage(previewResult);
                }

                MainPic.Invalidate();
            };

            DialogResult result = dlg.ShowDialog();

            // 미리보기 상태를 버리고 원본으로 되돌린 뒤,
            // 확인이면 Command로 최종 적용
            Image? currentPreview = MainPic.Image;
            ReplaceMainImage(new Bitmap(originalPreview));

            if (currentPreview != null &&
                !ReferenceEquals(currentPreview, originalPreview) &&
                !ReferenceEquals(currentPreview, MainPic.Image))
            {
                currentPreview.Dispose();
            }

            if (result == DialogResult.OK)
            {
                ICommand command = new BrightnessContrastCommand(
                    this,
                    dlg.BrightnessValue,
                    dlg.ContrastValue
                );

                commandManager.ExecuteCommand(command);
                UpdateUndoRedoButtons();
            }

            originalPreview.Dispose();
            MainPic.Invalidate();

        }

        private void ToolStrip_sticker_Click(object sender, EventArgs e)
        {
            if (_stickerForm == null || _stickerForm.IsDisposed)
            {
                _stickerForm = new Sticker_property();
                // 수동으로 위치를 지정
                _stickerForm.StartPosition = FormStartPosition.Manual;

                //스티커 창이 항상 메인 폼 위에 떠 있게 실행
                _stickerForm.Owner = this;
                _stickerForm.Show();
                UpdateStickerFormLocation();
            }
        }

        private void ToolStrip_text_Click(object sender, EventArgs e)
        {
            // 텍스트 모드
            SwitchEditorTool(EditorTool.Text);
        }

        private void ToolStrip_frame_Click(object sender, EventArgs e)
        {
            //프레임 이벤트 메서드
            pnlFrames.Visible = !pnlFrames.Visible;
            if (pnlFrames.Visible) pnlFrames.BringToFront();
        }

        private void 펜ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            SyncEditorToFace();
            DeactivateAllInteractiveModes();
            HideAllToolPanels();

            pnDraw.Visible = true;
            FaceMode.isBrushMode = true;

            if (FaceMode.workingImage != null && FaceMode.drawLayer == null)
            {
                FaceMode.drawLayer = new Bitmap(FaceMode.workingImage.Width, FaceMode.workingImage.Height);
                using (Graphics g = Graphics.FromImage(FaceMode.drawLayer))
                {
                    g.Clear(Color.Transparent);
                }
            }
        }

        private void 지우개ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            SyncEditorToFace();
            DeactivateAllInteractiveModes();
            HideAllToolPanels();

            pnDraw.Visible = true;
            FaceMode.isBrushMode = false;
            FaceMode.isEraserMode = true;

            if (FaceMode.workingImage != null && FaceMode.drawLayer == null)
            {
                FaceMode.drawLayer = new Bitmap(FaceMode.workingImage.Width, FaceMode.workingImage.Height);
                using (Graphics g = Graphics.FromImage(FaceMode.drawLayer))
                {
                    g.Clear(Color.Transparent);
                }
            }
        }

        private void ToolStrip_eyes_Click(object sender, EventArgs e)
        {
            btnApplyEyes.Enabled = true;
            btnApplyEyes.Visible = true;
            trackBar1.Enabled = true;
            trackBar1.Visible = true;
            label3.Enabled = true;
            label3.Visible = true;

            if (MainPic.Image == null) return;
            if (shapePredictor == null || faceDetector == null)
            {
                InitializeDlib();
                if (shapePredictor == null || faceDetector == null) return;
            }

            Cursor = Cursors.WaitCursor;
            try
            {
                previewBaseBitmap?.Dispose();
                eyePreviewBitmap?.Dispose();
                eyePreviewBitmap = null;

                previewBaseBitmap = new Bitmap((Bitmap)MainPic.Image);

                using (Bitmap dlibBmp = new Bitmap(previewBaseBitmap.Width, previewBaseBitmap.Height, PixelFormat.Format24bppRgb))
                {
                    using (Graphics g = Graphics.FromImage(dlibBmp))
                        g.DrawImage(previewBaseBitmap, 0, 0);

                    System.Drawing.Rectangle rect = new System.Drawing.Rectangle(0, 0, dlibBmp.Width, dlibBmp.Height);
                    BitmapData data = dlibBmp.LockBits(rect, ImageLockMode.ReadOnly, PixelFormat.Format24bppRgb);

                    byte[] rgbValues = new byte[data.Stride * dlibBmp.Height];
                    Marshal.Copy(data.Scan0, rgbValues, 0, rgbValues.Length);
                    dlibBmp.UnlockBits(data);

                    using (var img = Dlib.LoadImageData<BgrPixel>(
                        rgbValues,
                        (uint)dlibBmp.Height,
                        (uint)dlibBmp.Width,
                        (uint)data.Stride))
                    {
                        var faces = faceDetector.Operator(img);

                        if (faces.Length > 0)
                        {
                            var shape = shapePredictor.Detect(img, faces[0]);

                            leftEyePoints.Clear();
                            rightEyePoints.Clear();

                            for (uint i = 36; i <= 41; i++)
                                leftEyePoints.Add(new System.Drawing.Point((int)shape.GetPart(i).X, (int)shape.GetPart(i).Y));

                            for (uint i = 42; i <= 47; i++)
                                rightEyePoints.Add(new System.Drawing.Point((int)shape.GetPart(i).X, (int)shape.GetPart(i).Y));

                            faceDetected = true;
                            currentEyeStrength = 0f;
                            trackBar1.Value = 0;

                            MessageBox.Show("인식 완료! 트랙바로 미리보기 후 적용 버튼으로 확정하세요.");
                        }
                        else
                        {
                            faceDetected = false;
                            MessageBox.Show("얼굴을 찾을 수 없습니다.");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"오류: {ex.Message}");
            }
            finally
            {
                Cursor = Cursors.Default;
            }
        }




        private void lb_screenMode_Click(object sender, EventArgs e)
        {
            if (_isLightMode)
            {
                TurntoDarkMode();
                _isLightMode = false;
            }
            else
            {
                TurntoLightMode();
                _isLightMode = true;
            }
        }

        private void Main_Load_1(object sender, EventArgs e)
        {
            this.BackColor = ColorTranslator.FromHtml("#F7F7F7");
            panel1.BackColor = ColorTranslator.FromHtml("#ECECEC");

            menuStrip1.Renderer = new CustomMenuRenderer();
        }
    }
}


