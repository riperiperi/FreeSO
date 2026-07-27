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
            Animator = new FSO.IDE.Common.AvatarAnimatorControl();
            AnimationImportBox = new CheckedListBox();
            ImportAnimButton = new Button();
            MeshImportBox = new ListBox();
            ImportGLTFButton = new Button();
            NewSceneButton = new Button();
            ImportMeshButton = new Button();
            BrowserTabs = new TabControl();
            AnimationsPage = new TabPage();
            AnimationAdd = new Button();
            AnimationList = new ListBox();
            AnimationSearch = new TextBox();
            AccessoriesPage = new TabPage();
            AccessoryList = new ListBox();
            AccessorySearch = new TextBox();
            AccessoryClear = new Button();
            AccessoryAdd = new Button();
            AccessoryRemove = new Button();
            OutfitsPage = new TabPage();
            OutfitList = new ListBox();
            OutfitSearch = new TextBox();
            OutfitSet = new Button();
            SkeletonCombo = new ComboBox();
            AllAnimsCheck = new CheckBox();
            SkeletonLabel = new Label();
            ImportSkeletonButton = new Button();
            ExportGLTFButton = new Button();
            BrowserTabs.SuspendLayout();
            AnimationsPage.SuspendLayout();
            AccessoriesPage.SuspendLayout();
            OutfitsPage.SuspendLayout();
            SuspendLayout();
            // 
            // Animator
            // 
            Animator.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            Animator.BorderStyle = BorderStyle.FixedSingle;
            Animator.Location = new Point(244, 15);
            Animator.Name = "Animator";
            Animator.Size = new Size(207, 360);
            Animator.TabIndex = 0;
            // 
            // AnimationImportBox
            // 
            AnimationImportBox.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Right;
            AnimationImportBox.FormattingEnabled = true;
            AnimationImportBox.IntegralHeight = false;
            AnimationImportBox.Location = new Point(460, 12);
            AnimationImportBox.Name = "AnimationImportBox";
            AnimationImportBox.Size = new Size(198, 233);
            AnimationImportBox.TabIndex = 1;
            AnimationImportBox.ItemCheck += AnimationImportBox_ItemCheck;
            AnimationImportBox.SelectedIndexChanged += AnimationImportBox_SelectedIndexChanged;
            // 
            // ImportAnimButton
            // 
            ImportAnimButton.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
            ImportAnimButton.Location = new Point(460, 251);
            ImportAnimButton.Name = "ImportAnimButton";
            ImportAnimButton.Size = new Size(198, 23);
            ImportAnimButton.TabIndex = 2;
            ImportAnimButton.Text = "Import Selected Animations";
            ImportAnimButton.UseVisualStyleBackColor = true;
            ImportAnimButton.Click += ImportAnimButton_Click;
            // 
            // MeshImportBox
            // 
            MeshImportBox.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
            MeshImportBox.FormattingEnabled = true;
            MeshImportBox.Location = new Point(460, 280);
            MeshImportBox.Name = "MeshImportBox";
            MeshImportBox.SelectionMode = SelectionMode.MultiSimple;
            MeshImportBox.Size = new Size(198, 95);
            MeshImportBox.TabIndex = 3;
            MeshImportBox.SelectedIndexChanged += MeshImportBox_SelectedIndexChanged;
            // 
            // ImportGLTFButton
            // 
            ImportGLTFButton.Anchor = AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            ImportGLTFButton.Location = new Point(244, 410);
            ImportGLTFButton.Name = "ImportGLTFButton";
            ImportGLTFButton.Size = new Size(207, 23);
            ImportGLTFButton.TabIndex = 4;
            ImportGLTFButton.Text = "Import glTF Scene";
            ImportGLTFButton.UseVisualStyleBackColor = true;
            ImportGLTFButton.Click += ImportGLTFButton_Click;
            // 
            // NewSceneButton
            // 
            NewSceneButton.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
            NewSceneButton.Location = new Point(244, 381);
            NewSceneButton.Name = "NewSceneButton";
            NewSceneButton.Size = new Size(71, 23);
            NewSceneButton.TabIndex = 5;
            NewSceneButton.Text = "New Scene";
            NewSceneButton.UseVisualStyleBackColor = true;
            NewSceneButton.Click += NewSceneButton_Click;
            // 
            // ImportMeshButton
            // 
            ImportMeshButton.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
            ImportMeshButton.Location = new Point(460, 381);
            ImportMeshButton.Name = "ImportMeshButton";
            ImportMeshButton.Size = new Size(198, 23);
            ImportMeshButton.TabIndex = 6;
            ImportMeshButton.Text = "Import Selected Meshes...";
            ImportMeshButton.UseVisualStyleBackColor = true;
            ImportMeshButton.Click += ImportMeshButton_Click;
            // 
            // BrowserTabs
            // 
            BrowserTabs.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left;
            BrowserTabs.Controls.Add(AnimationsPage);
            BrowserTabs.Controls.Add(AccessoriesPage);
            BrowserTabs.Controls.Add(OutfitsPage);
            BrowserTabs.Location = new Point(12, 39);
            BrowserTabs.Name = "BrowserTabs";
            BrowserTabs.SelectedIndex = 0;
            BrowserTabs.Size = new Size(226, 397);
            BrowserTabs.TabIndex = 7;
            // 
            // AnimationsPage
            // 
            AnimationsPage.Controls.Add(AnimationAdd);
            AnimationsPage.Controls.Add(AnimationList);
            AnimationsPage.Controls.Add(AnimationSearch);
            AnimationsPage.Location = new Point(4, 22);
            AnimationsPage.Name = "AnimationsPage";
            AnimationsPage.Padding = new Padding(3);
            AnimationsPage.Size = new Size(218, 371);
            AnimationsPage.TabIndex = 0;
            AnimationsPage.Text = "Animations";
            AnimationsPage.UseVisualStyleBackColor = true;
            // 
            // AnimationAdd
            // 
            AnimationAdd.Anchor = AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            AnimationAdd.Location = new Point(-1, 346);
            AnimationAdd.Name = "AnimationAdd";
            AnimationAdd.Size = new Size(218, 23);
            AnimationAdd.TabIndex = 16;
            AnimationAdd.Text = "Add to Export";
            AnimationAdd.UseVisualStyleBackColor = true;
            AnimationAdd.Click += AnimationAdd_Click;
            // 
            // AnimationList
            // 
            AnimationList.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            AnimationList.FormattingEnabled = true;
            AnimationList.IntegralHeight = false;
            AnimationList.Location = new Point(0, 27);
            AnimationList.Name = "AnimationList";
            AnimationList.Size = new Size(216, 316);
            AnimationList.TabIndex = 17;
            AnimationList.SelectedIndexChanged += AnimationList_SelectedIndexChanged;
            // 
            // AnimationSearch
            // 
            AnimationSearch.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            AnimationSearch.Location = new Point(0, 4);
            AnimationSearch.Name = "AnimationSearch";
            AnimationSearch.Size = new Size(216, 22);
            AnimationSearch.TabIndex = 18;
            AnimationSearch.TextChanged += AnimationSearch_TextChanged;
            // 
            // AccessoriesPage
            // 
            AccessoriesPage.Controls.Add(AccessoryList);
            AccessoriesPage.Controls.Add(AccessorySearch);
            AccessoriesPage.Controls.Add(AccessoryClear);
            AccessoriesPage.Controls.Add(AccessoryAdd);
            AccessoriesPage.Controls.Add(AccessoryRemove);
            AccessoriesPage.Location = new Point(4, 22);
            AccessoriesPage.Name = "AccessoriesPage";
            AccessoriesPage.Padding = new Padding(3);
            AccessoriesPage.Size = new Size(218, 371);
            AccessoriesPage.TabIndex = 1;
            AccessoriesPage.Text = "Accessories";
            AccessoriesPage.UseVisualStyleBackColor = true;
            // 
            // AccessoryList
            // 
            AccessoryList.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            AccessoryList.FormattingEnabled = true;
            AccessoryList.IntegralHeight = false;
            AccessoryList.Location = new Point(0, 27);
            AccessoryList.Name = "AccessoryList";
            AccessoryList.Size = new Size(216, 316);
            AccessoryList.TabIndex = 16;
            AccessoryList.SelectedIndexChanged += AccessoryList_SelectedIndexChanged;
            // 
            // AccessorySearch
            // 
            AccessorySearch.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            AccessorySearch.Location = new Point(0, 4);
            AccessorySearch.Name = "AccessorySearch";
            AccessorySearch.Size = new Size(216, 22);
            AccessorySearch.TabIndex = 16;
            AccessorySearch.TextChanged += AccessorySearch_TextChanged;
            // 
            // AccessoryClear
            // 
            AccessoryClear.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
            AccessoryClear.Location = new Point(161, 346);
            AccessoryClear.Name = "AccessoryClear";
            AccessoryClear.Size = new Size(56, 23);
            AccessoryClear.TabIndex = 10;
            AccessoryClear.Text = "Clear All";
            AccessoryClear.UseVisualStyleBackColor = true;
            AccessoryClear.Click += AccessoryClear_Click;
            // 
            // AccessoryAdd
            // 
            AccessoryAdd.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
            AccessoryAdd.Location = new Point(-1, 346);
            AccessoryAdd.Name = "AccessoryAdd";
            AccessoryAdd.Size = new Size(56, 23);
            AccessoryAdd.TabIndex = 8;
            AccessoryAdd.Text = "Add";
            AccessoryAdd.UseVisualStyleBackColor = true;
            AccessoryAdd.Click += AccessoryAdd_Click;
            // 
            // AccessoryRemove
            // 
            AccessoryRemove.Anchor = AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            AccessoryRemove.Location = new Point(61, 346);
            AccessoryRemove.Name = "AccessoryRemove";
            AccessoryRemove.Size = new Size(94, 23);
            AccessoryRemove.TabIndex = 9;
            AccessoryRemove.Text = "Remove";
            AccessoryRemove.UseVisualStyleBackColor = true;
            AccessoryRemove.Click += AccessoryRemove_Click;
            // 
            // OutfitsPage
            // 
            OutfitsPage.Controls.Add(OutfitList);
            OutfitsPage.Controls.Add(OutfitSearch);
            OutfitsPage.Controls.Add(OutfitSet);
            OutfitsPage.Location = new Point(4, 22);
            OutfitsPage.Name = "OutfitsPage";
            OutfitsPage.Size = new Size(218, 371);
            OutfitsPage.TabIndex = 2;
            OutfitsPage.Text = "Outfits";
            OutfitsPage.UseVisualStyleBackColor = true;
            // 
            // OutfitList
            // 
            OutfitList.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            OutfitList.FormattingEnabled = true;
            OutfitList.IntegralHeight = false;
            OutfitList.Location = new Point(0, 27);
            OutfitList.Name = "OutfitList";
            OutfitList.Size = new Size(216, 316);
            OutfitList.TabIndex = 17;
            OutfitList.SelectedIndexChanged += OutfitList_SelectedIndexChanged;
            // 
            // OutfitSearch
            // 
            OutfitSearch.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            OutfitSearch.Location = new Point(0, 4);
            OutfitSearch.Name = "OutfitSearch";
            OutfitSearch.Size = new Size(216, 22);
            OutfitSearch.TabIndex = 18;
            OutfitSearch.TextChanged += OutfitSearch_TextChanged;
            // 
            // OutfitSet
            // 
            OutfitSet.Anchor = AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            OutfitSet.Location = new Point(-1, 346);
            OutfitSet.Name = "OutfitSet";
            OutfitSet.Size = new Size(218, 23);
            OutfitSet.TabIndex = 11;
            OutfitSet.Text = "Set as Head";
            OutfitSet.UseVisualStyleBackColor = true;
            OutfitSet.Click += OutfitSet_Click;
            // 
            // SkeletonCombo
            // 
            SkeletonCombo.FormattingEnabled = true;
            SkeletonCombo.Items.AddRange(new object[] { "adult", "kat", "dog" });
            SkeletonCombo.Location = new Point(71, 12);
            SkeletonCombo.Name = "SkeletonCombo";
            SkeletonCombo.Size = new Size(62, 21);
            SkeletonCombo.TabIndex = 12;
            SkeletonCombo.Text = "adult";
            SkeletonCombo.SelectedIndexChanged += SkeletonCombo_SelectedIndexChanged;
            // 
            // AllAnimsCheck
            // 
            AllAnimsCheck.AutoSize = true;
            AllAnimsCheck.Location = new Point(140, 15);
            AllAnimsCheck.Name = "AllAnimsCheck";
            AllAnimsCheck.Size = new Size(105, 17);
            AllAnimsCheck.TabIndex = 13;
            AllAnimsCheck.Text = "Show All Anims";
            AllAnimsCheck.UseVisualStyleBackColor = true;
            AllAnimsCheck.CheckedChanged += AllAnimsCheck_CheckedChanged;
            // 
            // SkeletonLabel
            // 
            SkeletonLabel.AutoSize = true;
            SkeletonLabel.Location = new Point(13, 15);
            SkeletonLabel.Name = "SkeletonLabel";
            SkeletonLabel.Size = new Size(55, 13);
            SkeletonLabel.TabIndex = 14;
            SkeletonLabel.Text = "Skeleton:";
            // 
            // ImportSkeletonButton
            // 
            ImportSkeletonButton.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
            ImportSkeletonButton.Location = new Point(460, 410);
            ImportSkeletonButton.Name = "ImportSkeletonButton";
            ImportSkeletonButton.Size = new Size(198, 23);
            ImportSkeletonButton.TabIndex = 15;
            ImportSkeletonButton.Text = "Import Skeleton (Experimental)";
            ImportSkeletonButton.UseVisualStyleBackColor = true;
            ImportSkeletonButton.Click += ImportSkeletonButton_Click;
            // 
            // ExportGLTFButton
            // 
            ExportGLTFButton.Anchor = AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            ExportGLTFButton.Location = new Point(321, 381);
            ExportGLTFButton.Name = "ExportGLTFButton";
            ExportGLTFButton.Size = new Size(130, 23);
            ExportGLTFButton.TabIndex = 16;
            ExportGLTFButton.Text = "Export glTF";
            ExportGLTFButton.UseVisualStyleBackColor = true;
            ExportGLTFButton.Click += ExportGLTFButton_Click;
            // 
            // AvatarTool
            // 
            AutoScaleDimensions = new SizeF(96F, 96F);
            AutoScaleMode = AutoScaleMode.Dpi;
            ClientSize = new Size(670, 450);
            Controls.Add(ExportGLTFButton);
            Controls.Add(ImportSkeletonButton);
            Controls.Add(SkeletonLabel);
            Controls.Add(AllAnimsCheck);
            Controls.Add(SkeletonCombo);
            Controls.Add(BrowserTabs);
            Controls.Add(ImportMeshButton);
            Controls.Add(NewSceneButton);
            Controls.Add(ImportGLTFButton);
            Controls.Add(MeshImportBox);
            Controls.Add(ImportAnimButton);
            Controls.Add(AnimationImportBox);
            Controls.Add(Animator);
            Icon = (Icon)resources.GetObject("$this.Icon");
            MinimumSize = new Size(686, 489);
            Name = "AvatarTool";
            Text = "Avatar Tool";
            FormClosing += AvatarTool_FormClosing;
            BrowserTabs.ResumeLayout(false);
            AnimationsPage.ResumeLayout(false);
            AnimationsPage.PerformLayout();
            AccessoriesPage.ResumeLayout(false);
            AccessoriesPage.PerformLayout();
            OutfitsPage.ResumeLayout(false);
            OutfitsPage.PerformLayout();
            ResumeLayout(false);
            PerformLayout();

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