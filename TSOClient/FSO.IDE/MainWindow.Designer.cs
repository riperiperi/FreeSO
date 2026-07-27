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
            TreeNode treeNode1 = new TreeNode("(BHAV #4000) Init");
            TreeNode treeNode2 = new TreeNode("(SPR2 #254) Fish Sprite");
            TreeNode treeNode3 = new TreeNode("(DGRP #100) Dead 1");
            TreeNode treeNode4 = new TreeNode("(DGRP #101) Dead 2");
            TreeNode treeNode5 = new TreeNode("aquarium.iff", new TreeNode[] { treeNode1, treeNode2, treeNode3, treeNode4 });
            TreeNode treeNode6 = new TreeNode("(BHAV #4023) Interaction - Read Inscription");
            TreeNode treeNode7 = new TreeNode("(CTSS #223) Plaque CTSS");
            TreeNode treeNode8 = new TreeNode("Content/Objects/objPlaque.iff", new TreeNode[] { treeNode6, treeNode7 });
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(MainWindow));
            CreateButton = new Button();
            EditButton = new Button();
            CloneButton = new Button();
            menuStrip1 = new MenuStrip();
            newToolStripMenuItem = new ToolStripMenuItem();
            objectToolStripMenuItem = new ToolStripMenuItem();
            semiGlobalToolStripMenuItem = new ToolStripMenuItem();
            toolsToolStripMenuItem = new ToolStripMenuItem();
            dataServiceEditorToolStripMenuItem = new ToolStripMenuItem();
            simAnticsAOTToolStripMenuItem = new ToolStripMenuItem();
            saveGlobalscsToolStripMenuItem = new ToolStripMenuItem();
            avatarToolToolStripMenuItem = new ToolStripMenuItem();
            openExternalIffToolStripMenuItem = new ToolStripMenuItem();
            fieldEncodingReverserToolStripMenuItem = new ToolStripMenuItem();
            houseSpyTS1ToolStripMenuItem = new ToolStripMenuItem();
            windowToolStripMenuItem = new ToolStripMenuItem();
            hideAllToolStripMenuItem = new ToolStripMenuItem();
            toolStripSeparator1 = new ToolStripSeparator();
            helpToolStripMenuItem = new ToolStripMenuItem();
            aboutToolStripMenuItem = new ToolStripMenuItem();
            UtilityTabs = new TabControl();
            OverviewTab = new TabPage();
            groupBox1 = new GroupBox();
            ChunkSelection = new Label();
            ChunkDiscard = new Button();
            AllTable = new TableLayoutPanel();
            SaveAll = new Button();
            DiscardAll = new Button();
            groupBox2 = new GroupBox();
            IffSelection = new Label();
            IffSave = new Button();
            IffDiscard = new Button();
            ChangesLabel = new Label();
            ChangesView = new TreeView();
            BrowserTab = new TabPage();
            NewOBJButton = new Button();
            Browser = new ObjectBrowser();
            InspectorTab = new TabPage();
            entityInspector1 = new EntityInspector();
            menuStrip1.SuspendLayout();
            UtilityTabs.SuspendLayout();
            OverviewTab.SuspendLayout();
            groupBox1.SuspendLayout();
            AllTable.SuspendLayout();
            groupBox2.SuspendLayout();
            BrowserTab.SuspendLayout();
            InspectorTab.SuspendLayout();
            SuspendLayout();
            // 
            // CreateButton
            // 
            CreateButton.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
            CreateButton.Location = new Point(531, 352);
            CreateButton.Name = "CreateButton";
            CreateButton.Size = new Size(186, 23);
            CreateButton.TabIndex = 21;
            CreateButton.Text = "Create New Object Instance";
            CreateButton.UseVisualStyleBackColor = true;
            CreateButton.Click += CreateButton_Click;
            // 
            // EditButton
            // 
            EditButton.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
            EditButton.Location = new Point(531, 323);
            EditButton.Name = "EditButton";
            EditButton.Size = new Size(186, 23);
            EditButton.TabIndex = 20;
            EditButton.Text = "Edit Object";
            EditButton.UseVisualStyleBackColor = true;
            EditButton.Click += button2_Click;
            // 
            // CloneButton
            // 
            CloneButton.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
            CloneButton.Enabled = false;
            CloneButton.Location = new Point(531, 410);
            CloneButton.Name = "CloneButton";
            CloneButton.Size = new Size(186, 23);
            CloneButton.TabIndex = 19;
            CloneButton.Text = "Clone Object (.piff)";
            CloneButton.UseVisualStyleBackColor = true;
            CloneButton.Click += button1_Click;
            // 
            // menuStrip1
            // 
            menuStrip1.Items.AddRange(new ToolStripItem[] { newToolStripMenuItem, toolsToolStripMenuItem, windowToolStripMenuItem, helpToolStripMenuItem });
            menuStrip1.Location = new Point(0, 0);
            menuStrip1.Name = "menuStrip1";
            menuStrip1.Size = new Size(762, 24);
            menuStrip1.TabIndex = 22;
            menuStrip1.Text = "menuStrip1";
            // 
            // newToolStripMenuItem
            // 
            newToolStripMenuItem.DropDownItems.AddRange(new ToolStripItem[] { objectToolStripMenuItem, semiGlobalToolStripMenuItem });
            newToolStripMenuItem.Name = "newToolStripMenuItem";
            newToolStripMenuItem.Size = new Size(43, 20);
            newToolStripMenuItem.Text = "New";
            // 
            // objectToolStripMenuItem
            // 
            objectToolStripMenuItem.Name = "objectToolStripMenuItem";
            objectToolStripMenuItem.Size = new Size(139, 22);
            objectToolStripMenuItem.Text = "Object";
            objectToolStripMenuItem.Click += NewOBJButton_Click;
            // 
            // semiGlobalToolStripMenuItem
            // 
            semiGlobalToolStripMenuItem.Name = "semiGlobalToolStripMenuItem";
            semiGlobalToolStripMenuItem.Size = new Size(139, 22);
            semiGlobalToolStripMenuItem.Text = "Semi-Global";
            semiGlobalToolStripMenuItem.Click += semiGlobalToolStripMenuItem_Click;
            // 
            // toolsToolStripMenuItem
            // 
            toolsToolStripMenuItem.DropDownItems.AddRange(new ToolStripItem[] { dataServiceEditorToolStripMenuItem, simAnticsAOTToolStripMenuItem, avatarToolToolStripMenuItem, openExternalIffToolStripMenuItem, fieldEncodingReverserToolStripMenuItem, houseSpyTS1ToolStripMenuItem });
            toolsToolStripMenuItem.Name = "toolsToolStripMenuItem";
            toolsToolStripMenuItem.Size = new Size(47, 20);
            toolsToolStripMenuItem.Text = "Tools";
            // 
            // dataServiceEditorToolStripMenuItem
            // 
            dataServiceEditorToolStripMenuItem.Name = "dataServiceEditorToolStripMenuItem";
            dataServiceEditorToolStripMenuItem.Size = new Size(199, 22);
            dataServiceEditorToolStripMenuItem.Text = "Data Service Editor";
            dataServiceEditorToolStripMenuItem.Click += dataServiceEditorToolStripMenuItem_Click;
            // 
            // simAnticsAOTToolStripMenuItem
            // 
            simAnticsAOTToolStripMenuItem.DropDownItems.AddRange(new ToolStripItem[] { saveGlobalscsToolStripMenuItem });
            simAnticsAOTToolStripMenuItem.Name = "simAnticsAOTToolStripMenuItem";
            simAnticsAOTToolStripMenuItem.Size = new Size(199, 22);
            simAnticsAOTToolStripMenuItem.Text = "SimAntics AOT";
            // 
            // saveGlobalscsToolStripMenuItem
            // 
            saveGlobalscsToolStripMenuItem.Name = "saveGlobalscsToolStripMenuItem";
            saveGlobalscsToolStripMenuItem.Size = new Size(216, 22);
            saveGlobalscsToolStripMenuItem.Text = "Generate AOT Sources (.cs)";
            saveGlobalscsToolStripMenuItem.Click += saveGlobalscsToolStripMenuItem_Click;
            // 
            // avatarToolToolStripMenuItem
            // 
            avatarToolToolStripMenuItem.Name = "avatarToolToolStripMenuItem";
            avatarToolToolStripMenuItem.Size = new Size(199, 22);
            avatarToolToolStripMenuItem.Text = "Avatar Tool";
            avatarToolToolStripMenuItem.Click += avatarToolToolStripMenuItem_Click;
            // 
            // openExternalIffToolStripMenuItem
            // 
            openExternalIffToolStripMenuItem.Name = "openExternalIffToolStripMenuItem";
            openExternalIffToolStripMenuItem.Size = new Size(199, 22);
            openExternalIffToolStripMenuItem.Text = "Open External Iff...";
            openExternalIffToolStripMenuItem.Click += openExternalIffToolStripMenuItem_Click;
            // 
            // fieldEncodingReverserToolStripMenuItem
            // 
            fieldEncodingReverserToolStripMenuItem.Name = "fieldEncodingReverserToolStripMenuItem";
            fieldEncodingReverserToolStripMenuItem.Size = new Size(199, 22);
            fieldEncodingReverserToolStripMenuItem.Text = "Field Encoding Reverser";
            fieldEncodingReverserToolStripMenuItem.Click += fieldEncodingReverserToolStripMenuItem_Click;
            // 
            // houseSpyTS1ToolStripMenuItem
            // 
            houseSpyTS1ToolStripMenuItem.Name = "houseSpyTS1ToolStripMenuItem";
            houseSpyTS1ToolStripMenuItem.Size = new Size(199, 22);
            houseSpyTS1ToolStripMenuItem.Text = "House Spy (TS1)";
            houseSpyTS1ToolStripMenuItem.Click += houseSpyTS1ToolStripMenuItem_Click;
            // 
            // windowToolStripMenuItem
            // 
            windowToolStripMenuItem.DropDownItems.AddRange(new ToolStripItem[] { hideAllToolStripMenuItem, toolStripSeparator1 });
            windowToolStripMenuItem.Name = "windowToolStripMenuItem";
            windowToolStripMenuItem.Size = new Size(63, 20);
            windowToolStripMenuItem.Text = "Window";
            windowToolStripMenuItem.DropDownOpening += windowToolStripMenuItem_DropDownOpening;
            // 
            // hideAllToolStripMenuItem
            // 
            hideAllToolStripMenuItem.Name = "hideAllToolStripMenuItem";
            hideAllToolStripMenuItem.Size = new Size(116, 22);
            hideAllToolStripMenuItem.Text = "Hide All";
            hideAllToolStripMenuItem.Click += hideAllToolStripMenuItem_Click;
            // 
            // toolStripSeparator1
            // 
            toolStripSeparator1.Name = "toolStripSeparator1";
            toolStripSeparator1.Size = new Size(113, 6);
            // 
            // helpToolStripMenuItem
            // 
            helpToolStripMenuItem.DropDownItems.AddRange(new ToolStripItem[] { aboutToolStripMenuItem });
            helpToolStripMenuItem.Name = "helpToolStripMenuItem";
            helpToolStripMenuItem.Size = new Size(44, 20);
            helpToolStripMenuItem.Text = "Help";
            // 
            // aboutToolStripMenuItem
            // 
            aboutToolStripMenuItem.Name = "aboutToolStripMenuItem";
            aboutToolStripMenuItem.Size = new Size(107, 22);
            aboutToolStripMenuItem.Text = "About";
            aboutToolStripMenuItem.Click += aboutToolStripMenuItem_Click;
            // 
            // UtilityTabs
            // 
            UtilityTabs.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            UtilityTabs.Controls.Add(OverviewTab);
            UtilityTabs.Controls.Add(BrowserTab);
            UtilityTabs.Controls.Add(InspectorTab);
            UtilityTabs.Location = new Point(12, 27);
            UtilityTabs.Name = "UtilityTabs";
            UtilityTabs.SelectedIndex = 0;
            UtilityTabs.Size = new Size(738, 484);
            UtilityTabs.TabIndex = 23;
            // 
            // OverviewTab
            // 
            OverviewTab.Controls.Add(groupBox1);
            OverviewTab.Controls.Add(AllTable);
            OverviewTab.Controls.Add(groupBox2);
            OverviewTab.Controls.Add(ChangesLabel);
            OverviewTab.Controls.Add(ChangesView);
            OverviewTab.Location = new Point(4, 22);
            OverviewTab.Name = "OverviewTab";
            OverviewTab.Padding = new Padding(3);
            OverviewTab.Size = new Size(730, 458);
            OverviewTab.TabIndex = 2;
            OverviewTab.Text = "Resources";
            OverviewTab.UseVisualStyleBackColor = true;
            // 
            // groupBox1
            // 
            groupBox1.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            groupBox1.Controls.Add(ChunkSelection);
            groupBox1.Controls.Add(ChunkDiscard);
            groupBox1.Location = new Point(592, 143);
            groupBox1.Name = "groupBox1";
            groupBox1.Size = new Size(132, 66);
            groupBox1.TabIndex = 30;
            groupBox1.TabStop = false;
            groupBox1.Text = "Chunk";
            // 
            // ChunkSelection
            // 
            ChunkSelection.Location = new Point(6, 16);
            ChunkSelection.Name = "ChunkSelection";
            ChunkSelection.Size = new Size(120, 16);
            ChunkSelection.TabIndex = 3;
            ChunkSelection.Text = "6 in selection.";
            // 
            // ChunkDiscard
            // 
            ChunkDiscard.Location = new Point(6, 35);
            ChunkDiscard.Name = "ChunkDiscard";
            ChunkDiscard.Size = new Size(120, 23);
            ChunkDiscard.TabIndex = 1;
            ChunkDiscard.Text = "Discard Changes";
            ChunkDiscard.UseVisualStyleBackColor = true;
            ChunkDiscard.Click += ChunkDiscard_Click;
            // 
            // AllTable
            // 
            AllTable.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            AllTable.ColumnCount = 2;
            AllTable.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            AllTable.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            AllTable.Controls.Add(SaveAll, 0, 0);
            AllTable.Controls.Add(DiscardAll, 1, 0);
            AllTable.Location = new Point(6, 3);
            AllTable.Margin = new Padding(0);
            AllTable.Name = "AllTable";
            AllTable.RowCount = 1;
            AllTable.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            AllTable.Size = new Size(580, 35);
            AllTable.TabIndex = 24;
            // 
            // SaveAll
            // 
            SaveAll.Dock = DockStyle.Fill;
            SaveAll.Location = new Point(3, 3);
            SaveAll.Name = "SaveAll";
            SaveAll.Size = new Size(284, 29);
            SaveAll.TabIndex = 26;
            SaveAll.Text = "Save All";
            SaveAll.UseVisualStyleBackColor = true;
            SaveAll.Click += SaveAll_Click;
            // 
            // DiscardAll
            // 
            DiscardAll.Dock = DockStyle.Fill;
            DiscardAll.Location = new Point(293, 3);
            DiscardAll.Name = "DiscardAll";
            DiscardAll.Size = new Size(284, 29);
            DiscardAll.TabIndex = 26;
            DiscardAll.Text = "Discard All";
            DiscardAll.UseVisualStyleBackColor = true;
            DiscardAll.Click += DiscardAll_Click;
            // 
            // groupBox2
            // 
            groupBox2.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            groupBox2.Controls.Add(IffSelection);
            groupBox2.Controls.Add(IffSave);
            groupBox2.Controls.Add(IffDiscard);
            groupBox2.Location = new Point(592, 41);
            groupBox2.Name = "groupBox2";
            groupBox2.Size = new Size(132, 96);
            groupBox2.TabIndex = 29;
            groupBox2.TabStop = false;
            groupBox2.Text = "Iff";
            // 
            // IffSelection
            // 
            IffSelection.ForeColor = SystemColors.ControlText;
            IffSelection.Location = new Point(6, 16);
            IffSelection.Name = "IffSelection";
            IffSelection.Size = new Size(120, 16);
            IffSelection.TabIndex = 3;
            IffSelection.Text = "2 files selected.";
            // 
            // IffSave
            // 
            IffSave.Location = new Point(6, 35);
            IffSave.Name = "IffSave";
            IffSave.Size = new Size(120, 23);
            IffSave.TabIndex = 2;
            IffSave.Text = "Save Changes";
            IffSave.UseVisualStyleBackColor = true;
            IffSave.Click += IffSave_Click;
            // 
            // IffDiscard
            // 
            IffDiscard.Location = new Point(6, 64);
            IffDiscard.Name = "IffDiscard";
            IffDiscard.Size = new Size(120, 23);
            IffDiscard.TabIndex = 1;
            IffDiscard.Text = "Discard Changes";
            IffDiscard.UseVisualStyleBackColor = true;
            IffDiscard.Click += IffDiscard_Click;
            // 
            // ChangesLabel
            // 
            ChangesLabel.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
            ChangesLabel.Location = new Point(6, 442);
            ChangesLabel.Name = "ChangesLabel";
            ChangesLabel.Size = new Size(370, 16);
            ChangesLabel.TabIndex = 27;
            ChangesLabel.Text = "Changed 6 chunks in 2 files.";
            // 
            // ChangesView
            // 
            ChangesView.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            ChangesView.CheckBoxes = true;
            ChangesView.FullRowSelect = true;
            ChangesView.Indent = 10;
            ChangesView.Location = new Point(6, 41);
            ChangesView.Name = "ChangesView";
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
            ChangesView.Nodes.AddRange(new TreeNode[] { treeNode5, treeNode8 });
            ChangesView.ShowRootLines = false;
            ChangesView.Size = new Size(580, 398);
            ChangesView.TabIndex = 24;
            ChangesView.AfterCheck += ChangesView_AfterCheck;
            // 
            // BrowserTab
            // 
            BrowserTab.Controls.Add(NewOBJButton);
            BrowserTab.Controls.Add(CreateButton);
            BrowserTab.Controls.Add(CloneButton);
            BrowserTab.Controls.Add(EditButton);
            BrowserTab.Controls.Add(Browser);
            BrowserTab.Location = new Point(4, 22);
            BrowserTab.Name = "BrowserTab";
            BrowserTab.Padding = new Padding(3);
            BrowserTab.Size = new Size(730, 458);
            BrowserTab.TabIndex = 0;
            BrowserTab.Text = "Object Browser";
            BrowserTab.UseVisualStyleBackColor = true;
            // 
            // NewOBJButton
            // 
            NewOBJButton.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
            NewOBJButton.Location = new Point(531, 381);
            NewOBJButton.Name = "NewOBJButton";
            NewOBJButton.Size = new Size(186, 23);
            NewOBJButton.TabIndex = 22;
            NewOBJButton.Text = "Create New Object";
            NewOBJButton.UseVisualStyleBackColor = true;
            NewOBJButton.Click += NewOBJButton_Click;
            // 
            // Browser
            // 
            Browser.Dock = DockStyle.Fill;
            Browser.Location = new Point(3, 3);
            Browser.Margin = new Padding(4, 3, 4, 3);
            Browser.Name = "Browser";
            Browser.Size = new Size(724, 452);
            Browser.TabIndex = 0;
            // 
            // InspectorTab
            // 
            InspectorTab.Controls.Add(entityInspector1);
            InspectorTab.Location = new Point(4, 22);
            InspectorTab.Name = "InspectorTab";
            InspectorTab.Padding = new Padding(3);
            InspectorTab.Size = new Size(730, 458);
            InspectorTab.TabIndex = 1;
            InspectorTab.Text = "VMEntity Inspector";
            InspectorTab.UseVisualStyleBackColor = true;
            // 
            // entityInspector1
            // 
            entityInspector1.Dock = DockStyle.Fill;
            entityInspector1.Location = new Point(3, 3);
            entityInspector1.Margin = new Padding(4, 3, 4, 3);
            entityInspector1.Name = "entityInspector1";
            entityInspector1.Size = new Size(724, 452);
            entityInspector1.TabIndex = 0;
            // 
            // MainWindow
            // 
            AutoScaleDimensions = new SizeF(96F, 96F);
            AutoScaleMode = AutoScaleMode.Dpi;
            ClientSize = new Size(762, 523);
            Controls.Add(UtilityTabs);
            Controls.Add(menuStrip1);
            Icon = (Icon)resources.GetObject("$this.Icon");
            MainMenuStrip = menuStrip1;
            Name = "MainWindow";
            Text = "Volcanic";
            Activated += MainWindow_Activated;
            FormClosed += MainWindow_FormClosed;
            menuStrip1.ResumeLayout(false);
            menuStrip1.PerformLayout();
            UtilityTabs.ResumeLayout(false);
            OverviewTab.ResumeLayout(false);
            groupBox1.ResumeLayout(false);
            AllTable.ResumeLayout(false);
            groupBox2.ResumeLayout(false);
            BrowserTab.ResumeLayout(false);
            InspectorTab.ResumeLayout(false);
            ResumeLayout(false);
            PerformLayout();

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
        private System.Windows.Forms.ToolStripMenuItem houseSpyTS1ToolStripMenuItem;
    }
}
