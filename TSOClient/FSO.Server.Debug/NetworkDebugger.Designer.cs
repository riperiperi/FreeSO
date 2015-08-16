namespace FSO.Server.Debug
{
    partial class NetworkDebugger
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(NetworkDebugger));
            this.menuStrip1 = new System.Windows.Forms.MenuStrip();
            this.fileToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.menuStash = new System.Windows.Forms.ToolStripMenuItem();
            this.tab = new System.Windows.Forms.TabControl();
            this.toolStrip1 = new System.Windows.Forms.ToolStrip();
            this.btnInspect = new System.Windows.Forms.ToolStripButton();
            this.btnCreate = new System.Windows.Forms.ToolStripDropDownButton();
            this.btnClear = new System.Windows.Forms.ToolStripButton();
            this.btnStash = new System.Windows.Forms.ToolStripButton();
            this.packetList = new System.Windows.Forms.ListView();
            this.packetIcons = new System.Windows.Forms.ImageList(this.components);
            this.splitContainer1 = new System.Windows.Forms.SplitContainer();
            this.menuStrip1.SuspendLayout();
            this.toolStrip1.SuspendLayout();
            this.splitContainer1.Panel1.SuspendLayout();
            this.splitContainer1.Panel2.SuspendLayout();
            this.splitContainer1.SuspendLayout();
            this.SuspendLayout();
            // 
            // menuStrip1
            // 
            this.menuStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.fileToolStripMenuItem,
            this.menuStash});
            this.menuStrip1.Location = new System.Drawing.Point(0, 0);
            this.menuStrip1.Name = "menuStrip1";
            this.menuStrip1.Size = new System.Drawing.Size(778, 24);
            this.menuStrip1.TabIndex = 0;
            this.menuStrip1.Text = "menuStrip1";
            // 
            // fileToolStripMenuItem
            // 
            this.fileToolStripMenuItem.Name = "fileToolStripMenuItem";
            this.fileToolStripMenuItem.Size = new System.Drawing.Size(37, 20);
            this.fileToolStripMenuItem.Text = "File";
            // 
            // menuStash
            // 
            this.menuStash.Name = "menuStash";
            this.menuStash.Size = new System.Drawing.Size(47, 20);
            this.menuStash.Text = "Stash";
            this.menuStash.DropDownOpening += new System.EventHandler(this.menuStash_DropDownOpening);
            // 
            // tab
            // 
            this.tab.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tab.Location = new System.Drawing.Point(0, 0);
            this.tab.Name = "tab";
            this.tab.SelectedIndex = 0;
            this.tab.Size = new System.Drawing.Size(515, 435);
            this.tab.TabIndex = 1;
            // 
            // toolStrip1
            // 
            this.toolStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.btnInspect,
            this.btnCreate,
            this.btnClear,
            this.btnStash});
            this.toolStrip1.Location = new System.Drawing.Point(0, 24);
            this.toolStrip1.Name = "toolStrip1";
            this.toolStrip1.Size = new System.Drawing.Size(778, 25);
            this.toolStrip1.TabIndex = 3;
            this.toolStrip1.Text = "toolStrip1";
            // 
            // btnInspect
            // 
            this.btnInspect.Image = ((System.Drawing.Image)(resources.GetObject("btnInspect.Image")));
            this.btnInspect.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.btnInspect.Name = "btnInspect";
            this.btnInspect.Size = new System.Drawing.Size(103, 22);
            this.btnInspect.Text = "Inspect Packet";
            // 
            // btnCreate
            // 
            this.btnCreate.Image = global::FSO.Server.Debug.Properties.Resources.blueprint__plus;
            this.btnCreate.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.btnCreate.Name = "btnCreate";
            this.btnCreate.Size = new System.Drawing.Size(108, 22);
            this.btnCreate.Text = "Create Packet";
            // 
            // btnClear
            // 
            this.btnClear.Image = global::FSO.Server.Debug.Properties.Resources.bin;
            this.btnClear.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.btnClear.Name = "btnClear";
            this.btnClear.Size = new System.Drawing.Size(54, 22);
            this.btnClear.Text = "Clear";
            this.btnClear.Click += new System.EventHandler(this.btnClear_Click);
            // 
            // btnStash
            // 
            this.btnStash.Enabled = false;
            this.btnStash.Image = global::FSO.Server.Debug.Properties.Resources.jar__plus;
            this.btnStash.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.btnStash.Name = "btnStash";
            this.btnStash.Size = new System.Drawing.Size(102, 22);
            this.btnStash.Text = "Stash Selected";
            this.btnStash.Click += new System.EventHandler(this.btnStash_Click);
            // 
            // packetList
            // 
            this.packetList.Dock = System.Windows.Forms.DockStyle.Fill;
            this.packetList.FullRowSelect = true;
            this.packetList.LargeImageList = this.packetIcons;
            this.packetList.Location = new System.Drawing.Point(0, 0);
            this.packetList.Name = "packetList";
            this.packetList.Size = new System.Drawing.Size(259, 435);
            this.packetList.SmallImageList = this.packetIcons;
            this.packetList.TabIndex = 0;
            this.packetList.UseCompatibleStateImageBehavior = false;
            this.packetList.View = System.Windows.Forms.View.List;
            this.packetList.DoubleClick += new System.EventHandler(this.packetList_DoubleClick);
            this.packetList.ItemSelectionChanged += new System.Windows.Forms.ListViewItemSelectionChangedEventHandler(this.packetList_ItemSelectionChanged);
            // 
            // packetIcons
            // 
            this.packetIcons.ImageStream = ((System.Windows.Forms.ImageListStreamer)(resources.GetObject("packetIcons.ImageStream")));
            this.packetIcons.TransparentColor = System.Drawing.Color.Transparent;
            this.packetIcons.Images.SetKeyName(0, "navigation-180.png");
            this.packetIcons.Images.SetKeyName(1, "navin.png");
            // 
            // splitContainer1
            // 
            this.splitContainer1.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
                        | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.splitContainer1.Location = new System.Drawing.Point(0, 52);
            this.splitContainer1.Name = "splitContainer1";
            // 
            // splitContainer1.Panel1
            // 
            this.splitContainer1.Panel1.Controls.Add(this.packetList);
            // 
            // splitContainer1.Panel2
            // 
            this.splitContainer1.Panel2.Controls.Add(this.tab);
            this.splitContainer1.Size = new System.Drawing.Size(778, 435);
            this.splitContainer1.SplitterDistance = 259;
            this.splitContainer1.TabIndex = 4;
            // 
            // NetworkInspector
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(778, 487);
            this.Controls.Add(this.toolStrip1);
            this.Controls.Add(this.menuStrip1);
            this.Controls.Add(this.splitContainer1);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.SizableToolWindow;
            this.MainMenuStrip = this.menuStrip1;
            this.Name = "NetworkInspector";
            this.Text = "Network Inspector";
            this.Load += new System.EventHandler(this.NetworkInspector_Load);
            this.menuStrip1.ResumeLayout(false);
            this.menuStrip1.PerformLayout();
            this.toolStrip1.ResumeLayout(false);
            this.toolStrip1.PerformLayout();
            this.splitContainer1.Panel1.ResumeLayout(false);
            this.splitContainer1.Panel2.ResumeLayout(false);
            this.splitContainer1.ResumeLayout(false);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.MenuStrip menuStrip1;
        private System.Windows.Forms.ToolStripMenuItem fileToolStripMenuItem;
        private System.Windows.Forms.TabControl tab;
        private System.Windows.Forms.ToolStrip toolStrip1;
        private System.Windows.Forms.ToolStripButton btnInspect;
        private System.Windows.Forms.ListView packetList;
        private System.Windows.Forms.ToolStripMenuItem menuStash;
        private System.Windows.Forms.ImageList packetIcons;
        private System.Windows.Forms.ToolStripButton btnClear;
        private System.Windows.Forms.SplitContainer splitContainer1;
        private System.Windows.Forms.ToolStripButton btnStash;
        private System.Windows.Forms.ToolStripDropDownButton btnCreate;
    }
}