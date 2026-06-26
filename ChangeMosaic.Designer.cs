namespace Photo
{
    partial class ChangeMosaic
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
            MCancel = new Button();
            MRedo = new Button();
            MUndo = new Button();
            MSaveDone = new Button();
            btnMCancel = new Button();
            btnMCheck = new Button();
            FreeGet = new Button();
            CircleGet = new Button();
            SquareGet = new Button();
            SuspendLayout();
            // 
            // MCancel
            // 
            MCancel.Location = new Point(28, 388);
            MCancel.Name = "MCancel";
            MCancel.Size = new Size(107, 50);
            MCancel.TabIndex = 2;
            MCancel.Text = "초기화";
            MCancel.UseVisualStyleBackColor = true;
            MCancel.Click += MCancel_Click;
            // 
            // MRedo
            // 
            MRedo.Location = new Point(84, 147);
            MRedo.Name = "MRedo";
            MRedo.Size = new Size(51, 50);
            MRedo.TabIndex = 3;
            MRedo.Text = "▶";
            MRedo.UseVisualStyleBackColor = true;
            MRedo.Click += MRedo_Click;
            // 
            // MUndo
            // 
            MUndo.Location = new Point(28, 147);
            MUndo.Name = "MUndo";
            MUndo.Size = new Size(50, 50);
            MUndo.TabIndex = 4;
            MUndo.Text = "◀";
            MUndo.UseVisualStyleBackColor = true;
            MUndo.Click += MUndo_Click;
            // 
            // MSaveDone
            // 
            MSaveDone.Location = new Point(28, 331);
            MSaveDone.Name = "MSaveDone";
            MSaveDone.Size = new Size(107, 50);
            MSaveDone.TabIndex = 5;
            MSaveDone.Text = "저장";
            MSaveDone.UseVisualStyleBackColor = true;
            MSaveDone.Click += MSaveDone_Click;
            // 
            // btnMCancel
            // 
            btnMCancel.Location = new Point(85, 243);
            btnMCancel.Name = "btnMCancel";
            btnMCancel.Size = new Size(50, 50);
            btnMCancel.TabIndex = 6;
            btnMCancel.Text = "❌️";
            btnMCancel.UseVisualStyleBackColor = true;
            btnMCancel.Visible = false;
            btnMCancel.Click += btnMCancel_Click;
            // 
            // btnMCheck
            // 
            btnMCheck.Location = new Point(29, 243);
            btnMCheck.Name = "btnMCheck";
            btnMCheck.Size = new Size(50, 50);
            btnMCheck.TabIndex = 7;
            btnMCheck.Text = "✔️";
            btnMCheck.UseVisualStyleBackColor = true;
            btnMCheck.Visible = false;
            btnMCheck.Click += btnMCheck_Click;
            // 
            // FreeGet
            // 
            FreeGet.Location = new Point(28, 68);
            FreeGet.Name = "FreeGet";
            FreeGet.Size = new Size(107, 50);
            FreeGet.TabIndex = 9;
            FreeGet.Text = "자유 고르기";
            FreeGet.UseVisualStyleBackColor = true;
            FreeGet.Click += FreeGet_Click;
            // 
            // CircleGet
            // 
            CircleGet.Location = new Point(89, 12);
            CircleGet.Name = "CircleGet";
            CircleGet.Size = new Size(46, 50);
            CircleGet.TabIndex = 10;
            CircleGet.Text = "○";
            CircleGet.UseVisualStyleBackColor = true;
            CircleGet.Click += CircleGet_Click;
            // 
            // SquareGet
            // 
            SquareGet.BackColor = SystemColors.ButtonHighlight;
            SquareGet.Location = new Point(28, 12);
            SquareGet.Name = "SquareGet";
            SquareGet.Size = new Size(46, 50);
            SquareGet.TabIndex = 11;
            SquareGet.Text = "□";
            SquareGet.UseVisualStyleBackColor = false;
            SquareGet.Click += SquareGet_Click;
            // 
            // ChangeMosaic
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(163, 450);
            Controls.Add(MCancel);
            Controls.Add(MRedo);
            Controls.Add(MUndo);
            Controls.Add(MSaveDone);
            Controls.Add(btnMCancel);
            Controls.Add(btnMCheck);
            Controls.Add(FreeGet);
            Controls.Add(CircleGet);
            Controls.Add(SquareGet);
            Name = "ChangeMosaic";
            Text = "ChangeMosaic";
            ResumeLayout(false);
        }

        #endregion

        private Button MCancel;
        private Button MRedo;
        private Button MUndo;
        private Button MSaveDone;
        private Button btnMCancel;
        private Button btnMCheck;
        private Button FreeGet;
        private Button CircleGet;
        private Button SquareGet;
    }
}