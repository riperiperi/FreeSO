namespace PDPatcher
{
    partial class Form1
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
            this.PrgTotal = new System.Windows.Forms.ProgressBar();
            this.LblTotalProgress = new System.Windows.Forms.Label();
            this.PrgFile = new System.Windows.Forms.ProgressBar();
            this.LblFileProgress = new System.Windows.Forms.Label();
            this.LblSpeed = new System.Windows.Forms.Label();
            this.pictureBox1 = new System.Windows.Forms.PictureBox();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox1)).BeginInit();
            this.SuspendLayout();
            // 
            // PrgTotal
            // 
            this.PrgTotal.Location = new System.Drawing.Point(12, 190);
            this.PrgTotal.Name = "PrgTotal";
            this.PrgTotal.Size = new System.Drawing.Size(646, 23);
            this.PrgTotal.TabIndex = 0;
            // 
            // LblTotalProgress
            // 
            this.LblTotalProgress.AutoSize = true;
            this.LblTotalProgress.Location = new System.Drawing.Point(12, 174);
            this.LblTotalProgress.Name = "LblTotalProgress";
            this.LblTotalProgress.Size = new System.Drawing.Size(75, 13);
            this.LblTotalProgress.TabIndex = 1;
            this.LblTotalProgress.Text = "Total Progress";
            // 
            // PrgFile
            // 
            this.PrgFile.Location = new System.Drawing.Point(12, 131);
            this.PrgFile.Name = "PrgFile";
            this.PrgFile.Size = new System.Drawing.Size(646, 23);
            this.PrgFile.TabIndex = 0;
            // 
            // LblFileProgress
            // 
            this.LblFileProgress.AutoSize = true;
            this.LblFileProgress.Location = new System.Drawing.Point(12, 115);
            this.LblFileProgress.Name = "LblFileProgress";
            this.LblFileProgress.Size = new System.Drawing.Size(67, 13);
            this.LblFileProgress.TabIndex = 1;
            this.LblFileProgress.Text = "File Progress";
            // 
            // LblSpeed
            // 
            this.LblSpeed.AutoSize = true;
            this.LblSpeed.Location = new System.Drawing.Point(12, 237);
            this.LblSpeed.Name = "LblSpeed";
            this.LblSpeed.Size = new System.Drawing.Size(47, 13);
            this.LblSpeed.TabIndex = 2;
            this.LblSpeed.Text = "Kb/Sec:";
            // 
            // pictureBox1
            // 
            this.pictureBox1.Image = global::PDPatcher.Properties.Resources.ProjectDollhouseDraft2nowatermark_small;
            this.pictureBox1.Location = new System.Drawing.Point(530, 12);
            this.pictureBox1.Name = "pictureBox1";
            this.pictureBox1.Size = new System.Drawing.Size(128, 113);
            this.pictureBox1.TabIndex = 3;
            this.pictureBox1.TabStop = false;
            // 
            // Form1
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(670, 262);
            this.Controls.Add(this.pictureBox1);
            this.Controls.Add(this.LblSpeed);
            this.Controls.Add(this.LblFileProgress);
            this.Controls.Add(this.LblTotalProgress);
            this.Controls.Add(this.PrgFile);
            this.Controls.Add(this.PrgTotal);
            this.Name = "Form1";
            this.Text = "PD Patcher";
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox1)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.ProgressBar PrgTotal;
        private System.Windows.Forms.Label LblTotalProgress;
        private System.Windows.Forms.ProgressBar PrgFile;
        private System.Windows.Forms.Label LblFileProgress;
        private System.Windows.Forms.Label LblSpeed;
        private System.Windows.Forms.PictureBox pictureBox1;
    }
}

