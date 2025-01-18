namespace FSO.IDE
{
    partial class MainWindow
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
            System.Windows.Forms.TreeNode treeNode1 = new System.Windows.Forms.TreeNode("(BHAV #4000) Init");
            System.Windows.Forms.TreeNode treeNode2 = new System.Windows.Forms.TreeNode("(SPR2 #254) Fish Sprite");
            System.Windows.Forms.TreeNode treeNode3 = new System.Windows.Forms.TreeNode("(DGRP #100) Dead 1");
            System.Windows.Forms.TreeNode treeNode4 = new System.Windows.Forms.TreeNode("(DGRP #101) Dead 2");
            System.Windows.Forms.TreeNode treeNode5 = new System.Windows.Forms.TreeNode("aquarium.iff", new System.Windows.Forms.TreeNode[] {
            treeNode1,
            treeNode2,
            treeNode3,
            treeNode4});
            System.Windows.Forms.TreeNode treeNode6 = new System.Windows.Forms.TreeNode("(BHAV #4023) Interaction - Read Inscription");
            System.Windows.Forms.TreeNode treeNode7 = new System.Windows.Forms.TreeNode("(CTSS #223) Plaque CTSS");
            System.Windows.Forms.TreeNode treeNode8 = new System.Windows.Forms.TreeNode("Content/Objects/objPlaque.iff", new System.Windows.Forms.TreeNode[] {
            treeNode6,
            treeNode7});
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(MainWindow));
            this.CreateButton = new System.Windows.Forms.Button();
            this.EditButton = new System.Windows.Forms.Button();
            this.CloneButton = new System.Windows.Forms.Button();
            this.menuStrip1 = new System.Windows.Forms.MenuStrip();
            this.newToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.objectToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.semiGlobalToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.toolsToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.dataServiceEditorToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.simAnticsAOTToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.saveGlobalscsToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.avatarToolToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.openExternalIffToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.iFFFileToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.sPFFileToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.fieldEncodingReverserToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.windowToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.hideAllToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripSeparator1 = new System.Windows.Forms.ToolStripSeparator();
            this.helpToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.aboutToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.UtilityTabs = new System.Windows.Forms.TabControl();
            this.OverviewTab = new System.Windows.Forms.TabPage();
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this.ChunkSelection = new System.Windows.Forms.Label();
            this.ChunkDiscard = new System.Windows.Forms.Button();
            this.AllTable = new System.Windows.Forms.TableLayoutPanel();
            this.SaveAll = new System.Windows.Forms.Button();
            this.DiscardAll = new System.Windows.Forms.Button();
            this.groupBox2 = new System.Windows.Forms.GroupBox();
            this.IffSelection = new System.Windows.Forms.Label();
            this.IffSave = new System.Windows.Forms.Button();
            this.IffDiscard = new System.Windows.Forms.Button();
            this.ChangesLabel = new System.Windows.Forms.Label();
            this.ChangesView = new System.Windows.Forms.TreeView();
            this.BrowserTab = new System.Windows.Forms.TabPage();
            this.NewOBJButton = new System.Windows.Forms.Button();
            this.Browser = new FSO.IDE.ObjectBrowser();
            this.InspectorTab = new System.Windows.Forms.TabPage();
            this.entityInspector1 = new FSO.IDE.EntityInspector();
            this.bothToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.menuStrip1.SuspendLayout();
            this.UtilityTabs.SuspendLayout();
            this.OverviewTab.SuspendLayout();
            this.groupBox1.SuspendLayout();
            this.AllTable.SuspendLayout();
            this.groupBox2.SuspendLayout();
            this.BrowserTab.SuspendLayout();
            this.InspectorTab.SuspendLayout();
            this.SuspendLayout();
            // 
            // CreateButton
            // 
            this.CreateButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.CreateButton.Location = new System.Drawing.Point(531, 352);
            this.CreateButton.Name = "CreateButton";
            this.CreateButton.Size = new System.Drawing.Size(186, 23);
            this.CreateButton.TabIndex = 21;
            this.CreateButton.Text = "Create New Object Instance";
            this.CreateButton.UseVisualStyleBackColor = true;
            this.CreateButton.Click += new System.EventHandler(this.CreateButton_Click);
            // 
            // EditButton
            // 
            this.EditButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.EditButton.Location = new System.Drawing.Point(531, 323);
            this.EditButton.Name = "EditButton";
            this.EditButton.Size = new System.Drawing.Size(186, 23);
            this.EditButton.TabIndex = 20;
            this.EditButton.Text = "Edit Object";
            this.EditButton.UseVisualStyleBackColor = true;
            this.EditButton.Click += new System.EventHandler(this.button2_Click);
            // 
            // CloneButton
            // 
            this.CloneButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.CloneButton.Enabled = false;
            this.CloneButton.Location = new System.Drawing.Point(531, 410);
            this.CloneButton.Name = "CloneButton";
            this.CloneButton.Size = new System.Drawing.Size(186, 23);
            this.CloneButton.TabIndex = 19;
            this.CloneButton.Text = "Clone Object (.piff)";
            this.CloneButton.UseVisualStyleBackColor = true;
            this.CloneButton.Click += new System.EventHandler(this.button1_Click);
            // 
            // menuStrip1
            // 
            this.menuStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.newToolStripMenuItem,
            this.toolsToolStripMenuItem,
            this.windowToolStripMenuItem,
            this.helpToolStripMenuItem});
            this.menuStrip1.Location = new System.Drawing.Point(0, 0);
            this.menuStrip1.Name = "menuStrip1";
            this.menuStrip1.Size = new System.Drawing.Size(762, 24);
            this.menuStrip1.TabIndex = 22;
            this.menuStrip1.Text = "menuStrip1";
            // 
            // newToolStripMenuItem
            // 
            this.newToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.objectToolStripMenuItem,
            this.semiGlobalToolStripMenuItem});
            this.newToolStripMenuItem.Name = "newToolStripMenuItem";
            this.newToolStripMenuItem.Size = new System.Drawing.Size(43, 20);
            this.newToolStripMenuItem.Text = "New";
            // 
            // objectToolStripMenuItem
            // 
            this.objectToolStripMenuItem.Name = "objectToolStripMenuItem";
            this.objectToolStripMenuItem.Size = new System.Drawing.Size(139, 22);
            this.objectToolStripMenuItem.Text = "Object";
            this.objectToolStripMenuItem.Click += new System.EventHandler(this.NewOBJButton_Click);
            // 
            // semiGlobalToolStripMenuItem
            // 
            this.semiGlobalToolStripMenuItem.Name = "semiGlobalToolStripMenuItem";
            this.semiGlobalToolStripMenuItem.Size = new System.Drawing.Size(139, 22);
            this.semiGlobalToolStripMenuItem.Text = "Semi-Global";
            this.semiGlobalToolStripMenuItem.Click += new System.EventHandler(this.semiGlobalToolStripMenuItem_Click);
            // 
            // toolsToolStripMenuItem
            // 
            this.toolsToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.dataServiceEditorToolStripMenuItem,
            this.simAnticsAOTToolStripMenuItem,
            this.avatarToolToolStripMenuItem,
            this.openExternalIffToolStripMenuItem,
            this.fieldEncodingReverserToolStripMenuItem});
            this.toolsToolStripMenuItem.Name = "toolsToolStripMenuItem";
            this.toolsToolStripMenuItem.Size = new System.Drawing.Size(47, 20);
            this.toolsToolStripMenuItem.Text = "Tools";
            // 
            // dataServiceEditorToolStripMenuItem
            // 
            this.dataServiceEditorToolStripMenuItem.Name = "dataServiceEditorToolStripMenuItem";
            this.dataServiceEditorToolStripMenuItem.Size = new System.Drawing.Size(199, 22);
            this.dataServiceEditorToolStripMenuItem.Text = "Data Service Editor";
            this.dataServiceEditorToolStripMenuItem.Click += new System.EventHandler(this.dataServiceEditorToolStripMenuItem_Click);
            // 
            // simAnticsAOTToolStripMenuItem
            // 
            this.simAnticsAOTToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.saveGlobalscsToolStripMenuItem});
            this.simAnticsAOTToolStripMenuItem.Name = "simAnticsAOTToolStripMenuItem";
            this.simAnticsAOTToolStripMenuItem.Size = new System.Drawing.Size(199, 22);
            this.simAnticsAOTToolStripMenuItem.Text = "SimAntics AOT";
            // 
            // saveGlobalscsToolStripMenuItem
            // 
            this.saveGlobalscsToolStripMenuItem.Name = "saveGlobalscsToolStripMenuItem";
            this.saveGlobalscsToolStripMenuItem.Size = new System.Drawing.Size(216, 22);
            this.saveGlobalscsToolStripMenuItem.Text = "Generate AOT Sources (.cs)";
            this.saveGlobalscsToolStripMenuItem.Click += new System.EventHandler(this.saveGlobalscsToolStripMenuItem_Click);
            // 
            // avatarToolToolStripMenuItem
            // 
            this.avatarToolToolStripMenuItem.Name = "avatarToolToolStripMenuItem";
            this.avatarToolToolStripMenuItem.Size = new System.Drawing.Size(199, 22);
            this.avatarToolToolStripMenuItem.Text = "Avatar Tool";
            this.avatarToolToolStripMenuItem.Click += new System.EventHandler(this.avatarToolToolStripMenuItem_Click);
            // 
            // openExternalIffToolStripMenuItem
            // 
            this.openExternalIffToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.iFFFileToolStripMenuItem,
            this.sPFFileToolStripMenuItem,
            this.bothToolStripMenuItem});
            this.openExternalIffToolStripMenuItem.Name = "openExternalIffToolStripMenuItem";
            this.openExternalIffToolStripMenuItem.Size = new System.Drawing.Size(199, 22);
            this.openExternalIffToolStripMenuItem.Text = "Open External Iff/Spf...";
            // 
            // iFFFileToolStripMenuItem
            // 
            this.iFFFileToolStripMenuItem.Name = "iFFFileToolStripMenuItem";
            this.iFFFileToolStripMenuItem.Size = new System.Drawing.Size(180, 22);
            this.iFFFileToolStripMenuItem.Text = "Iff File...";
            this.iFFFileToolStripMenuItem.Click += new System.EventHandler(this.openExternalIffToolStripMenuItem_Click);
            // 
            // sPFFileToolStripMenuItem
            // 
            this.sPFFileToolStripMenuItem.Name = "sPFFileToolStripMenuItem";
            this.sPFFileToolStripMenuItem.Size = new System.Drawing.Size(180, 22);
            this.sPFFileToolStripMenuItem.Text = "Spf File...";
            this.sPFFileToolStripMenuItem.Click += new System.EventHandler(this.sPFFileToolStripMenuItem_Click);
            // 
            // fieldEncodingReverserToolStripMenuItem
            // 
            this.fieldEncodingReverserToolStripMenuItem.Name = "fieldEncodingReverserToolStripMenuItem";
            this.fieldEncodingReverserToolStripMenuItem.Size = new System.Drawing.Size(199, 22);
            this.fieldEncodingReverserToolStripMenuItem.Text = "Field Encoding Reverser";
            this.fieldEncodingReverserToolStripMenuItem.Click += new System.EventHandler(this.fieldEncodingReverserToolStripMenuItem_Click);
            // 
            // windowToolStripMenuItem
            // 
            this.windowToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.hideAllToolStripMenuItem,
            this.toolStripSeparator1});
            this.windowToolStripMenuItem.Name = "windowToolStripMenuItem";
            this.windowToolStripMenuItem.Size = new System.Drawing.Size(63, 20);
            this.windowToolStripMenuItem.Text = "Window";
            this.windowToolStripMenuItem.DropDownOpening += new System.EventHandler(this.windowToolStripMenuItem_DropDownOpening);
            // 
            // hideAllToolStripMenuItem
            // 
            this.hideAllToolStripMenuItem.Name = "hideAllToolStripMenuItem";
            this.hideAllToolStripMenuItem.Size = new System.Drawing.Size(180, 22);
            this.hideAllToolStripMenuItem.Text = "Hide All";
            this.hideAllToolStripMenuItem.Click += new System.EventHandler(this.hideAllToolStripMenuItem_Click);
            // 
            // toolStripSeparator1
            // 
            this.toolStripSeparator1.Name = "toolStripSeparator1";
            this.toolStripSeparator1.Size = new System.Drawing.Size(177, 6);
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
            this.aboutToolStripMenuItem.Size = new System.Drawing.Size(107, 22);
            this.aboutToolStripMenuItem.Text = "About";
            this.aboutToolStripMenuItem.Click += new System.EventHandler(this.aboutToolStripMenuItem_Click);
            // 
            // UtilityTabs
            // 
            this.UtilityTabs.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.UtilityTabs.Controls.Add(this.OverviewTab);
            this.UtilityTabs.Controls.Add(this.BrowserTab);
            this.UtilityTabs.Controls.Add(this.InspectorTab);
            this.UtilityTabs.Location = new System.Drawing.Point(12, 27);
            this.UtilityTabs.Name = "UtilityTabs";
            this.UtilityTabs.SelectedIndex = 0;
            this.UtilityTabs.Size = new System.Drawing.Size(738, 484);
            this.UtilityTabs.TabIndex = 23;
            // 
            // OverviewTab
            // 
            this.OverviewTab.Controls.Add(this.groupBox1);
            this.OverviewTab.Controls.Add(this.AllTable);
            this.OverviewTab.Controls.Add(this.groupBox2);
            this.OverviewTab.Controls.Add(this.ChangesLabel);
            this.OverviewTab.Controls.Add(this.ChangesView);
            this.OverviewTab.Location = new System.Drawing.Point(4, 22);
            this.OverviewTab.Name = "OverviewTab";
            this.OverviewTab.Padding = new System.Windows.Forms.Padding(3);
            this.OverviewTab.Size = new System.Drawing.Size(730, 458);
            this.OverviewTab.TabIndex = 2;
            this.OverviewTab.Text = "Resources";
            this.OverviewTab.UseVisualStyleBackColor = true;
            // 
            // groupBox1
            // 
            this.groupBox1.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.groupBox1.Controls.Add(this.ChunkSelection);
            this.groupBox1.Controls.Add(this.ChunkDiscard);
            this.groupBox1.Location = new System.Drawing.Point(592, 143);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.Size = new System.Drawing.Size(132, 66);
            this.groupBox1.TabIndex = 30;
            this.groupBox1.TabStop = false;
            this.groupBox1.Text = "Chunk";
            // 
            // ChunkSelection
            // 
            this.ChunkSelection.Location = new System.Drawing.Point(6, 16);
            this.ChunkSelection.Name = "ChunkSelection";
            this.ChunkSelection.Size = new System.Drawing.Size(120, 16);
            this.ChunkSelection.TabIndex = 3;
            this.ChunkSelection.Text = "6 in selection.";
            // 
            // ChunkDiscard
            // 
            this.ChunkDiscard.Location = new System.Drawing.Point(6, 35);
            this.ChunkDiscard.Name = "ChunkDiscard";
            this.ChunkDiscard.Size = new System.Drawing.Size(120, 23);
            this.ChunkDiscard.TabIndex = 1;
            this.ChunkDiscard.Text = "Discard Changes";
            this.ChunkDiscard.UseVisualStyleBackColor = true;
            this.ChunkDiscard.Click += new System.EventHandler(this.ChunkDiscard_Click);
            // 
            // AllTable
            // 
            this.AllTable.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.AllTable.ColumnCount = 2;
            this.AllTable.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.AllTable.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.AllTable.Controls.Add(this.SaveAll, 0, 0);
            this.AllTable.Controls.Add(this.DiscardAll, 1, 0);
            this.AllTable.Location = new System.Drawing.Point(6, 3);
            this.AllTable.Margin = new System.Windows.Forms.Padding(0);
            this.AllTable.Name = "AllTable";
            this.AllTable.RowCount = 1;
            this.AllTable.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.AllTable.Size = new System.Drawing.Size(580, 35);
            this.AllTable.TabIndex = 24;
            // 
            // SaveAll
            // 
            this.SaveAll.Dock = System.Windows.Forms.DockStyle.Fill;
            this.SaveAll.Location = new System.Drawing.Point(3, 3);
            this.SaveAll.Name = "SaveAll";
            this.SaveAll.Size = new System.Drawing.Size(284, 29);
            this.SaveAll.TabIndex = 26;
            this.SaveAll.Text = "Save All";
            this.SaveAll.UseVisualStyleBackColor = true;
            this.SaveAll.Click += new System.EventHandler(this.SaveAll_Click);
            // 
            // DiscardAll
            // 
            this.DiscardAll.Dock = System.Windows.Forms.DockStyle.Fill;
            this.DiscardAll.Location = new System.Drawing.Point(293, 3);
            this.DiscardAll.Name = "DiscardAll";
            this.DiscardAll.Size = new System.Drawing.Size(284, 29);
            this.DiscardAll.TabIndex = 26;
            this.DiscardAll.Text = "Discard All";
            this.DiscardAll.UseVisualStyleBackColor = true;
            this.DiscardAll.Click += new System.EventHandler(this.DiscardAll_Click);
            // 
            // groupBox2
            // 
            this.groupBox2.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.groupBox2.Controls.Add(this.IffSelection);
            this.groupBox2.Controls.Add(this.IffSave);
            this.groupBox2.Controls.Add(this.IffDiscard);
            this.groupBox2.Location = new System.Drawing.Point(592, 41);
            this.groupBox2.Name = "groupBox2";
            this.groupBox2.Size = new System.Drawing.Size(132, 96);
            this.groupBox2.TabIndex = 29;
            this.groupBox2.TabStop = false;
            this.groupBox2.Text = "Iff";
            // 
            // IffSelection
            // 
            this.IffSelection.ForeColor = System.Drawing.SystemColors.ControlText;
            this.IffSelection.Location = new System.Drawing.Point(6, 16);
            this.IffSelection.Name = "IffSelection";
            this.IffSelection.Size = new System.Drawing.Size(120, 16);
            this.IffSelection.TabIndex = 3;
            this.IffSelection.Text = "2 files selected.";
            // 
            // IffSave
            // 
            this.IffSave.Location = new System.Drawing.Point(6, 35);
            this.IffSave.Name = "IffSave";
            this.IffSave.Size = new System.Drawing.Size(120, 23);
            this.IffSave.TabIndex = 2;
            this.IffSave.Text = "Save Changes";
            this.IffSave.UseVisualStyleBackColor = true;
            this.IffSave.Click += new System.EventHandler(this.IffSave_Click);
            // 
            // IffDiscard
            // 
            this.IffDiscard.Location = new System.Drawing.Point(6, 64);
            this.IffDiscard.Name = "IffDiscard";
            this.IffDiscard.Size = new System.Drawing.Size(120, 23);
            this.IffDiscard.TabIndex = 1;
            this.IffDiscard.Text = "Discard Changes";
            this.IffDiscard.UseVisualStyleBackColor = true;
            this.IffDiscard.Click += new System.EventHandler(this.IffDiscard_Click);
            // 
            // ChangesLabel
            // 
            this.ChangesLabel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.ChangesLabel.Location = new System.Drawing.Point(6, 442);
            this.ChangesLabel.Name = "ChangesLabel";
            this.ChangesLabel.Size = new System.Drawing.Size(370, 16);
            this.ChangesLabel.TabIndex = 27;
            this.ChangesLabel.Text = "Changed 6 chunks in 2 files.";
            // 
            // ChangesView
            // 
            this.ChangesView.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.ChangesView.CheckBoxes = true;
            this.ChangesView.FullRowSelect = true;
            this.ChangesView.Indent = 10;
            this.ChangesView.Location = new System.Drawing.Point(6, 41);
            this.ChangesView.Name = "ChangesView";
            treeNode1.Name = "Node1";
            treeNode1.Text = "(BHAV #4000) Init";
            treeNode2.Name = "Node2";
            treeNode2.Text = "(SPR2 #254) Fish Sprite";
            treeNode3.Name = "Node3";
            treeNode3.Text = "(DGRP #100) Dead 1";
            treeNode4.Name = "Node4";
            treeNode4.Text = "(DGRP #101) Dead 2";
            treeNode5.Name = "exampleNode";
            treeNode5.Text = "aquarium.iff";
            treeNode6.Name = "Node7";
            treeNode6.Text = "(BHAV #4023) Interaction - Read Inscription";
            treeNode7.Name = "Node8";
            treeNode7.Text = "(CTSS #223) Plaque CTSS";
            treeNode8.Name = "Node6";
            treeNode8.Text = "Content/Objects/objPlaque.iff";
            this.ChangesView.Nodes.AddRange(new System.Windows.Forms.TreeNode[] {
            treeNode5,
            treeNode8});
            this.ChangesView.ShowRootLines = false;
            this.ChangesView.Size = new System.Drawing.Size(580, 398);
            this.ChangesView.TabIndex = 24;
            this.ChangesView.AfterCheck += new System.Windows.Forms.TreeViewEventHandler(this.ChangesView_AfterCheck);
            // 
            // BrowserTab
            // 
            this.BrowserTab.Controls.Add(this.NewOBJButton);
            this.BrowserTab.Controls.Add(this.CreateButton);
            this.BrowserTab.Controls.Add(this.CloneButton);
            this.BrowserTab.Controls.Add(this.EditButton);
            this.BrowserTab.Controls.Add(this.Browser);
            this.BrowserTab.Location = new System.Drawing.Point(4, 22);
            this.BrowserTab.Name = "BrowserTab";
            this.BrowserTab.Padding = new System.Windows.Forms.Padding(3);
            this.BrowserTab.Size = new System.Drawing.Size(730, 458);
            this.BrowserTab.TabIndex = 0;
            this.BrowserTab.Text = "Object Browser";
            this.BrowserTab.UseVisualStyleBackColor = true;
            // 
            // NewOBJButton
            // 
            this.NewOBJButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.NewOBJButton.Location = new System.Drawing.Point(531, 381);
            this.NewOBJButton.Name = "NewOBJButton";
            this.NewOBJButton.Size = new System.Drawing.Size(186, 23);
            this.NewOBJButton.TabIndex = 22;
            this.NewOBJButton.Text = "Create New Object";
            this.NewOBJButton.UseVisualStyleBackColor = true;
            this.NewOBJButton.Click += new System.EventHandler(this.NewOBJButton_Click);
            // 
            // Browser
            // 
            this.Browser.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.Browser.Location = new System.Drawing.Point(3, 3);
            this.Browser.Name = "Browser";
            this.Browser.Size = new System.Drawing.Size(724, 452);
            this.Browser.TabIndex = 0;
            // 
            // InspectorTab
            // 
            this.InspectorTab.Controls.Add(this.entityInspector1);
            this.InspectorTab.Location = new System.Drawing.Point(4, 22);
            this.InspectorTab.Name = "InspectorTab";
            this.InspectorTab.Padding = new System.Windows.Forms.Padding(3);
            this.InspectorTab.Size = new System.Drawing.Size(730, 458);
            this.InspectorTab.TabIndex = 1;
            this.InspectorTab.Text = "VMEntity Inspector";
            this.InspectorTab.UseVisualStyleBackColor = true;
            // 
            // entityInspector1
            // 
            this.entityInspector1.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.entityInspector1.Location = new System.Drawing.Point(3, 3);
            this.entityInspector1.Name = "entityInspector1";
            this.entityInspector1.Size = new System.Drawing.Size(724, 452);
            this.entityInspector1.TabIndex = 0;
            // 
            // bothToolStripMenuItem
            // 
            this.bothToolStripMenuItem.Name = "bothToolStripMenuItem";
            this.bothToolStripMenuItem.Size = new System.Drawing.Size(180, 22);
            this.bothToolStripMenuItem.Text = "Both...";
            this.bothToolStripMenuItem.Click += new System.EventHandler(this.bothToolStripMenuItem_Click);
            // 
            // MainWindow
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(762, 523);
            this.Controls.Add(this.UtilityTabs);
            this.Controls.Add(this.menuStrip1);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.MainMenuStrip = this.menuStrip1;
            this.Name = "MainWindow";
            this.Text = "Volcanic";
            this.Activated += new System.EventHandler(this.MainWindow_Activated);
            this.FormClosed += new System.Windows.Forms.FormClosedEventHandler(this.MainWindow_FormClosed);
            this.menuStrip1.ResumeLayout(false);
            this.menuStrip1.PerformLayout();
            this.UtilityTabs.ResumeLayout(false);
            this.OverviewTab.ResumeLayout(false);
            this.groupBox1.ResumeLayout(false);
            this.AllTable.ResumeLayout(false);
            this.groupBox2.ResumeLayout(false);
            this.BrowserTab.ResumeLayout(false);
            this.InspectorTab.ResumeLayout(false);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private ObjectBrowser Browser;
        private System.Windows.Forms.Button CreateButton;
        private System.Windows.Forms.Button EditButton;
        private System.Windows.Forms.Button CloneButton;
        private System.Windows.Forms.MenuStrip menuStrip1;
        private System.Windows.Forms.ToolStripMenuItem toolsToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem windowToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem hideAllToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem helpToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem aboutToolStripMenuItem;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparator1;
        private System.Windows.Forms.TabControl UtilityTabs;
        private System.Windows.Forms.TabPage BrowserTab;
        private System.Windows.Forms.TabPage InspectorTab;
        private System.Windows.Forms.ToolStripMenuItem newToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem objectToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem semiGlobalToolStripMenuItem;
        private System.Windows.Forms.TabPage OverviewTab;
        private System.Windows.Forms.GroupBox groupBox2;
        private System.Windows.Forms.Label ChangesLabel;
        private System.Windows.Forms.Button DiscardAll;
        private System.Windows.Forms.TreeView ChangesView;
        private System.Windows.Forms.TableLayoutPanel AllTable;
        private System.Windows.Forms.Button SaveAll;
        private System.Windows.Forms.Label IffSelection;
        private System.Windows.Forms.Button IffSave;
        private System.Windows.Forms.Button IffDiscard;
        private System.Windows.Forms.GroupBox groupBox1;
        private System.Windows.Forms.Label ChunkSelection;
        private System.Windows.Forms.Button ChunkDiscard;
        private EntityInspector entityInspector1;
        private System.Windows.Forms.Button NewOBJButton;
        private System.Windows.Forms.ToolStripMenuItem dataServiceEditorToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem simAnticsAOTToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem saveGlobalscsToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem avatarToolToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem openExternalIffToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem fieldEncodingReverserToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem iFFFileToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem sPFFileToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem bothToolStripMenuItem;
    }
}
