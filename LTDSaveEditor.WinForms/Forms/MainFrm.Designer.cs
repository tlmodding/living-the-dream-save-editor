namespace LTDSaveEditor.WinForms
{
    partial class MainFrm
    {
        /// <summary>
        ///  Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        ///  Clean up any resources being used.
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
        ///  Required method for Designer support - do not modify
        ///  the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(MainFrm));
            instructionLabel = new Label();
            SuspendLayout();
            // 
            // instructionLabel
            // 
            instructionLabel.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
            instructionLabel.AutoSize = true;
            instructionLabel.ForeColor = Color.White;
            instructionLabel.Location = new Point(109, 187);
            instructionLabel.Margin = new Padding(0);
            instructionLabel.Name = "instructionLabel";
            instructionLabel.Size = new Size(146, 15);
            instructionLabel.TabIndex = 0;
            instructionLabel.Text = "Drop your save folder here";
            // 
            // MainFrm
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            BackColor = Color.FromArgb(40, 40, 40);
            BackgroundImage = (Image)resources.GetObject("$this.BackgroundImage");
            BackgroundImageLayout = ImageLayout.Center;
            ClientSize = new Size(360, 260);
            Controls.Add(instructionLabel);
            DoubleBuffered = true;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            Icon = (Icon)resources.GetObject("$this.Icon");
            MaximizeBox = false;
            Name = "MainFrm";
            StartPosition = FormStartPosition.CenterScreen;
            Text = "Living the Dream: Save Editor";
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private Label instructionLabel;
    }
}
