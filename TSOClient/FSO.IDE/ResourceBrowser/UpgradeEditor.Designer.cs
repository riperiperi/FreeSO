namespace FSO.IDE.ResourceBrowser
{
    partial class UpgradeEditor
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

        #region Component Designer generated code

        /// <summary> 
        /// Required method for Designer support - do not modify 
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(UpgradeEditor));
            System.Windows.Forms.TreeNode treeNode1 = new System.Windows.Forms.TreeNode("Max Fun Cheap (4096:0 - 50)");
            System.Windows.Forms.TreeNode treeNode2 = new System.Windows.Forms.TreeNode("Max Fun Exp (4096:1 - 70)");
            System.Windows.Forms.TreeNode treeNode3 = new System.Windows.Forms.TreeNode("Max Fun (default: 50)", new System.Windows.Forms.TreeNode[] {
            treeNode1,
            treeNode2});
            this.ObjectSpecificBox = new System.Windows.Forms.GroupBox();
            this.UpgradeArrowLabel = new System.Windows.Forms.Label();
            this.FlagOriginal = new System.Windows.Forms.CheckBox();
            this.FlagsLabel = new System.Windows.Forms.Label();
            this.EndLevelCombo = new System.Windows.Forms.ComboBox();
            this.StartLevelCombo = new System.Windows.Forms.ComboBox();
            this.EndLabel = new System.Windows.Forms.Label();
            this.StartLabel = new System.Windows.Forms.Label();
            this.IffSpecificBox = new System.Windows.Forms.GroupBox();
            this.RemoveUpgradeButton = new System.Windows.Forms.Button();
            this.RemoveSubButton = new System.Windows.Forms.Button();
            this.AddSubButton = new System.Windows.Forms.Button();
            this.SubSpecificBox = new System.Windows.Forms.GroupBox();
            this.SubDescriptionLabel = new System.Windows.Forms.Label();
            this.SubTargetValue = new System.Windows.Forms.NumericUpDown();
            this.SubTargetTuning = new System.Windows.Forms.ComboBox();
            this.SubTargetValueRadio = new System.Windows.Forms.RadioButton();
            this.SubTargetTuningRadio = new System.Windows.Forms.RadioButton();
            this.SubToLabel = new System.Windows.Forms.Label();
            this.SubFromLabel = new System.Windows.Forms.Label();
            this.SubFromTuning = new System.Windows.Forms.ComboBox();
            this.LevelsTabControl = new System.Windows.Forms.TabControl();
            this.ConstantPage = new System.Windows.Forms.TabPage();
            this.ConstantSubList = new System.Windows.Forms.ListBox();
            this.GroupPage = new System.Windows.Forms.TabPage();
            this.GroupAdd = new System.Windows.Forms.Button();
            this.GroupRemove = new System.Windows.Forms.Button();
            this.GroupNameBox = new System.Windows.Forms.TextBox();
            this.GroupNameLabel = new System.Windows.Forms.Label();
            this.GroupTree = new System.Windows.Forms.TreeView();
            this.TemplateUpgradePage = new System.Windows.Forms.TabPage();
            this.UpgradeContainer = new System.Windows.Forms.SplitContainer();
            this.DescriptionText = new System.Windows.Forms.TextBox();
            this.DescriptionLabel = new System.Windows.Forms.Label();
            this.AdObjectButton = new System.Windows.Forms.Button();
            this.AdText = new System.Windows.Forms.TextBox();
            this.AdLabel = new System.Windows.Forms.Label();
            this.RelativeCheck = new System.Windows.Forms.CheckBox();
            this.SimoleonLabel = new System.Windows.Forms.Label();
            this.UpgradeValue = new System.Windows.Forms.NumericUpDown();
            this.PriceValueRadio = new System.Windows.Forms.RadioButton();
            this.PriceObjectRadio = new System.Windows.Forms.RadioButton();
            this.UpgradeObjectButton = new System.Windows.Forms.Button();
            this.UpgradeNameText = new System.Windows.Forms.ComboBox();
            this.PriceLabel = new System.Windows.Forms.Label();
            this.NameLabel = new System.Windows.Forms.Label();
            this.UpgradeSubList = new System.Windows.Forms.ListBox();
            this.EntryCRDButton = new System.Windows.Forms.Button();
            this.UpgradeStatusLabel = new System.Windows.Forms.Label();
            this.SaveButton = new System.Windows.Forms.Button();
            this.CopyButton = new System.Windows.Forms.Button();
            this.PasteButton = new System.Windows.Forms.Button();
            this.ObjectSpecificBox.SuspendLayout();
            this.IffSpecificBox.SuspendLayout();
            this.SubSpecificBox.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.SubTargetValue)).BeginInit();
            this.LevelsTabControl.SuspendLayout();
            this.ConstantPage.SuspendLayout();
            this.GroupPage.SuspendLayout();
            this.TemplateUpgradePage.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.UpgradeContainer)).BeginInit();
            this.UpgradeContainer.Panel1.SuspendLayout();
            this.UpgradeContainer.Panel2.SuspendLayout();
            this.UpgradeContainer.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.UpgradeValue)).BeginInit();
            this.SuspendLayout();
            // 
            // ObjectSpecificBox
            // 
            this.ObjectSpecificBox.Controls.Add(this.UpgradeArrowLabel);
            this.ObjectSpecificBox.Controls.Add(this.FlagOriginal);
            this.ObjectSpecificBox.Controls.Add(this.FlagsLabel);
            this.ObjectSpecificBox.Controls.Add(this.EndLevelCombo);
            this.ObjectSpecificBox.Controls.Add(this.StartLevelCombo);
            this.ObjectSpecificBox.Controls.Add(this.EndLabel);
            this.ObjectSpecificBox.Controls.Add(this.StartLabel);
            this.ObjectSpecificBox.Location = new System.Drawing.Point(3, 375);
            this.ObjectSpecificBox.Name = "ObjectSpecificBox";
            this.ObjectSpecificBox.Size = new System.Drawing.Size(756, 81);
            this.ObjectSpecificBox.TabIndex = 0;
            this.ObjectSpecificBox.TabStop = false;
            this.ObjectSpecificBox.Text = "Object Properties (Table - End - Expensive)";
            // 
            // UpgradeArrowLabel
            // 
            this.UpgradeArrowLabel.AutoSize = true;
            this.UpgradeArrowLabel.Font = new System.Drawing.Font("Microsoft Sans Serif", 15.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.UpgradeArrowLabel.Location = new System.Drawing.Point(185, 38);
            this.UpgradeArrowLabel.Name = "UpgradeArrowLabel";
            this.UpgradeArrowLabel.Size = new System.Drawing.Size(26, 25);
            this.UpgradeArrowLabel.TabIndex = 6;
            this.UpgradeArrowLabel.Text = "⯈";
            // 
            // FlagOriginal
            // 
            this.FlagOriginal.AutoSize = true;
            this.FlagOriginal.Location = new System.Drawing.Point(403, 32);
            this.FlagOriginal.Name = "FlagOriginal";
            this.FlagOriginal.Size = new System.Drawing.Size(161, 17);
            this.FlagOriginal.TabIndex = 5;
            this.FlagOriginal.Text = "Use Original Tuning for Initial";
            this.FlagOriginal.UseVisualStyleBackColor = true;
            this.FlagOriginal.CheckedChanged += new System.EventHandler(this.FlagOriginal_CheckedChanged);
            // 
            // FlagsLabel
            // 
            this.FlagsLabel.AutoSize = true;
            this.FlagsLabel.Location = new System.Drawing.Point(400, 16);
            this.FlagsLabel.Name = "FlagsLabel";
            this.FlagsLabel.Size = new System.Drawing.Size(32, 13);
            this.FlagsLabel.TabIndex = 4;
            this.FlagsLabel.Text = "Flags";
            // 
            // EndLevelCombo
            // 
            this.EndLevelCombo.FormattingEnabled = true;
            this.EndLevelCombo.Location = new System.Drawing.Point(216, 42);
            this.EndLevelCombo.Name = "EndLevelCombo";
            this.EndLevelCombo.Size = new System.Drawing.Size(169, 21);
            this.EndLevelCombo.TabIndex = 3;
            this.EndLevelCombo.SelectedIndexChanged += new System.EventHandler(this.EndLevelCombo_SelectedIndexChanged);
            // 
            // StartLevelCombo
            // 
            this.StartLevelCombo.FormattingEnabled = true;
            this.StartLevelCombo.Location = new System.Drawing.Point(10, 42);
            this.StartLevelCombo.Name = "StartLevelCombo";
            this.StartLevelCombo.Size = new System.Drawing.Size(169, 21);
            this.StartLevelCombo.TabIndex = 2;
            this.StartLevelCombo.SelectedIndexChanged += new System.EventHandler(this.StartLevelCombo_SelectedIndexChanged);
            // 
            // EndLabel
            // 
            this.EndLabel.AutoSize = true;
            this.EndLabel.Location = new System.Drawing.Point(213, 26);
            this.EndLabel.Name = "EndLabel";
            this.EndLabel.Size = new System.Drawing.Size(55, 13);
            this.EndLabel.TabIndex = 1;
            this.EndLabel.Text = "End Level";
            // 
            // StartLabel
            // 
            this.StartLabel.AutoSize = true;
            this.StartLabel.Location = new System.Drawing.Point(7, 26);
            this.StartLabel.Name = "StartLabel";
            this.StartLabel.Size = new System.Drawing.Size(60, 13);
            this.StartLabel.TabIndex = 0;
            this.StartLabel.Text = "Initial Level";
            // 
            // IffSpecificBox
            // 
            this.IffSpecificBox.Controls.Add(this.RemoveUpgradeButton);
            this.IffSpecificBox.Controls.Add(this.RemoveSubButton);
            this.IffSpecificBox.Controls.Add(this.AddSubButton);
            this.IffSpecificBox.Controls.Add(this.SubSpecificBox);
            this.IffSpecificBox.Controls.Add(this.LevelsTabControl);
            this.IffSpecificBox.Location = new System.Drawing.Point(3, 35);
            this.IffSpecificBox.Name = "IffSpecificBox";
            this.IffSpecificBox.Size = new System.Drawing.Size(756, 334);
            this.IffSpecificBox.TabIndex = 1;
            this.IffSpecificBox.TabStop = false;
            this.IffSpecificBox.Text = "tablesend.iff";
            // 
            // RemoveUpgradeButton
            // 
            this.RemoveUpgradeButton.Location = new System.Drawing.Point(639, 11);
            this.RemoveUpgradeButton.Name = "RemoveUpgradeButton";
            this.RemoveUpgradeButton.Size = new System.Drawing.Size(111, 23);
            this.RemoveUpgradeButton.TabIndex = 4;
            this.RemoveUpgradeButton.Text = "Remove Upgrade";
            this.RemoveUpgradeButton.UseVisualStyleBackColor = true;
            this.RemoveUpgradeButton.Click += new System.EventHandler(this.RemoveUpgradeButton_Click);
            // 
            // RemoveSubButton
            // 
            this.RemoveSubButton.Location = new System.Drawing.Point(365, 305);
            this.RemoveSubButton.Name = "RemoveSubButton";
            this.RemoveSubButton.Size = new System.Drawing.Size(117, 23);
            this.RemoveSubButton.TabIndex = 3;
            this.RemoveSubButton.Text = "Remove Substitution";
            this.RemoveSubButton.UseVisualStyleBackColor = true;
            this.RemoveSubButton.Click += new System.EventHandler(this.RemoveSubButton_Click);
            // 
            // AddSubButton
            // 
            this.AddSubButton.Location = new System.Drawing.Point(6, 305);
            this.AddSubButton.Name = "AddSubButton";
            this.AddSubButton.Size = new System.Drawing.Size(100, 23);
            this.AddSubButton.TabIndex = 2;
            this.AddSubButton.Text = "Add Subsitution";
            this.AddSubButton.UseVisualStyleBackColor = true;
            this.AddSubButton.Click += new System.EventHandler(this.AddSubButton_Click);
            // 
            // SubSpecificBox
            // 
            this.SubSpecificBox.Controls.Add(this.SubDescriptionLabel);
            this.SubSpecificBox.Controls.Add(this.SubTargetValue);
            this.SubSpecificBox.Controls.Add(this.SubTargetTuning);
            this.SubSpecificBox.Controls.Add(this.SubTargetValueRadio);
            this.SubSpecificBox.Controls.Add(this.SubTargetTuningRadio);
            this.SubSpecificBox.Controls.Add(this.SubToLabel);
            this.SubSpecificBox.Controls.Add(this.SubFromLabel);
            this.SubSpecificBox.Controls.Add(this.SubFromTuning);
            this.SubSpecificBox.Location = new System.Drawing.Point(485, 33);
            this.SubSpecificBox.Name = "SubSpecificBox";
            this.SubSpecificBox.Size = new System.Drawing.Size(265, 295);
            this.SubSpecificBox.TabIndex = 1;
            this.SubSpecificBox.TabStop = false;
            this.SubSpecificBox.Text = "Selected Substitution";
            // 
            // SubDescriptionLabel
            // 
            this.SubDescriptionLabel.Location = new System.Drawing.Point(6, 16);
            this.SubDescriptionLabel.Name = "SubDescriptionLabel";
            this.SubDescriptionLabel.Size = new System.Drawing.Size(258, 94);
            this.SubDescriptionLabel.TabIndex = 7;
            this.SubDescriptionLabel.Text = resources.GetString("SubDescriptionLabel.Text");
            // 
            // SubTargetValue
            // 
            this.SubTargetValue.Location = new System.Drawing.Point(27, 267);
            this.SubTargetValue.Maximum = new decimal(new int[] {
            32767,
            0,
            0,
            0});
            this.SubTargetValue.Minimum = new decimal(new int[] {
            32768,
            0,
            0,
            -2147483648});
            this.SubTargetValue.Name = "SubTargetValue";
            this.SubTargetValue.Size = new System.Drawing.Size(68, 20);
            this.SubTargetValue.TabIndex = 6;
            this.SubTargetValue.Value = new decimal(new int[] {
            32768,
            0,
            0,
            -2147483648});
            this.SubTargetValue.ValueChanged += new System.EventHandler(this.SubTargetValue_ValueChanged);
            // 
            // SubTargetTuning
            // 
            this.SubTargetTuning.FormattingEnabled = true;
            this.SubTargetTuning.Location = new System.Drawing.Point(27, 206);
            this.SubTargetTuning.Name = "SubTargetTuning";
            this.SubTargetTuning.Size = new System.Drawing.Size(231, 21);
            this.SubTargetTuning.TabIndex = 5;
            this.SubTargetTuning.SelectedIndexChanged += new System.EventHandler(this.SubTargetTuning_SelectedIndexChanged);
            // 
            // SubTargetValueRadio
            // 
            this.SubTargetValueRadio.AutoSize = true;
            this.SubTargetValueRadio.Location = new System.Drawing.Point(9, 244);
            this.SubTargetValueRadio.Name = "SubTargetValueRadio";
            this.SubTargetValueRadio.Size = new System.Drawing.Size(86, 17);
            this.SubTargetValueRadio.TabIndex = 4;
            this.SubTargetValueRadio.TabStop = true;
            this.SubTargetValueRadio.Text = "Literal Value:";
            this.SubTargetValueRadio.UseVisualStyleBackColor = true;
            this.SubTargetValueRadio.CheckedChanged += new System.EventHandler(this.SubTargetValueRadio_CheckedChanged);
            // 
            // SubTargetTuningRadio
            // 
            this.SubTargetTuningRadio.AutoSize = true;
            this.SubTargetTuningRadio.Location = new System.Drawing.Point(9, 183);
            this.SubTargetTuningRadio.Name = "SubTargetTuningRadio";
            this.SubTargetTuningRadio.Size = new System.Drawing.Size(90, 17);
            this.SubTargetTuningRadio.TabIndex = 3;
            this.SubTargetTuningRadio.TabStop = true;
            this.SubTargetTuningRadio.Text = "Other Tuning:";
            this.SubTargetTuningRadio.UseVisualStyleBackColor = true;
            this.SubTargetTuningRadio.CheckedChanged += new System.EventHandler(this.SubTargetTuningRadio_CheckedChanged);
            // 
            // SubToLabel
            // 
            this.SubToLabel.AutoSize = true;
            this.SubToLabel.Location = new System.Drawing.Point(6, 164);
            this.SubToLabel.Name = "SubToLabel";
            this.SubToLabel.Size = new System.Drawing.Size(23, 13);
            this.SubToLabel.TabIndex = 2;
            this.SubToLabel.Text = "To:";
            // 
            // SubFromLabel
            // 
            this.SubFromLabel.AutoSize = true;
            this.SubFromLabel.Location = new System.Drawing.Point(6, 119);
            this.SubFromLabel.Name = "SubFromLabel";
            this.SubFromLabel.Size = new System.Drawing.Size(33, 13);
            this.SubFromLabel.TabIndex = 1;
            this.SubFromLabel.Text = "From:";
            // 
            // SubFromTuning
            // 
            this.SubFromTuning.FormattingEnabled = true;
            this.SubFromTuning.Location = new System.Drawing.Point(9, 135);
            this.SubFromTuning.Name = "SubFromTuning";
            this.SubFromTuning.Size = new System.Drawing.Size(249, 21);
            this.SubFromTuning.TabIndex = 0;
            this.SubFromTuning.SelectedIndexChanged += new System.EventHandler(this.SubFromTuning_SelectedIndexChanged);
            // 
            // LevelsTabControl
            // 
            this.LevelsTabControl.Controls.Add(this.ConstantPage);
            this.LevelsTabControl.Controls.Add(this.GroupPage);
            this.LevelsTabControl.Controls.Add(this.TemplateUpgradePage);
            this.LevelsTabControl.Location = new System.Drawing.Point(6, 19);
            this.LevelsTabControl.Name = "LevelsTabControl";
            this.LevelsTabControl.SelectedIndex = 0;
            this.LevelsTabControl.Size = new System.Drawing.Size(480, 284);
            this.LevelsTabControl.TabIndex = 0;
            this.LevelsTabControl.SelectedIndexChanged += new System.EventHandler(this.LevelsTabControl_SelectedIndexChanged);
            // 
            // ConstantPage
            // 
            this.ConstantPage.Controls.Add(this.ConstantSubList);
            this.ConstantPage.Location = new System.Drawing.Point(4, 22);
            this.ConstantPage.Name = "ConstantPage";
            this.ConstantPage.Padding = new System.Windows.Forms.Padding(3);
            this.ConstantPage.Size = new System.Drawing.Size(472, 258);
            this.ConstantPage.TabIndex = 0;
            this.ConstantPage.Text = "Constant";
            this.ConstantPage.UseVisualStyleBackColor = true;
            // 
            // ConstantSubList
            // 
            this.ConstantSubList.Dock = System.Windows.Forms.DockStyle.Fill;
            this.ConstantSubList.FormattingEnabled = true;
            this.ConstantSubList.Items.AddRange(new object[] {
            "Room Impact Cheap (4097:0 - 0) -> Room Impact Moderate (4097:1 - 1)",
            "Comfort Max Cheap (4097:3 - 20) -> 0"});
            this.ConstantSubList.Location = new System.Drawing.Point(3, 3);
            this.ConstantSubList.Name = "ConstantSubList";
            this.ConstantSubList.ScrollAlwaysVisible = true;
            this.ConstantSubList.Size = new System.Drawing.Size(466, 252);
            this.ConstantSubList.TabIndex = 0;
            // 
            // GroupPage
            // 
            this.GroupPage.Controls.Add(this.GroupAdd);
            this.GroupPage.Controls.Add(this.GroupRemove);
            this.GroupPage.Controls.Add(this.GroupNameBox);
            this.GroupPage.Controls.Add(this.GroupNameLabel);
            this.GroupPage.Controls.Add(this.GroupTree);
            this.GroupPage.Location = new System.Drawing.Point(4, 22);
            this.GroupPage.Name = "GroupPage";
            this.GroupPage.Padding = new System.Windows.Forms.Padding(3);
            this.GroupPage.Size = new System.Drawing.Size(472, 258);
            this.GroupPage.TabIndex = 2;
            this.GroupPage.Text = "Groups";
            this.GroupPage.UseVisualStyleBackColor = true;
            // 
            // GroupAdd
            // 
            this.GroupAdd.Location = new System.Drawing.Point(298, 19);
            this.GroupAdd.Name = "GroupAdd";
            this.GroupAdd.Size = new System.Drawing.Size(75, 23);
            this.GroupAdd.TabIndex = 4;
            this.GroupAdd.Text = "Add Group";
            this.GroupAdd.UseVisualStyleBackColor = true;
            this.GroupAdd.Click += new System.EventHandler(this.GroupAdd_Click);
            // 
            // GroupRemove
            // 
            this.GroupRemove.Location = new System.Drawing.Point(379, 19);
            this.GroupRemove.Name = "GroupRemove";
            this.GroupRemove.Size = new System.Drawing.Size(89, 23);
            this.GroupRemove.TabIndex = 3;
            this.GroupRemove.Text = "Remove Group";
            this.GroupRemove.UseVisualStyleBackColor = true;
            this.GroupRemove.Click += new System.EventHandler(this.GroupRemove_Click);
            // 
            // GroupNameBox
            // 
            this.GroupNameBox.Location = new System.Drawing.Point(4, 20);
            this.GroupNameBox.Name = "GroupNameBox";
            this.GroupNameBox.Size = new System.Drawing.Size(165, 20);
            this.GroupNameBox.TabIndex = 2;
            this.GroupNameBox.TextChanged += new System.EventHandler(this.GroupNameBox_TextChanged);
            // 
            // GroupNameLabel
            // 
            this.GroupNameLabel.AutoSize = true;
            this.GroupNameLabel.Location = new System.Drawing.Point(0, 4);
            this.GroupNameLabel.Name = "GroupNameLabel";
            this.GroupNameLabel.Size = new System.Drawing.Size(67, 13);
            this.GroupNameLabel.TabIndex = 1;
            this.GroupNameLabel.Text = "Group Name";
            // 
            // GroupTree
            // 
            this.GroupTree.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.GroupTree.FullRowSelect = true;
            this.GroupTree.HideSelection = false;
            this.GroupTree.Location = new System.Drawing.Point(3, 45);
            this.GroupTree.Name = "GroupTree";
            treeNode1.Name = "Node1";
            treeNode1.Text = "Max Fun Cheap (4096:0 - 50)";
            treeNode2.Name = "Node2";
            treeNode2.Text = "Max Fun Exp (4096:1 - 70)";
            treeNode3.Checked = true;
            treeNode3.Name = "Node0";
            treeNode3.Text = "Max Fun (default: 50)";
            this.GroupTree.Nodes.AddRange(new System.Windows.Forms.TreeNode[] {
            treeNode3});
            this.GroupTree.ShowRootLines = false;
            this.GroupTree.Size = new System.Drawing.Size(466, 210);
            this.GroupTree.TabIndex = 0;
            this.GroupTree.AfterSelect += new System.Windows.Forms.TreeViewEventHandler(this.GroupTree_AfterSelect);
            // 
            // TemplateUpgradePage
            // 
            this.TemplateUpgradePage.Controls.Add(this.UpgradeContainer);
            this.TemplateUpgradePage.Location = new System.Drawing.Point(4, 22);
            this.TemplateUpgradePage.Name = "TemplateUpgradePage";
            this.TemplateUpgradePage.Padding = new System.Windows.Forms.Padding(3);
            this.TemplateUpgradePage.Size = new System.Drawing.Size(472, 258);
            this.TemplateUpgradePage.TabIndex = 1;
            this.TemplateUpgradePage.Text = "Template Upgrade";
            this.TemplateUpgradePage.UseVisualStyleBackColor = true;
            // 
            // UpgradeContainer
            // 
            this.UpgradeContainer.Dock = System.Windows.Forms.DockStyle.Fill;
            this.UpgradeContainer.IsSplitterFixed = true;
            this.UpgradeContainer.Location = new System.Drawing.Point(3, 3);
            this.UpgradeContainer.Name = "UpgradeContainer";
            this.UpgradeContainer.Orientation = System.Windows.Forms.Orientation.Horizontal;
            // 
            // UpgradeContainer.Panel1
            // 
            this.UpgradeContainer.Panel1.Controls.Add(this.DescriptionText);
            this.UpgradeContainer.Panel1.Controls.Add(this.DescriptionLabel);
            this.UpgradeContainer.Panel1.Controls.Add(this.AdObjectButton);
            this.UpgradeContainer.Panel1.Controls.Add(this.AdText);
            this.UpgradeContainer.Panel1.Controls.Add(this.AdLabel);
            this.UpgradeContainer.Panel1.Controls.Add(this.RelativeCheck);
            this.UpgradeContainer.Panel1.Controls.Add(this.SimoleonLabel);
            this.UpgradeContainer.Panel1.Controls.Add(this.UpgradeValue);
            this.UpgradeContainer.Panel1.Controls.Add(this.PriceValueRadio);
            this.UpgradeContainer.Panel1.Controls.Add(this.PriceObjectRadio);
            this.UpgradeContainer.Panel1.Controls.Add(this.UpgradeObjectButton);
            this.UpgradeContainer.Panel1.Controls.Add(this.UpgradeNameText);
            this.UpgradeContainer.Panel1.Controls.Add(this.PriceLabel);
            this.UpgradeContainer.Panel1.Controls.Add(this.NameLabel);
            // 
            // UpgradeContainer.Panel2
            // 
            this.UpgradeContainer.Panel2.Controls.Add(this.UpgradeSubList);
            this.UpgradeContainer.Size = new System.Drawing.Size(466, 252);
            this.UpgradeContainer.SplitterDistance = 126;
            this.UpgradeContainer.TabIndex = 1;
            // 
            // DescriptionText
            // 
            this.DescriptionText.Location = new System.Drawing.Point(3, 102);
            this.DescriptionText.Name = "DescriptionText";
            this.DescriptionText.Size = new System.Drawing.Size(460, 20);
            this.DescriptionText.TabIndex = 13;
            this.DescriptionText.TextChanged += new System.EventHandler(this.DescriptionText_TextChanged);
            // 
            // DescriptionLabel
            // 
            this.DescriptionLabel.AutoSize = true;
            this.DescriptionLabel.Location = new System.Drawing.Point(-3, 86);
            this.DescriptionLabel.Name = "DescriptionLabel";
            this.DescriptionLabel.Size = new System.Drawing.Size(236, 13);
            this.DescriptionLabel.TabIndex = 12;
            this.DescriptionLabel.Text = "Description (for devs, but also shown on custom)";
            // 
            // AdObjectButton
            // 
            this.AdObjectButton.Location = new System.Drawing.Point(352, 16);
            this.AdObjectButton.Name = "AdObjectButton";
            this.AdObjectButton.Size = new System.Drawing.Size(107, 23);
            this.AdObjectButton.TabIndex = 11;
            this.AdObjectButton.Text = "Ads from Object";
            this.AdObjectButton.UseVisualStyleBackColor = true;
            this.AdObjectButton.Click += new System.EventHandler(this.AdObjectButton_Click);
            // 
            // AdText
            // 
            this.AdText.Location = new System.Drawing.Point(159, 18);
            this.AdText.Name = "AdText";
            this.AdText.Size = new System.Drawing.Size(187, 20);
            this.AdText.TabIndex = 10;
            this.AdText.Text = "comfort:9;room:3";
            this.AdText.TextChanged += new System.EventHandler(this.AdText_TextChanged);
            // 
            // AdLabel
            // 
            this.AdLabel.AutoSize = true;
            this.AdLabel.Location = new System.Drawing.Point(154, 2);
            this.AdLabel.Name = "AdLabel";
            this.AdLabel.Size = new System.Drawing.Size(79, 13);
            this.AdLabel.TabIndex = 9;
            this.AdLabel.Text = "Advertisements";
            // 
            // RelativeCheck
            // 
            this.RelativeCheck.AutoSize = true;
            this.RelativeCheck.Location = new System.Drawing.Point(390, 57);
            this.RelativeCheck.Name = "RelativeCheck";
            this.RelativeCheck.Size = new System.Drawing.Size(65, 17);
            this.RelativeCheck.TabIndex = 8;
            this.RelativeCheck.Text = "Relative";
            this.RelativeCheck.UseVisualStyleBackColor = true;
            this.RelativeCheck.CheckedChanged += new System.EventHandler(this.RelativeCheck_CheckedChanged);
            // 
            // SimoleonLabel
            // 
            this.SimoleonLabel.AutoSize = true;
            this.SimoleonLabel.Location = new System.Drawing.Point(276, 57);
            this.SimoleonLabel.Name = "SimoleonLabel";
            this.SimoleonLabel.Size = new System.Drawing.Size(13, 13);
            this.SimoleonLabel.TabIndex = 7;
            this.SimoleonLabel.Text = "§";
            // 
            // UpgradeValue
            // 
            this.UpgradeValue.Location = new System.Drawing.Point(289, 55);
            this.UpgradeValue.Maximum = new decimal(new int[] {
            99999999,
            0,
            0,
            0});
            this.UpgradeValue.Name = "UpgradeValue";
            this.UpgradeValue.Size = new System.Drawing.Size(83, 20);
            this.UpgradeValue.TabIndex = 6;
            this.UpgradeValue.ThousandsSeparator = true;
            this.UpgradeValue.Value = new decimal(new int[] {
            99999999,
            0,
            0,
            0});
            this.UpgradeValue.ValueChanged += new System.EventHandler(this.UpgradeValue_ValueChanged);
            // 
            // PriceValueRadio
            // 
            this.PriceValueRadio.AutoSize = true;
            this.PriceValueRadio.Location = new System.Drawing.Point(258, 58);
            this.PriceValueRadio.Name = "PriceValueRadio";
            this.PriceValueRadio.Size = new System.Drawing.Size(14, 13);
            this.PriceValueRadio.TabIndex = 5;
            this.PriceValueRadio.TabStop = true;
            this.PriceValueRadio.UseVisualStyleBackColor = true;
            this.PriceValueRadio.CheckedChanged += new System.EventHandler(this.PriceValueRadio_CheckedChanged);
            // 
            // PriceObjectRadio
            // 
            this.PriceObjectRadio.AutoSize = true;
            this.PriceObjectRadio.Location = new System.Drawing.Point(36, 58);
            this.PriceObjectRadio.Name = "PriceObjectRadio";
            this.PriceObjectRadio.Size = new System.Drawing.Size(14, 13);
            this.PriceObjectRadio.TabIndex = 4;
            this.PriceObjectRadio.TabStop = true;
            this.PriceObjectRadio.UseVisualStyleBackColor = true;
            this.PriceObjectRadio.CheckedChanged += new System.EventHandler(this.PriceObjectRadio_CheckedChanged);
            // 
            // UpgradeObjectButton
            // 
            this.UpgradeObjectButton.Location = new System.Drawing.Point(56, 53);
            this.UpgradeObjectButton.Name = "UpgradeObjectButton";
            this.UpgradeObjectButton.Size = new System.Drawing.Size(184, 23);
            this.UpgradeObjectButton.TabIndex = 3;
            this.UpgradeObjectButton.Text = "Select Object";
            this.UpgradeObjectButton.UseVisualStyleBackColor = true;
            this.UpgradeObjectButton.Click += new System.EventHandler(this.UpgradeObjectButton_Click);
            // 
            // UpgradeNameText
            // 
            this.UpgradeNameText.FormattingEnabled = true;
            this.UpgradeNameText.Items.AddRange(new object[] {
            "Cheap",
            "Moderate",
            "Expensive",
            "Very Expensive",
            "Extra",
            "Limited Edition",
            "Drivable",
            "Custom..."});
            this.UpgradeNameText.Location = new System.Drawing.Point(3, 18);
            this.UpgradeNameText.Name = "UpgradeNameText";
            this.UpgradeNameText.Size = new System.Drawing.Size(142, 21);
            this.UpgradeNameText.TabIndex = 2;
            this.UpgradeNameText.SelectedIndexChanged += new System.EventHandler(this.UpgradeNameText_SelectedIndexChanged);
            // 
            // PriceLabel
            // 
            this.PriceLabel.AutoSize = true;
            this.PriceLabel.Location = new System.Drawing.Point(-3, 57);
            this.PriceLabel.Name = "PriceLabel";
            this.PriceLabel.Size = new System.Drawing.Size(34, 13);
            this.PriceLabel.TabIndex = 2;
            this.PriceLabel.Text = "Price:";
            // 
            // NameLabel
            // 
            this.NameLabel.AutoSize = true;
            this.NameLabel.Location = new System.Drawing.Point(-3, 2);
            this.NameLabel.Name = "NameLabel";
            this.NameLabel.Size = new System.Drawing.Size(35, 13);
            this.NameLabel.TabIndex = 0;
            this.NameLabel.Text = "Name";
            // 
            // UpgradeSubList
            // 
            this.UpgradeSubList.Dock = System.Windows.Forms.DockStyle.Fill;
            this.UpgradeSubList.FormattingEnabled = true;
            this.UpgradeSubList.Items.AddRange(new object[] {
            "Room Impact Cheap (4097:0 - 0) -> Room Impact Moderate (4097:1 - 1)",
            "Comfort Max Cheap (4097:3 - 20) -> 0"});
            this.UpgradeSubList.Location = new System.Drawing.Point(0, 0);
            this.UpgradeSubList.Name = "UpgradeSubList";
            this.UpgradeSubList.ScrollAlwaysVisible = true;
            this.UpgradeSubList.Size = new System.Drawing.Size(466, 122);
            this.UpgradeSubList.TabIndex = 1;
            this.UpgradeSubList.SelectedIndexChanged += new System.EventHandler(this.UpgradeSubList_SelectedIndexChanged);
            // 
            // EntryCRDButton
            // 
            this.EntryCRDButton.Location = new System.Drawing.Point(619, 3);
            this.EntryCRDButton.Name = "EntryCRDButton";
            this.EntryCRDButton.Size = new System.Drawing.Size(140, 23);
            this.EntryCRDButton.TabIndex = 2;
            this.EntryCRDButton.Text = "Create Upgrades Entry";
            this.EntryCRDButton.UseVisualStyleBackColor = true;
            this.EntryCRDButton.Click += new System.EventHandler(this.EntryCRDButton_Click);
            // 
            // UpgradeStatusLabel
            // 
            this.UpgradeStatusLabel.AutoSize = true;
            this.UpgradeStatusLabel.Location = new System.Drawing.Point(6, 8);
            this.UpgradeStatusLabel.Name = "UpgradeStatusLabel";
            this.UpgradeStatusLabel.Size = new System.Drawing.Size(219, 13);
            this.UpgradeStatusLabel.TabIndex = 3;
            this.UpgradeStatusLabel.Text = "Upgrade Editing is not available when online.";
            // 
            // SaveButton
            // 
            this.SaveButton.Location = new System.Drawing.Point(554, 3);
            this.SaveButton.Name = "SaveButton";
            this.SaveButton.Size = new System.Drawing.Size(59, 23);
            this.SaveButton.TabIndex = 4;
            this.SaveButton.Text = "Save";
            this.SaveButton.UseVisualStyleBackColor = true;
            this.SaveButton.Click += new System.EventHandler(this.SaveButton_Click);
            // 
            // CopyButton
            // 
            this.CopyButton.Location = new System.Drawing.Point(383, 3);
            this.CopyButton.Name = "CopyButton";
            this.CopyButton.Size = new System.Drawing.Size(75, 23);
            this.CopyButton.TabIndex = 5;
            this.CopyButton.Text = "Copy Levels";
            this.CopyButton.UseVisualStyleBackColor = true;
            this.CopyButton.Click += new System.EventHandler(this.CopyButton_Click);
            // 
            // PasteButton
            // 
            this.PasteButton.Location = new System.Drawing.Point(464, 3);
            this.PasteButton.Name = "PasteButton";
            this.PasteButton.Size = new System.Drawing.Size(84, 23);
            this.PasteButton.TabIndex = 6;
            this.PasteButton.Text = "Paste Levels";
            this.PasteButton.UseVisualStyleBackColor = true;
            this.PasteButton.Click += new System.EventHandler(this.PasteButton_Click);
            // 
            // UpgradeEditor
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.PasteButton);
            this.Controls.Add(this.CopyButton);
            this.Controls.Add(this.SaveButton);
            this.Controls.Add(this.UpgradeStatusLabel);
            this.Controls.Add(this.EntryCRDButton);
            this.Controls.Add(this.IffSpecificBox);
            this.Controls.Add(this.ObjectSpecificBox);
            this.Name = "UpgradeEditor";
            this.Size = new System.Drawing.Size(762, 459);
            this.ObjectSpecificBox.ResumeLayout(false);
            this.ObjectSpecificBox.PerformLayout();
            this.IffSpecificBox.ResumeLayout(false);
            this.SubSpecificBox.ResumeLayout(false);
            this.SubSpecificBox.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.SubTargetValue)).EndInit();
            this.LevelsTabControl.ResumeLayout(false);
            this.ConstantPage.ResumeLayout(false);
            this.GroupPage.ResumeLayout(false);
            this.GroupPage.PerformLayout();
            this.TemplateUpgradePage.ResumeLayout(false);
            this.UpgradeContainer.Panel1.ResumeLayout(false);
            this.UpgradeContainer.Panel1.PerformLayout();
            this.UpgradeContainer.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.UpgradeContainer)).EndInit();
            this.UpgradeContainer.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.UpgradeValue)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.GroupBox ObjectSpecificBox;
        private System.Windows.Forms.Label EndLabel;
        private System.Windows.Forms.Label StartLabel;
        private System.Windows.Forms.GroupBox IffSpecificBox;
        private System.Windows.Forms.TabControl LevelsTabControl;
        private System.Windows.Forms.TabPage ConstantPage;
        private System.Windows.Forms.ListBox ConstantSubList;
        private System.Windows.Forms.TabPage TemplateUpgradePage;
        private System.Windows.Forms.SplitContainer UpgradeContainer;
        private System.Windows.Forms.Label NameLabel;
        private System.Windows.Forms.ListBox UpgradeSubList;
        private System.Windows.Forms.ComboBox UpgradeNameText;
        private System.Windows.Forms.Label PriceLabel;
        private System.Windows.Forms.CheckBox RelativeCheck;
        private System.Windows.Forms.Label SimoleonLabel;
        private System.Windows.Forms.NumericUpDown UpgradeValue;
        private System.Windows.Forms.RadioButton PriceValueRadio;
        private System.Windows.Forms.RadioButton PriceObjectRadio;
        private System.Windows.Forms.Button UpgradeObjectButton;
        private System.Windows.Forms.Button RemoveSubButton;
        private System.Windows.Forms.Button AddSubButton;
        private System.Windows.Forms.GroupBox SubSpecificBox;
        private System.Windows.Forms.Button AdObjectButton;
        private System.Windows.Forms.TextBox AdText;
        private System.Windows.Forms.Label AdLabel;
        private System.Windows.Forms.NumericUpDown SubTargetValue;
        private System.Windows.Forms.ComboBox SubTargetTuning;
        private System.Windows.Forms.RadioButton SubTargetValueRadio;
        private System.Windows.Forms.RadioButton SubTargetTuningRadio;
        private System.Windows.Forms.Label SubToLabel;
        private System.Windows.Forms.Label SubFromLabel;
        private System.Windows.Forms.ComboBox SubFromTuning;
        private System.Windows.Forms.TextBox DescriptionText;
        private System.Windows.Forms.Label DescriptionLabel;
        private System.Windows.Forms.Label SubDescriptionLabel;
        private System.Windows.Forms.CheckBox FlagOriginal;
        private System.Windows.Forms.Label FlagsLabel;
        private System.Windows.Forms.ComboBox EndLevelCombo;
        private System.Windows.Forms.ComboBox StartLevelCombo;
        private System.Windows.Forms.Label UpgradeArrowLabel;
        private System.Windows.Forms.Button EntryCRDButton;
        private System.Windows.Forms.Label UpgradeStatusLabel;
        private System.Windows.Forms.Button SaveButton;
        private System.Windows.Forms.Button CopyButton;
        private System.Windows.Forms.Button PasteButton;
        private System.Windows.Forms.Button RemoveUpgradeButton;
        private System.Windows.Forms.TabPage GroupPage;
        private System.Windows.Forms.Button GroupAdd;
        private System.Windows.Forms.Button GroupRemove;
        private System.Windows.Forms.TextBox GroupNameBox;
        private System.Windows.Forms.Label GroupNameLabel;
        private System.Windows.Forms.TreeView GroupTree;
    }
}
