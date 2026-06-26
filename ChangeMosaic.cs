using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Photo
{
    public partial class ChangeMosaic : Form
    {
        private Main mainForm;//메인폼
        public ChangeMosaic(Main form)
        {
            InitializeComponent();
            mainForm = form;
            SetSelectBtnOff();
        }

        private void SquareGet_Click(object sender, EventArgs e)//ㅁ 선택
        {
            mainForm.SetMode(ImageEditor.CutMode.Square);
            SquareGet.BackColor = Color.MediumAquamarine;
            CircleGet.BackColor = SystemColors.ButtonHighlight;
            FreeGet.BackColor = SystemColors.ButtonHighlight;

        }
        private void CircleGet_Click(object sender, EventArgs e)//ㅇ 선택
        {
            mainForm.SetMode(ImageEditor.CutMode.Circle);
            SquareGet.BackColor = SystemColors.ButtonHighlight;
            CircleGet.BackColor = Color.MediumAquamarine;
            FreeGet.BackColor = SystemColors.ButtonHighlight;
        }
        private void FreeGet_Click(object sender, EventArgs e)//자유 선택
        {
            mainForm.SetMode(ImageEditor.CutMode.Free);
            SquareGet.BackColor = SystemColors.ButtonHighlight;
            CircleGet.BackColor = SystemColors.ButtonHighlight;
            FreeGet.BackColor = Color.MediumAquamarine;
        }

        private void MUndo_Click(object sender, EventArgs e)//실행 취소
        {
            mainForm.Undo();
        }
        private void MRedo_Click(object sender, EventArgs e)// 다시 실행
        {
            mainForm.Redo();
        }

        private void btnMCheck_Click(object sender, EventArgs e)//적용
        {
            mainForm.Confirm();
            btnMCheck.Visible = false;
            btnMCancel.Visible = false;
            mainForm.SetMode(ImageEditor.CutMode.None);
            SquareGet.BackColor = SystemColors.ButtonHighlight;
            CircleGet.BackColor = SystemColors.ButtonHighlight;
            FreeGet.BackColor = SystemColors.ButtonHighlight;
        }        
        private void btnMCancel_Click(object sender, EventArgs e)//취소
        {
            btnMCheck.Visible = false;
            btnMCancel.Visible = false;
            mainForm.SetMode(ImageEditor.CutMode.None);
            SquareGet.BackColor = SystemColors.ButtonHighlight;
            CircleGet.BackColor = SystemColors.ButtonHighlight;
            FreeGet.BackColor = SystemColors.ButtonHighlight;
            
        }

        private void MSaveDone_Click(object sender, EventArgs e)//확정(되돌리기 불가)
        {
            mainForm.SaveWithConfirm();
        }
        private void MCancel_Click(object sender, EventArgs e)//초기화 (되돌리기 가능)
        {
            mainForm.ResetToOriginal();
        }
        
        public void SetSelectBtnOn()//버튼 모두 보이기
        {
            btnMCheck.Visible = true;
            btnMCancel.Visible = true;
        }
        public void SetSelectBtnOff()//버튼 모두 가리기
        {
            btnMCheck.Visible = false;
            btnMCancel.Visible = false;
        }
    }
}
