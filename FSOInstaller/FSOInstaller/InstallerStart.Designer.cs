namespace FSOInstaller
{
    partial class InstallerStart
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(InstallerStart));
            this.InstallDir = new System.Windows.Forms.TextBox();
            this.BrowseButton = new System.Windows.Forms.Button();
            this.SourceURL = new System.Windows.Forms.TextBox();
            this.LanguageCombo = new System.Windows.Forms.ComboBox();
            this.InstallButton = new System.Windows.Forms.PictureBox();
            this.pictureBox1 = new System.Windows.Forms.PictureBox();
            ((System.ComponentModel.ISupportInitialize)(this.InstallButton)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox1)).BeginInit();
            this.SuspendLayout();
            // 
            // InstallDir
            // 
            this.InstallDir.Location = new System.Drawing.Point(110, 186);
            this.InstallDir.Name = "InstallDir";
            this.InstallDir.Size = new System.Drawing.Size(196, 20);
            this.InstallDir.TabIndex = 1;
            this.InstallDir.Text = "C:/Program Files/FreeSO/";
            // 
            // BrowseButton
            // 
            this.BrowseButton.Location = new System.Drawing.Point(312, 184);
            this.BrowseButton.Name = "BrowseButton";
            this.BrowseButton.Size = new System.Drawing.Size(75, 23);
            this.BrowseButton.TabIndex = 2;
            this.BrowseButton.Text = "Browse...";
            this.BrowseButton.UseVisualStyleBackColor = true;
            this.BrowseButton.Click += new System.EventHandler(this.BrowseButton_Click);
            // 
            // SourceURL
            // 
            this.SourceURL.Location = new System.Drawing.Point(110, 240);
            this.SourceURL.Name = "SourceURL";
            this.SourceURL.Size = new System.Drawing.Size(277, 20);
            this.SourceURL.TabIndex = 3;
            this.SourceURL.Text = "http://largedownloads.ea.com/pub/misc/tso/manifest.txt";
            // 
            // LanguageCombo
            // 
            this.LanguageCombo.Cursor = System.Windows.Forms.Cursors.Default;
            this.LanguageCombo.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.LanguageCombo.FormattingEnabled = true;
            this.LanguageCombo.Location = new System.Drawing.Point(386, 372);
            this.LanguageCombo.Name = "LanguageCombo";
            this.LanguageCombo.Size = new System.Drawing.Size(107, 21);
            this.LanguageCombo.TabIndex = 5;
            // 
            // InstallButton
            // 
            this.InstallButton.Image = global::FSOInstaller.Properties.Resources.installbtn;
            this.InstallButton.Location = new System.Drawing.Point(149, 280);
            this.InstallButton.Name = "InstallButton";
            this.InstallButton.Size = new System.Drawing.Size(201, 64);
            this.InstallButton.TabIndex = 4;
            this.InstallButton.TabStop = false;
            this.InstallButton.Click += new System.EventHandler(this.InstallButton_Click);
            // 
            // pictureBox1
            // 
            this.pictureBox1.Image = global::FSOInstaller.Properties.Resources.installermain;
            this.pictureBox1.Location = new System.Drawing.Point(0, 0);
            this.pictureBox1.Name = "pictureBox1";
            this.pictureBox1.Size = new System.Drawing.Size(500, 400);
            this.pictureBox1.TabIndex = 0;
            this.pictureBox1.TabStop = false;
            // 
            // InstallerStart
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(500, 400);
            this.Controls.Add(this.LanguageCombo);
            this.Controls.Add(this.InstallButton);
            this.Controls.Add(this.SourceURL);
            this.Controls.Add(this.BrowseButton);
            this.Controls.Add(this.InstallDir);
            this.Controls.Add(this.pictureBox1);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "InstallerStart";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "FreeSO Installer";
            ((System.ComponentModel.ISupportInitialize)(this.InstallButton)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox1)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.PictureBox pictureBox1;
        private System.Windows.Forms.TextBox InstallDir;
        private System.Windows.Forms.Button BrowseButton;
        private System.Windows.Forms.TextBox SourceURL;
        private System.Windows.Forms.PictureBox InstallButton;
        private System.Windows.Forms.ComboBox LanguageCombo;
    }
}