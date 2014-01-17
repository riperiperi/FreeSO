namespace Manifestation
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
            this.menuStrip1 = new System.Windows.Forms.MenuStrip();
            this.fileToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.newManifestToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.addFileToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.saveManifestToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.loadManifestToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.exitToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.label1 = new System.Windows.Forms.Label();
            this.LstFiles = new System.Windows.Forms.ListBox();
            this.NumMajor = new System.Windows.Forms.NumericUpDown();
            this.label2 = new System.Windows.Forms.Label();
            this.NumMinor = new System.Windows.Forms.NumericUpDown();
            this.label3 = new System.Windows.Forms.Label();
            this.NumPatch = new System.Windows.Forms.NumericUpDown();
            this.label4 = new System.Windows.Forms.Label();
            this.GrpURLs = new System.Windows.Forms.GroupBox();
            this.BtnUpdateURL = new System.Windows.Forms.Button();
            this.LblBaseURL = new System.Windows.Forms.Label();
            this.TxtBaseURL = new System.Windows.Forms.TextBox();
            this.helpToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.usageToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.aboutToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.menuStrip1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.NumMajor)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.NumMinor)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.NumPatch)).BeginInit();
            this.GrpURLs.SuspendLayout();
            this.SuspendLayout();
            // 
            // menuStrip1
            // 
            this.menuStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.fileToolStripMenuItem,
            this.helpToolStripMenuItem});
            this.menuStrip1.Location = new System.Drawing.Point(0, 0);
            this.menuStrip1.Name = "menuStrip1";
            this.menuStrip1.Size = new System.Drawing.Size(736, 24);
            this.menuStrip1.TabIndex = 0;
            this.menuStrip1.Text = "menuStrip1";
            // 
            // fileToolStripMenuItem
            // 
            this.fileToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.newManifestToolStripMenuItem,
            this.addFileToolStripMenuItem,
            this.saveManifestToolStripMenuItem,
            this.loadManifestToolStripMenuItem,
            this.exitToolStripMenuItem});
            this.fileToolStripMenuItem.Name = "fileToolStripMenuItem";
            this.fileToolStripMenuItem.Size = new System.Drawing.Size(37, 20);
            this.fileToolStripMenuItem.Text = "File";
            // 
            // newManifestToolStripMenuItem
            // 
            this.newManifestToolStripMenuItem.Name = "newManifestToolStripMenuItem";
            this.newManifestToolStripMenuItem.Size = new System.Drawing.Size(158, 22);
            this.newManifestToolStripMenuItem.Text = "New Manifest...";
            this.newManifestToolStripMenuItem.Click += new System.EventHandler(this.newManifestToolStripMenuItem_Click);
            // 
            // addFileToolStripMenuItem
            // 
            this.addFileToolStripMenuItem.Name = "addFileToolStripMenuItem";
            this.addFileToolStripMenuItem.Size = new System.Drawing.Size(158, 22);
            this.addFileToolStripMenuItem.Text = "Add Folder...";
            this.addFileToolStripMenuItem.Click += new System.EventHandler(this.addFileToolStripMenuItem_Click);
            // 
            // saveManifestToolStripMenuItem
            // 
            this.saveManifestToolStripMenuItem.Name = "saveManifestToolStripMenuItem";
            this.saveManifestToolStripMenuItem.Size = new System.Drawing.Size(158, 22);
            this.saveManifestToolStripMenuItem.Text = "Save Manifest...";
            this.saveManifestToolStripMenuItem.Click += new System.EventHandler(this.saveManifestToolStripMenuItem_Click);
            // 
            // loadManifestToolStripMenuItem
            // 
            this.loadManifestToolStripMenuItem.Name = "loadManifestToolStripMenuItem";
            this.loadManifestToolStripMenuItem.Size = new System.Drawing.Size(158, 22);
            this.loadManifestToolStripMenuItem.Text = "Load Manifest...";
            // 
            // exitToolStripMenuItem
            // 
            this.exitToolStripMenuItem.Name = "exitToolStripMenuItem";
            this.exitToolStripMenuItem.Size = new System.Drawing.Size(158, 22);
            this.exitToolStripMenuItem.Text = "Exit";
            this.exitToolStripMenuItem.Click += new System.EventHandler(this.exitToolStripMenuItem_Click);
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(13, 117);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(82, 13);
            this.label1.TabIndex = 1;
            this.label1.Text = "Files in Manifest";
            // 
            // LstFiles
            // 
            this.LstFiles.FormattingEnabled = true;
            this.LstFiles.Location = new System.Drawing.Point(16, 133);
            this.LstFiles.Name = "LstFiles";
            this.LstFiles.Size = new System.Drawing.Size(466, 355);
            this.LstFiles.TabIndex = 2;
            // 
            // NumMajor
            // 
            this.NumMajor.Location = new System.Drawing.Point(491, 149);
            this.NumMajor.Name = "NumMajor";
            this.NumMajor.Size = new System.Drawing.Size(35, 20);
            this.NumMajor.TabIndex = 3;
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(488, 133);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(71, 13);
            this.label2.TabIndex = 4;
            this.label2.Text = "Major Version";
            // 
            // NumMinor
            // 
            this.NumMinor.Location = new System.Drawing.Point(564, 149);
            this.NumMinor.Name = "NumMinor";
            this.NumMinor.Size = new System.Drawing.Size(35, 20);
            this.NumMinor.TabIndex = 3;
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(561, 133);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(71, 13);
            this.label3.TabIndex = 4;
            this.label3.Text = "Minor Version";
            // 
            // NumPatch
            // 
            this.NumPatch.Location = new System.Drawing.Point(638, 149);
            this.NumPatch.Name = "NumPatch";
            this.NumPatch.Size = new System.Drawing.Size(35, 20);
            this.NumPatch.TabIndex = 3;
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Location = new System.Drawing.Point(635, 133);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(73, 13);
            this.label4.TabIndex = 4;
            this.label4.Text = "Patch Version";
            // 
            // GrpURLs
            // 
            this.GrpURLs.Controls.Add(this.BtnUpdateURL);
            this.GrpURLs.Controls.Add(this.LblBaseURL);
            this.GrpURLs.Controls.Add(this.TxtBaseURL);
            this.GrpURLs.Location = new System.Drawing.Point(491, 230);
            this.GrpURLs.Name = "GrpURLs";
            this.GrpURLs.Size = new System.Drawing.Size(200, 135);
            this.GrpURLs.TabIndex = 5;
            this.GrpURLs.TabStop = false;
            this.GrpURLs.Text = "URLs";
            // 
            // BtnUpdateURL
            // 
            this.BtnUpdateURL.Location = new System.Drawing.Point(10, 85);
            this.BtnUpdateURL.Name = "BtnUpdateURL";
            this.BtnUpdateURL.Size = new System.Drawing.Size(113, 23);
            this.BtnUpdateURL.TabIndex = 2;
            this.BtnUpdateURL.Text = "Update URLs";
            this.BtnUpdateURL.UseVisualStyleBackColor = true;
            this.BtnUpdateURL.Click += new System.EventHandler(this.BtnUpdateURL_Click);
            // 
            // LblBaseURL
            // 
            this.LblBaseURL.AutoSize = true;
            this.LblBaseURL.Location = new System.Drawing.Point(7, 27);
            this.LblBaseURL.Name = "LblBaseURL";
            this.LblBaseURL.Size = new System.Drawing.Size(53, 13);
            this.LblBaseURL.TabIndex = 1;
            this.LblBaseURL.Text = "BaseURL";
            // 
            // TxtBaseURL
            // 
            this.TxtBaseURL.Location = new System.Drawing.Point(6, 46);
            this.TxtBaseURL.Name = "TxtBaseURL";
            this.TxtBaseURL.Size = new System.Drawing.Size(188, 20);
            this.TxtBaseURL.TabIndex = 0;
            // 
            // helpToolStripMenuItem
            // 
            this.helpToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.usageToolStripMenuItem,
            this.aboutToolStripMenuItem});
            this.helpToolStripMenuItem.Name = "helpToolStripMenuItem";
            this.helpToolStripMenuItem.Size = new System.Drawing.Size(44, 20);
            this.helpToolStripMenuItem.Text = "Help";
            // 
            // usageToolStripMenuItem
            // 
            this.usageToolStripMenuItem.Name = "usageToolStripMenuItem";
            this.usageToolStripMenuItem.Size = new System.Drawing.Size(152, 22);
            this.usageToolStripMenuItem.Text = "Usage...";
            this.usageToolStripMenuItem.Click += new System.EventHandler(this.usageToolStripMenuItem_Click);
            // 
            // aboutToolStripMenuItem
            // 
            this.aboutToolStripMenuItem.Name = "aboutToolStripMenuItem";
            this.aboutToolStripMenuItem.Size = new System.Drawing.Size(152, 22);
            this.aboutToolStripMenuItem.Text = "About...";
            this.aboutToolStripMenuItem.Click += new System.EventHandler(this.aboutToolStripMenuItem_Click);
            // 
            // Form1
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(736, 513);
            this.Controls.Add(this.GrpURLs);
            this.Controls.Add(this.label4);
            this.Controls.Add(this.label3);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.NumPatch);
            this.Controls.Add(this.NumMinor);
            this.Controls.Add(this.NumMajor);
            this.Controls.Add(this.LstFiles);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.menuStrip1);
            this.MainMenuStrip = this.menuStrip1;
            this.Name = "Form1";
            this.Text = "Manifestation";
            this.menuStrip1.ResumeLayout(false);
            this.menuStrip1.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.NumMajor)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.NumMinor)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.NumPatch)).EndInit();
            this.GrpURLs.ResumeLayout(false);
            this.GrpURLs.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.MenuStrip menuStrip1;
        private System.Windows.Forms.ToolStripMenuItem fileToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem newManifestToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem addFileToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem saveManifestToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem loadManifestToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem exitToolStripMenuItem;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.ListBox LstFiles;
        private System.Windows.Forms.NumericUpDown NumMajor;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.NumericUpDown NumMinor;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.NumericUpDown NumPatch;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.GroupBox GrpURLs;
        private System.Windows.Forms.Label LblBaseURL;
        private System.Windows.Forms.TextBox TxtBaseURL;
        private System.Windows.Forms.Button BtnUpdateURL;
        private System.Windows.Forms.ToolStripMenuItem helpToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem usageToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem aboutToolStripMenuItem;
    }
}

