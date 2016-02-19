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
            this.piffDebugButton = new System.Windows.Forms.ToolStripMenuItem();
            this.menuStrip1.SuspendLayout();
            this.SuspendLayout();
            // 
            // iffRes
            // 
            this.iffRes.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.iffRes.Location = new System.Drawing.Point(3, 25);
            this.iffRes.Name = "iffRes";
            this.iffRes.Size = new System.Drawing.Size(762, 450);
            this.iffRes.TabIndex = 0;
            // 
            // menuStrip1
            // 
            this.menuStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.piffDebugButton});
            this.menuStrip1.Location = new System.Drawing.Point(0, 0);
            this.menuStrip1.Name = "menuStrip1";
            this.menuStrip1.Size = new System.Drawing.Size(768, 24);
            this.menuStrip1.TabIndex = 1;
            this.menuStrip1.Text = "menuStrip1";
            // 
            // piffDebugButton
            // 
            this.piffDebugButton.Name = "piffDebugButton";
            this.piffDebugButton.Size = new System.Drawing.Size(112, 20);
            this.piffDebugButton.Text = "Save .piff (debug)";
            this.piffDebugButton.Click += new System.EventHandler(this.piffDebugButton_Click);
            // 
            // IffResourceViewer
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(768, 478);
            this.Controls.Add(this.iffRes);
            this.Controls.Add(this.menuStrip1);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.MainMenuStrip = this.menuStrip1;
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
        private System.Windows.Forms.ToolStripMenuItem piffDebugButton;
    }
}