using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Photo
{
    public partial class CutMain : Form
    {
        private Main mainForm;
        public CutMain(Main form)
        {
            InitializeComponent();
            mainForm = form;
            SetSelectBtnOff();
        }
        
        private void SquareCut_Click(object sender, EventArgs e)//ㅁ 선택
        {
            mainForm.SetMode(ImageEditor.CutMode.Square);
            SquareCut.BackColor = Color.MediumAquamarine;
            CircleCut.BackColor = SystemColors.ButtonHighlight;
            FreeCut.BackColor = SystemColors.ButtonHighlight;
            
        }
        private void CircleCut_Click(object sender, EventArgs e)//ㅇ 선택
        {
            mainForm.SetMode(ImageEditor.CutMode.Circle);
            SquareCut.BackColor = SystemColors.ButtonHighlight;
            CircleCut.BackColor = Color.MediumAquamarine;
            FreeCut.BackColor = SystemColors.ButtonHighlight;
            
        }
        private void FreeCut_Click(object sender, EventArgs e)// 자유 선택
        {
            mainForm.SetMode(ImageEditor.CutMode.Free);
            SquareCut.BackColor = SystemColors.ButtonHighlight;
            CircleCut.BackColor = SystemColors.ButtonHighlight;
            FreeCut.BackColor = Color.MediumAquamarine;
            
        }
        private void Reverse_Click(object sender, EventArgs e)// 선택 반전 처리
        {
            bool isOn = mainForm.ToggleReClickOn();
            if (isOn)
                Reverse.BackColor = Color.MediumAquamarine;
            else
                Reverse.BackColor = SystemColors.ButtonHighlight;
        }
        
        public void ToggleReClickOff()//반전 버튼 처리
        {
            Reverse.BackColor = SystemColors.ButtonHighlight;
        }
        
        private void btnCheck_Click(object sender, EventArgs e)//적용
        {
            mainForm.Confirm();
            btnCheck.Visible = false;
            btnCancel.Visible = false;
            mainForm.SetMode(ImageEditor.CutMode.None);
            SquareCut.BackColor = SystemColors.ButtonHighlight;
            CircleCut.BackColor = SystemColors.ButtonHighlight;
            FreeCut.BackColor = SystemColors.ButtonHighlight;
            
            
        }
        private void btnCancel_Click(object sender, EventArgs e)//취소
        {
            btnCheck.Visible = false;
            btnCancel.Visible = false;
            mainForm.SetMode(ImageEditor.CutMode.None);
            SquareCut.BackColor = SystemColors.ButtonHighlight;
            CircleCut.BackColor = SystemColors.ButtonHighlight;
            FreeCut.BackColor = SystemColors.ButtonHighlight;
            ToggleReClickOff();


        }
        
        private void Undo_Click(object sender, EventArgs e)//실행 취소
        {
            mainForm.Undo();
        }
        private void Redo_Click(object sender, EventArgs e)//다시 실행
        {
            mainForm.Redo();
        }
        
        private void SaveDone_Click(object sender, EventArgs e)//확정 (되돌리기 불가)
        {
            mainForm.SaveWithConfirm();
        }
        private void Cancel_Click(object sender, EventArgs e)//초기화 (되돌리기 가능)
        {
            mainForm.ResetToOriginal();
        }
       
        public void SetSelectBtnOn()//버튼 모두 보이기
        {
            btnCheck.Visible = true;
            btnCancel.Visible = true;
        }
        public void SetSelectBtnOff()//버튼 모두 가리기
        {
            btnCheck.Visible = false;
            btnCancel.Visible = false;
        }
    }
}
