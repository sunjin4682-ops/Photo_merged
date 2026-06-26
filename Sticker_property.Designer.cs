namespace Photo
{
    partial class Sticker_property
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
            flowStickers = new FlowLayoutPanel();
            SuspendLayout();
            // 
            // flowStickers
            // 
            flowStickers.AutoScroll = true;
            flowStickers.Dock = DockStyle.Fill;
            flowStickers.Location = new Point(0, 0);
            flowStickers.Name = "flowStickers";
            flowStickers.Size = new Size(232, 380);
            flowStickers.TabIndex = 0;
            // 
            // Sticker_property
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            AutoScroll = true;
            ClientSize = new Size(232, 380);
            Controls.Add(flowStickers);
            Name = "Sticker_property";
            Text = "Sticker_property";
            ResumeLayout(false);
        }

        #endregion

        private FlowLayoutPanel flowStickers;
    }
}