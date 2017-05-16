namespace FSO.Server.Debug
{
    partial class PacketView
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

        #region Component Designer generated code

        /// <summary> 
        /// Required method for Designer support - do not modify 
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(PacketView));
            this.toolStrip1 = new System.Windows.Forms.ToolStrip();
            this.btnClose = new System.Windows.Forms.ToolStripButton();
            this.toolStripSeparator1 = new System.Windows.Forms.ToolStripSeparator();
            this.btnStash = new System.Windows.Forms.ToolStripButton();
            this.menuSend = new System.Windows.Forms.ToolStripDropDownButton();
            this.toolStripSeparator3 = new System.Windows.Forms.ToolStripSeparator();
            this.btnParse = new System.Windows.Forms.ToolStripButton();
            this.tabControl1 = new System.Windows.Forms.TabControl();
            this.tabPage1 = new System.Windows.Forms.TabPage();
            this.toolStrip2 = new System.Windows.Forms.ToolStrip();
            this.btnTools = new System.Windows.Forms.ToolStripButton();
            this.toolStripSeparator2 = new System.Windows.Forms.ToolStripSeparator();
            this.btnWriteUint32 = new System.Windows.Forms.ToolStripButton();
            this.btnWriteint32 = new System.Windows.Forms.ToolStripButton();
            this.btnWriteUint16 = new System.Windows.Forms.ToolStripButton();
            this.btnWriteint16 = new System.Windows.Forms.ToolStripButton();
            this.btnWritePascalString = new System.Windows.Forms.ToolStripButton();
            this.splitContainer1 = new System.Windows.Forms.SplitContainer();
            this.hex = new Be.Windows.Forms.HexBox();
            this.analyzeResults = new System.Windows.Forms.ListBox();
            this.tabPage2 = new System.Windows.Forms.TabPage();
            this.parsedInspetor = new System.Windows.Forms.PropertyGrid();
            this.btnImportBytes = new System.Windows.Forms.ToolStripButton();
            this.importBrowser = new System.Windows.Forms.OpenFileDialog();
            this.toolStripButton1 = new System.Windows.Forms.ToolStripDropDownButton();
            this.btnExportByteArray = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStrip1.SuspendLayout();
            this.tabControl1.SuspendLayout();
            this.tabPage1.SuspendLayout();
            this.toolStrip2.SuspendLayout();
            this.splitContainer1.Panel1.SuspendLayout();
            this.splitContainer1.Panel2.SuspendLayout();
            this.splitContainer1.SuspendLayout();
            this.tabPage2.SuspendLayout();
            this.SuspendLayout();
            // 
            // toolStrip1
            // 
            this.toolStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.btnClose,
            this.toolStripSeparator1,
            this.btnStash,
            this.menuSend,
            this.toolStripSeparator3,
            this.btnParse});
            this.toolStrip1.Location = new System.Drawing.Point(0, 0);
            this.toolStrip1.Name = "toolStrip1";
            this.toolStrip1.Size = new System.Drawing.Size(628, 25);
            this.toolStrip1.TabIndex = 0;
            this.toolStrip1.Text = "toolStrip1";
            // 
            // btnClose
            // 
            this.btnClose.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.btnClose.Image = global::FSO.Server.Debug.Properties.Resources.cross_button;
            this.btnClose.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.btnClose.Name = "btnClose";
            this.btnClose.Size = new System.Drawing.Size(23, 22);
            this.btnClose.Text = "Close";
            this.btnClose.Click += new System.EventHandler(this.btnClose_Click);
            // 
            // toolStripSeparator1
            // 
            this.toolStripSeparator1.Name = "toolStripSeparator1";
            this.toolStripSeparator1.Size = new System.Drawing.Size(6, 25);
            // 
            // btnStash
            // 
            this.btnStash.Image = global::FSO.Server.Debug.Properties.Resources.jar__plus;
            this.btnStash.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.btnStash.Name = "btnStash";
            this.btnStash.Size = new System.Drawing.Size(55, 22);
            this.btnStash.Text = "Stash";
            this.btnStash.Click += new System.EventHandler(this.btnStash_Click);
            // 
            // menuSend
            // 
            this.menuSend.Image = ((System.Drawing.Image)(resources.GetObject("menuSend.Image")));
            this.menuSend.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.menuSend.Name = "menuSend";
            this.menuSend.Size = new System.Drawing.Size(62, 22);
            this.menuSend.Text = "Send";
            this.menuSend.DropDownOpening += new System.EventHandler(this.menuSend_DropDownOpening);
            // 
            // toolStripSeparator3
            // 
            this.toolStripSeparator3.Name = "toolStripSeparator3";
            this.toolStripSeparator3.Size = new System.Drawing.Size(6, 25);
            // 
            // btnParse
            // 
            this.btnParse.Image = ((System.Drawing.Image)(resources.GetObject("btnParse.Image")));
            this.btnParse.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.btnParse.Name = "btnParse";
            this.btnParse.Size = new System.Drawing.Size(93, 22);
            this.btnParse.Text = "Parse Packet";
            this.btnParse.Click += new System.EventHandler(this.btnParse_Click);
            // 
            // tabControl1
            // 
            this.tabControl1.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
                        | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.tabControl1.Controls.Add(this.tabPage1);
            this.tabControl1.Controls.Add(this.tabPage2);
            this.tabControl1.Location = new System.Drawing.Point(3, 28);
            this.tabControl1.Name = "tabControl1";
            this.tabControl1.SelectedIndex = 0;
            this.tabControl1.Size = new System.Drawing.Size(622, 360);
            this.tabControl1.TabIndex = 2;
            // 
            // tabPage1
            // 
            this.tabPage1.Controls.Add(this.toolStrip2);
            this.tabPage1.Controls.Add(this.splitContainer1);
            this.tabPage1.Location = new System.Drawing.Point(4, 22);
            this.tabPage1.Name = "tabPage1";
            this.tabPage1.Padding = new System.Windows.Forms.Padding(3);
            this.tabPage1.Size = new System.Drawing.Size(614, 334);
            this.tabPage1.TabIndex = 0;
            this.tabPage1.Text = "Hex";
            this.tabPage1.UseVisualStyleBackColor = true;
            // 
            // toolStrip2
            // 
            this.toolStrip2.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.btnTools,
            this.toolStripSeparator2,
            this.btnWriteUint32,
            this.btnWriteint32,
            this.btnWriteUint16,
            this.btnWriteint16,
            this.btnWritePascalString,
            this.btnImportBytes,
            this.toolStripButton1});
            this.toolStrip2.Location = new System.Drawing.Point(3, 3);
            this.toolStrip2.Name = "toolStrip2";
            this.toolStrip2.Size = new System.Drawing.Size(608, 25);
            this.toolStrip2.TabIndex = 5;
            this.toolStrip2.Text = "toolStrip2";
            // 
            // btnTools
            // 
            this.btnTools.Image = ((System.Drawing.Image)(resources.GetObject("btnTools.Image")));
            this.btnTools.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.btnTools.Name = "btnTools";
            this.btnTools.Size = new System.Drawing.Size(68, 22);
            this.btnTools.Text = "Analyse";
            this.btnTools.Click += new System.EventHandler(this.btnTools_Click);
            // 
            // toolStripSeparator2
            // 
            this.toolStripSeparator2.Name = "toolStripSeparator2";
            this.toolStripSeparator2.Size = new System.Drawing.Size(6, 25);
            // 
            // btnWriteUint32
            // 
            this.btnWriteUint32.Image = ((System.Drawing.Image)(resources.GetObject("btnWriteUint32.Image")));
            this.btnWriteUint32.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.btnWriteUint32.Name = "btnWriteUint32";
            this.btnWriteUint32.Size = new System.Drawing.Size(61, 22);
            this.btnWriteUint32.Text = "UInt32";
            // 
            // btnWriteint32
            // 
            this.btnWriteint32.Image = ((System.Drawing.Image)(resources.GetObject("btnWriteint32.Image")));
            this.btnWriteint32.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.btnWriteint32.Name = "btnWriteint32";
            this.btnWriteint32.Size = new System.Drawing.Size(53, 22);
            this.btnWriteint32.Text = "Int32";
            // 
            // btnWriteUint16
            // 
            this.btnWriteUint16.Image = ((System.Drawing.Image)(resources.GetObject("btnWriteUint16.Image")));
            this.btnWriteUint16.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.btnWriteUint16.Name = "btnWriteUint16";
            this.btnWriteUint16.Size = new System.Drawing.Size(61, 22);
            this.btnWriteUint16.Text = "UInt16";
            // 
            // btnWriteint16
            // 
            this.btnWriteint16.Image = ((System.Drawing.Image)(resources.GetObject("btnWriteint16.Image")));
            this.btnWriteint16.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.btnWriteint16.Name = "btnWriteint16";
            this.btnWriteint16.Size = new System.Drawing.Size(53, 22);
            this.btnWriteint16.Text = "Int16";
            // 
            // btnWritePascalString
            // 
            this.btnWritePascalString.Image = ((System.Drawing.Image)(resources.GetObject("btnWritePascalString.Image")));
            this.btnWritePascalString.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.btnWritePascalString.Name = "btnWritePascalString";
            this.btnWritePascalString.Size = new System.Drawing.Size(94, 22);
            this.btnWritePascalString.Text = "Pascal String";
            // 
            // splitContainer1
            // 
            this.splitContainer1.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
                        | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.splitContainer1.Location = new System.Drawing.Point(0, 31);
            this.splitContainer1.Name = "splitContainer1";
            this.splitContainer1.Orientation = System.Windows.Forms.Orientation.Horizontal;
            // 
            // splitContainer1.Panel1
            // 
            this.splitContainer1.Panel1.Controls.Add(this.hex);
            // 
            // splitContainer1.Panel2
            // 
            this.splitContainer1.Panel2.BackColor = System.Drawing.Color.Gainsboro;
            this.splitContainer1.Panel2.Controls.Add(this.analyzeResults);
            this.splitContainer1.Size = new System.Drawing.Size(614, 307);
            this.splitContainer1.SplitterDistance = 150;
            this.splitContainer1.TabIndex = 4;
            // 
            // hex
            // 
            this.hex.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this.hex.Dock = System.Windows.Forms.DockStyle.Fill;
            this.hex.Font = new System.Drawing.Font("Courier New", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.hex.InfoForeColor = System.Drawing.Color.Empty;
            this.hex.LineInfoVisible = true;
            this.hex.Location = new System.Drawing.Point(0, 0);
            this.hex.Name = "hex";
            this.hex.ShadowSelectionColor = System.Drawing.Color.FromArgb(((int)(((byte)(100)))), ((int)(((byte)(60)))), ((int)(((byte)(188)))), ((int)(((byte)(255)))));
            this.hex.Size = new System.Drawing.Size(614, 150);
            this.hex.StringViewVisible = true;
            this.hex.TabIndex = 2;
            this.hex.VScrollBarVisible = true;
            // 
            // analyzeResults
            // 
            this.analyzeResults.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
                        | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.analyzeResults.FormattingEnabled = true;
            this.analyzeResults.Location = new System.Drawing.Point(304, 16);
            this.analyzeResults.Name = "analyzeResults";
            this.analyzeResults.Size = new System.Drawing.Size(304, 121);
            this.analyzeResults.TabIndex = 0;
            this.analyzeResults.SelectedIndexChanged += new System.EventHandler(this.analyzeResults_SelectedIndexChanged);
            // 
            // tabPage2
            // 
            this.tabPage2.Controls.Add(this.parsedInspetor);
            this.tabPage2.Location = new System.Drawing.Point(4, 22);
            this.tabPage2.Name = "tabPage2";
            this.tabPage2.Size = new System.Drawing.Size(614, 334);
            this.tabPage2.TabIndex = 1;
            this.tabPage2.Text = "Parsed Packet";
            this.tabPage2.UseVisualStyleBackColor = true;
            // 
            // parsedInspetor
            // 
            this.parsedInspetor.Location = new System.Drawing.Point(3, 3);
            this.parsedInspetor.Name = "parsedInspetor";
            this.parsedInspetor.Size = new System.Drawing.Size(608, 328);
            this.parsedInspetor.TabIndex = 0;
            // 
            // btnImportBytes
            // 
            this.btnImportBytes.Image = ((System.Drawing.Image)(resources.GetObject("btnImportBytes.Image")));
            this.btnImportBytes.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.btnImportBytes.Name = "btnImportBytes";
            this.btnImportBytes.Size = new System.Drawing.Size(84, 22);
            this.btnImportBytes.Text = "Import File";
            this.btnImportBytes.Click += new System.EventHandler(this.btnImportBytes_Click);
            // 
            // toolStripButton1
            // 
            this.toolStripButton1.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.btnExportByteArray});
            this.toolStripButton1.Image = ((System.Drawing.Image)(resources.GetObject("toolStripButton1.Image")));
            this.toolStripButton1.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.toolStripButton1.Name = "toolStripButton1";
            this.toolStripButton1.Size = new System.Drawing.Size(69, 22);
            this.toolStripButton1.Text = "Export";
            // 
            // btnExportByteArray
            // 
            this.btnExportByteArray.Name = "btnExportByteArray";
            this.btnExportByteArray.Size = new System.Drawing.Size(207, 22);
            this.btnExportByteArray.Text = "To clipboard as C# byte[]";
            this.btnExportByteArray.Click += new System.EventHandler(this.btnExportByteArray_Click);
            // 
            // PacketView
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.tabControl1);
            this.Controls.Add(this.toolStrip1);
            this.Name = "PacketView";
            this.Size = new System.Drawing.Size(628, 391);
            this.Load += new System.EventHandler(this.PacketView_Load);
            this.toolStrip1.ResumeLayout(false);
            this.toolStrip1.PerformLayout();
            this.tabControl1.ResumeLayout(false);
            this.tabPage1.ResumeLayout(false);
            this.tabPage1.PerformLayout();
            this.toolStrip2.ResumeLayout(false);
            this.toolStrip2.PerformLayout();
            this.splitContainer1.Panel1.ResumeLayout(false);
            this.splitContainer1.Panel2.ResumeLayout(false);
            this.splitContainer1.ResumeLayout(false);
            this.tabPage2.ResumeLayout(false);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.ToolStrip toolStrip1;
        private System.Windows.Forms.ToolStripButton btnClose;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparator1;
        private System.Windows.Forms.TabControl tabControl1;
        private System.Windows.Forms.TabPage tabPage1;
        private Be.Windows.Forms.HexBox hex;
        private System.Windows.Forms.ToolStripButton btnStash;
        private System.Windows.Forms.SplitContainer splitContainer1;
        private System.Windows.Forms.ToolStrip toolStrip2;
        private System.Windows.Forms.ToolStripButton btnTools;
        private System.Windows.Forms.ToolStripDropDownButton menuSend;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparator2;
        private System.Windows.Forms.ToolStripButton btnWriteint32;
        private System.Windows.Forms.ToolStripButton btnWriteUint32;
        private System.Windows.Forms.ToolStripButton btnWriteUint16;
        private System.Windows.Forms.ToolStripButton btnWriteint16;
        private System.Windows.Forms.ToolStripButton btnWritePascalString;
        private System.Windows.Forms.ListBox analyzeResults;
        private System.Windows.Forms.TabPage tabPage2;
        private System.Windows.Forms.PropertyGrid parsedInspetor;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparator3;
        private System.Windows.Forms.ToolStripButton btnParse;
        private System.Windows.Forms.ToolStripButton btnImportBytes;
        private System.Windows.Forms.OpenFileDialog importBrowser;
        private System.Windows.Forms.ToolStripDropDownButton toolStripButton1;
        private System.Windows.Forms.ToolStripMenuItem btnExportByteArray;
    }
}
