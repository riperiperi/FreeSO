namespace FSO.IDE
{
    partial class ObjectBrowser
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
            TreeNode treeNode1 = new TreeNode("Accessory Rack - Cheap");
            TreeNode treeNode2 = new TreeNode("Accessory Rack - Expensive");
            TreeNode treeNode3 = new TreeNode("Accessory Rack - Moderate");
            TreeNode treeNode4 = new TreeNode("accessoryrack", new TreeNode[] { treeNode1, treeNode2, treeNode3 });
            TreeNode treeNode5 = new TreeNode("Puzzle - 2 Person Portal - North");
            TreeNode treeNode6 = new TreeNode("Puzzle - 2 Person Portal - South");
            TreeNode treeNode7 = new TreeNode("Puzzle - 2 Person Portal - Tunnel");
            TreeNode treeNode8 = new TreeNode("Puzzle - 2 Person Portal", new TreeNode[] { treeNode5, treeNode6, treeNode7 });
            TreeNode treeNode9 = new TreeNode("2 Person Portal Controller");
            TreeNode treeNode10 = new TreeNode("2personpuzzle", new TreeNode[] { treeNode8, treeNode9 });
            ObjectSearch = new TextBox();
            ObjectTree = new TreeView();
            ObjNameLabel = new Label();
            ObjDescLabel = new Label();
            SearchButton = new Button();
            SearchDescribe = new Label();
            ObjMultitileLabel = new Label();
            ObjThumbnail = new FSO.IDE.Common.ObjThumbnailControl();
            SuspendLayout();
            // 
            // ObjectSearch
            // 
            ObjectSearch.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            ObjectSearch.Location = new Point(12, 13);
            ObjectSearch.Name = "ObjectSearch";
            ObjectSearch.Size = new Size(210, 22);
            ObjectSearch.TabIndex = 7;
            ObjectSearch.TextChanged += ObjectSearch_TextChanged;
            ObjectSearch.KeyDown += ObjectSearch_KeyDown;
            // 
            // ObjectTree
            // 
            ObjectTree.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            ObjectTree.FullRowSelect = true;
            ObjectTree.HideSelection = false;
            ObjectTree.HotTracking = true;
            ObjectTree.ImeMode = ImeMode.Off;
            ObjectTree.Indent = 15;
            ObjectTree.ItemHeight = 16;
            ObjectTree.Location = new Point(12, 39);
            ObjectTree.Name = "ObjectTree";
            treeNode1.Name = "Node2";
            treeNode1.Text = "Accessory Rack - Cheap";
            treeNode2.Name = "Node3";
            treeNode2.Text = "Accessory Rack - Expensive";
            treeNode3.Name = "Node4";
            treeNode3.Text = "Accessory Rack - Moderate";
            treeNode4.Name = "Node0";
            treeNode4.Text = "accessoryrack";
            treeNode5.Name = "Node7";
            treeNode5.Text = "Puzzle - 2 Person Portal - North";
            treeNode6.Name = "Node8";
            treeNode6.Text = "Puzzle - 2 Person Portal - South";
            treeNode7.Name = "Node9";
            treeNode7.Text = "Puzzle - 2 Person Portal - Tunnel";
            treeNode8.Name = "Node6";
            treeNode8.Text = "Puzzle - 2 Person Portal";
            treeNode9.Name = "Node10";
            treeNode9.Text = "2 Person Portal Controller";
            treeNode10.Name = "Node5";
            treeNode10.Text = "2personpuzzle";
            ObjectTree.Nodes.AddRange(new TreeNode[] { treeNode4, treeNode10 });
            ObjectTree.RightToLeft = RightToLeft.No;
            ObjectTree.ShowRootLines = false;
            ObjectTree.Size = new Size(272, 315);
            ObjectTree.TabIndex = 9;
            ObjectTree.TabStop = false;
            ObjectTree.AfterSelect += ObjectTree_AfterSelect;
            // 
            // ObjNameLabel
            // 
            ObjNameLabel.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            ObjNameLabel.AutoEllipsis = true;
            ObjNameLabel.Font = new Font("Microsoft Sans Serif", 9.75F, FontStyle.Regular, GraphicsUnit.Point, 0);
            ObjNameLabel.Location = new Point(294, 204);
            ObjNameLabel.Name = "ObjNameLabel";
            ObjNameLabel.Size = new Size(186, 17);
            ObjNameLabel.TabIndex = 12;
            ObjNameLabel.Text = "Accessory Rack - Cheap";
            ObjNameLabel.TextAlign = ContentAlignment.TopCenter;
            // 
            // ObjDescLabel
            // 
            ObjDescLabel.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            ObjDescLabel.Location = new Point(294, 222);
            ObjDescLabel.Name = "ObjDescLabel";
            ObjDescLabel.Size = new Size(186, 17);
            ObjDescLabel.TabIndex = 14;
            ObjDescLabel.Text = "§2000 - Job Object";
            ObjDescLabel.TextAlign = ContentAlignment.TopCenter;
            // 
            // SearchButton
            // 
            SearchButton.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            SearchButton.Location = new Point(228, 11);
            SearchButton.Name = "SearchButton";
            SearchButton.Size = new Size(56, 23);
            SearchButton.TabIndex = 15;
            SearchButton.Text = "Search";
            SearchButton.UseVisualStyleBackColor = true;
            SearchButton.Click += SearchButton_Click;
            // 
            // SearchDescribe
            // 
            SearchDescribe.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
            SearchDescribe.Location = new Point(12, 357);
            SearchDescribe.Name = "SearchDescribe";
            SearchDescribe.Size = new Size(234, 23);
            SearchDescribe.TabIndex = 16;
            SearchDescribe.Text = "Showing all objects.";
            // 
            // ObjMultitileLabel
            // 
            ObjMultitileLabel.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            ObjMultitileLabel.Location = new Point(294, 237);
            ObjMultitileLabel.Name = "ObjMultitileLabel";
            ObjMultitileLabel.Size = new Size(186, 17);
            ObjMultitileLabel.TabIndex = 17;
            ObjMultitileLabel.Text = "Multitile Master Object";
            ObjMultitileLabel.TextAlign = ContentAlignment.TopCenter;
            // 
            // ObjThumbnail
            // 
            ObjThumbnail.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            ObjThumbnail.Location = new Point(294, 13);
            ObjThumbnail.Name = "ObjThumbnail";
            ObjThumbnail.Size = new Size(186, 186);
            ObjThumbnail.TabIndex = 19;
            // 
            // ObjectBrowser
            // 
            AutoScaleDimensions = new SizeF(96F, 96F);
            AutoScaleMode = AutoScaleMode.Dpi;
            Controls.Add(ObjThumbnail);
            Controls.Add(ObjMultitileLabel);
            Controls.Add(SearchDescribe);
            Controls.Add(SearchButton);
            Controls.Add(ObjDescLabel);
            Controls.Add(ObjNameLabel);
            Controls.Add(ObjectSearch);
            Controls.Add(ObjectTree);
            Name = "ObjectBrowser";
            Size = new Size(492, 382);
            Load += ObjectBrowser_Load;
            ResumeLayout(false);
            PerformLayout();

        }

        #endregion
        private System.Windows.Forms.TextBox ObjectSearch;
        private System.Windows.Forms.TreeView ObjectTree;
        private System.Windows.Forms.Label ObjNameLabel;
        private System.Windows.Forms.Label ObjDescLabel;
        private System.Windows.Forms.Button SearchButton;
        private System.Windows.Forms.Label SearchDescribe;
        private System.Windows.Forms.Label ObjMultitileLabel;
        private Common.ObjThumbnailControl ObjThumbnail;
    }
}