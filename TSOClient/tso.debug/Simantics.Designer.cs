namespace FSO.Debug
{
    partial class Simantics
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(Simantics));
            this.toolStrip1 = new System.Windows.Forms.ToolStrip();
            this.refreshBtn = new System.Windows.Forms.ToolStripButton();
            this.toolStripDropDownButton1 = new System.Windows.Forms.ToolStripDropDownButton();
            this.menuShowSims = new System.Windows.Forms.ToolStripMenuItem();
            this.menuShowObjects = new System.Windows.Forms.ToolStripMenuItem();
            this.btnSelect = new System.Windows.Forms.ToolStripButton();
            this.entityList = new System.Windows.Forms.ListBox();
            this.entityInfo = new System.Windows.Forms.TabControl();
            this.tabInfo = new System.Windows.Forms.TabPage();
            this.propertiesTab = new System.Windows.Forms.TabPage();
            this.propertyGrid = new System.Windows.Forms.PropertyGrid();
            this.bhavTab = new System.Windows.Forms.TabPage();
            this.toolStrip2 = new System.Windows.Forms.ToolStrip();
            this.btnInspectBhav = new System.Windows.Forms.ToolStripButton();
            this.bhavExecuteBtn = new System.Windows.Forms.ToolStripButton();
            this.bhavList = new System.Windows.Forms.ListBox();
            this.ttabTab = new System.Windows.Forms.TabPage();
            this.toolStrip3 = new System.Windows.Forms.ToolStrip();
            this.toolStripButton2 = new System.Windows.Forms.ToolStripButton();
            this.interactionList = new System.Windows.Forms.ListBox();
            this.textBox1 = new System.Windows.Forms.TextBox();
            this.pictureBox1 = new System.Windows.Forms.PictureBox();
            this.toolStrip1.SuspendLayout();
            this.entityInfo.SuspendLayout();
            this.propertiesTab.SuspendLayout();
            this.bhavTab.SuspendLayout();
            this.toolStrip2.SuspendLayout();
            this.ttabTab.SuspendLayout();
            this.toolStrip3.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox1)).BeginInit();
            this.SuspendLayout();
            // 
            // toolStrip1
            // 
            this.toolStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.refreshBtn,
            this.toolStripDropDownButton1,
            this.btnSelect});
            this.toolStrip1.Location = new System.Drawing.Point(0, 0);
            this.toolStrip1.Name = "toolStrip1";
            this.toolStrip1.Size = new System.Drawing.Size(363, 25);
            this.toolStrip1.TabIndex = 0;
            this.toolStrip1.Text = "toolStrip1";
            // 
            // refreshBtn
            // 
            this.refreshBtn.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.refreshBtn.Image = ((System.Drawing.Image)(resources.GetObject("refreshBtn.Image")));
            this.refreshBtn.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.refreshBtn.Name = "refreshBtn";
            this.refreshBtn.Size = new System.Drawing.Size(23, 22);
            this.refreshBtn.Text = "toolStripButton1";
            // 
            // toolStripDropDownButton1
            // 
            this.toolStripDropDownButton1.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.menuShowSims,
            this.menuShowObjects});
            this.toolStripDropDownButton1.Image = ((System.Drawing.Image)(resources.GetObject("toolStripDropDownButton1.Image")));
            this.toolStripDropDownButton1.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.toolStripDropDownButton1.Name = "toolStripDropDownButton1";
            this.toolStripDropDownButton1.Size = new System.Drawing.Size(62, 22);
            this.toolStripDropDownButton1.Text = "Filter";
            // 
            // menuShowSims
            // 
            this.menuShowSims.Checked = true;
            this.menuShowSims.CheckState = System.Windows.Forms.CheckState.Checked;
            this.menuShowSims.Image = global::FSO.Debug.Properties.Resources.users;
            this.menuShowSims.Name = "menuShowSims";
            this.menuShowSims.Size = new System.Drawing.Size(146, 22);
            this.menuShowSims.Text = "Show Sims";
            this.menuShowSims.Click += new System.EventHandler(this.menuShowSims_Click);
            // 
            // menuShowObjects
            // 
            this.menuShowObjects.Checked = true;
            this.menuShowObjects.CheckState = System.Windows.Forms.CheckState.Checked;
            this.menuShowObjects.Image = global::FSO.Debug.Properties.Resources.box_add;
            this.menuShowObjects.Name = "menuShowObjects";
            this.menuShowObjects.Size = new System.Drawing.Size(146, 22);
            this.menuShowObjects.Text = "Show Objects";
            this.menuShowObjects.Click += new System.EventHandler(this.menuShowObjects_Click);
            // 
            // btnSelect
            // 
            this.btnSelect.Image = ((System.Drawing.Image)(resources.GetObject("btnSelect.Image")));
            this.btnSelect.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.btnSelect.Name = "btnSelect";
            this.btnSelect.Size = new System.Drawing.Size(79, 22);
            this.btnSelect.Text = "Set Active";
            this.btnSelect.Click += new System.EventHandler(this.btnSelect_Click);
            // 
            // entityList
            // 
            this.entityList.FormattingEnabled = true;
            this.entityList.Location = new System.Drawing.Point(0, 54);
            this.entityList.Name = "entityList";
            this.entityList.Size = new System.Drawing.Size(363, 199);
            this.entityList.TabIndex = 1;
            this.entityList.SelectedIndexChanged += new System.EventHandler(this.entityList_SelectedIndexChanged);
            // 
            // entityInfo
            // 
            this.entityInfo.Controls.Add(this.tabInfo);
            this.entityInfo.Controls.Add(this.propertiesTab);
            this.entityInfo.Controls.Add(this.bhavTab);
            this.entityInfo.Controls.Add(this.ttabTab);
            this.entityInfo.Location = new System.Drawing.Point(12, 262);
            this.entityInfo.Multiline = true;
            this.entityInfo.Name = "entityInfo";
            this.entityInfo.SelectedIndex = 0;
            this.entityInfo.Size = new System.Drawing.Size(339, 314);
            this.entityInfo.TabIndex = 2;
            // 
            // tabInfo
            // 
            this.tabInfo.Location = new System.Drawing.Point(4, 22);
            this.tabInfo.Name = "tabInfo";
            this.tabInfo.Padding = new System.Windows.Forms.Padding(3);
            this.tabInfo.Size = new System.Drawing.Size(331, 288);
            this.tabInfo.TabIndex = 0;
            this.tabInfo.Text = "Info";
            this.tabInfo.UseVisualStyleBackColor = true;
            // 
            // propertiesTab
            // 
            this.propertiesTab.Controls.Add(this.propertyGrid);
            this.propertiesTab.Location = new System.Drawing.Point(4, 22);
            this.propertiesTab.Name = "propertiesTab";
            this.propertiesTab.Size = new System.Drawing.Size(331, 288);
            this.propertiesTab.TabIndex = 1;
            this.propertiesTab.Text = "Properties";
            this.propertiesTab.UseVisualStyleBackColor = true;
            // 
            // propertyGrid
            // 
            this.propertyGrid.Dock = System.Windows.Forms.DockStyle.Fill;
            this.propertyGrid.Location = new System.Drawing.Point(0, 0);
            this.propertyGrid.Name = "propertyGrid";
            this.propertyGrid.Size = new System.Drawing.Size(331, 288);
            this.propertyGrid.TabIndex = 0;
            // 
            // bhavTab
            // 
            this.bhavTab.Controls.Add(this.toolStrip2);
            this.bhavTab.Controls.Add(this.bhavList);
            this.bhavTab.Location = new System.Drawing.Point(4, 22);
            this.bhavTab.Name = "bhavTab";
            this.bhavTab.Padding = new System.Windows.Forms.Padding(3);
            this.bhavTab.Size = new System.Drawing.Size(331, 288);
            this.bhavTab.TabIndex = 2;
            this.bhavTab.Text = "BHAV";
            this.bhavTab.UseVisualStyleBackColor = true;
            // 
            // toolStrip2
            // 
            this.toolStrip2.GripStyle = System.Windows.Forms.ToolStripGripStyle.Hidden;
            this.toolStrip2.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.btnInspectBhav,
            this.bhavExecuteBtn});
            this.toolStrip2.Location = new System.Drawing.Point(3, 3);
            this.toolStrip2.Name = "toolStrip2";
            this.toolStrip2.Size = new System.Drawing.Size(325, 25);
            this.toolStrip2.TabIndex = 1;
            this.toolStrip2.Text = "toolStrip2";
            // 
            // btnInspectBhav
            // 
            this.btnInspectBhav.Image = ((System.Drawing.Image)(resources.GetObject("btnInspectBhav.Image")));
            this.btnInspectBhav.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.btnInspectBhav.Name = "btnInspectBhav";
            this.btnInspectBhav.Size = new System.Drawing.Size(65, 22);
            this.btnInspectBhav.Text = "Inspect";
            this.btnInspectBhav.Click += new System.EventHandler(this.btnInspectBhav_Click);
            // 
            // bhavExecuteBtn
            // 
            this.bhavExecuteBtn.Image = ((System.Drawing.Image)(resources.GetObject("bhavExecuteBtn.Image")));
            this.bhavExecuteBtn.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.bhavExecuteBtn.Name = "bhavExecuteBtn";
            this.bhavExecuteBtn.Size = new System.Drawing.Size(67, 22);
            this.bhavExecuteBtn.Text = "Execute";
            this.bhavExecuteBtn.Click += new System.EventHandler(this.bhavExecuteBtn_Click);
            // 
            // bhavList
            // 
            this.bhavList.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.bhavList.FormattingEnabled = true;
            this.bhavList.Location = new System.Drawing.Point(3, 34);
            this.bhavList.Name = "bhavList";
            this.bhavList.Size = new System.Drawing.Size(325, 251);
            this.bhavList.TabIndex = 0;
            // 
            // ttabTab
            // 
            this.ttabTab.Controls.Add(this.toolStrip3);
            this.ttabTab.Controls.Add(this.interactionList);
            this.ttabTab.Location = new System.Drawing.Point(4, 22);
            this.ttabTab.Name = "ttabTab";
            this.ttabTab.Padding = new System.Windows.Forms.Padding(3);
            this.ttabTab.Size = new System.Drawing.Size(331, 288);
            this.ttabTab.TabIndex = 3;
            this.ttabTab.Text = "TTAB";
            this.ttabTab.UseVisualStyleBackColor = true;
            // 
            // toolStrip3
            // 
            this.toolStrip3.GripStyle = System.Windows.Forms.ToolStripGripStyle.Hidden;
            this.toolStrip3.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.toolStripButton2});
            this.toolStrip3.Location = new System.Drawing.Point(3, 3);
            this.toolStrip3.Name = "toolStrip3";
            this.toolStrip3.Size = new System.Drawing.Size(325, 25);
            this.toolStrip3.TabIndex = 3;
            this.toolStrip3.Text = "toolStrip3";
            // 
            // toolStripButton2
            // 
            this.toolStripButton2.Image = ((System.Drawing.Image)(resources.GetObject("toolStripButton2.Image")));
            this.toolStripButton2.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.toolStripButton2.Name = "toolStripButton2";
            this.toolStripButton2.Size = new System.Drawing.Size(67, 22);
            this.toolStripButton2.Text = "Execute";
            this.toolStripButton2.Click += new System.EventHandler(this.TTABExecute_Click);
            // 
            // interactionList
            // 
            this.interactionList.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.interactionList.FormattingEnabled = true;
            this.interactionList.Location = new System.Drawing.Point(3, 34);
            this.interactionList.Name = "interactionList";
            this.interactionList.Size = new System.Drawing.Size(325, 251);
            this.interactionList.TabIndex = 2;
            // 
            // textBox1
            // 
            this.textBox1.Location = new System.Drawing.Point(30, 29);
            this.textBox1.Name = "textBox1";
            this.textBox1.Size = new System.Drawing.Size(321, 20);
            this.textBox1.TabIndex = 3;
            this.textBox1.TextChanged += new System.EventHandler(this.textBox1_TextChanged);
            // 
            // pictureBox1
            // 
            this.pictureBox1.Image = global::FSO.Debug.Properties.Resources.search;
            this.pictureBox1.Location = new System.Drawing.Point(8, 31);
            this.pictureBox1.Name = "pictureBox1";
            this.pictureBox1.Size = new System.Drawing.Size(16, 16);
            this.pictureBox1.TabIndex = 4;
            this.pictureBox1.TabStop = false;
            this.pictureBox1.Click += new System.EventHandler(this.pictureBox1_Click);
            // 
            // Simantics
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(363, 588);
            this.Controls.Add(this.pictureBox1);
            this.Controls.Add(this.textBox1);
            this.Controls.Add(this.entityInfo);
            this.Controls.Add(this.entityList);
            this.Controls.Add(this.toolStrip1);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedToolWindow;
            this.Name = "Simantics";
            this.Text = "Simantics";
            this.Load += new System.EventHandler(this.Simantics_Load);
            this.toolStrip1.ResumeLayout(false);
            this.toolStrip1.PerformLayout();
            this.entityInfo.ResumeLayout(false);
            this.propertiesTab.ResumeLayout(false);
            this.bhavTab.ResumeLayout(false);
            this.bhavTab.PerformLayout();
            this.toolStrip2.ResumeLayout(false);
            this.toolStrip2.PerformLayout();
            this.ttabTab.ResumeLayout(false);
            this.ttabTab.PerformLayout();
            this.toolStrip3.ResumeLayout(false);
            this.toolStrip3.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox1)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.ToolStrip toolStrip1;
        private System.Windows.Forms.ListBox entityList;
        private System.Windows.Forms.TabControl entityInfo;
        private System.Windows.Forms.TabPage tabInfo;
        private System.Windows.Forms.TabPage propertiesTab;
        private System.Windows.Forms.PropertyGrid propertyGrid;
        private System.Windows.Forms.ToolStripButton refreshBtn;
        private System.Windows.Forms.ToolStripDropDownButton toolStripDropDownButton1;
        private System.Windows.Forms.ToolStripMenuItem menuShowSims;
        private System.Windows.Forms.ToolStripMenuItem menuShowObjects;
        private System.Windows.Forms.TextBox textBox1;
        private System.Windows.Forms.PictureBox pictureBox1;
        private System.Windows.Forms.TabPage bhavTab;
        private System.Windows.Forms.TabPage ttabTab;
        private System.Windows.Forms.ListBox bhavList;
        private System.Windows.Forms.ToolStrip toolStrip2;
        private System.Windows.Forms.ToolStripButton btnInspectBhav;
        private System.Windows.Forms.ToolStripButton bhavExecuteBtn;
        private System.Windows.Forms.ToolStripButton btnSelect;
        private System.Windows.Forms.ToolStrip toolStrip3;
        private System.Windows.Forms.ToolStripButton toolStripButton2;
        private System.Windows.Forms.ListBox interactionList;
    }
}