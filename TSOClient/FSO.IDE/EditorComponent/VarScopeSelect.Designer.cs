namespace FSO.IDE.EditorComponent
{
    partial class VarScopeSelect
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
            System.Windows.Forms.TreeNode treeNode1 = new System.Windows.Forms.TreeNode("Object Data");
            System.Windows.Forms.TreeNode treeNode2 = new System.Windows.Forms.TreeNode("Person Data");
            System.Windows.Forms.TreeNode treeNode3 = new System.Windows.Forms.TreeNode("My", new System.Windows.Forms.TreeNode[] {
            treeNode1,
            treeNode2});
            System.Windows.Forms.TreeNode treeNode4 = new System.Windows.Forms.TreeNode("Dynamic Sprite Flag[temp]");
            System.Windows.Forms.TreeNode treeNode5 = new System.Windows.Forms.TreeNode("Stack Object\'s", new System.Windows.Forms.TreeNode[] {
            treeNode4});
            this.SourceTree = new System.Windows.Forms.TreeView();
            this.SourceSearch = new System.Windows.Forms.TextBox();
            this.SourceDesc = new System.Windows.Forms.Label();
            this.OKButton = new System.Windows.Forms.Button();
            this.CancelButton = new System.Windows.Forms.Button();
            this.DataSearch = new System.Windows.Forms.TextBox();
            this.SourceLabel = new System.Windows.Forms.Label();
            this.DataLabel = new System.Windows.Forms.Label();
            this.DataList = new System.Windows.Forms.ListBox();
            this.DataValue = new System.Windows.Forms.NumericUpDown();
            this.DataDesc = new System.Windows.Forms.Label();
            ((System.ComponentModel.ISupportInitialize)(this.DataValue)).BeginInit();
            this.SuspendLayout();
            // 
            // SourceTree
            // 
            this.SourceTree.FullRowSelect = true;
            this.SourceTree.HideSelection = false;
            this.SourceTree.HotTracking = true;
            this.SourceTree.ImeMode = System.Windows.Forms.ImeMode.Off;
            this.SourceTree.Indent = 15;
            this.SourceTree.ItemHeight = 16;
            this.SourceTree.Location = new System.Drawing.Point(12, 52);
            this.SourceTree.Name = "SourceTree";
            treeNode1.Name = "Node4";
            treeNode1.Text = "Object Data";
            treeNode2.Name = "Node5";
            treeNode2.Text = "Person Data";
            treeNode3.Name = "Node0";
            treeNode3.Text = "My";
            treeNode4.Name = "Node7";
            treeNode4.Text = "Dynamic Sprite Flag[temp]";
            treeNode5.Name = "Node6";
            treeNode5.Text = "Stack Object\'s";
            this.SourceTree.Nodes.AddRange(new System.Windows.Forms.TreeNode[] {
            treeNode3,
            treeNode5});
            this.SourceTree.RightToLeft = System.Windows.Forms.RightToLeft.No;
            this.SourceTree.ShowRootLines = false;
            this.SourceTree.Size = new System.Drawing.Size(195, 196);
            this.SourceTree.TabIndex = 2;
            this.SourceTree.TabStop = false;
            this.SourceTree.AfterSelect += new System.Windows.Forms.TreeViewEventHandler(this.SourceTree_AfterSelect);
            this.SourceTree.Enter += new System.EventHandler(this.SourceTree_Enter);
            this.SourceTree.KeyDown += new System.Windows.Forms.KeyEventHandler(this.SourceTree_KeyDown);
            // 
            // SourceSearch
            // 
            this.SourceSearch.Location = new System.Drawing.Point(12, 26);
            this.SourceSearch.Name = "SourceSearch";
            this.SourceSearch.Size = new System.Drawing.Size(195, 20);
            this.SourceSearch.TabIndex = 0;
            this.SourceSearch.TextChanged += new System.EventHandler(this.SourceSearch_TextChanged);
            this.SourceSearch.KeyDown += new System.Windows.Forms.KeyEventHandler(this.SourceSearch_KeyDown);
            // 
            // SourceDesc
            // 
            this.SourceDesc.Location = new System.Drawing.Point(10, 253);
            this.SourceDesc.Margin = new System.Windows.Forms.Padding(0);
            this.SourceDesc.Name = "SourceDesc";
            this.SourceDesc.Size = new System.Drawing.Size(196, 43);
            this.SourceDesc.TabIndex = 2;
            this.SourceDesc.Text = "Accesses the local variables of this stack frame, using the specified temp as an " +
    "index.";
            // 
            // OKButton
            // 
            this.OKButton.Location = new System.Drawing.Point(306, 268);
            this.OKButton.Name = "OKButton";
            this.OKButton.Size = new System.Drawing.Size(75, 23);
            this.OKButton.TabIndex = 3;
            this.OKButton.Text = "OK";
            this.OKButton.UseVisualStyleBackColor = true;
            this.OKButton.Click += new System.EventHandler(this.OKButton_Click);
            // 
            // CancelButton
            // 
            this.CancelButton.Location = new System.Drawing.Point(225, 268);
            this.CancelButton.Name = "CancelButton";
            this.CancelButton.Size = new System.Drawing.Size(75, 23);
            this.CancelButton.TabIndex = 4;
            this.CancelButton.Text = "Cancel";
            this.CancelButton.UseVisualStyleBackColor = true;
            this.CancelButton.Click += new System.EventHandler(this.CancelButton_Click);
            // 
            // DataSearch
            // 
            this.DataSearch.Location = new System.Drawing.Point(225, 26);
            this.DataSearch.Name = "DataSearch";
            this.DataSearch.Size = new System.Drawing.Size(93, 20);
            this.DataSearch.TabIndex = 1;
            this.DataSearch.TextChanged += new System.EventHandler(this.DataSearch_TextChanged);
            this.DataSearch.KeyDown += new System.Windows.Forms.KeyEventHandler(this.DataSearch_KeyDown);
            // 
            // SourceLabel
            // 
            this.SourceLabel.AutoSize = true;
            this.SourceLabel.Location = new System.Drawing.Point(9, 11);
            this.SourceLabel.Name = "SourceLabel";
            this.SourceLabel.Size = new System.Drawing.Size(41, 13);
            this.SourceLabel.TabIndex = 6;
            this.SourceLabel.Text = "Source";
            // 
            // DataLabel
            // 
            this.DataLabel.AutoSize = true;
            this.DataLabel.Location = new System.Drawing.Point(222, 11);
            this.DataLabel.Name = "DataLabel";
            this.DataLabel.Size = new System.Drawing.Size(30, 13);
            this.DataLabel.TabIndex = 7;
            this.DataLabel.Text = "Data";
            // 
            // DataList
            // 
            this.DataList.FormattingEnabled = true;
            this.DataList.Items.AddRange(new object[] {
            "Front",
            "Back",
            "Size"});
            this.DataList.Location = new System.Drawing.Point(225, 52);
            this.DataList.Name = "DataList";
            this.DataList.Size = new System.Drawing.Size(156, 121);
            this.DataList.TabIndex = 3;
            this.DataList.TabStop = false;
            this.DataList.SelectedIndexChanged += new System.EventHandler(this.DataList_SelectedIndexChanged);
            this.DataList.Enter += new System.EventHandler(this.DataList_Enter);
            // 
            // DataValue
            // 
            this.DataValue.Location = new System.Drawing.Point(324, 26);
            this.DataValue.Maximum = new decimal(new int[] {
            32767,
            0,
            0,
            0});
            this.DataValue.Minimum = new decimal(new int[] {
            32768,
            0,
            0,
            -2147483648});
            this.DataValue.Name = "DataValue";
            this.DataValue.Size = new System.Drawing.Size(57, 20);
            this.DataValue.TabIndex = 2;
            this.DataValue.ValueChanged += new System.EventHandler(this.DataValue_ValueChanged);
            // 
            // DataDesc
            // 
            this.DataDesc.Location = new System.Drawing.Point(225, 178);
            this.DataDesc.Name = "DataDesc";
            this.DataDesc.Size = new System.Drawing.Size(156, 77);
            this.DataDesc.TabIndex = 8;
            this.DataDesc.Text = "How \"dirty\" an object is. Used by the maid to determine what objects to clean. ";
            // 
            // VarScopeSelect
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(393, 303);
            this.Controls.Add(this.DataDesc);
            this.Controls.Add(this.DataValue);
            this.Controls.Add(this.DataList);
            this.Controls.Add(this.DataLabel);
            this.Controls.Add(this.SourceLabel);
            this.Controls.Add(this.DataSearch);
            this.Controls.Add(this.CancelButton);
            this.Controls.Add(this.OKButton);
            this.Controls.Add(this.SourceDesc);
            this.Controls.Add(this.SourceSearch);
            this.Controls.Add(this.SourceTree);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.Name = "VarScopeSelect";
            this.Text = "Select a Variable Scope";
            ((System.ComponentModel.ISupportInitialize)(this.DataValue)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.TreeView SourceTree;
        private System.Windows.Forms.TextBox SourceSearch;
        private System.Windows.Forms.Label SourceDesc;
        private System.Windows.Forms.Button OKButton;
        private System.Windows.Forms.Button CancelButton;
        private System.Windows.Forms.TextBox DataSearch;
        private System.Windows.Forms.Label SourceLabel;
        private System.Windows.Forms.Label DataLabel;
        private System.Windows.Forms.ListBox DataList;
        private System.Windows.Forms.NumericUpDown DataValue;
        private System.Windows.Forms.Label DataDesc;
    }
}