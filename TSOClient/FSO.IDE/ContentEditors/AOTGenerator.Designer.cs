namespace FSO.IDE.ContentEditors
{
    partial class AOTGenerator
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(AOTGenerator));
            this.AOTProgress = new System.Windows.Forms.ProgressBar();
            this.DescriptionLabel = new System.Windows.Forms.Label();
            this.AOTToggle = new System.Windows.Forms.Button();
            this.OutDir = new System.Windows.Forms.TextBox();
            this.OutDirLabel = new System.Windows.Forms.Label();
            this.IncludeUser = new System.Windows.Forms.CheckBox();
            this.AOTStatus = new System.Windows.Forms.Label();
            this.SuspendLayout();
            // 
            // AOTProgress
            // 
            this.AOTProgress.Location = new System.Drawing.Point(12, 187);
            this.AOTProgress.Name = "AOTProgress";
            this.AOTProgress.Size = new System.Drawing.Size(392, 32);
            this.AOTProgress.TabIndex = 0;
            // 
            // DescriptionLabel
            // 
            this.DescriptionLabel.AutoSize = true;
            this.DescriptionLabel.Location = new System.Drawing.Point(9, 9);
            this.DescriptionLabel.MaximumSize = new System.Drawing.Size(400, 0);
            this.DescriptionLabel.Name = "DescriptionLabel";
            this.DescriptionLabel.Size = new System.Drawing.Size(400, 117);
            this.DescriptionLabel.TabIndex = 1;
            this.DescriptionLabel.Text = resources.GetString("DescriptionLabel.Text");
            // 
            // AOTToggle
            // 
            this.AOTToggle.Location = new System.Drawing.Point(329, 225);
            this.AOTToggle.Name = "AOTToggle";
            this.AOTToggle.Size = new System.Drawing.Size(75, 23);
            this.AOTToggle.TabIndex = 3;
            this.AOTToggle.Text = "Begin";
            this.AOTToggle.UseVisualStyleBackColor = true;
            this.AOTToggle.Click += new System.EventHandler(this.AOTToggle_Click);
            // 
            // OutDir
            // 
            this.OutDir.Location = new System.Drawing.Point(12, 161);
            this.OutDir.Name = "OutDir";
            this.OutDir.Size = new System.Drawing.Size(282, 20);
            this.OutDir.TabIndex = 4;
            this.OutDir.Text = "../../../../../TS1.Scripts/";
            // 
            // OutDirLabel
            // 
            this.OutDirLabel.AutoSize = true;
            this.OutDirLabel.Location = new System.Drawing.Point(12, 145);
            this.OutDirLabel.Name = "OutDirLabel";
            this.OutDirLabel.Size = new System.Drawing.Size(87, 13);
            this.OutDirLabel.TabIndex = 5;
            this.OutDirLabel.Text = "Output Directory:";
            // 
            // IncludeUser
            // 
            this.IncludeUser.AutoSize = true;
            this.IncludeUser.Location = new System.Drawing.Point(318, 163);
            this.IncludeUser.Name = "IncludeUser";
            this.IncludeUser.Size = new System.Drawing.Size(86, 17);
            this.IncludeUser.TabIndex = 6;
            this.IncludeUser.Text = "Include User";
            this.IncludeUser.UseVisualStyleBackColor = true;
            // 
            // AOTStatus
            // 
            this.AOTStatus.AutoSize = true;
            this.AOTStatus.Location = new System.Drawing.Point(12, 230);
            this.AOTStatus.Name = "AOTStatus";
            this.AOTStatus.Size = new System.Drawing.Size(52, 13);
            this.AOTStatus.TabIndex = 7;
            this.AOTStatus.Text = "Waiting...";
            // 
            // AOTGenerator
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(416, 257);
            this.Controls.Add(this.AOTStatus);
            this.Controls.Add(this.IncludeUser);
            this.Controls.Add(this.OutDirLabel);
            this.Controls.Add(this.OutDir);
            this.Controls.Add(this.AOTToggle);
            this.Controls.Add(this.DescriptionLabel);
            this.Controls.Add(this.AOTProgress);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.Name = "AOTGenerator";
            this.Text = "Generate AOT Sources (.cs)";
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.ProgressBar AOTProgress;
        private System.Windows.Forms.Label DescriptionLabel;
        private System.Windows.Forms.Button AOTToggle;
        private System.Windows.Forms.TextBox OutDir;
        private System.Windows.Forms.Label OutDirLabel;
        private System.Windows.Forms.CheckBox IncludeUser;
        private System.Windows.Forms.Label AOTStatus;
    }
}