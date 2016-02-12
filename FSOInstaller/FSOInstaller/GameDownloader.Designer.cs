namespace FSOInstaller
{
    partial class GameDownloader
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(GameDownloader));
            this.pictureBox1 = new System.Windows.Forms.PictureBox();
            this._9SegmentImage1 = new FSOInstaller.Components._9SegmentImage();
            this.tsoProgressBar1 = new FSOInstaller.Components.TSOProgressBar();
            this.label1 = new System.Windows.Forms.Label();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox1)).BeginInit();
            this.SuspendLayout();
            // 
            // pictureBox1
            // 
            this.pictureBox1.Location = new System.Drawing.Point(75, 41);
            this.pictureBox1.Name = "pictureBox1";
            this.pictureBox1.Size = new System.Drawing.Size(207, 97);
            this.pictureBox1.TabIndex = 0;
            this.pictureBox1.TabStop = false;
            // 
            // _9SegmentImage1
            // 
            this._9SegmentImage1.Dock = System.Windows.Forms.DockStyle.Fill;
            this._9SegmentImage1.ImagePath = "Packed/Setup/Resources/UI/Graphics/E2B14588_Dialog_SolidAlpha.tga";
            this._9SegmentImage1.Location = new System.Drawing.Point(0, 0);
            this._9SegmentImage1.Name = "_9SegmentImage1";
            this._9SegmentImage1.SegMargin = 60;
            this._9SegmentImage1.Size = new System.Drawing.Size(497, 269);
            this._9SegmentImage1.TabIndex = 1;
            this._9SegmentImage1.Text = "_9SegmentImage1";
            // 
            // tsoProgressBar1
            // 
            this.tsoProgressBar1.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(80)))), ((int)(((byte)(119)))), ((int)(((byte)(163)))));
            this.tsoProgressBar1.Location = new System.Drawing.Point(107, 88);
            this.tsoProgressBar1.Name = "tsoProgressBar1";
            this.tsoProgressBar1.Size = new System.Drawing.Size(291, 25);
            this.tsoProgressBar1.TabIndex = 2;
            this.tsoProgressBar1.Text = "tsoProgressBar1";
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(61)))), ((int)(((byte)(89)))), ((int)(((byte)(121)))));
            this.label1.Font = new System.Drawing.Font("Comic Sans MS", 11.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label1.Location = new System.Drawing.Point(211, 2);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(52, 21);
            this.label1.TabIndex = 3;
            this.label1.Text = "label1";
            // 
            // GameDownloader
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.Color.White;
            this.BackgroundImageLayout = System.Windows.Forms.ImageLayout.None;
            this.ClientSize = new System.Drawing.Size(497, 269);
            this.ControlBox = false;
            this.Controls.Add(this.label1);
            this.Controls.Add(this.tsoProgressBar1);
            this.Controls.Add(this._9SegmentImage1);
            this.Controls.Add(this.pictureBox1);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.None;
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.Name = "GameDownloader";
            this.Text = "FreeSO Installer";
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox1)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.PictureBox pictureBox1;
        private Components._9SegmentImage _9SegmentImage1;
        private Components.TSOProgressBar tsoProgressBar1;
        private System.Windows.Forms.Label label1;
    }
}