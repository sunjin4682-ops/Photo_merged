namespace Photo
{
    partial class CutMain
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            SquareCut = new Button();
            FreeCut = new Button();
            Reverse = new Button();
            SaveDone = new Button();
            Cancel = new Button();
            Undo = new Button();
            Redo = new Button();
            CircleCut = new Button();
            btnCheck = new Button();
            btnCancel = new Button();
            SuspendLayout();
            // 
            // SquareCut
            // 
            SquareCut.BackColor = SystemColors.ButtonHighlight;
            SquareCut.Location = new Point(22, 12);
            SquareCut.Name = "SquareCut";
            SquareCut.Size = new Size(46, 50);
            SquareCut.TabIndex = 1;
            SquareCut.Text = "□";
            SquareCut.UseVisualStyleBackColor = false;
            SquareCut.Click += SquareCut_Click;
            // 
            // FreeCut
            // 
            FreeCut.Location = new Point(22, 68);
            FreeCut.Name = "FreeCut";
            FreeCut.Size = new Size(107, 50);
            FreeCut.TabIndex = 1;
            FreeCut.Text = "자유 자르기";
            FreeCut.UseVisualStyleBackColor = true;
            FreeCut.Click += FreeCut_Click;
            // 
            // Reverse
            // 
            Reverse.Location = new Point(22, 124);
            Reverse.Name = "Reverse";
            Reverse.Size = new Size(107, 50);
            Reverse.TabIndex = 1;
            Reverse.Text = "반전 자르기";
            Reverse.UseVisualStyleBackColor = true;
            Reverse.Click += Reverse_Click;
            // 
            // SaveDone
            // 
            SaveDone.Location = new Point(22, 331);
            SaveDone.Name = "SaveDone";
            SaveDone.Size = new Size(107, 50);
            SaveDone.TabIndex = 1;
            SaveDone.Text = "저장";
            SaveDone.UseVisualStyleBackColor = true;
            SaveDone.Click += SaveDone_Click;
            // 
            // Cancel
            // 
            Cancel.Location = new Point(22, 388);
            Cancel.Name = "Cancel";
            Cancel.Size = new Size(107, 50);
            Cancel.TabIndex = 1;
            Cancel.Text = "초기화";
            Cancel.UseVisualStyleBackColor = true;
            Cancel.Click += Cancel_Click;
            // 
            // Undo
            // 
            Undo.Location = new Point(22, 198);
            Undo.Name = "Undo";
            Undo.Size = new Size(50, 50);
            Undo.TabIndex = 1;
            Undo.Text = "◀";
            Undo.UseVisualStyleBackColor = true;
            Undo.Click += Undo_Click;
            // 
            // Redo
            // 
            Redo.Location = new Point(78, 198);
            Redo.Name = "Redo";
            Redo.Size = new Size(51, 50);
            Redo.TabIndex = 1;
            Redo.Text = "▶";
            Redo.UseVisualStyleBackColor = true;
            Redo.Click += Redo_Click;
            // 
            // CircleCut
            // 
            CircleCut.Location = new Point(83, 12);
            CircleCut.Name = "CircleCut";
            CircleCut.Size = new Size(46, 50);
            CircleCut.TabIndex = 1;
            CircleCut.Text = "○";
            CircleCut.UseVisualStyleBackColor = true;
            CircleCut.Click += CircleCut_Click;
            // 
            // btnCheck
            // 
            btnCheck.Location = new Point(22, 275);
            btnCheck.Name = "btnCheck";
            btnCheck.Size = new Size(50, 50);
            btnCheck.TabIndex = 1;
            btnCheck.Text = "✔️";
            btnCheck.UseVisualStyleBackColor = true;
            btnCheck.Visible = false;
            btnCheck.Click += btnCheck_Click;
            // 
            // btnCancel
            // 
            btnCancel.Location = new Point(78, 275);
            btnCancel.Name = "btnCancel";
            btnCancel.Size = new Size(50, 50);
            btnCancel.TabIndex = 1;
            btnCancel.Text = "❌️";
            btnCancel.UseVisualStyleBackColor = true;
            btnCancel.Visible = false;
            btnCancel.Click += btnCancel_Click;
            // 
            // CutMain
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(152, 450);
            Controls.Add(Cancel);
            Controls.Add(Redo);
            Controls.Add(Undo);
            Controls.Add(SaveDone);
            Controls.Add(btnCancel);
            Controls.Add(btnCheck);
            Controls.Add(Reverse);
            Controls.Add(FreeCut);
            Controls.Add(CircleCut);
            Controls.Add(SquareCut);
            Name = "CutMain";
            Text = "자르기 편집";
            ResumeLayout(false);
        }

        #endregion
        private Button SquareCut;
        private Button FreeCut;
        private Button Reverse;
        private Button SaveDone;
        private Button Cancel;
        private Button Undo;
        private Button Redo;
        private Button CircleCut;
        private Button btnCheck;
        private Button btnCancel;
    }
}