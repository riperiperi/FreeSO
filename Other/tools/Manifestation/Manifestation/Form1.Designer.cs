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
            this.LstFiles = new System.Windows.Forms.ListBox();
            this.LblFiles = new System.Windows.Forms.Label();
            this.menuStrip1 = new System.Windows.Forms.MenuStrip();
            this.fileToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.addFilesToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.openManifestToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.saveManifestToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.addParentToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.addChildToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.exitToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.helpToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.explanationToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.LblParent = new System.Windows.Forms.Label();
            this.LblChild = new System.Windows.Forms.Label();
            this.TxtVersion = new System.Windows.Forms.TextBox();
            this.LblVersion = new System.Windows.Forms.Label();
            this.menuStrip1.SuspendLayout();
            this.SuspendLayout();
            // 
            // LstFiles
            // 
            this.LstFiles.FormattingEnabled = true;
            this.LstFiles.Location = new System.Drawing.Point(13, 116);
            this.LstFiles.Name = "LstFiles";
            this.LstFiles.Size = new System.Drawing.Size(315, 381);
            this.LstFiles.TabIndex = 0;
            this.LstFiles.SelectedIndexChanged += new System.EventHandler(this.LstFiles_SelectedIndexChanged);
            // 
            // LblFiles
            // 
            this.LblFiles.AutoSize = true;
            this.LblFiles.Location = new System.Drawing.Point(10, 88);
            this.LblFiles.Name = "LblFiles";
            this.LblFiles.Size = new System.Drawing.Size(81, 13);
            this.LblFiles.TabIndex = 1;
            this.LblFiles.Text = "Files in manifest";
            // 
            // menuStrip1
            // 
            this.menuStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.fileToolStripMenuItem,
            this.helpToolStripMenuItem});
            this.menuStrip1.Location = new System.Drawing.Point(0, 0);
            this.menuStrip1.Name = "menuStrip1";
            this.menuStrip1.Size = new System.Drawing.Size(568, 24);
            this.menuStrip1.TabIndex = 2;
            this.menuStrip1.Text = "MnuMain";
            // 
            // fileToolStripMenuItem
            // 
            this.fileToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.addFilesToolStripMenuItem,
            this.openManifestToolStripMenuItem,
            this.saveManifestToolStripMenuItem,
            this.addParentToolStripMenuItem,
            this.addChildToolStripMenuItem,
            this.exitToolStripMenuItem});
            this.fileToolStripMenuItem.Name = "fileToolStripMenuItem";
            this.fileToolStripMenuItem.Size = new System.Drawing.Size(37, 20);
            this.fileToolStripMenuItem.Text = "File";
            // 
            // addFilesToolStripMenuItem
            // 
            this.addFilesToolStripMenuItem.Name = "addFilesToolStripMenuItem";
            this.addFilesToolStripMenuItem.Size = new System.Drawing.Size(195, 22);
            this.addFilesToolStripMenuItem.Text = "Add files to manifest... ";
            this.addFilesToolStripMenuItem.Click += new System.EventHandler(this.addFilesToolStripMenuItem_Click);
            // 
            // openManifestToolStripMenuItem
            // 
            this.openManifestToolStripMenuItem.Name = "openManifestToolStripMenuItem";
            this.openManifestToolStripMenuItem.Size = new System.Drawing.Size(195, 22);
            this.openManifestToolStripMenuItem.Text = "Open manifest...";
            this.openManifestToolStripMenuItem.Click += new System.EventHandler(this.openManifestToolStripMenuItem_Click);
            // 
            // saveManifestToolStripMenuItem
            // 
            this.saveManifestToolStripMenuItem.Name = "saveManifestToolStripMenuItem";
            this.saveManifestToolStripMenuItem.Size = new System.Drawing.Size(195, 22);
            this.saveManifestToolStripMenuItem.Text = "Save manifest...";
            this.saveManifestToolStripMenuItem.Click += new System.EventHandler(this.saveManifestToolStripMenuItem_Click);
            // 
            // addParentToolStripMenuItem
            // 
            this.addParentToolStripMenuItem.Name = "addParentToolStripMenuItem";
            this.addParentToolStripMenuItem.Size = new System.Drawing.Size(195, 22);
            this.addParentToolStripMenuItem.Text = "Add parent...";
            this.addParentToolStripMenuItem.Click += new System.EventHandler(this.addParentToolStripMenuItem_Click);
            // 
            // addChildToolStripMenuItem
            // 
            this.addChildToolStripMenuItem.Name = "addChildToolStripMenuItem";
            this.addChildToolStripMenuItem.Size = new System.Drawing.Size(195, 22);
            this.addChildToolStripMenuItem.Text = "Add child...";
            this.addChildToolStripMenuItem.Click += new System.EventHandler(this.addChildToolStripMenuItem_Click);
            // 
            // exitToolStripMenuItem
            // 
            this.exitToolStripMenuItem.Name = "exitToolStripMenuItem";
            this.exitToolStripMenuItem.Size = new System.Drawing.Size(195, 22);
            this.exitToolStripMenuItem.Text = "Exit";
            this.exitToolStripMenuItem.Click += new System.EventHandler(this.exitToolStripMenuItem_Click);
            // 
            // helpToolStripMenuItem
            // 
            this.helpToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.explanationToolStripMenuItem});
            this.helpToolStripMenuItem.Name = "helpToolStripMenuItem";
            this.helpToolStripMenuItem.Size = new System.Drawing.Size(44, 20);
            this.helpToolStripMenuItem.Text = "Help";
            // 
            // explanationToolStripMenuItem
            // 
            this.explanationToolStripMenuItem.Name = "explanationToolStripMenuItem";
            this.explanationToolStripMenuItem.Size = new System.Drawing.Size(144, 22);
            this.explanationToolStripMenuItem.Text = "Explanation...";
            this.explanationToolStripMenuItem.Click += new System.EventHandler(this.explanationToolStripMenuItem_Click);
            // 
            // LblParent
            // 
            this.LblParent.AutoSize = true;
            this.LblParent.Location = new System.Drawing.Point(359, 116);
            this.LblParent.Name = "LblParent";
            this.LblParent.Size = new System.Drawing.Size(90, 13);
            this.LblParent.TabIndex = 3;
            this.LblParent.Text = "Manifest\'s parent:";
            // 
            // LblChild
            // 
            this.LblChild.AutoSize = true;
            this.LblChild.Location = new System.Drawing.Point(359, 141);
            this.LblChild.Name = "LblChild";
            this.LblChild.Size = new System.Drawing.Size(82, 13);
            this.LblChild.TabIndex = 3;
            this.LblChild.Text = "Manifest\'s child:";
            // 
            // TxtVersion
            // 
            this.TxtVersion.Location = new System.Drawing.Point(456, 161);
            this.TxtVersion.Name = "TxtVersion";
            this.TxtVersion.Size = new System.Drawing.Size(100, 20);
            this.TxtVersion.TabIndex = 4;
            // 
            // LblVersion
            // 
            this.LblVersion.AutoSize = true;
            this.LblVersion.Location = new System.Drawing.Point(359, 168);
            this.LblVersion.Name = "LblVersion";
            this.LblVersion.Size = new System.Drawing.Size(91, 13);
            this.LblVersion.TabIndex = 3;
            this.LblVersion.Text = "Manifest\'s version";
            // 
            // Form1
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(568, 518);
            this.Controls.Add(this.TxtVersion);
            this.Controls.Add(this.LblVersion);
            this.Controls.Add(this.LblChild);
            this.Controls.Add(this.LblParent);
            this.Controls.Add(this.LblFiles);
            this.Controls.Add(this.LstFiles);
            this.Controls.Add(this.menuStrip1);
            this.MainMenuStrip = this.menuStrip1;
            this.Name = "Form1";
            this.Text = "Manifestation";
            this.menuStrip1.ResumeLayout(false);
            this.menuStrip1.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.ListBox LstFiles;
        private System.Windows.Forms.Label LblFiles;
        private System.Windows.Forms.MenuStrip menuStrip1;
        private System.Windows.Forms.ToolStripMenuItem fileToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem addFilesToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem saveManifestToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem exitToolStripMenuItem;
        private System.Windows.Forms.Label LblParent;
        private System.Windows.Forms.Label LblChild;
        private System.Windows.Forms.ToolStripMenuItem addParentToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem addChildToolStripMenuItem;
        private System.Windows.Forms.TextBox TxtVersion;
        private System.Windows.Forms.Label LblVersion;
        private System.Windows.Forms.ToolStripMenuItem helpToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem explanationToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem openManifestToolStripMenuItem;
    }
}

