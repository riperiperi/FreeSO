namespace FSO.Patcher
{
    partial class Patcher
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(Patcher));
            this.StatusLabel = new System.Windows.Forms.Label();
            this.SuspendLayout();
            // 
            // StatusLabel
            // 
            this.StatusLabel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.StatusLabel.Location = new System.Drawing.Point(0, 0);
            this.StatusLabel.Name = "StatusLabel";
            this.StatusLabel.Size = new System.Drawing.Size(249, 39);
            this.StatusLabel.TabIndex = 0;
            this.StatusLabel.Text = "Preparing...";
            this.StatusLabel.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            this.StatusLabel.Click += new System.EventHandler(this.StatusLabel_Click);
            // 
            // Patcher
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(249, 39);
            this.Controls.Add(this.StatusLabel);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.Name = "Patcher";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "FreeSO Updater";
            this.Load += new System.EventHandler(this.Patcher_Load);
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.Label StatusLabel;
    }
}

