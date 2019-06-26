namespace FSO.IDE.ContentEditors
{
    partial class TSODataDefinitionEditor
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
            System.Windows.Forms.TreeNode treeNode1 = new System.Windows.Forms.TreeNode("Avatar_ID");
            System.Windows.Forms.TreeNode treeNode2 = new System.Windows.Forms.TreeNode("Avatar", new System.Windows.Forms.TreeNode[] {
            treeNode1});
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(TSODataDefinitionEditor));
            this.PropGrid = new System.Windows.Forms.PropertyGrid();
            this.DataViewTabs = new System.Windows.Forms.TabControl();
            this.Struct1Tab = new System.Windows.Forms.TabPage();
            this.TreeView1S = new System.Windows.Forms.TreeView();
            this.Struct2Tab = new System.Windows.Forms.TabPage();
            this.TreeView2S = new System.Windows.Forms.TreeView();
            this.StructDTab = new System.Windows.Forms.TabPage();
            this.TreeViewDS = new System.Windows.Forms.TreeView();
            this.menuStrip1 = new System.Windows.Forms.MenuStrip();
            this.fileToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.saveToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.loadToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.activateIngameToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.NewRoot = new System.Windows.Forms.Button();
            this.Delete = new System.Windows.Forms.Button();
            this.NewChild = new System.Windows.Forms.Button();
            this.saveAsToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.DataViewTabs.SuspendLayout();
            this.Struct1Tab.SuspendLayout();
            this.Struct2Tab.SuspendLayout();
            this.StructDTab.SuspendLayout();
            this.menuStrip1.SuspendLayout();
            this.SuspendLayout();
            // 
            // PropGrid
            // 
            this.PropGrid.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.PropGrid.Location = new System.Drawing.Point(458, 27);
            this.PropGrid.Name = "PropGrid";
            this.PropGrid.Size = new System.Drawing.Size(331, 382);
            this.PropGrid.TabIndex = 0;
            this.PropGrid.PropertyValueChanged += new System.Windows.Forms.PropertyValueChangedEventHandler(this.PropGrid_PropertyValueChanged);
            // 
            // DataViewTabs
            // 
            this.DataViewTabs.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.DataViewTabs.Controls.Add(this.Struct1Tab);
            this.DataViewTabs.Controls.Add(this.Struct2Tab);
            this.DataViewTabs.Controls.Add(this.StructDTab);
            this.DataViewTabs.Location = new System.Drawing.Point(12, 27);
            this.DataViewTabs.Name = "DataViewTabs";
            this.DataViewTabs.SelectedIndex = 0;
            this.DataViewTabs.Size = new System.Drawing.Size(440, 411);
            this.DataViewTabs.TabIndex = 1;
            // 
            // Struct1Tab
            // 
            this.Struct1Tab.Controls.Add(this.TreeView1S);
            this.Struct1Tab.Location = new System.Drawing.Point(4, 22);
            this.Struct1Tab.Name = "Struct1Tab";
            this.Struct1Tab.Padding = new System.Windows.Forms.Padding(3);
            this.Struct1Tab.Size = new System.Drawing.Size(432, 385);
            this.Struct1Tab.TabIndex = 0;
            this.Struct1Tab.Text = "1st Level";
            this.Struct1Tab.UseVisualStyleBackColor = true;
            // 
            // TreeView1S
            // 
            this.TreeView1S.Dock = System.Windows.Forms.DockStyle.Fill;
            this.TreeView1S.FullRowSelect = true;
            this.TreeView1S.HideSelection = false;
            this.TreeView1S.Indent = 10;
            this.TreeView1S.Location = new System.Drawing.Point(3, 3);
            this.TreeView1S.Name = "TreeView1S";
            treeNode1.Name = "Node1";
            treeNode1.Text = "Avatar_ID";
            treeNode2.Name = "Node0";
            treeNode2.Text = "Avatar";
            this.TreeView1S.Nodes.AddRange(new System.Windows.Forms.TreeNode[] {
            treeNode2});
            this.TreeView1S.ShowNodeToolTips = true;
            this.TreeView1S.Size = new System.Drawing.Size(426, 379);
            this.TreeView1S.TabIndex = 0;
            this.TreeView1S.AfterSelect += new System.Windows.Forms.TreeViewEventHandler(this.TreeView1S_AfterSelect);
            // 
            // Struct2Tab
            // 
            this.Struct2Tab.Controls.Add(this.TreeView2S);
            this.Struct2Tab.Location = new System.Drawing.Point(4, 22);
            this.Struct2Tab.Name = "Struct2Tab";
            this.Struct2Tab.Padding = new System.Windows.Forms.Padding(3);
            this.Struct2Tab.Size = new System.Drawing.Size(532, 385);
            this.Struct2Tab.TabIndex = 1;
            this.Struct2Tab.Text = "2nd Level";
            this.Struct2Tab.UseVisualStyleBackColor = true;
            // 
            // TreeView2S
            // 
            this.TreeView2S.Dock = System.Windows.Forms.DockStyle.Fill;
            this.TreeView2S.FullRowSelect = true;
            this.TreeView2S.HideSelection = false;
            this.TreeView2S.Indent = 10;
            this.TreeView2S.Location = new System.Drawing.Point(3, 3);
            this.TreeView2S.Name = "TreeView2S";
            this.TreeView2S.Size = new System.Drawing.Size(526, 379);
            this.TreeView2S.TabIndex = 0;
            this.TreeView2S.AfterSelect += new System.Windows.Forms.TreeViewEventHandler(this.TreeView2S_AfterSelect);
            // 
            // StructDTab
            // 
            this.StructDTab.Controls.Add(this.TreeViewDS);
            this.StructDTab.Location = new System.Drawing.Point(4, 22);
            this.StructDTab.Name = "StructDTab";
            this.StructDTab.Padding = new System.Windows.Forms.Padding(3);
            this.StructDTab.Size = new System.Drawing.Size(532, 385);
            this.StructDTab.TabIndex = 2;
            this.StructDTab.Text = "Derived";
            this.StructDTab.UseVisualStyleBackColor = true;
            // 
            // TreeViewDS
            // 
            this.TreeViewDS.Dock = System.Windows.Forms.DockStyle.Fill;
            this.TreeViewDS.FullRowSelect = true;
            this.TreeViewDS.HideSelection = false;
            this.TreeViewDS.Indent = 10;
            this.TreeViewDS.Location = new System.Drawing.Point(3, 3);
            this.TreeViewDS.Name = "TreeViewDS";
            this.TreeViewDS.Size = new System.Drawing.Size(526, 379);
            this.TreeViewDS.TabIndex = 0;
            this.TreeViewDS.AfterSelect += new System.Windows.Forms.TreeViewEventHandler(this.TreeViewDS_AfterSelect);
            // 
            // menuStrip1
            // 
            this.menuStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.fileToolStripMenuItem});
            this.menuStrip1.Location = new System.Drawing.Point(0, 0);
            this.menuStrip1.Name = "menuStrip1";
            this.menuStrip1.Size = new System.Drawing.Size(801, 24);
            this.menuStrip1.TabIndex = 2;
            this.menuStrip1.Text = "menuStrip1";
            // 
            // fileToolStripMenuItem
            // 
            this.fileToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.loadToolStripMenuItem,
            this.saveToolStripMenuItem,
            this.saveAsToolStripMenuItem,
            this.activateIngameToolStripMenuItem});
            this.fileToolStripMenuItem.Name = "fileToolStripMenuItem";
            this.fileToolStripMenuItem.Size = new System.Drawing.Size(37, 20);
            this.fileToolStripMenuItem.Text = "File";
            // 
            // saveToolStripMenuItem
            // 
            this.saveToolStripMenuItem.Name = "saveToolStripMenuItem";
            this.saveToolStripMenuItem.Size = new System.Drawing.Size(180, 22);
            this.saveToolStripMenuItem.Text = "Save";
            this.saveToolStripMenuItem.Click += new System.EventHandler(this.saveToolStripMenuItem_Click);
            // 
            // loadToolStripMenuItem
            // 
            this.loadToolStripMenuItem.Name = "loadToolStripMenuItem";
            this.loadToolStripMenuItem.Size = new System.Drawing.Size(180, 22);
            this.loadToolStripMenuItem.Text = "Load";
            this.loadToolStripMenuItem.Click += new System.EventHandler(this.loadToolStripMenuItem_Click);
            // 
            // activateIngameToolStripMenuItem
            // 
            this.activateIngameToolStripMenuItem.Name = "activateIngameToolStripMenuItem";
            this.activateIngameToolStripMenuItem.Size = new System.Drawing.Size(180, 22);
            this.activateIngameToolStripMenuItem.Text = "Activate Ingame";
            this.activateIngameToolStripMenuItem.Click += new System.EventHandler(this.activateIngameToolStripMenuItem_Click);
            // 
            // NewRoot
            // 
            this.NewRoot.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.NewRoot.Location = new System.Drawing.Point(458, 415);
            this.NewRoot.Name = "NewRoot";
            this.NewRoot.Size = new System.Drawing.Size(75, 23);
            this.NewRoot.TabIndex = 3;
            this.NewRoot.Text = "New Root";
            this.NewRoot.UseVisualStyleBackColor = true;
            this.NewRoot.Click += new System.EventHandler(this.NewRoot_Click);
            // 
            // Delete
            // 
            this.Delete.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.Delete.Location = new System.Drawing.Point(714, 415);
            this.Delete.Name = "Delete";
            this.Delete.Size = new System.Drawing.Size(75, 23);
            this.Delete.TabIndex = 4;
            this.Delete.Text = "Delete";
            this.Delete.UseVisualStyleBackColor = true;
            this.Delete.Click += new System.EventHandler(this.Delete_Click);
            // 
            // NewChild
            // 
            this.NewChild.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.NewChild.Location = new System.Drawing.Point(539, 415);
            this.NewChild.Name = "NewChild";
            this.NewChild.Size = new System.Drawing.Size(75, 23);
            this.NewChild.TabIndex = 5;
            this.NewChild.Text = "New Child";
            this.NewChild.UseVisualStyleBackColor = true;
            this.NewChild.Click += new System.EventHandler(this.NewChild_Click);
            // 
            // saveAsToolStripMenuItem
            // 
            this.saveAsToolStripMenuItem.Name = "saveAsToolStripMenuItem";
            this.saveAsToolStripMenuItem.Size = new System.Drawing.Size(180, 22);
            this.saveAsToolStripMenuItem.Text = "Save As...";
            this.saveAsToolStripMenuItem.Click += new System.EventHandler(this.saveAsToolStripMenuItem_Click);
            // 
            // TSODataDefinitionEditor
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(801, 450);
            this.Controls.Add(this.NewChild);
            this.Controls.Add(this.Delete);
            this.Controls.Add(this.NewRoot);
            this.Controls.Add(this.DataViewTabs);
            this.Controls.Add(this.PropGrid);
            this.Controls.Add(this.menuStrip1);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.MainMenuStrip = this.menuStrip1;
            this.Name = "TSODataDefinitionEditor";
            this.Text = "Data Service Editor";
            this.Load += new System.EventHandler(this.TSODataDefinitionEditor_Load);
            this.DataViewTabs.ResumeLayout(false);
            this.Struct1Tab.ResumeLayout(false);
            this.Struct2Tab.ResumeLayout(false);
            this.StructDTab.ResumeLayout(false);
            this.menuStrip1.ResumeLayout(false);
            this.menuStrip1.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.PropertyGrid PropGrid;
        private System.Windows.Forms.TabControl DataViewTabs;
        private System.Windows.Forms.TabPage Struct1Tab;
        private System.Windows.Forms.TreeView TreeView1S;
        private System.Windows.Forms.TabPage Struct2Tab;
        private System.Windows.Forms.TreeView TreeView2S;
        private System.Windows.Forms.TabPage StructDTab;
        private System.Windows.Forms.TreeView TreeViewDS;
        private System.Windows.Forms.MenuStrip menuStrip1;
        private System.Windows.Forms.ToolStripMenuItem fileToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem saveToolStripMenuItem;
        private System.Windows.Forms.Button NewRoot;
        private System.Windows.Forms.Button Delete;
        private System.Windows.Forms.Button NewChild;
        private System.Windows.Forms.ToolStripMenuItem loadToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem activateIngameToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem saveAsToolStripMenuItem;
    }
}