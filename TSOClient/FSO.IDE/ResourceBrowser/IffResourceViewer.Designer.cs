namespace FSO.IDE.ResourceBrowser
{
    partial class IffResourceViewer
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(IffResourceViewer));
            this.iffRes = new FSO.IDE.ResourceBrowser.IFFResComponent();
            this.menuStrip1 = new System.Windows.Forms.MenuStrip();
            this.fileToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.viewToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.resourcesToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.patchesPIFFToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.saveIFFToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.piffEditor = new FSO.IDE.ResourceBrowser.PIFFEditor();
            this.menuStrip1.SuspendLayout();
            this.SuspendLayout();
            // 
            // iffRes
            // 
            this.iffRes.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.iffRes.Location = new System.Drawing.Point(3, 28);
            this.iffRes.Name = "iffRes";
            this.iffRes.Size = new System.Drawing.Size(762, 459);
            this.iffRes.TabIndex = 0;
            // 
            // menuStrip1
            // 
            this.menuStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.fileToolStripMenuItem,
            this.viewToolStripMenuItem});
            this.menuStrip1.Location = new System.Drawing.Point(0, 0);
            this.menuStrip1.Name = "menuStrip1";
            this.menuStrip1.Size = new System.Drawing.Size(768, 24);
            this.menuStrip1.TabIndex = 1;
            this.menuStrip1.Text = "menuStrip1";
            // 
            // fileToolStripMenuItem
            // 
            this.fileToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.saveIFFToolStripMenuItem});
            this.fileToolStripMenuItem.Name = "fileToolStripMenuItem";
            this.fileToolStripMenuItem.Size = new System.Drawing.Size(37, 20);
            this.fileToolStripMenuItem.Text = "File";
            // 
            // viewToolStripMenuItem
            // 
            this.viewToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.resourcesToolStripMenuItem,
            this.patchesPIFFToolStripMenuItem});
            this.viewToolStripMenuItem.Name = "viewToolStripMenuItem";
            this.viewToolStripMenuItem.Size = new System.Drawing.Size(44, 20);
            this.viewToolStripMenuItem.Text = "View";
            // 
            // resourcesToolStripMenuItem
            // 
            this.resourcesToolStripMenuItem.Name = "resourcesToolStripMenuItem";
            this.resourcesToolStripMenuItem.Size = new System.Drawing.Size(180, 22);
            this.resourcesToolStripMenuItem.Text = "Resources";
            this.resourcesToolStripMenuItem.Click += new System.EventHandler(this.resourcesToolStripMenuItem_Click);
            // 
            // patchesPIFFToolStripMenuItem
            // 
            this.patchesPIFFToolStripMenuItem.Name = "patchesPIFFToolStripMenuItem";
            this.patchesPIFFToolStripMenuItem.Size = new System.Drawing.Size(180, 22);
            this.patchesPIFFToolStripMenuItem.Text = "Patches (PIFF)";
            this.patchesPIFFToolStripMenuItem.Click += new System.EventHandler(this.patchesPIFFToolStripMenuItem_Click);
            // 
            // saveIFFToolStripMenuItem
            // 
            this.saveIFFToolStripMenuItem.Name = "saveIFFToolStripMenuItem";
            this.saveIFFToolStripMenuItem.Size = new System.Drawing.Size(180, 22);
            this.saveIFFToolStripMenuItem.Text = "Save IFF";
            // 
            // piffEditor
            // 
            this.piffEditor.Location = new System.Drawing.Point(3, 27);
            this.piffEditor.Name = "piffEditor";
            this.piffEditor.Size = new System.Drawing.Size(762, 459);
            this.piffEditor.TabIndex = 2;
            this.piffEditor.Visible = false;
            // 
            // IffResourceViewer
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(768, 489);
            this.Controls.Add(this.piffEditor);
            this.Controls.Add(this.iffRes);
            this.Controls.Add(this.menuStrip1);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.MainMenuStrip = this.menuStrip1;
            this.MaximizeBox = false;
            this.MinimumSize = new System.Drawing.Size(784, 504);
            this.Name = "IffResourceViewer";
            this.Text = "Edit Iff - globals";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.IffResourceViewer_FormClosing);
            this.menuStrip1.ResumeLayout(false);
            this.menuStrip1.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private IFFResComponent iffRes;
        private System.Windows.Forms.MenuStrip menuStrip1;
        private System.Windows.Forms.ToolStripMenuItem fileToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem viewToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem saveIFFToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem resourcesToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem patchesPIFFToolStripMenuItem;
        private PIFFEditor piffEditor;
    }
}