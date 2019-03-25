namespace FSO.Patcher
{
    partial class FormsPatcher
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(FormsPatcher));
            this.OverallProgress = new System.Windows.Forms.ProgressBar();
            this.StatusText = new System.Windows.Forms.Label();
            this.SingleProgress = new System.Windows.Forms.ProgressBar();
            this.OverallStatus = new System.Windows.Forms.Label();
            this.OverallNum = new System.Windows.Forms.Label();
            this.SingleStatus = new System.Windows.Forms.Label();
            this.SingleNum = new System.Windows.Forms.Label();
            this.panel1 = new System.Windows.Forms.Panel();
            this.panel1.SuspendLayout();
            this.SuspendLayout();
            // 
            // OverallProgress
            // 
            this.OverallProgress.Location = new System.Drawing.Point(15, 51);
            this.OverallProgress.Name = "OverallProgress";
            this.OverallProgress.Size = new System.Drawing.Size(357, 23);
            this.OverallProgress.TabIndex = 0;
            // 
            // StatusText
            // 
            this.StatusText.AutoEllipsis = true;
            this.StatusText.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.StatusText.ForeColor = System.Drawing.SystemColors.ControlLightLight;
            this.StatusText.Location = new System.Drawing.Point(0, 0);
            this.StatusText.Name = "StatusText";
            this.StatusText.Size = new System.Drawing.Size(385, 28);
            this.StatusText.TabIndex = 1;
            this.StatusText.Text = "Updating FreeSO... Please wait!";
            this.StatusText.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // SingleProgress
            // 
            this.SingleProgress.Location = new System.Drawing.Point(15, 100);
            this.SingleProgress.Name = "SingleProgress";
            this.SingleProgress.Size = new System.Drawing.Size(357, 23);
            this.SingleProgress.TabIndex = 2;
            // 
            // OverallStatus
            // 
            this.OverallStatus.AutoEllipsis = true;
            this.OverallStatus.Location = new System.Drawing.Point(12, 35);
            this.OverallStatus.Name = "OverallStatus";
            this.OverallStatus.Size = new System.Drawing.Size(314, 13);
            this.OverallStatus.TabIndex = 3;
            this.OverallStatus.Text = "Starting...";
            // 
            // OverallNum
            // 
            this.OverallNum.Location = new System.Drawing.Point(332, 35);
            this.OverallNum.Name = "OverallNum";
            this.OverallNum.Size = new System.Drawing.Size(40, 13);
            this.OverallNum.TabIndex = 4;
            this.OverallNum.Text = "?/?";
            this.OverallNum.TextAlign = System.Drawing.ContentAlignment.TopRight;
            // 
            // SingleStatus
            // 
            this.SingleStatus.AutoEllipsis = true;
            this.SingleStatus.Location = new System.Drawing.Point(12, 84);
            this.SingleStatus.Name = "SingleStatus";
            this.SingleStatus.Size = new System.Drawing.Size(317, 13);
            this.SingleStatus.TabIndex = 5;
            this.SingleStatus.Text = "Starting...";
            // 
            // SingleNum
            // 
            this.SingleNum.Location = new System.Drawing.Point(315, 84);
            this.SingleNum.Name = "SingleNum";
            this.SingleNum.Size = new System.Drawing.Size(57, 13);
            this.SingleNum.TabIndex = 6;
            this.SingleNum.Text = "?/?";
            this.SingleNum.TextAlign = System.Drawing.ContentAlignment.TopRight;
            // 
            // panel1
            // 
            this.panel1.BackColor = System.Drawing.SystemColors.Highlight;
            this.panel1.Controls.Add(this.StatusText);
            this.panel1.Location = new System.Drawing.Point(0, 0);
            this.panel1.Name = "panel1";
            this.panel1.Size = new System.Drawing.Size(385, 28);
            this.panel1.TabIndex = 7;
            // 
            // FormsPatcher
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(384, 137);
            this.Controls.Add(this.panel1);
            this.Controls.Add(this.SingleNum);
            this.Controls.Add(this.SingleStatus);
            this.Controls.Add(this.OverallNum);
            this.Controls.Add(this.OverallStatus);
            this.Controls.Add(this.SingleProgress);
            this.Controls.Add(this.OverallProgress);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.Name = "FormsPatcher";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "FreeSO Patcher";
            this.Load += new System.EventHandler(this.FormsPatcher_Load);
            this.panel1.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.ProgressBar OverallProgress;
        private System.Windows.Forms.Label StatusText;
        private System.Windows.Forms.ProgressBar SingleProgress;
        private System.Windows.Forms.Label OverallStatus;
        private System.Windows.Forms.Label OverallNum;
        private System.Windows.Forms.Label SingleStatus;
        private System.Windows.Forms.Label SingleNum;
        private System.Windows.Forms.Panel panel1;
    }
}