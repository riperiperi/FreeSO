namespace FSO.IDE.ContentEditors
{
    partial class AvatarTool
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(AvatarTool));
            this.Animator = new FSO.IDE.Common.AvatarAnimatorControl();
            this.AnimationImportBox = new System.Windows.Forms.CheckedListBox();
            this.ImportAnimButton = new System.Windows.Forms.Button();
            this.MeshImportBox = new System.Windows.Forms.ListBox();
            this.ImportGLTFButton = new System.Windows.Forms.Button();
            this.NewSceneButton = new System.Windows.Forms.Button();
            this.ImportMeshButton = new System.Windows.Forms.Button();
            this.BrowserTabs = new System.Windows.Forms.TabControl();
            this.AnimationsPage = new System.Windows.Forms.TabPage();
            this.AnimationAdd = new System.Windows.Forms.Button();
            this.AnimationList = new System.Windows.Forms.ListBox();
            this.AnimationSearch = new System.Windows.Forms.TextBox();
            this.AccessoriesPage = new System.Windows.Forms.TabPage();
            this.AccessoryList = new System.Windows.Forms.ListBox();
            this.AccessorySearch = new System.Windows.Forms.TextBox();
            this.AccessoryClear = new System.Windows.Forms.Button();
            this.AccessoryAdd = new System.Windows.Forms.Button();
            this.AccessoryRemove = new System.Windows.Forms.Button();
            this.OutfitsPage = new System.Windows.Forms.TabPage();
            this.OutfitList = new System.Windows.Forms.ListBox();
            this.OutfitSearch = new System.Windows.Forms.TextBox();
            this.OutfitSet = new System.Windows.Forms.Button();
            this.SkeletonCombo = new System.Windows.Forms.ComboBox();
            this.AllAnimsCheck = new System.Windows.Forms.CheckBox();
            this.SkeletonLabel = new System.Windows.Forms.Label();
            this.ImportSkeletonButton = new System.Windows.Forms.Button();
            this.ExportGLTFButton = new System.Windows.Forms.Button();
            this.BrowserTabs.SuspendLayout();
            this.AnimationsPage.SuspendLayout();
            this.AccessoriesPage.SuspendLayout();
            this.OutfitsPage.SuspendLayout();
            this.SuspendLayout();
            // 
            // Animator
            // 
            this.Animator.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.Animator.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.Animator.Location = new System.Drawing.Point(244, 15);
            this.Animator.Name = "Animator";
            this.Animator.Size = new System.Drawing.Size(207, 360);
            this.Animator.TabIndex = 0;
            // 
            // AnimationImportBox
            // 
            this.AnimationImportBox.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.AnimationImportBox.FormattingEnabled = true;
            this.AnimationImportBox.IntegralHeight = false;
            this.AnimationImportBox.Location = new System.Drawing.Point(460, 12);
            this.AnimationImportBox.Name = "AnimationImportBox";
            this.AnimationImportBox.Size = new System.Drawing.Size(198, 233);
            this.AnimationImportBox.TabIndex = 1;
            this.AnimationImportBox.ItemCheck += new System.Windows.Forms.ItemCheckEventHandler(this.AnimationImportBox_ItemCheck);
            this.AnimationImportBox.SelectedIndexChanged += new System.EventHandler(this.AnimationImportBox_SelectedIndexChanged);
            // 
            // ImportAnimButton
            // 
            this.ImportAnimButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.ImportAnimButton.Location = new System.Drawing.Point(460, 251);
            this.ImportAnimButton.Name = "ImportAnimButton";
            this.ImportAnimButton.Size = new System.Drawing.Size(198, 23);
            this.ImportAnimButton.TabIndex = 2;
            this.ImportAnimButton.Text = "Import Selected Animations";
            this.ImportAnimButton.UseVisualStyleBackColor = true;
            this.ImportAnimButton.Click += new System.EventHandler(this.ImportAnimButton_Click);
            // 
            // MeshImportBox
            // 
            this.MeshImportBox.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.MeshImportBox.FormattingEnabled = true;
            this.MeshImportBox.Location = new System.Drawing.Point(460, 280);
            this.MeshImportBox.Name = "MeshImportBox";
            this.MeshImportBox.SelectionMode = System.Windows.Forms.SelectionMode.MultiSimple;
            this.MeshImportBox.Size = new System.Drawing.Size(198, 95);
            this.MeshImportBox.TabIndex = 3;
            this.MeshImportBox.SelectedIndexChanged += new System.EventHandler(this.MeshImportBox_SelectedIndexChanged);
            // 
            // ImportGLTFButton
            // 
            this.ImportGLTFButton.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.ImportGLTFButton.Location = new System.Drawing.Point(244, 410);
            this.ImportGLTFButton.Name = "ImportGLTFButton";
            this.ImportGLTFButton.Size = new System.Drawing.Size(207, 23);
            this.ImportGLTFButton.TabIndex = 4;
            this.ImportGLTFButton.Text = "Import glTF Scene";
            this.ImportGLTFButton.UseVisualStyleBackColor = true;
            this.ImportGLTFButton.Click += new System.EventHandler(this.ImportGLTFButton_Click);
            // 
            // NewSceneButton
            // 
            this.NewSceneButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.NewSceneButton.Location = new System.Drawing.Point(244, 381);
            this.NewSceneButton.Name = "NewSceneButton";
            this.NewSceneButton.Size = new System.Drawing.Size(71, 23);
            this.NewSceneButton.TabIndex = 5;
            this.NewSceneButton.Text = "New Scene";
            this.NewSceneButton.UseVisualStyleBackColor = true;
            this.NewSceneButton.Click += new System.EventHandler(this.NewSceneButton_Click);
            // 
            // ImportMeshButton
            // 
            this.ImportMeshButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.ImportMeshButton.Location = new System.Drawing.Point(460, 381);
            this.ImportMeshButton.Name = "ImportMeshButton";
            this.ImportMeshButton.Size = new System.Drawing.Size(198, 23);
            this.ImportMeshButton.TabIndex = 6;
            this.ImportMeshButton.Text = "Import Selected Meshes...";
            this.ImportMeshButton.UseVisualStyleBackColor = true;
            this.ImportMeshButton.Click += new System.EventHandler(this.ImportMeshButton_Click);
            // 
            // BrowserTabs
            // 
            this.BrowserTabs.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left)));
            this.BrowserTabs.Controls.Add(this.AnimationsPage);
            this.BrowserTabs.Controls.Add(this.AccessoriesPage);
            this.BrowserTabs.Controls.Add(this.OutfitsPage);
            this.BrowserTabs.Location = new System.Drawing.Point(12, 39);
            this.BrowserTabs.Name = "BrowserTabs";
            this.BrowserTabs.SelectedIndex = 0;
            this.BrowserTabs.Size = new System.Drawing.Size(226, 397);
            this.BrowserTabs.TabIndex = 7;
            // 
            // AnimationsPage
            // 
            this.AnimationsPage.Controls.Add(this.AnimationAdd);
            this.AnimationsPage.Controls.Add(this.AnimationList);
            this.AnimationsPage.Controls.Add(this.AnimationSearch);
            this.AnimationsPage.Location = new System.Drawing.Point(4, 22);
            this.AnimationsPage.Name = "AnimationsPage";
            this.AnimationsPage.Padding = new System.Windows.Forms.Padding(3);
            this.AnimationsPage.Size = new System.Drawing.Size(218, 371);
            this.AnimationsPage.TabIndex = 0;
            this.AnimationsPage.Text = "Animations";
            this.AnimationsPage.UseVisualStyleBackColor = true;
            // 
            // AnimationAdd
            // 
            this.AnimationAdd.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.AnimationAdd.Location = new System.Drawing.Point(-1, 346);
            this.AnimationAdd.Name = "AnimationAdd";
            this.AnimationAdd.Size = new System.Drawing.Size(218, 23);
            this.AnimationAdd.TabIndex = 16;
            this.AnimationAdd.Text = "Add to Export";
            this.AnimationAdd.UseVisualStyleBackColor = true;
            this.AnimationAdd.Click += new System.EventHandler(this.AnimationAdd_Click);
            // 
            // AnimationList
            // 
            this.AnimationList.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.AnimationList.FormattingEnabled = true;
            this.AnimationList.IntegralHeight = false;
            this.AnimationList.Location = new System.Drawing.Point(0, 27);
            this.AnimationList.Name = "AnimationList";
            this.AnimationList.Size = new System.Drawing.Size(216, 316);
            this.AnimationList.TabIndex = 17;
            this.AnimationList.SelectedIndexChanged += new System.EventHandler(this.AnimationList_SelectedIndexChanged);
            // 
            // AnimationSearch
            // 
            this.AnimationSearch.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.AnimationSearch.Location = new System.Drawing.Point(0, 4);
            this.AnimationSearch.Name = "AnimationSearch";
            this.AnimationSearch.Size = new System.Drawing.Size(216, 20);
            this.AnimationSearch.TabIndex = 18;
            this.AnimationSearch.TextChanged += new System.EventHandler(this.AnimationSearch_TextChanged);
            // 
            // AccessoriesPage
            // 
            this.AccessoriesPage.Controls.Add(this.AccessoryList);
            this.AccessoriesPage.Controls.Add(this.AccessorySearch);
            this.AccessoriesPage.Controls.Add(this.AccessoryClear);
            this.AccessoriesPage.Controls.Add(this.AccessoryAdd);
            this.AccessoriesPage.Controls.Add(this.AccessoryRemove);
            this.AccessoriesPage.Location = new System.Drawing.Point(4, 22);
            this.AccessoriesPage.Name = "AccessoriesPage";
            this.AccessoriesPage.Padding = new System.Windows.Forms.Padding(3);
            this.AccessoriesPage.Size = new System.Drawing.Size(218, 371);
            this.AccessoriesPage.TabIndex = 1;
            this.AccessoriesPage.Text = "Accessories";
            this.AccessoriesPage.UseVisualStyleBackColor = true;
            // 
            // AccessoryList
            // 
            this.AccessoryList.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.AccessoryList.FormattingEnabled = true;
            this.AccessoryList.IntegralHeight = false;
            this.AccessoryList.Location = new System.Drawing.Point(0, 27);
            this.AccessoryList.Name = "AccessoryList";
            this.AccessoryList.Size = new System.Drawing.Size(216, 316);
            this.AccessoryList.TabIndex = 16;
            this.AccessoryList.SelectedIndexChanged += new System.EventHandler(this.AccessoryList_SelectedIndexChanged);
            // 
            // AccessorySearch
            // 
            this.AccessorySearch.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.AccessorySearch.Location = new System.Drawing.Point(0, 4);
            this.AccessorySearch.Name = "AccessorySearch";
            this.AccessorySearch.Size = new System.Drawing.Size(216, 20);
            this.AccessorySearch.TabIndex = 16;
            this.AccessorySearch.TextChanged += new System.EventHandler(this.AccessorySearch_TextChanged);
            // 
            // AccessoryClear
            // 
            this.AccessoryClear.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.AccessoryClear.Location = new System.Drawing.Point(161, 346);
            this.AccessoryClear.Name = "AccessoryClear";
            this.AccessoryClear.Size = new System.Drawing.Size(56, 23);
            this.AccessoryClear.TabIndex = 10;
            this.AccessoryClear.Text = "Clear All";
            this.AccessoryClear.UseVisualStyleBackColor = true;
            this.AccessoryClear.Click += new System.EventHandler(this.AccessoryClear_Click);
            // 
            // AccessoryAdd
            // 
            this.AccessoryAdd.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.AccessoryAdd.Location = new System.Drawing.Point(-1, 346);
            this.AccessoryAdd.Name = "AccessoryAdd";
            this.AccessoryAdd.Size = new System.Drawing.Size(56, 23);
            this.AccessoryAdd.TabIndex = 8;
            this.AccessoryAdd.Text = "Add";
            this.AccessoryAdd.UseVisualStyleBackColor = true;
            this.AccessoryAdd.Click += new System.EventHandler(this.AccessoryAdd_Click);
            // 
            // AccessoryRemove
            // 
            this.AccessoryRemove.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.AccessoryRemove.Location = new System.Drawing.Point(61, 346);
            this.AccessoryRemove.Name = "AccessoryRemove";
            this.AccessoryRemove.Size = new System.Drawing.Size(94, 23);
            this.AccessoryRemove.TabIndex = 9;
            this.AccessoryRemove.Text = "Remove";
            this.AccessoryRemove.UseVisualStyleBackColor = true;
            this.AccessoryRemove.Click += new System.EventHandler(this.AccessoryRemove_Click);
            // 
            // OutfitsPage
            // 
            this.OutfitsPage.Controls.Add(this.OutfitList);
            this.OutfitsPage.Controls.Add(this.OutfitSearch);
            this.OutfitsPage.Controls.Add(this.OutfitSet);
            this.OutfitsPage.Location = new System.Drawing.Point(4, 22);
            this.OutfitsPage.Name = "OutfitsPage";
            this.OutfitsPage.Size = new System.Drawing.Size(218, 371);
            this.OutfitsPage.TabIndex = 2;
            this.OutfitsPage.Text = "Outfits";
            this.OutfitsPage.UseVisualStyleBackColor = true;
            // 
            // OutfitList
            // 
            this.OutfitList.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.OutfitList.FormattingEnabled = true;
            this.OutfitList.IntegralHeight = false;
            this.OutfitList.Location = new System.Drawing.Point(0, 27);
            this.OutfitList.Name = "OutfitList";
            this.OutfitList.Size = new System.Drawing.Size(216, 316);
            this.OutfitList.TabIndex = 17;
            this.OutfitList.SelectedIndexChanged += new System.EventHandler(this.OutfitList_SelectedIndexChanged);
            // 
            // OutfitSearch
            // 
            this.OutfitSearch.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.OutfitSearch.Location = new System.Drawing.Point(0, 4);
            this.OutfitSearch.Name = "OutfitSearch";
            this.OutfitSearch.Size = new System.Drawing.Size(216, 20);
            this.OutfitSearch.TabIndex = 18;
            this.OutfitSearch.TextChanged += new System.EventHandler(this.OutfitSearch_TextChanged);
            // 
            // OutfitSet
            // 
            this.OutfitSet.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.OutfitSet.Location = new System.Drawing.Point(-1, 346);
            this.OutfitSet.Name = "OutfitSet";
            this.OutfitSet.Size = new System.Drawing.Size(218, 23);
            this.OutfitSet.TabIndex = 11;
            this.OutfitSet.Text = "Set as Head";
            this.OutfitSet.UseVisualStyleBackColor = true;
            this.OutfitSet.Click += new System.EventHandler(this.OutfitSet_Click);
            // 
            // SkeletonCombo
            // 
            this.SkeletonCombo.FormattingEnabled = true;
            this.SkeletonCombo.Items.AddRange(new object[] {
            "adult",
            "kat",
            "dog"});
            this.SkeletonCombo.Location = new System.Drawing.Point(71, 12);
            this.SkeletonCombo.Name = "SkeletonCombo";
            this.SkeletonCombo.Size = new System.Drawing.Size(62, 21);
            this.SkeletonCombo.TabIndex = 12;
            this.SkeletonCombo.Text = "adult";
            this.SkeletonCombo.SelectedIndexChanged += new System.EventHandler(this.SkeletonCombo_SelectedIndexChanged);
            // 
            // AllAnimsCheck
            // 
            this.AllAnimsCheck.AutoSize = true;
            this.AllAnimsCheck.Location = new System.Drawing.Point(140, 15);
            this.AllAnimsCheck.Name = "AllAnimsCheck";
            this.AllAnimsCheck.Size = new System.Drawing.Size(98, 17);
            this.AllAnimsCheck.TabIndex = 13;
            this.AllAnimsCheck.Text = "Show All Anims";
            this.AllAnimsCheck.UseVisualStyleBackColor = true;
            this.AllAnimsCheck.CheckedChanged += new System.EventHandler(this.AllAnimsCheck_CheckedChanged);
            // 
            // SkeletonLabel
            // 
            this.SkeletonLabel.AutoSize = true;
            this.SkeletonLabel.Location = new System.Drawing.Point(13, 15);
            this.SkeletonLabel.Name = "SkeletonLabel";
            this.SkeletonLabel.Size = new System.Drawing.Size(52, 13);
            this.SkeletonLabel.TabIndex = 14;
            this.SkeletonLabel.Text = "Skeleton:";
            // 
            // ImportSkeletonButton
            // 
            this.ImportSkeletonButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.ImportSkeletonButton.Location = new System.Drawing.Point(460, 410);
            this.ImportSkeletonButton.Name = "ImportSkeletonButton";
            this.ImportSkeletonButton.Size = new System.Drawing.Size(198, 23);
            this.ImportSkeletonButton.TabIndex = 15;
            this.ImportSkeletonButton.Text = "Import Skeleton (Experimental)";
            this.ImportSkeletonButton.UseVisualStyleBackColor = true;
            this.ImportSkeletonButton.Click += new System.EventHandler(this.ImportSkeletonButton_Click);
            // 
            // ExportGLTFButton
            // 
            this.ExportGLTFButton.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.ExportGLTFButton.Location = new System.Drawing.Point(321, 381);
            this.ExportGLTFButton.Name = "ExportGLTFButton";
            this.ExportGLTFButton.Size = new System.Drawing.Size(130, 23);
            this.ExportGLTFButton.TabIndex = 16;
            this.ExportGLTFButton.Text = "Export glTF";
            this.ExportGLTFButton.UseVisualStyleBackColor = true;
            this.ExportGLTFButton.Click += new System.EventHandler(this.ExportGLTFButton_Click);
            // 
            // AvatarTool
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(670, 450);
            this.Controls.Add(this.ExportGLTFButton);
            this.Controls.Add(this.ImportSkeletonButton);
            this.Controls.Add(this.SkeletonLabel);
            this.Controls.Add(this.AllAnimsCheck);
            this.Controls.Add(this.SkeletonCombo);
            this.Controls.Add(this.BrowserTabs);
            this.Controls.Add(this.ImportMeshButton);
            this.Controls.Add(this.NewSceneButton);
            this.Controls.Add(this.ImportGLTFButton);
            this.Controls.Add(this.MeshImportBox);
            this.Controls.Add(this.ImportAnimButton);
            this.Controls.Add(this.AnimationImportBox);
            this.Controls.Add(this.Animator);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.MinimumSize = new System.Drawing.Size(686, 489);
            this.Name = "AvatarTool";
            this.Text = "Avatar Tool";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.AvatarTool_FormClosing);
            this.BrowserTabs.ResumeLayout(false);
            this.AnimationsPage.ResumeLayout(false);
            this.AnimationsPage.PerformLayout();
            this.AccessoriesPage.ResumeLayout(false);
            this.AccessoriesPage.PerformLayout();
            this.OutfitsPage.ResumeLayout(false);
            this.OutfitsPage.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private Common.AvatarAnimatorControl Animator;
        private System.Windows.Forms.CheckedListBox AnimationImportBox;
        private System.Windows.Forms.Button ImportAnimButton;
        private System.Windows.Forms.ListBox MeshImportBox;
        private System.Windows.Forms.Button ImportGLTFButton;
        private System.Windows.Forms.Button NewSceneButton;
        private System.Windows.Forms.Button ImportMeshButton;
        private System.Windows.Forms.TabControl BrowserTabs;
        private System.Windows.Forms.TabPage AnimationsPage;
        private System.Windows.Forms.TabPage AccessoriesPage;
        private System.Windows.Forms.ListBox AccessoryList;
        private System.Windows.Forms.TextBox AccessorySearch;
        private System.Windows.Forms.TabPage OutfitsPage;
        private System.Windows.Forms.Button AccessoryAdd;
        private System.Windows.Forms.Button AccessoryRemove;
        private System.Windows.Forms.Button AccessoryClear;
        private System.Windows.Forms.Button OutfitSet;
        private System.Windows.Forms.ComboBox SkeletonCombo;
        private System.Windows.Forms.CheckBox AllAnimsCheck;
        private System.Windows.Forms.Label SkeletonLabel;
        private System.Windows.Forms.Button ImportSkeletonButton;
        private System.Windows.Forms.Button AnimationAdd;
        private System.Windows.Forms.ListBox AnimationList;
        private System.Windows.Forms.TextBox AnimationSearch;
        private System.Windows.Forms.ListBox OutfitList;
        private System.Windows.Forms.TextBox OutfitSearch;
        private System.Windows.Forms.Button ExportGLTFButton;
    }
}