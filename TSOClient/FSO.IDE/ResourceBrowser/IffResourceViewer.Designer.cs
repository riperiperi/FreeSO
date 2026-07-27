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
            iffRes = new IFFResComponent();
            menuStrip1 = new MenuStrip();
            fileToolStripMenuItem = new ToolStripMenuItem();
            saveIFFToolStripMenuItem = new ToolStripMenuItem();
            viewToolStripMenuItem = new ToolStripMenuItem();
            resourcesToolStripMenuItem = new ToolStripMenuItem();
            patchesPIFFToolStripMenuItem = new ToolStripMenuItem();
            piffEditor = new PIFFEditor();
            menuStrip1.SuspendLayout();
            SuspendLayout();
            // 
            // iffRes
            // 
            iffRes.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            iffRes.Location = new Point(3, 28);
            iffRes.Margin = new Padding(4, 3, 4, 3);
            iffRes.Name = "iffRes";
            iffRes.Size = new Size(762, 459);
            iffRes.TabIndex = 0;
            // 
            // menuStrip1
            // 
            menuStrip1.Items.AddRange(new ToolStripItem[] { fileToolStripMenuItem, viewToolStripMenuItem });
            menuStrip1.Location = new Point(0, 0);
            menuStrip1.Name = "menuStrip1";
            menuStrip1.Size = new Size(768, 24);
            menuStrip1.TabIndex = 1;
            menuStrip1.Text = "menuStrip1";
            // 
            // fileToolStripMenuItem
            // 
            fileToolStripMenuItem.DropDownItems.AddRange(new ToolStripItem[] { saveIFFToolStripMenuItem });
            fileToolStripMenuItem.Name = "fileToolStripMenuItem";
            fileToolStripMenuItem.Size = new Size(37, 20);
            fileToolStripMenuItem.Text = "File";
            // 
            // saveIFFToolStripMenuItem
            // 
            saveIFFToolStripMenuItem.Name = "saveIFFToolStripMenuItem";
            saveIFFToolStripMenuItem.Size = new Size(116, 22);
            saveIFFToolStripMenuItem.Text = "Save IFF";
            // 
            // viewToolStripMenuItem
            // 
            viewToolStripMenuItem.DropDownItems.AddRange(new ToolStripItem[] { resourcesToolStripMenuItem, patchesPIFFToolStripMenuItem });
            viewToolStripMenuItem.Name = "viewToolStripMenuItem";
            viewToolStripMenuItem.Size = new Size(44, 20);
            viewToolStripMenuItem.Text = "View";
            // 
            // resourcesToolStripMenuItem
            // 
            resourcesToolStripMenuItem.Name = "resourcesToolStripMenuItem";
            resourcesToolStripMenuItem.Size = new Size(148, 22);
            resourcesToolStripMenuItem.Text = "Resources";
            resourcesToolStripMenuItem.Click += resourcesToolStripMenuItem_Click;
            // 
            // patchesPIFFToolStripMenuItem
            // 
            patchesPIFFToolStripMenuItem.Name = "patchesPIFFToolStripMenuItem";
            patchesPIFFToolStripMenuItem.Size = new Size(148, 22);
            patchesPIFFToolStripMenuItem.Text = "Patches (PIFF)";
            patchesPIFFToolStripMenuItem.Click += patchesPIFFToolStripMenuItem_Click;
            // 
            // piffEditor
            // 
            piffEditor.Location = new Point(3, 27);
            piffEditor.Margin = new Padding(4, 3, 4, 3);
            piffEditor.Name = "piffEditor";
            piffEditor.Size = new Size(762, 459);
            piffEditor.TabIndex = 2;
            piffEditor.Visible = false;
            // 
            // IffResourceViewer
            // 
            AutoScaleDimensions = new SizeF(96F, 96F);
            AutoScaleMode = AutoScaleMode.Dpi;
            ClientSize = new Size(768, 489);
            Controls.Add(piffEditor);
            Controls.Add(iffRes);
            Controls.Add(menuStrip1);
            FormBorderStyle = FormBorderStyle.FixedSingle;
            Icon = (Icon)resources.GetObject("$this.Icon");
            MainMenuStrip = menuStrip1;
            MaximizeBox = false;
            MinimumSize = new Size(784, 504);
            Name = "IffResourceViewer";
            Text = "Edit Iff - globals";
            FormClosing += IffResourceViewer_FormClosing;
            menuStrip1.ResumeLayout(false);
            menuStrip1.PerformLayout();
            ResumeLayout(false);
            PerformLayout();

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