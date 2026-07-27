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
            TreeNode treeNode1 = new TreeNode("Avatar_ID");
            TreeNode treeNode2 = new TreeNode("Avatar", new TreeNode[] { treeNode1 });
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(TSODataDefinitionEditor));
            PropGrid = new PropertyGrid();
            DataViewTabs = new TabControl();
            Struct1Tab = new TabPage();
            TreeView1S = new TreeView();
            Struct2Tab = new TabPage();
            TreeView2S = new TreeView();
            StructDTab = new TabPage();
            TreeViewDS = new TreeView();
            menuStrip1 = new MenuStrip();
            fileToolStripMenuItem = new ToolStripMenuItem();
            loadToolStripMenuItem = new ToolStripMenuItem();
            saveToolStripMenuItem = new ToolStripMenuItem();
            saveAsToolStripMenuItem = new ToolStripMenuItem();
            activateIngameToolStripMenuItem = new ToolStripMenuItem();
            NewRoot = new Button();
            Delete = new Button();
            NewChild = new Button();
            DataViewTabs.SuspendLayout();
            Struct1Tab.SuspendLayout();
            Struct2Tab.SuspendLayout();
            StructDTab.SuspendLayout();
            menuStrip1.SuspendLayout();
            SuspendLayout();
            // 
            // PropGrid
            // 
            PropGrid.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Right;
            PropGrid.BackColor = SystemColors.Control;
            PropGrid.Location = new Point(458, 27);
            PropGrid.Name = "PropGrid";
            PropGrid.Size = new Size(331, 382);
            PropGrid.TabIndex = 0;
            PropGrid.PropertyValueChanged += PropGrid_PropertyValueChanged;
            // 
            // DataViewTabs
            // 
            DataViewTabs.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            DataViewTabs.Controls.Add(Struct1Tab);
            DataViewTabs.Controls.Add(Struct2Tab);
            DataViewTabs.Controls.Add(StructDTab);
            DataViewTabs.Location = new Point(12, 27);
            DataViewTabs.Name = "DataViewTabs";
            DataViewTabs.SelectedIndex = 0;
            DataViewTabs.Size = new Size(440, 411);
            DataViewTabs.TabIndex = 1;
            // 
            // Struct1Tab
            // 
            Struct1Tab.Controls.Add(TreeView1S);
            Struct1Tab.Location = new Point(4, 22);
            Struct1Tab.Name = "Struct1Tab";
            Struct1Tab.Padding = new Padding(3);
            Struct1Tab.Size = new Size(432, 385);
            Struct1Tab.TabIndex = 0;
            Struct1Tab.Text = "1st Level";
            Struct1Tab.UseVisualStyleBackColor = true;
            // 
            // TreeView1S
            // 
            TreeView1S.Dock = DockStyle.Fill;
            TreeView1S.FullRowSelect = true;
            TreeView1S.HideSelection = false;
            TreeView1S.Indent = 10;
            TreeView1S.Location = new Point(3, 3);
            TreeView1S.Name = "TreeView1S";
            treeNode1.Name = "Node1";
            treeNode1.Text = "Avatar_ID";
            treeNode2.Name = "Node0";
            treeNode2.Text = "Avatar";
            TreeView1S.Nodes.AddRange(new TreeNode[] { treeNode2 });
            TreeView1S.ShowNodeToolTips = true;
            TreeView1S.Size = new Size(426, 379);
            TreeView1S.TabIndex = 0;
            TreeView1S.AfterSelect += TreeView1S_AfterSelect;
            // 
            // Struct2Tab
            // 
            Struct2Tab.Controls.Add(TreeView2S);
            Struct2Tab.Location = new Point(4, 22);
            Struct2Tab.Name = "Struct2Tab";
            Struct2Tab.Padding = new Padding(3);
            Struct2Tab.Size = new Size(432, 385);
            Struct2Tab.TabIndex = 1;
            Struct2Tab.Text = "2nd Level";
            Struct2Tab.UseVisualStyleBackColor = true;
            // 
            // TreeView2S
            // 
            TreeView2S.Dock = DockStyle.Fill;
            TreeView2S.FullRowSelect = true;
            TreeView2S.HideSelection = false;
            TreeView2S.Indent = 10;
            TreeView2S.Location = new Point(3, 3);
            TreeView2S.Name = "TreeView2S";
            TreeView2S.Size = new Size(426, 379);
            TreeView2S.TabIndex = 0;
            TreeView2S.AfterSelect += TreeView2S_AfterSelect;
            // 
            // StructDTab
            // 
            StructDTab.Controls.Add(TreeViewDS);
            StructDTab.Location = new Point(4, 22);
            StructDTab.Name = "StructDTab";
            StructDTab.Padding = new Padding(3);
            StructDTab.Size = new Size(432, 385);
            StructDTab.TabIndex = 2;
            StructDTab.Text = "Derived";
            StructDTab.UseVisualStyleBackColor = true;
            // 
            // TreeViewDS
            // 
            TreeViewDS.Dock = DockStyle.Fill;
            TreeViewDS.FullRowSelect = true;
            TreeViewDS.HideSelection = false;
            TreeViewDS.Indent = 10;
            TreeViewDS.Location = new Point(3, 3);
            TreeViewDS.Name = "TreeViewDS";
            TreeViewDS.Size = new Size(426, 379);
            TreeViewDS.TabIndex = 0;
            TreeViewDS.AfterSelect += TreeViewDS_AfterSelect;
            // 
            // menuStrip1
            // 
            menuStrip1.Items.AddRange(new ToolStripItem[] { fileToolStripMenuItem });
            menuStrip1.Location = new Point(0, 0);
            menuStrip1.Name = "menuStrip1";
            menuStrip1.Size = new Size(801, 24);
            menuStrip1.TabIndex = 2;
            menuStrip1.Text = "menuStrip1";
            // 
            // fileToolStripMenuItem
            // 
            fileToolStripMenuItem.DropDownItems.AddRange(new ToolStripItem[] { loadToolStripMenuItem, saveToolStripMenuItem, saveAsToolStripMenuItem, activateIngameToolStripMenuItem });
            fileToolStripMenuItem.Name = "fileToolStripMenuItem";
            fileToolStripMenuItem.Size = new Size(37, 20);
            fileToolStripMenuItem.Text = "File";
            // 
            // loadToolStripMenuItem
            // 
            loadToolStripMenuItem.Name = "loadToolStripMenuItem";
            loadToolStripMenuItem.Size = new Size(160, 22);
            loadToolStripMenuItem.Text = "Load";
            loadToolStripMenuItem.Click += loadToolStripMenuItem_Click;
            // 
            // saveToolStripMenuItem
            // 
            saveToolStripMenuItem.Name = "saveToolStripMenuItem";
            saveToolStripMenuItem.Size = new Size(160, 22);
            saveToolStripMenuItem.Text = "Save";
            saveToolStripMenuItem.Click += saveToolStripMenuItem_Click;
            // 
            // saveAsToolStripMenuItem
            // 
            saveAsToolStripMenuItem.Name = "saveAsToolStripMenuItem";
            saveAsToolStripMenuItem.Size = new Size(160, 22);
            saveAsToolStripMenuItem.Text = "Save As...";
            saveAsToolStripMenuItem.Click += saveAsToolStripMenuItem_Click;
            // 
            // activateIngameToolStripMenuItem
            // 
            activateIngameToolStripMenuItem.Name = "activateIngameToolStripMenuItem";
            activateIngameToolStripMenuItem.Size = new Size(160, 22);
            activateIngameToolStripMenuItem.Text = "Activate Ingame";
            activateIngameToolStripMenuItem.Click += activateIngameToolStripMenuItem_Click;
            // 
            // NewRoot
            // 
            NewRoot.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
            NewRoot.Location = new Point(458, 415);
            NewRoot.Name = "NewRoot";
            NewRoot.Size = new Size(75, 23);
            NewRoot.TabIndex = 3;
            NewRoot.Text = "New Root";
            NewRoot.UseVisualStyleBackColor = true;
            NewRoot.Click += NewRoot_Click;
            // 
            // Delete
            // 
            Delete.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
            Delete.Location = new Point(714, 415);
            Delete.Name = "Delete";
            Delete.Size = new Size(75, 23);
            Delete.TabIndex = 4;
            Delete.Text = "Delete";
            Delete.UseVisualStyleBackColor = true;
            Delete.Click += Delete_Click;
            // 
            // NewChild
            // 
            NewChild.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
            NewChild.Location = new Point(539, 415);
            NewChild.Name = "NewChild";
            NewChild.Size = new Size(75, 23);
            NewChild.TabIndex = 5;
            NewChild.Text = "New Child";
            NewChild.UseVisualStyleBackColor = true;
            NewChild.Click += NewChild_Click;
            // 
            // TSODataDefinitionEditor
            // 
            AutoScaleDimensions = new SizeF(96F, 96F);
            AutoScaleMode = AutoScaleMode.Dpi;
            ClientSize = new Size(801, 450);
            Controls.Add(NewChild);
            Controls.Add(Delete);
            Controls.Add(NewRoot);
            Controls.Add(DataViewTabs);
            Controls.Add(PropGrid);
            Controls.Add(menuStrip1);
            Icon = (Icon)resources.GetObject("$this.Icon");
            MainMenuStrip = menuStrip1;
            Name = "TSODataDefinitionEditor";
            Text = "Data Service Editor";
            Load += TSODataDefinitionEditor_Load;
            DataViewTabs.ResumeLayout(false);
            Struct1Tab.ResumeLayout(false);
            Struct2Tab.ResumeLayout(false);
            StructDTab.ResumeLayout(false);
            menuStrip1.ResumeLayout(false);
            menuStrip1.PerformLayout();
            ResumeLayout(false);
            PerformLayout();

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