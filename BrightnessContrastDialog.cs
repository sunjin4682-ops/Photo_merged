using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


// -----------------------------------------------------------------------------
// 파일 역할: 밝기/대비 조절용 입력 다이얼로그를 정의한다.
// 병합 작업 때 빠르게 구조를 파악할 수 있도록 주석을 보강한 버전이다.
// -----------------------------------------------------------------------------

namespace Photo
{
    // BrightnessContrastDialog 관련 역할을 담당하는 타입이다.
    public partial class BrightnessContrastDialog : Form
    {
        private readonly TrackBar tbBrightness;
        private readonly TrackBar tbContrast;
        private readonly Label lblBrightnessValue;
        private readonly Label lblContrastValue;
        private readonly Button btnOk;
        private readonly Button btnCancel;
        private readonly Button btnReset;

        public int BrightnessValue => tbBrightness.Value;
        public double ContrastValue => tbContrast.Value / 100.0;

        public event Action<int, double>? ValuesChanged;

        // 이 파일의 핵심 동작을 수행하는 메서드.
        public BrightnessContrastDialog()
        {
            Text = "밝기 / 대비";
            FormBorderStyle = FormBorderStyle.FixedDialog;
            StartPosition = FormStartPosition.CenterParent;
            MaximizeBox = false;
            MinimizeBox = false;
            ClientSize = new Size(360, 220);

            Label lbl1 = new Label
            {
                Text = "밝기",
                Left = 20,
                Top = 20,
                Width = 80
            };

            tbBrightness = new TrackBar
            {
                Left = 20,
                Top = 45,
                Width = 240,
                Minimum = -100,
                Maximum = 100,
                TickFrequency = 10,
                Value = 0
            };

            lblBrightnessValue = new Label
            {
                Left = 275,
                Top = 45,
                Width = 60,
                Text = "0"
            };

            Label lbl2 = new Label
            {
                Text = "대비",
                Left = 20,
                Top = 95,
                Width = 80
            };

            tbContrast = new TrackBar
            {
                Left = 20,
                Top = 120,
                Width = 240,
                Minimum = 0,
                Maximum = 300,
                TickFrequency = 25,
                Value = 100
            };

            lblContrastValue = new Label
            {
                Left = 275,
                Top = 120,
                Width = 60,
                Text = "1.00"
            };

            btnReset = new Button
            {
                Text = "초기화",
                Left = 20,
                Top = 175,
                Width = 80
            };

            btnOk = new Button
            {
                Text = "확인",
                Left = 190,
                Top = 175,
                Width = 70,
                DialogResult = DialogResult.OK
            };

            btnCancel = new Button
            {
                Text = "취소",
                Left = 270,
                Top = 175,
                Width = 70,
                DialogResult = DialogResult.Cancel
            };

            Controls.Add(lbl1);
            Controls.Add(tbBrightness);
            Controls.Add(lblBrightnessValue);
            Controls.Add(lbl2);
            Controls.Add(tbContrast);
            Controls.Add(lblContrastValue);
            Controls.Add(btnReset);
            Controls.Add(btnOk);
            Controls.Add(btnCancel);

            AcceptButton = btnOk;
            CancelButton = btnCancel;

            tbBrightness.Scroll += TrackBar_Scroll;
            tbContrast.Scroll += TrackBar_Scroll;
            btnReset.Click += BtnReset_Click;
        }

        // 트랙바 스크롤 값을 반영한다.
        private void TrackBar_Scroll(object? sender, EventArgs e)
        {
            lblBrightnessValue.Text = tbBrightness.Value.ToString();
            lblContrastValue.Text = (tbContrast.Value / 100.0).ToString("0.00");

            ValuesChanged?.Invoke(BrightnessValue, ContrastValue);
        }

        // 버튼/메뉴 클릭 이벤트를 처리한다.
        private void BtnReset_Click(object? sender, EventArgs e)
        {
            tbBrightness.Value = 0;
            tbContrast.Value = 100;

            lblBrightnessValue.Text = "0";
            lblContrastValue.Text = "1.00";

            ValuesChanged?.Invoke(BrightnessValue, ContrastValue);
        }
    }
}
