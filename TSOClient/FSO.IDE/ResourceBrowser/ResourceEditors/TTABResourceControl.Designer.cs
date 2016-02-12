namespace FSO.IDE.ResourceBrowser.ResourceEditors
{
    partial class TTABResourceControl
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
            System.Windows.Forms.ListViewItem listViewItem1 = new System.Windows.Forms.ListViewItem(new string[] {
            "0",
            "---",
            "Interaction - Boring Interaction"}, -1);
            System.Windows.Forms.TreeNode treeNode1 = new System.Windows.Forms.TreeNode("Boring Interaction (0 / --- / Interaction - Boring Interaction)");
            System.Windows.Forms.TreeNode treeNode2 = new System.Windows.Forms.TreeNode("Alright Interaction (1 / Helper - Is Alright? / Interaction - Alright Interaction" +
        ")");
            System.Windows.Forms.TreeNode treeNode3 = new System.Windows.Forms.TreeNode("Just Kidding (2 / Interaction - Just Kidding TEST / Interaction - Just Kidding)");
            System.Windows.Forms.TreeNode treeNode4 = new System.Windows.Forms.TreeNode("Exciting Stuff...", new System.Windows.Forms.TreeNode[] {
            treeNode3});
            System.Windows.Forms.TreeNode treeNode5 = new System.Windows.Forms.TreeNode("Test...", new System.Windows.Forms.TreeNode[] {
            treeNode1,
            treeNode2,
            treeNode4});
            System.Windows.Forms.TreeNode treeNode6 = new System.Windows.Forms.TreeNode("Repair (3 / --- / Interaction - Repair)");
            this.InteractionList = new System.Windows.Forms.ListView();
            this.IDHeader = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.CheckHeader = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.ActionHeader = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.MoveDownBtn = new System.Windows.Forms.Button();
            this.RemoveBtn = new System.Windows.Forms.Button();
            this.AddBtn = new System.Windows.Forms.Button();
            this.MoveUpBtn = new System.Windows.Forms.Button();
            this.ActionButton = new System.Windows.Forms.Button();
            this.CheckButton = new System.Windows.Forms.Button();
            this.AllowBox = new System.Windows.Forms.GroupBox();
            this.AllowDogs = new System.Windows.Forms.CheckBox();
            this.AllowCats = new System.Windows.Forms.CheckBox();
            this.AllowGhosts = new System.Windows.Forms.CheckBox();
            this.AllowCSRs = new System.Windows.Forms.CheckBox();
            this.AllowOwner = new System.Windows.Forms.CheckBox();
            this.AllowRoomies = new System.Windows.Forms.CheckBox();
            this.AllowFriends = new System.Windows.Forms.CheckBox();
            this.AllowVisitors = new System.Windows.Forms.CheckBox();
            this.MetaBox = new System.Windows.Forms.GroupBox();
            this.JoinInput = new System.Windows.Forms.NumericUpDown();
            this.JoinLabel = new System.Windows.Forms.Label();
            this.AutonomyInput = new System.Windows.Forms.NumericUpDown();
            this.AttenuationCombo = new System.Windows.Forms.ComboBox();
            this.AutonomyLabel = new System.Windows.Forms.Label();
            this.AttenuationLabel = new System.Windows.Forms.Label();
            this.LanguageCombo = new System.Windows.Forms.ComboBox();
            this.InteractionPathName = new System.Windows.Forms.TextBox();
            this.PathNameLabel = new System.Windows.Forms.Label();
            this.FlagsBox = new System.Windows.Forms.GroupBox();
            this.FlagDead = new System.Windows.Forms.CheckBox();
            this.FlagCheck = new System.Windows.Forms.CheckBox();
            this.FlagRepair = new System.Windows.Forms.CheckBox();
            this.FlagCarrying = new System.Windows.Forms.CheckBox();
            this.InteractionFlagsLabel = new System.Windows.Forms.Label();
            this.FlagConsecutive = new System.Windows.Forms.CheckBox();
            this.FlagRunImmediately = new System.Windows.Forms.CheckBox();
            this.FlagAutoFirst = new System.Windows.Forms.CheckBox();
            this.FlagMustRun = new System.Windows.Forms.CheckBox();
            this.FlagLeapfrog = new System.Windows.Forms.CheckBox();
            this.FlagDebug = new System.Windows.Forms.CheckBox();
            this.ViewTab = new System.Windows.Forms.TabControl();
            this.tabPage1 = new System.Windows.Forms.TabPage();
            this.tabPage2 = new System.Windows.Forms.TabPage();
            this.PieAdd = new System.Windows.Forms.Button();
            this.PieRemove = new System.Windows.Forms.Button();
            this.PieView = new System.Windows.Forms.TreeView();
            this.MotiveList = new System.Windows.Forms.ListBox();
            this.MotiveBox = new System.Windows.Forms.GroupBox();
            this.ClearMotives = new System.Windows.Forms.Button();
            this.MaxMotive = new System.Windows.Forms.NumericUpDown();
            this.MaxLabel = new System.Windows.Forms.Label();
            this.MinMotive = new System.Windows.Forms.NumericUpDown();
            this.MinLabel = new System.Windows.Forms.Label();
            this.MotivePersonality = new System.Windows.Forms.ComboBox();
            this.VaryLabel = new System.Windows.Forms.Label();
            this.SearchBox = new System.Windows.Forms.TextBox();
            this.SearchIcon = new System.Windows.Forms.PictureBox();
            this.Selector = new FSO.IDE.ResourceBrowser.OBJDSelectorControl();
            this.AllowBox.SuspendLayout();
            this.MetaBox.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.JoinInput)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.AutonomyInput)).BeginInit();
            this.FlagsBox.SuspendLayout();
            this.ViewTab.SuspendLayout();
            this.tabPage1.SuspendLayout();
            this.tabPage2.SuspendLayout();
            this.MotiveBox.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.MaxMotive)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.MinMotive)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.SearchIcon)).BeginInit();
            this.SuspendLayout();
            // 
            // InteractionList
            // 
            this.InteractionList.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.InteractionList.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
            this.IDHeader,
            this.CheckHeader,
            this.ActionHeader});
            this.InteractionList.FullRowSelect = true;
            this.InteractionList.HideSelection = false;
            this.InteractionList.Items.AddRange(new System.Windows.Forms.ListViewItem[] {
            listViewItem1});
            this.InteractionList.Location = new System.Drawing.Point(0, 0);
            this.InteractionList.MultiSelect = false;
            this.InteractionList.Name = "InteractionList";
            this.InteractionList.Size = new System.Drawing.Size(454, 122);
            this.InteractionList.TabIndex = 0;
            this.InteractionList.UseCompatibleStateImageBehavior = false;
            this.InteractionList.View = System.Windows.Forms.View.Details;
            this.InteractionList.SelectedIndexChanged += new System.EventHandler(this.InteractionList_SelectedIndexChanged);
            // 
            // IDHeader
            // 
            this.IDHeader.Text = "#";
            this.IDHeader.Width = 30;
            // 
            // CheckHeader
            // 
            this.CheckHeader.Text = "Check Tree";
            this.CheckHeader.Width = 192;
            // 
            // ActionHeader
            // 
            this.ActionHeader.Text = "Action Tree";
            this.ActionHeader.Width = 196;
            // 
            // MoveDownBtn
            // 
            this.MoveDownBtn.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.MoveDownBtn.Font = new System.Drawing.Font("Microsoft Sans Serif", 11.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.MoveDownBtn.Location = new System.Drawing.Point(457, 93);
            this.MoveDownBtn.Margin = new System.Windows.Forms.Padding(0);
            this.MoveDownBtn.Name = "MoveDownBtn";
            this.MoveDownBtn.Size = new System.Drawing.Size(26, 26);
            this.MoveDownBtn.TabIndex = 11;
            this.MoveDownBtn.Text = "↓";
            this.MoveDownBtn.UseVisualStyleBackColor = true;
            this.MoveDownBtn.Click += new System.EventHandler(this.MoveDownBtn_Click);
            // 
            // RemoveBtn
            // 
            this.RemoveBtn.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.RemoveBtn.Font = new System.Drawing.Font("Microsoft Sans Serif", 11.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.RemoveBtn.Location = new System.Drawing.Point(457, 63);
            this.RemoveBtn.Margin = new System.Windows.Forms.Padding(0);
            this.RemoveBtn.Name = "RemoveBtn";
            this.RemoveBtn.Size = new System.Drawing.Size(26, 26);
            this.RemoveBtn.TabIndex = 12;
            this.RemoveBtn.Text = "-";
            this.RemoveBtn.UseVisualStyleBackColor = true;
            this.RemoveBtn.Click += new System.EventHandler(this.RemoveBtn_Click);
            // 
            // AddBtn
            // 
            this.AddBtn.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.AddBtn.Font = new System.Drawing.Font("Microsoft Sans Serif", 11.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.AddBtn.Location = new System.Drawing.Point(457, 33);
            this.AddBtn.Margin = new System.Windows.Forms.Padding(0);
            this.AddBtn.Name = "AddBtn";
            this.AddBtn.Size = new System.Drawing.Size(26, 26);
            this.AddBtn.TabIndex = 13;
            this.AddBtn.Text = "+";
            this.AddBtn.UseVisualStyleBackColor = true;
            this.AddBtn.Click += new System.EventHandler(this.AddBtn_Click);
            // 
            // MoveUpBtn
            // 
            this.MoveUpBtn.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.MoveUpBtn.Font = new System.Drawing.Font("Microsoft Sans Serif", 11.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.MoveUpBtn.Location = new System.Drawing.Point(457, 3);
            this.MoveUpBtn.Margin = new System.Windows.Forms.Padding(0);
            this.MoveUpBtn.Name = "MoveUpBtn";
            this.MoveUpBtn.Size = new System.Drawing.Size(26, 26);
            this.MoveUpBtn.TabIndex = 14;
            this.MoveUpBtn.Text = "↑";
            this.MoveUpBtn.UseVisualStyleBackColor = true;
            this.MoveUpBtn.Click += new System.EventHandler(this.MoveUpBtn_Click);
            // 
            // ActionButton
            // 
            this.ActionButton.Location = new System.Drawing.Point(3, 156);
            this.ActionButton.Name = "ActionButton";
            this.ActionButton.Size = new System.Drawing.Size(85, 23);
            this.ActionButton.TabIndex = 15;
            this.ActionButton.Text = "Set Action...";
            this.ActionButton.UseVisualStyleBackColor = true;
            this.ActionButton.Click += new System.EventHandler(this.ActionButton_Click);
            // 
            // CheckButton
            // 
            this.CheckButton.Location = new System.Drawing.Point(94, 156);
            this.CheckButton.Name = "CheckButton";
            this.CheckButton.Size = new System.Drawing.Size(85, 23);
            this.CheckButton.TabIndex = 16;
            this.CheckButton.Text = "Set Check...";
            this.CheckButton.UseVisualStyleBackColor = true;
            this.CheckButton.Click += new System.EventHandler(this.CheckButton_Click);
            // 
            // AllowBox
            // 
            this.AllowBox.Controls.Add(this.AllowDogs);
            this.AllowBox.Controls.Add(this.AllowCats);
            this.AllowBox.Controls.Add(this.AllowGhosts);
            this.AllowBox.Controls.Add(this.AllowCSRs);
            this.AllowBox.Controls.Add(this.AllowOwner);
            this.AllowBox.Controls.Add(this.AllowRoomies);
            this.AllowBox.Controls.Add(this.AllowFriends);
            this.AllowBox.Controls.Add(this.AllowVisitors);
            this.AllowBox.Location = new System.Drawing.Point(4, 181);
            this.AllowBox.Name = "AllowBox";
            this.AllowBox.Size = new System.Drawing.Size(175, 88);
            this.AllowBox.TabIndex = 17;
            this.AllowBox.TabStop = false;
            this.AllowBox.Text = "Allow...";
            // 
            // AllowDogs
            // 
            this.AllowDogs.AutoSize = true;
            this.AllowDogs.Location = new System.Drawing.Point(103, 67);
            this.AllowDogs.Margin = new System.Windows.Forms.Padding(3, 0, 3, 0);
            this.AllowDogs.Name = "AllowDogs";
            this.AllowDogs.Size = new System.Drawing.Size(51, 17);
            this.AllowDogs.TabIndex = 7;
            this.AllowDogs.Text = "Dogs";
            this.AllowDogs.UseVisualStyleBackColor = true;
            this.AllowDogs.CheckedChanged += new System.EventHandler(this.FlagCheckedChanged);
            // 
            // AllowCats
            // 
            this.AllowCats.AutoSize = true;
            this.AllowCats.Location = new System.Drawing.Point(103, 50);
            this.AllowCats.Margin = new System.Windows.Forms.Padding(3, 0, 3, 0);
            this.AllowCats.Name = "AllowCats";
            this.AllowCats.Size = new System.Drawing.Size(47, 17);
            this.AllowCats.TabIndex = 6;
            this.AllowCats.Text = "Cats";
            this.AllowCats.UseVisualStyleBackColor = true;
            this.AllowCats.CheckedChanged += new System.EventHandler(this.FlagCheckedChanged);
            // 
            // AllowGhosts
            // 
            this.AllowGhosts.AutoSize = true;
            this.AllowGhosts.Location = new System.Drawing.Point(103, 33);
            this.AllowGhosts.Margin = new System.Windows.Forms.Padding(3, 0, 3, 0);
            this.AllowGhosts.Name = "AllowGhosts";
            this.AllowGhosts.Size = new System.Drawing.Size(59, 17);
            this.AllowGhosts.TabIndex = 5;
            this.AllowGhosts.Text = "Ghosts";
            this.AllowGhosts.UseVisualStyleBackColor = true;
            this.AllowGhosts.CheckedChanged += new System.EventHandler(this.FlagCheckedChanged);
            // 
            // AllowCSRs
            // 
            this.AllowCSRs.AutoSize = true;
            this.AllowCSRs.Location = new System.Drawing.Point(103, 16);
            this.AllowCSRs.Margin = new System.Windows.Forms.Padding(3, 0, 3, 0);
            this.AllowCSRs.Name = "AllowCSRs";
            this.AllowCSRs.Size = new System.Drawing.Size(53, 17);
            this.AllowCSRs.TabIndex = 4;
            this.AllowCSRs.Text = "CSRs";
            this.AllowCSRs.UseVisualStyleBackColor = true;
            this.AllowCSRs.CheckedChanged += new System.EventHandler(this.FlagCheckedChanged);
            // 
            // AllowOwner
            // 
            this.AllowOwner.AutoSize = true;
            this.AllowOwner.Location = new System.Drawing.Point(6, 67);
            this.AllowOwner.Margin = new System.Windows.Forms.Padding(3, 0, 3, 0);
            this.AllowOwner.Name = "AllowOwner";
            this.AllowOwner.Size = new System.Drawing.Size(57, 17);
            this.AllowOwner.TabIndex = 3;
            this.AllowOwner.Text = "Owner";
            this.AllowOwner.UseVisualStyleBackColor = true;
            this.AllowOwner.CheckedChanged += new System.EventHandler(this.FlagCheckedChanged);
            // 
            // AllowRoomies
            // 
            this.AllowRoomies.AutoSize = true;
            this.AllowRoomies.Location = new System.Drawing.Point(6, 50);
            this.AllowRoomies.Margin = new System.Windows.Forms.Padding(3, 0, 3, 0);
            this.AllowRoomies.Name = "AllowRoomies";
            this.AllowRoomies.Size = new System.Drawing.Size(82, 17);
            this.AllowRoomies.TabIndex = 2;
            this.AllowRoomies.Text = "Roommates";
            this.AllowRoomies.UseVisualStyleBackColor = true;
            this.AllowRoomies.CheckedChanged += new System.EventHandler(this.FlagCheckedChanged);
            // 
            // AllowFriends
            // 
            this.AllowFriends.AutoSize = true;
            this.AllowFriends.Location = new System.Drawing.Point(6, 33);
            this.AllowFriends.Margin = new System.Windows.Forms.Padding(3, 0, 3, 0);
            this.AllowFriends.Name = "AllowFriends";
            this.AllowFriends.Size = new System.Drawing.Size(60, 17);
            this.AllowFriends.TabIndex = 1;
            this.AllowFriends.Text = "Friends";
            this.AllowFriends.UseVisualStyleBackColor = true;
            this.AllowFriends.CheckedChanged += new System.EventHandler(this.FlagCheckedChanged);
            // 
            // AllowVisitors
            // 
            this.AllowVisitors.AutoSize = true;
            this.AllowVisitors.Location = new System.Drawing.Point(6, 16);
            this.AllowVisitors.Margin = new System.Windows.Forms.Padding(3, 0, 3, 0);
            this.AllowVisitors.Name = "AllowVisitors";
            this.AllowVisitors.Size = new System.Drawing.Size(59, 17);
            this.AllowVisitors.TabIndex = 0;
            this.AllowVisitors.Text = "Visitors";
            this.AllowVisitors.UseVisualStyleBackColor = true;
            this.AllowVisitors.CheckedChanged += new System.EventHandler(this.FlagCheckedChanged);
            // 
            // MetaBox
            // 
            this.MetaBox.Controls.Add(this.JoinInput);
            this.MetaBox.Controls.Add(this.JoinLabel);
            this.MetaBox.Controls.Add(this.AutonomyInput);
            this.MetaBox.Controls.Add(this.AttenuationCombo);
            this.MetaBox.Controls.Add(this.AutonomyLabel);
            this.MetaBox.Controls.Add(this.AttenuationLabel);
            this.MetaBox.Controls.Add(this.LanguageCombo);
            this.MetaBox.Controls.Add(this.InteractionPathName);
            this.MetaBox.Controls.Add(this.PathNameLabel);
            this.MetaBox.Location = new System.Drawing.Point(188, 152);
            this.MetaBox.Name = "MetaBox";
            this.MetaBox.Size = new System.Drawing.Size(310, 116);
            this.MetaBox.TabIndex = 18;
            this.MetaBox.TabStop = false;
            this.MetaBox.Text = "Meta";
            // 
            // JoinInput
            // 
            this.JoinInput.Location = new System.Drawing.Point(235, 84);
            this.JoinInput.Maximum = new decimal(new int[] {
            255,
            0,
            0,
            0});
            this.JoinInput.Minimum = new decimal(new int[] {
            1,
            0,
            0,
            -2147483648});
            this.JoinInput.Name = "JoinInput";
            this.JoinInput.Size = new System.Drawing.Size(69, 20);
            this.JoinInput.TabIndex = 9;
            this.JoinInput.Value = new decimal(new int[] {
            1,
            0,
            0,
            -2147483648});
            // 
            // JoinLabel
            // 
            this.JoinLabel.AutoSize = true;
            this.JoinLabel.Location = new System.Drawing.Point(232, 67);
            this.JoinLabel.Name = "JoinLabel";
            this.JoinLabel.Size = new System.Drawing.Size(72, 13);
            this.JoinLabel.TabIndex = 8;
            this.JoinLabel.Text = "Join Action #:";
            // 
            // AutonomyInput
            // 
            this.AutonomyInput.Location = new System.Drawing.Point(105, 84);
            this.AutonomyInput.Name = "AutonomyInput";
            this.AutonomyInput.Size = new System.Drawing.Size(104, 20);
            this.AutonomyInput.TabIndex = 7;
            // 
            // AttenuationCombo
            // 
            this.AttenuationCombo.FormattingEnabled = true;
            this.AttenuationCombo.Location = new System.Drawing.Point(6, 83);
            this.AttenuationCombo.Name = "AttenuationCombo";
            this.AttenuationCombo.Size = new System.Drawing.Size(90, 21);
            this.AttenuationCombo.TabIndex = 6;
            // 
            // AutonomyLabel
            // 
            this.AutonomyLabel.AutoSize = true;
            this.AutonomyLabel.Location = new System.Drawing.Point(102, 67);
            this.AutonomyLabel.Name = "AutonomyLabel";
            this.AutonomyLabel.Size = new System.Drawing.Size(107, 13);
            this.AutonomyLabel.TabIndex = 5;
            this.AutonomyLabel.Text = "Autonomy Threshold:";
            // 
            // AttenuationLabel
            // 
            this.AttenuationLabel.AutoSize = true;
            this.AttenuationLabel.Location = new System.Drawing.Point(3, 67);
            this.AttenuationLabel.Name = "AttenuationLabel";
            this.AttenuationLabel.Size = new System.Drawing.Size(64, 13);
            this.AttenuationLabel.TabIndex = 4;
            this.AttenuationLabel.Text = "Attenuation:";
            // 
            // LanguageCombo
            // 
            this.LanguageCombo.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.LanguageCombo.FormattingEnabled = true;
            this.LanguageCombo.Items.AddRange(new object[] {
            "English"});
            this.LanguageCombo.Location = new System.Drawing.Point(200, 31);
            this.LanguageCombo.Name = "LanguageCombo";
            this.LanguageCombo.Size = new System.Drawing.Size(104, 21);
            this.LanguageCombo.TabIndex = 2;
            // 
            // InteractionPathName
            // 
            this.InteractionPathName.Location = new System.Drawing.Point(6, 32);
            this.InteractionPathName.Name = "InteractionPathName";
            this.InteractionPathName.Size = new System.Drawing.Size(188, 20);
            this.InteractionPathName.TabIndex = 1;
            this.InteractionPathName.Text = "Test.../Boring Interaction";
            this.InteractionPathName.TextChanged += new System.EventHandler(this.InteractionPathName_TextChanged);
            // 
            // PathNameLabel
            // 
            this.PathNameLabel.AutoSize = true;
            this.PathNameLabel.Location = new System.Drawing.Point(3, 16);
            this.PathNameLabel.Name = "PathNameLabel";
            this.PathNameLabel.Size = new System.Drawing.Size(116, 13);
            this.PathNameLabel.TabIndex = 0;
            this.PathNameLabel.Text = "Interaction Path Name:";
            // 
            // FlagsBox
            // 
            this.FlagsBox.Controls.Add(this.FlagDead);
            this.FlagsBox.Controls.Add(this.FlagCheck);
            this.FlagsBox.Controls.Add(this.FlagRepair);
            this.FlagsBox.Controls.Add(this.FlagCarrying);
            this.FlagsBox.Controls.Add(this.InteractionFlagsLabel);
            this.FlagsBox.Controls.Add(this.FlagConsecutive);
            this.FlagsBox.Controls.Add(this.FlagRunImmediately);
            this.FlagsBox.Controls.Add(this.FlagAutoFirst);
            this.FlagsBox.Controls.Add(this.FlagMustRun);
            this.FlagsBox.Controls.Add(this.FlagLeapfrog);
            this.FlagsBox.Controls.Add(this.FlagDebug);
            this.FlagsBox.Location = new System.Drawing.Point(4, 275);
            this.FlagsBox.Name = "FlagsBox";
            this.FlagsBox.Size = new System.Drawing.Size(175, 176);
            this.FlagsBox.TabIndex = 19;
            this.FlagsBox.TabStop = false;
            this.FlagsBox.Text = "Flags";
            // 
            // FlagDead
            // 
            this.FlagDead.AutoSize = true;
            this.FlagDead.Location = new System.Drawing.Point(5, 157);
            this.FlagDead.Margin = new System.Windows.Forms.Padding(3, 0, 3, 0);
            this.FlagDead.Name = "FlagDead";
            this.FlagDead.Size = new System.Drawing.Size(130, 17);
            this.FlagDead.TabIndex = 10;
            this.FlagDead.Text = "Available When Dead";
            this.FlagDead.UseVisualStyleBackColor = true;
            this.FlagDead.CheckedChanged += new System.EventHandler(this.FlagCheckedChanged);
            // 
            // FlagCheck
            // 
            this.FlagCheck.AutoSize = true;
            this.FlagCheck.Location = new System.Drawing.Point(5, 140);
            this.FlagCheck.Margin = new System.Windows.Forms.Padding(3, 0, 3, 0);
            this.FlagCheck.Name = "FlagCheck";
            this.FlagCheck.Size = new System.Drawing.Size(141, 17);
            this.FlagCheck.TabIndex = 9;
            this.FlagCheck.Text = "Always Run Check Tree";
            this.FlagCheck.UseVisualStyleBackColor = true;
            this.FlagCheck.CheckedChanged += new System.EventHandler(this.FlagCheckedChanged);
            // 
            // FlagRepair
            // 
            this.FlagRepair.AutoSize = true;
            this.FlagRepair.Location = new System.Drawing.Point(5, 123);
            this.FlagRepair.Margin = new System.Windows.Forms.Padding(3, 0, 3, 0);
            this.FlagRepair.Name = "FlagRepair";
            this.FlagRepair.Size = new System.Drawing.Size(121, 17);
            this.FlagRepair.TabIndex = 8;
            this.FlagRepair.Text = "Is Repair Interaction";
            this.FlagRepair.UseVisualStyleBackColor = true;
            this.FlagRepair.CheckedChanged += new System.EventHandler(this.FlagCheckedChanged);
            // 
            // FlagCarrying
            // 
            this.FlagCarrying.AutoSize = true;
            this.FlagCarrying.Location = new System.Drawing.Point(5, 106);
            this.FlagCarrying.Margin = new System.Windows.Forms.Padding(3, 0, 3, 0);
            this.FlagCarrying.Name = "FlagCarrying";
            this.FlagCarrying.Size = new System.Drawing.Size(142, 17);
            this.FlagCarrying.TabIndex = 7;
            this.FlagCarrying.Text = "Available When Carrying";
            this.FlagCarrying.UseVisualStyleBackColor = true;
            this.FlagCarrying.CheckedChanged += new System.EventHandler(this.FlagCheckedChanged);
            // 
            // InteractionFlagsLabel
            // 
            this.InteractionFlagsLabel.AutoSize = true;
            this.InteractionFlagsLabel.Location = new System.Drawing.Point(3, 91);
            this.InteractionFlagsLabel.Name = "InteractionFlagsLabel";
            this.InteractionFlagsLabel.Size = new System.Drawing.Size(117, 13);
            this.InteractionFlagsLabel.TabIndex = 6;
            this.InteractionFlagsLabel.Text = "Interaction Mask Flags:";
            // 
            // FlagConsecutive
            // 
            this.FlagConsecutive.AutoSize = true;
            this.FlagConsecutive.Location = new System.Drawing.Point(5, 67);
            this.FlagConsecutive.Margin = new System.Windows.Forms.Padding(3, 0, 3, 0);
            this.FlagConsecutive.Name = "FlagConsecutive";
            this.FlagConsecutive.Size = new System.Drawing.Size(113, 17);
            this.FlagConsecutive.TabIndex = 5;
            this.FlagConsecutive.Text = "Allow Consecutive";
            this.FlagConsecutive.UseVisualStyleBackColor = true;
            this.FlagConsecutive.CheckedChanged += new System.EventHandler(this.FlagCheckedChanged);
            // 
            // FlagRunImmediately
            // 
            this.FlagRunImmediately.AutoSize = true;
            this.FlagRunImmediately.Location = new System.Drawing.Point(5, 50);
            this.FlagRunImmediately.Margin = new System.Windows.Forms.Padding(3, 0, 3, 0);
            this.FlagRunImmediately.Name = "FlagRunImmediately";
            this.FlagRunImmediately.Size = new System.Drawing.Size(104, 17);
            this.FlagRunImmediately.TabIndex = 4;
            this.FlagRunImmediately.Text = "Run Immediately";
            this.FlagRunImmediately.UseVisualStyleBackColor = true;
            this.FlagRunImmediately.CheckedChanged += new System.EventHandler(this.FlagCheckedChanged);
            // 
            // FlagAutoFirst
            // 
            this.FlagAutoFirst.AutoSize = true;
            this.FlagAutoFirst.Location = new System.Drawing.Point(82, 33);
            this.FlagAutoFirst.Name = "FlagAutoFirst";
            this.FlagAutoFirst.Size = new System.Drawing.Size(70, 17);
            this.FlagAutoFirst.TabIndex = 3;
            this.FlagAutoFirst.Text = "Auto First";
            this.FlagAutoFirst.UseVisualStyleBackColor = true;
            this.FlagAutoFirst.CheckedChanged += new System.EventHandler(this.FlagCheckedChanged);
            // 
            // FlagMustRun
            // 
            this.FlagMustRun.AutoSize = true;
            this.FlagMustRun.Location = new System.Drawing.Point(5, 33);
            this.FlagMustRun.Margin = new System.Windows.Forms.Padding(3, 0, 3, 0);
            this.FlagMustRun.Name = "FlagMustRun";
            this.FlagMustRun.Size = new System.Drawing.Size(72, 17);
            this.FlagMustRun.TabIndex = 2;
            this.FlagMustRun.Text = "Must-Run";
            this.FlagMustRun.UseVisualStyleBackColor = true;
            this.FlagMustRun.CheckedChanged += new System.EventHandler(this.FlagCheckedChanged);
            // 
            // FlagLeapfrog
            // 
            this.FlagLeapfrog.AutoSize = true;
            this.FlagLeapfrog.Location = new System.Drawing.Point(82, 16);
            this.FlagLeapfrog.Margin = new System.Windows.Forms.Padding(3, 0, 3, 0);
            this.FlagLeapfrog.Name = "FlagLeapfrog";
            this.FlagLeapfrog.Size = new System.Drawing.Size(68, 17);
            this.FlagLeapfrog.TabIndex = 1;
            this.FlagLeapfrog.Text = "Leapfrog";
            this.FlagLeapfrog.UseVisualStyleBackColor = true;
            this.FlagLeapfrog.CheckedChanged += new System.EventHandler(this.FlagCheckedChanged);
            // 
            // FlagDebug
            // 
            this.FlagDebug.AutoSize = true;
            this.FlagDebug.Location = new System.Drawing.Point(5, 16);
            this.FlagDebug.Margin = new System.Windows.Forms.Padding(3, 0, 3, 0);
            this.FlagDebug.Name = "FlagDebug";
            this.FlagDebug.Size = new System.Drawing.Size(58, 17);
            this.FlagDebug.TabIndex = 0;
            this.FlagDebug.Text = "Debug";
            this.FlagDebug.UseVisualStyleBackColor = true;
            this.FlagDebug.CheckedChanged += new System.EventHandler(this.FlagCheckedChanged);
            // 
            // ViewTab
            // 
            this.ViewTab.Controls.Add(this.tabPage1);
            this.ViewTab.Controls.Add(this.tabPage2);
            this.ViewTab.Location = new System.Drawing.Point(4, 5);
            this.ViewTab.Name = "ViewTab";
            this.ViewTab.SelectedIndex = 0;
            this.ViewTab.Size = new System.Drawing.Size(494, 148);
            this.ViewTab.TabIndex = 20;
            // 
            // tabPage1
            // 
            this.tabPage1.Controls.Add(this.InteractionList);
            this.tabPage1.Controls.Add(this.MoveUpBtn);
            this.tabPage1.Controls.Add(this.AddBtn);
            this.tabPage1.Controls.Add(this.RemoveBtn);
            this.tabPage1.Controls.Add(this.MoveDownBtn);
            this.tabPage1.Location = new System.Drawing.Point(4, 22);
            this.tabPage1.Name = "tabPage1";
            this.tabPage1.Padding = new System.Windows.Forms.Padding(3);
            this.tabPage1.Size = new System.Drawing.Size(486, 122);
            this.tabPage1.TabIndex = 0;
            this.tabPage1.Text = "Action View";
            this.tabPage1.UseVisualStyleBackColor = true;
            // 
            // tabPage2
            // 
            this.tabPage2.Controls.Add(this.PieAdd);
            this.tabPage2.Controls.Add(this.PieRemove);
            this.tabPage2.Controls.Add(this.PieView);
            this.tabPage2.Location = new System.Drawing.Point(4, 22);
            this.tabPage2.Name = "tabPage2";
            this.tabPage2.Padding = new System.Windows.Forms.Padding(3);
            this.tabPage2.Size = new System.Drawing.Size(486, 122);
            this.tabPage2.TabIndex = 1;
            this.tabPage2.Text = "Pie Menu View";
            this.tabPage2.UseVisualStyleBackColor = true;
            // 
            // PieAdd
            // 
            this.PieAdd.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.PieAdd.Font = new System.Drawing.Font("Microsoft Sans Serif", 11.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.PieAdd.Location = new System.Drawing.Point(457, 33);
            this.PieAdd.Margin = new System.Windows.Forms.Padding(0);
            this.PieAdd.Name = "PieAdd";
            this.PieAdd.Size = new System.Drawing.Size(26, 26);
            this.PieAdd.TabIndex = 15;
            this.PieAdd.Text = "+";
            this.PieAdd.UseVisualStyleBackColor = true;
            this.PieAdd.Click += new System.EventHandler(this.AddBtn_Click);
            // 
            // PieRemove
            // 
            this.PieRemove.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.PieRemove.Font = new System.Drawing.Font("Microsoft Sans Serif", 11.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.PieRemove.Location = new System.Drawing.Point(457, 63);
            this.PieRemove.Margin = new System.Windows.Forms.Padding(0);
            this.PieRemove.Name = "PieRemove";
            this.PieRemove.Size = new System.Drawing.Size(26, 26);
            this.PieRemove.TabIndex = 14;
            this.PieRemove.Text = "-";
            this.PieRemove.UseVisualStyleBackColor = true;
            this.PieRemove.Click += new System.EventHandler(this.RemoveBtn_Click);
            // 
            // PieView
            // 
            this.PieView.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.PieView.FullRowSelect = true;
            this.PieView.HideSelection = false;
            this.PieView.Indent = 10;
            this.PieView.Location = new System.Drawing.Point(0, 0);
            this.PieView.Margin = new System.Windows.Forms.Padding(0);
            this.PieView.Name = "PieView";
            treeNode1.Name = "Node1";
            treeNode1.Text = "Boring Interaction (0 / --- / Interaction - Boring Interaction)";
            treeNode2.Name = "Node2";
            treeNode2.Text = "Alright Interaction (1 / Helper - Is Alright? / Interaction - Alright Interaction" +
    ")";
            treeNode3.Name = "Node4";
            treeNode3.Text = "Just Kidding (2 / Interaction - Just Kidding TEST / Interaction - Just Kidding)";
            treeNode4.Name = "Node3";
            treeNode4.Text = "Exciting Stuff...";
            treeNode5.Name = "Node0";
            treeNode5.Text = "Test...";
            treeNode6.Name = "Node5";
            treeNode6.Text = "Repair (3 / --- / Interaction - Repair)";
            this.PieView.Nodes.AddRange(new System.Windows.Forms.TreeNode[] {
            treeNode5,
            treeNode6});
            this.PieView.Size = new System.Drawing.Size(454, 122);
            this.PieView.TabIndex = 0;
            this.PieView.AfterSelect += new System.Windows.Forms.TreeViewEventHandler(this.PieView_AfterSelect);
            // 
            // MotiveList
            // 
            this.MotiveList.FormattingEnabled = true;
            this.MotiveList.HorizontalScrollbar = true;
            this.MotiveList.IntegralHeight = false;
            this.MotiveList.Items.AddRange(new object[] {
            "Energy: 99..100, Cooking Skill",
            "Comfort: 99..100, Cooking Skill",
            "Hunger: 99..100, Cooking Skill",
            "Hygiene: 99..100, Cooking Skill",
            "Bladder: 99..100, Cooking Skill",
            "Room: 99..100, Cooking Skill",
            "Social: 99..100, Cooking Skill",
            "Fun: 99..100, Cooking Skill",
            "Mood: 99..100, Cooking Skill"});
            this.MotiveList.Location = new System.Drawing.Point(6, 19);
            this.MotiveList.Name = "MotiveList";
            this.MotiveList.Size = new System.Drawing.Size(113, 147);
            this.MotiveList.TabIndex = 10;
            this.MotiveList.SelectedIndexChanged += new System.EventHandler(this.MotiveList_SelectedIndexChanged);
            // 
            // MotiveBox
            // 
            this.MotiveBox.Controls.Add(this.ClearMotives);
            this.MotiveBox.Controls.Add(this.MaxMotive);
            this.MotiveBox.Controls.Add(this.MaxLabel);
            this.MotiveBox.Controls.Add(this.MinMotive);
            this.MotiveBox.Controls.Add(this.MinLabel);
            this.MotiveBox.Controls.Add(this.MotivePersonality);
            this.MotiveBox.Controls.Add(this.VaryLabel);
            this.MotiveBox.Controls.Add(this.MotiveList);
            this.MotiveBox.Location = new System.Drawing.Point(188, 275);
            this.MotiveBox.Name = "MotiveBox";
            this.MotiveBox.Size = new System.Drawing.Size(228, 175);
            this.MotiveBox.TabIndex = 11;
            this.MotiveBox.TabStop = false;
            this.MotiveBox.Text = "Motive Advertisements";
            // 
            // ClearMotives
            // 
            this.ClearMotives.Location = new System.Drawing.Point(125, 143);
            this.ClearMotives.Name = "ClearMotives";
            this.ClearMotives.Size = new System.Drawing.Size(97, 23);
            this.ClearMotives.TabIndex = 17;
            this.ClearMotives.Text = "Clear All";
            this.ClearMotives.UseVisualStyleBackColor = true;
            // 
            // MaxMotive
            // 
            this.MaxMotive.Location = new System.Drawing.Point(178, 34);
            this.MaxMotive.Name = "MaxMotive";
            this.MaxMotive.Size = new System.Drawing.Size(44, 20);
            this.MaxMotive.TabIndex = 16;
            this.MaxMotive.ValueChanged += new System.EventHandler(this.MaxMotive_ValueChanged);
            // 
            // MaxLabel
            // 
            this.MaxLabel.AutoSize = true;
            this.MaxLabel.Location = new System.Drawing.Point(175, 19);
            this.MaxLabel.Name = "MaxLabel";
            this.MaxLabel.Size = new System.Drawing.Size(30, 13);
            this.MaxLabel.TabIndex = 15;
            this.MaxLabel.Text = "Max:";
            // 
            // MinMotive
            // 
            this.MinMotive.Location = new System.Drawing.Point(125, 34);
            this.MinMotive.Name = "MinMotive";
            this.MinMotive.Size = new System.Drawing.Size(44, 20);
            this.MinMotive.TabIndex = 14;
            this.MinMotive.ValueChanged += new System.EventHandler(this.MinMotive_ValueChanged);
            // 
            // MinLabel
            // 
            this.MinLabel.AutoSize = true;
            this.MinLabel.Location = new System.Drawing.Point(122, 19);
            this.MinLabel.Name = "MinLabel";
            this.MinLabel.Size = new System.Drawing.Size(27, 13);
            this.MinLabel.TabIndex = 13;
            this.MinLabel.Text = "Min:";
            // 
            // MotivePersonality
            // 
            this.MotivePersonality.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.MotivePersonality.FormattingEnabled = true;
            this.MotivePersonality.Location = new System.Drawing.Point(125, 79);
            this.MotivePersonality.Name = "MotivePersonality";
            this.MotivePersonality.Size = new System.Drawing.Size(97, 21);
            this.MotivePersonality.TabIndex = 12;
            this.MotivePersonality.SelectedIndexChanged += new System.EventHandler(this.MotivePersonality_SelectedIndexChanged);
            // 
            // VaryLabel
            // 
            this.VaryLabel.AutoSize = true;
            this.VaryLabel.Location = new System.Drawing.Point(122, 63);
            this.VaryLabel.Name = "VaryLabel";
            this.VaryLabel.Size = new System.Drawing.Size(99, 13);
            this.VaryLabel.TabIndex = 11;
            this.VaryLabel.Text = "Vary by Personality:";
            // 
            // SearchBox
            // 
            this.SearchBox.Location = new System.Drawing.Point(333, 3);
            this.SearchBox.Name = "SearchBox";
            this.SearchBox.Size = new System.Drawing.Size(161, 20);
            this.SearchBox.TabIndex = 24;
            // 
            // SearchIcon
            // 
            this.SearchIcon.Image = global::FSO.IDE.Properties.Resources.search;
            this.SearchIcon.Location = new System.Drawing.Point(311, 4);
            this.SearchIcon.Name = "SearchIcon";
            this.SearchIcon.Size = new System.Drawing.Size(18, 20);
            this.SearchIcon.TabIndex = 15;
            this.SearchIcon.TabStop = false;
            // 
            // Selector
            // 
            this.Selector.Location = new System.Drawing.Point(422, 381);
            this.Selector.Name = "Selector";
            this.Selector.Size = new System.Drawing.Size(77, 68);
            this.Selector.TabIndex = 25;
            // 
            // TTABResourceControl
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.Selector);
            this.Controls.Add(this.SearchIcon);
            this.Controls.Add(this.SearchBox);
            this.Controls.Add(this.MotiveBox);
            this.Controls.Add(this.ViewTab);
            this.Controls.Add(this.FlagsBox);
            this.Controls.Add(this.MetaBox);
            this.Controls.Add(this.AllowBox);
            this.Controls.Add(this.CheckButton);
            this.Controls.Add(this.ActionButton);
            this.Margin = new System.Windows.Forms.Padding(0);
            this.Name = "TTABResourceControl";
            this.Size = new System.Drawing.Size(502, 455);
            this.AllowBox.ResumeLayout(false);
            this.AllowBox.PerformLayout();
            this.MetaBox.ResumeLayout(false);
            this.MetaBox.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.JoinInput)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.AutonomyInput)).EndInit();
            this.FlagsBox.ResumeLayout(false);
            this.FlagsBox.PerformLayout();
            this.ViewTab.ResumeLayout(false);
            this.tabPage1.ResumeLayout(false);
            this.tabPage2.ResumeLayout(false);
            this.MotiveBox.ResumeLayout(false);
            this.MotiveBox.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.MaxMotive)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.MinMotive)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.SearchIcon)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.ListView InteractionList;
        private System.Windows.Forms.ColumnHeader IDHeader;
        private System.Windows.Forms.ColumnHeader CheckHeader;
        private System.Windows.Forms.ColumnHeader ActionHeader;
        private System.Windows.Forms.Button MoveDownBtn;
        private System.Windows.Forms.Button RemoveBtn;
        private System.Windows.Forms.Button AddBtn;
        private System.Windows.Forms.Button MoveUpBtn;
        private System.Windows.Forms.Button ActionButton;
        private System.Windows.Forms.Button CheckButton;
        private System.Windows.Forms.GroupBox AllowBox;
        private System.Windows.Forms.CheckBox AllowDogs;
        private System.Windows.Forms.CheckBox AllowCats;
        private System.Windows.Forms.CheckBox AllowGhosts;
        private System.Windows.Forms.CheckBox AllowCSRs;
        private System.Windows.Forms.CheckBox AllowOwner;
        private System.Windows.Forms.CheckBox AllowRoomies;
        private System.Windows.Forms.CheckBox AllowFriends;
        private System.Windows.Forms.CheckBox AllowVisitors;
        private System.Windows.Forms.GroupBox MetaBox;
        private System.Windows.Forms.NumericUpDown AutonomyInput;
        private System.Windows.Forms.ComboBox AttenuationCombo;
        private System.Windows.Forms.Label AutonomyLabel;
        private System.Windows.Forms.Label AttenuationLabel;
        private System.Windows.Forms.ComboBox LanguageCombo;
        private System.Windows.Forms.TextBox InteractionPathName;
        private System.Windows.Forms.Label PathNameLabel;
        private System.Windows.Forms.NumericUpDown JoinInput;
        private System.Windows.Forms.Label JoinLabel;
        private System.Windows.Forms.GroupBox FlagsBox;
        private System.Windows.Forms.CheckBox FlagRunImmediately;
        private System.Windows.Forms.CheckBox FlagAutoFirst;
        private System.Windows.Forms.CheckBox FlagMustRun;
        private System.Windows.Forms.CheckBox FlagLeapfrog;
        private System.Windows.Forms.CheckBox FlagDebug;
        private System.Windows.Forms.CheckBox FlagDead;
        private System.Windows.Forms.CheckBox FlagCheck;
        private System.Windows.Forms.CheckBox FlagRepair;
        private System.Windows.Forms.CheckBox FlagCarrying;
        private System.Windows.Forms.Label InteractionFlagsLabel;
        private System.Windows.Forms.CheckBox FlagConsecutive;
        private System.Windows.Forms.TabControl ViewTab;
        private System.Windows.Forms.TabPage tabPage1;
        private System.Windows.Forms.TabPage tabPage2;
        private System.Windows.Forms.TreeView PieView;
        private System.Windows.Forms.ListBox MotiveList;
        private System.Windows.Forms.GroupBox MotiveBox;
        private System.Windows.Forms.Label VaryLabel;
        private System.Windows.Forms.Button ClearMotives;
        private System.Windows.Forms.NumericUpDown MaxMotive;
        private System.Windows.Forms.Label MaxLabel;
        private System.Windows.Forms.NumericUpDown MinMotive;
        private System.Windows.Forms.Label MinLabel;
        private System.Windows.Forms.ComboBox MotivePersonality;
        private System.Windows.Forms.TextBox SearchBox;
        private System.Windows.Forms.PictureBox SearchIcon;
        private System.Windows.Forms.Button PieAdd;
        private System.Windows.Forms.Button PieRemove;
        private OBJDSelectorControl Selector;
    }
}
