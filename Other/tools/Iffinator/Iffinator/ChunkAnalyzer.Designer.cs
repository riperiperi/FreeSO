namespace Iffinator
{
    partial class ChunkAnalyzer
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
            this.LblTSOPath = new System.Windows.Forms.Label();
            this.TxtChunkType = new System.Windows.Forms.TextBox();
            this.LblChunkType = new System.Windows.Forms.Label();
            this.TxtTSOPath = new System.Windows.Forms.TextBox();
            this.LstChunkTypes = new System.Windows.Forms.ListBox();
            this.LstChunkInfo = new System.Windows.Forms.ListBox();
            this.BtnAnalyze = new System.Windows.Forms.Button();
            this.menuStrip1 = new System.Windows.Forms.MenuStrip();
            this.helpToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.whatIsThisToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.LblScanning = new System.Windows.Forms.Label();
            this.BtnAbort = new System.Windows.Forms.Button();
            this.menuStrip1.SuspendLayout();
            this.SuspendLayout();
            // 
            // LblTSOPath
            // 
            this.LblTSOPath.AutoSize = true;
            this.LblTSOPath.Location = new System.Drawing.Point(13, 50);
            this.LblTSOPath.Name = "LblTSOPath";
            this.LblTSOPath.Size = new System.Drawing.Size(56, 13);
            this.LblTSOPath.TabIndex = 0;
            this.LblTSOPath.Text = "TSO path:";
            // 
            // TxtChunkType
            // 
            this.TxtChunkType.Location = new System.Drawing.Point(16, 179);
            this.TxtChunkType.Name = "TxtChunkType";
            this.TxtChunkType.Size = new System.Drawing.Size(100, 20);
            this.TxtChunkType.TabIndex = 1;
            // 
            // LblChunkType
            // 
            this.LblChunkType.AutoSize = true;
            this.LblChunkType.Location = new System.Drawing.Point(13, 163);
            this.LblChunkType.Name = "LblChunkType";
            this.LblChunkType.Size = new System.Drawing.Size(91, 13);
            this.LblChunkType.TabIndex = 2;
            this.LblChunkType.Text = "Chunk to look for:";
            // 
            // TxtTSOPath
            // 
            this.TxtTSOPath.Location = new System.Drawing.Point(12, 66);
            this.TxtTSOPath.Name = "TxtTSOPath";
            this.TxtTSOPath.Size = new System.Drawing.Size(145, 20);
            this.TxtTSOPath.TabIndex = 1;
            this.TxtTSOPath.Visible = false;
            // 
            // LstChunkTypes
            // 
            this.LstChunkTypes.FormattingEnabled = true;
            this.LstChunkTypes.Location = new System.Drawing.Point(320, 29);
            this.LstChunkTypes.Name = "LstChunkTypes";
            this.LstChunkTypes.Size = new System.Drawing.Size(205, 316);
            this.LstChunkTypes.TabIndex = 3;
            this.LstChunkTypes.SelectedIndexChanged += new System.EventHandler(this.LstChunkTypes_SelectedIndexChanged);
            // 
            // LstChunkInfo
            // 
            this.LstChunkInfo.FormattingEnabled = true;
            this.LstChunkInfo.Location = new System.Drawing.Point(541, 29);
            this.LstChunkInfo.Name = "LstChunkInfo";
            this.LstChunkInfo.Size = new System.Drawing.Size(220, 199);
            this.LstChunkInfo.TabIndex = 4;
            // 
            // BtnAnalyze
            // 
            this.BtnAnalyze.Location = new System.Drawing.Point(19, 320);
            this.BtnAnalyze.Name = "BtnAnalyze";
            this.BtnAnalyze.Size = new System.Drawing.Size(75, 23);
            this.BtnAnalyze.TabIndex = 5;
            this.BtnAnalyze.Text = "Analyze";
            this.BtnAnalyze.UseVisualStyleBackColor = true;
            this.BtnAnalyze.Click += new System.EventHandler(this.button1_Click);
            // 
            // menuStrip1
            // 
            this.menuStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.helpToolStripMenuItem});
            this.menuStrip1.Location = new System.Drawing.Point(0, 0);
            this.menuStrip1.Name = "menuStrip1";
            this.menuStrip1.Size = new System.Drawing.Size(773, 24);
            this.menuStrip1.TabIndex = 6;
            this.menuStrip1.Text = "menuStrip1";
            // 
            // helpToolStripMenuItem
            // 
            this.helpToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.whatIsThisToolStripMenuItem});
            this.helpToolStripMenuItem.Name = "helpToolStripMenuItem";
            this.helpToolStripMenuItem.Size = new System.Drawing.Size(44, 20);
            this.helpToolStripMenuItem.Text = "Help";
            // 
            // whatIsThisToolStripMenuItem
            // 
            this.whatIsThisToolStripMenuItem.Name = "whatIsThisToolStripMenuItem";
            this.whatIsThisToolStripMenuItem.Size = new System.Drawing.Size(140, 22);
            this.whatIsThisToolStripMenuItem.Text = "What is this?";
            this.whatIsThisToolStripMenuItem.Click += new System.EventHandler(this.whatIsThisToolStripMenuItem_Click);
            // 
            // LblScanning
            // 
            this.LblScanning.AutoSize = true;
            this.LblScanning.Location = new System.Drawing.Point(16, 105);
            this.LblScanning.Name = "LblScanning";
            this.LblScanning.Size = new System.Drawing.Size(55, 13);
            this.LblScanning.TabIndex = 7;
            this.LblScanning.Text = "Scanning:";
            this.LblScanning.Visible = false;
            // 
            // BtnAbort
            // 
            this.BtnAbort.Location = new System.Drawing.Point(100, 320);
            this.BtnAbort.Name = "BtnAbort";
            this.BtnAbort.Size = new System.Drawing.Size(75, 23);
            this.BtnAbort.TabIndex = 5;
            this.BtnAbort.Text = "Abort";
            this.BtnAbort.UseVisualStyleBackColor = true;
            this.BtnAbort.Visible = false;
            this.BtnAbort.Click += new System.EventHandler(this.BtnAbort_Click);
            // 
            // ChunkAnalyzer
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(773, 355);
            this.Controls.Add(this.LblScanning);
            this.Controls.Add(this.BtnAbort);
            this.Controls.Add(this.BtnAnalyze);
            this.Controls.Add(this.LstChunkInfo);
            this.Controls.Add(this.LstChunkTypes);
            this.Controls.Add(this.LblChunkType);
            this.Controls.Add(this.TxtTSOPath);
            this.Controls.Add(this.TxtChunkType);
            this.Controls.Add(this.LblTSOPath);
            this.Controls.Add(this.menuStrip1);
            this.MainMenuStrip = this.menuStrip1;
            this.Name = "ChunkAnalyzer";
            this.Text = "ChunkAnalyzer";
            this.menuStrip1.ResumeLayout(false);
            this.menuStrip1.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Label LblTSOPath;
        private System.Windows.Forms.TextBox TxtChunkType;
        private System.Windows.Forms.Label LblChunkType;
        private System.Windows.Forms.TextBox TxtTSOPath;
        private System.Windows.Forms.ListBox LstChunkTypes;
        private System.Windows.Forms.ListBox LstChunkInfo;
        private System.Windows.Forms.Button BtnAnalyze;
        private System.Windows.Forms.MenuStrip menuStrip1;
        private System.Windows.Forms.ToolStripMenuItem helpToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem whatIsThisToolStripMenuItem;
        private System.Windows.Forms.Label LblScanning;
        private System.Windows.Forms.Button BtnAbort;
    }
}