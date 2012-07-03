namespace Iffinator
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
            this.components = new System.ComponentModel.Container();
            this.menuStrip1 = new System.Windows.Forms.MenuStrip();
            this.fileToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.openiffToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.extractiffToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.extractImageSpritesToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.exitToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.analyzeToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.chunkAnalyzerToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.helpToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.aboutToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.LblNumChunks = new System.Windows.Forms.Label();
            this.PictCurrentFrame = new System.Windows.Forms.PictureBox();
            this.BtnPrevFrame = new System.Windows.Forms.Button();
            this.BtnNextFrame = new System.Windows.Forms.Button();
            this.LstSPR2s = new System.Windows.Forms.ListBox();
            this.RdiSpr2 = new System.Windows.Forms.RadioButton();
            this.RdiStr = new System.Windows.Forms.RadioButton();
            this.RdiDgrp = new System.Windows.Forms.RadioButton();
            this.RdiBhavs = new System.Windows.Forms.RadioButton();
            this.RdiSPR = new System.Windows.Forms.RadioButton();
            this.contextMenuStrip1 = new System.Windows.Forms.ContextMenuStrip(this.components);
            this.ChkZBuffer = new System.Windows.Forms.CheckBox();
            this.menuStrip1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.PictCurrentFrame)).BeginInit();
            this.SuspendLayout();
            // 
            // menuStrip1
            // 
            this.menuStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.fileToolStripMenuItem,
            this.analyzeToolStripMenuItem,
            this.helpToolStripMenuItem});
            this.menuStrip1.Location = new System.Drawing.Point(0, 0);
            this.menuStrip1.Name = "menuStrip1";
            this.menuStrip1.Size = new System.Drawing.Size(624, 24);
            this.menuStrip1.TabIndex = 0;
            this.menuStrip1.Text = "menuStrip1";
            // 
            // fileToolStripMenuItem
            // 
            this.fileToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.openiffToolStripMenuItem,
            this.extractiffToolStripMenuItem,
            this.extractImageSpritesToolStripMenuItem,
            this.exitToolStripMenuItem});
            this.fileToolStripMenuItem.Name = "fileToolStripMenuItem";
            this.fileToolStripMenuItem.Size = new System.Drawing.Size(37, 20);
            this.fileToolStripMenuItem.Text = "File";
            // 
            // openiffToolStripMenuItem
            // 
            this.openiffToolStripMenuItem.Name = "openiffToolStripMenuItem";
            this.openiffToolStripMenuItem.Size = new System.Drawing.Size(192, 22);
            this.openiffToolStripMenuItem.Text = "Open *.iff...";
            this.openiffToolStripMenuItem.Click += new System.EventHandler(this.openiffToolStripMenuItem_Click);
            // 
            // extractiffToolStripMenuItem
            // 
            this.extractiffToolStripMenuItem.Name = "extractiffToolStripMenuItem";
            this.extractiffToolStripMenuItem.Size = new System.Drawing.Size(192, 22);
            this.extractiffToolStripMenuItem.Text = "Extract *.iff...";
            this.extractiffToolStripMenuItem.Click += new System.EventHandler(this.extractiffToolStripMenuItem_Click);
            // 
            // extractImageSpritesToolStripMenuItem
            // 
            this.extractImageSpritesToolStripMenuItem.Name = "extractImageSpritesToolStripMenuItem";
            this.extractImageSpritesToolStripMenuItem.Size = new System.Drawing.Size(192, 22);
            this.extractImageSpritesToolStripMenuItem.Text = "Extract Image Sprites...";
            this.extractImageSpritesToolStripMenuItem.Click += new System.EventHandler(this.extractImageSpritesToolStripMenuItem_Click);
            // 
            // exitToolStripMenuItem
            // 
            this.exitToolStripMenuItem.Name = "exitToolStripMenuItem";
            this.exitToolStripMenuItem.Size = new System.Drawing.Size(192, 22);
            this.exitToolStripMenuItem.Text = "Exit";
            this.exitToolStripMenuItem.Click += new System.EventHandler(this.exitToolStripMenuItem_Click);
            // 
            // analyzeToolStripMenuItem
            // 
            this.analyzeToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.chunkAnalyzerToolStripMenuItem});
            this.analyzeToolStripMenuItem.Name = "analyzeToolStripMenuItem";
            this.analyzeToolStripMenuItem.Size = new System.Drawing.Size(60, 20);
            this.analyzeToolStripMenuItem.Text = "Analyze";
            // 
            // chunkAnalyzerToolStripMenuItem
            // 
            this.chunkAnalyzerToolStripMenuItem.Name = "chunkAnalyzerToolStripMenuItem";
            this.chunkAnalyzerToolStripMenuItem.Size = new System.Drawing.Size(155, 22);
            this.chunkAnalyzerToolStripMenuItem.Text = "Chunk analyzer";
            this.chunkAnalyzerToolStripMenuItem.Click += new System.EventHandler(this.chunkAnalyzerToolStripMenuItem_Click);
            // 
            // helpToolStripMenuItem
            // 
            this.helpToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.aboutToolStripMenuItem});
            this.helpToolStripMenuItem.Name = "helpToolStripMenuItem";
            this.helpToolStripMenuItem.Size = new System.Drawing.Size(44, 20);
            this.helpToolStripMenuItem.Text = "Help";
            // 
            // aboutToolStripMenuItem
            // 
            this.aboutToolStripMenuItem.Name = "aboutToolStripMenuItem";
            this.aboutToolStripMenuItem.Size = new System.Drawing.Size(116, 22);
            this.aboutToolStripMenuItem.Text = "About...";
            this.aboutToolStripMenuItem.Click += new System.EventHandler(this.aboutToolStripMenuItem_Click);
            // 
            // LblNumChunks
            // 
            this.LblNumChunks.AutoSize = true;
            this.LblNumChunks.Location = new System.Drawing.Point(13, 44);
            this.LblNumChunks.Name = "LblNumChunks";
            this.LblNumChunks.Size = new System.Drawing.Size(97, 13);
            this.LblNumChunks.TabIndex = 1;
            this.LblNumChunks.Text = "Number of chunks:";
            this.LblNumChunks.Visible = false;
            // 
            // PictCurrentFrame
            // 
            this.PictCurrentFrame.Location = new System.Drawing.Point(12, 168);
            this.PictCurrentFrame.Name = "PictCurrentFrame";
            this.PictCurrentFrame.Size = new System.Drawing.Size(216, 218);
            this.PictCurrentFrame.SizeMode = System.Windows.Forms.PictureBoxSizeMode.Zoom;
            this.PictCurrentFrame.TabIndex = 2;
            this.PictCurrentFrame.TabStop = false;
            // 
            // BtnPrevFrame
            // 
            this.BtnPrevFrame.Location = new System.Drawing.Point(12, 392);
            this.BtnPrevFrame.Name = "BtnPrevFrame";
            this.BtnPrevFrame.Size = new System.Drawing.Size(30, 22);
            this.BtnPrevFrame.TabIndex = 3;
            this.BtnPrevFrame.Text = "<";
            this.BtnPrevFrame.UseVisualStyleBackColor = true;
            this.BtnPrevFrame.Click += new System.EventHandler(this.BtnPrevFrame_Click);
            // 
            // BtnNextFrame
            // 
            this.BtnNextFrame.Location = new System.Drawing.Point(119, 392);
            this.BtnNextFrame.Name = "BtnNextFrame";
            this.BtnNextFrame.Size = new System.Drawing.Size(30, 22);
            this.BtnNextFrame.TabIndex = 3;
            this.BtnNextFrame.Text = ">";
            this.BtnNextFrame.UseVisualStyleBackColor = true;
            this.BtnNextFrame.Click += new System.EventHandler(this.BtnNextFrame_Click);
            // 
            // LstSPR2s
            // 
            this.LstSPR2s.FormattingEnabled = true;
            this.LstSPR2s.Location = new System.Drawing.Point(238, 46);
            this.LstSPR2s.Name = "LstSPR2s";
            this.LstSPR2s.Size = new System.Drawing.Size(374, 368);
            this.LstSPR2s.TabIndex = 4;
            this.LstSPR2s.SelectedValueChanged += new System.EventHandler(this.LstSPR2s_SelectedValueChanged);
            // 
            // RdiSpr2
            // 
            this.RdiSpr2.AutoSize = true;
            this.RdiSpr2.Checked = true;
            this.RdiSpr2.Location = new System.Drawing.Point(174, 76);
            this.RdiSpr2.Name = "RdiSpr2";
            this.RdiSpr2.Size = new System.Drawing.Size(58, 17);
            this.RdiSpr2.TabIndex = 6;
            this.RdiSpr2.TabStop = true;
            this.RdiSpr2.Text = "SPR2s";
            this.RdiSpr2.UseVisualStyleBackColor = true;
            this.RdiSpr2.CheckedChanged += new System.EventHandler(this.RdiSpr2_CheckedChanged);
            // 
            // RdiStr
            // 
            this.RdiStr.AutoSize = true;
            this.RdiStr.Location = new System.Drawing.Point(173, 122);
            this.RdiStr.Name = "RdiStr";
            this.RdiStr.Size = new System.Drawing.Size(59, 17);
            this.RdiStr.TabIndex = 7;
            this.RdiStr.Text = "STR#s";
            this.RdiStr.UseVisualStyleBackColor = true;
            this.RdiStr.CheckedChanged += new System.EventHandler(this.RdiStr_CheckedChanged);
            // 
            // RdiDgrp
            // 
            this.RdiDgrp.AutoSize = true;
            this.RdiDgrp.Location = new System.Drawing.Point(174, 99);
            this.RdiDgrp.Name = "RdiDgrp";
            this.RdiDgrp.Size = new System.Drawing.Size(61, 17);
            this.RdiDgrp.TabIndex = 8;
            this.RdiDgrp.TabStop = true;
            this.RdiDgrp.Text = "DGRPs";
            this.RdiDgrp.UseVisualStyleBackColor = true;
            this.RdiDgrp.CheckedChanged += new System.EventHandler(this.RdiDgrp_CheckedChanged);
            // 
            // RdiBhavs
            // 
            this.RdiBhavs.AutoSize = true;
            this.RdiBhavs.Location = new System.Drawing.Point(173, 145);
            this.RdiBhavs.Name = "RdiBhavs";
            this.RdiBhavs.Size = new System.Drawing.Size(59, 17);
            this.RdiBhavs.TabIndex = 7;
            this.RdiBhavs.Text = "BHAVs";
            this.RdiBhavs.UseVisualStyleBackColor = true;
            this.RdiBhavs.CheckedChanged += new System.EventHandler(this.RdiBhavs_CheckedChanged);
            // 
            // RdiSPR
            // 
            this.RdiSPR.AutoSize = true;
            this.RdiSPR.Location = new System.Drawing.Point(173, 53);
            this.RdiSPR.Name = "RdiSPR";
            this.RdiSPR.Size = new System.Drawing.Size(52, 17);
            this.RdiSPR.TabIndex = 9;
            this.RdiSPR.TabStop = true;
            this.RdiSPR.Text = "SPRs";
            this.RdiSPR.UseVisualStyleBackColor = true;
            this.RdiSPR.CheckedChanged += new System.EventHandler(this.RdiSPR_CheckedChanged);
            // 
            // contextMenuStrip1
            // 
            this.contextMenuStrip1.Name = "contextMenuStrip1";
            this.contextMenuStrip1.Size = new System.Drawing.Size(61, 4);
            // 
            // ChkZBuffer
            // 
            this.ChkZBuffer.AutoSize = true;
            this.ChkZBuffer.Location = new System.Drawing.Point(12, 145);
            this.ChkZBuffer.Name = "ChkZBuffer";
            this.ChkZBuffer.Size = new System.Drawing.Size(87, 17);
            this.ChkZBuffer.TabIndex = 10;
            this.ChkZBuffer.Text = "View z-buffer";
            this.ChkZBuffer.UseVisualStyleBackColor = true;
            // 
            // Form1
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(624, 427);
            this.Controls.Add(this.ChkZBuffer);
            this.Controls.Add(this.RdiSPR);
            this.Controls.Add(this.RdiDgrp);
            this.Controls.Add(this.RdiBhavs);
            this.Controls.Add(this.RdiStr);
            this.Controls.Add(this.RdiSpr2);
            this.Controls.Add(this.LstSPR2s);
            this.Controls.Add(this.BtnNextFrame);
            this.Controls.Add(this.BtnPrevFrame);
            this.Controls.Add(this.PictCurrentFrame);
            this.Controls.Add(this.LblNumChunks);
            this.Controls.Add(this.menuStrip1);
            this.MainMenuStrip = this.menuStrip1;
            this.Name = "Form1";
            this.Text = "Iffinator";
            this.menuStrip1.ResumeLayout(false);
            this.menuStrip1.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.PictCurrentFrame)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.MenuStrip menuStrip1;
        private System.Windows.Forms.ToolStripMenuItem fileToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem openiffToolStripMenuItem;
        private System.Windows.Forms.Label LblNumChunks;
        private System.Windows.Forms.ToolStripMenuItem extractiffToolStripMenuItem;
        private System.Windows.Forms.PictureBox PictCurrentFrame;
        private System.Windows.Forms.Button BtnPrevFrame;
        private System.Windows.Forms.Button BtnNextFrame;
        private System.Windows.Forms.ListBox LstSPR2s;
        private System.Windows.Forms.RadioButton RdiSpr2;
        private System.Windows.Forms.RadioButton RdiStr;
        private System.Windows.Forms.RadioButton RdiDgrp;
        private System.Windows.Forms.ToolStripMenuItem extractImageSpritesToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem helpToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem aboutToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem exitToolStripMenuItem;
        private System.Windows.Forms.RadioButton RdiBhavs;
        private System.Windows.Forms.RadioButton RdiSPR;
        private System.Windows.Forms.ToolStripMenuItem analyzeToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem chunkAnalyzerToolStripMenuItem;
        private System.Windows.Forms.ContextMenuStrip contextMenuStrip1;
        private System.Windows.Forms.CheckBox ChkZBuffer;
    }
}

