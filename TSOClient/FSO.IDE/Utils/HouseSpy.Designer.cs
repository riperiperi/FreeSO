namespace FSO.IDE.Utils
{
    partial class HouseSpy
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
            System.Windows.Forms.ListViewItem listViewItem1 = new System.Windows.Forms.ListViewItem(new string[] {
            "0",
            "65536",
            "63356",
            "65536",
            "65536",
            "99",
            "0,0,0,0",
            "100",
            "8192",
            "0.3",
            "99"}, -1);
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(HouseSpy));
            this.objectList = new System.Windows.Forms.ListBox();
            this.peopleLabel = new System.Windows.Forms.Label();
            this.menuStrip1 = new System.Windows.Forms.MenuStrip();
            this.fileToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.openToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.personBox = new System.Windows.Forms.GroupBox();
            this.positionLabel = new System.Windows.Forms.Label();
            this.unknownsLabel = new System.Windows.Forms.Label();
            this.animationLabel = new System.Windows.Forms.Label();
            this.accessoriesLabel = new System.Windows.Forms.Label();
            this.accessoriesList = new System.Windows.Forms.ListBox();
            this.useCountLabel = new System.Windows.Forms.Label();
            this.useCountList = new System.Windows.Forms.ListView();
            this.useIDColumn = new System.Windows.Forms.ColumnHeader();
            this.useStackColumn = new System.Windows.Forms.ColumnHeader();
            this.useFlagColumn = new System.Windows.Forms.ColumnHeader();
            this.motiveChangeLabel = new System.Windows.Forms.Label();
            this.motiveChangeList = new System.Windows.Forms.ListView();
            this.motiveIDColumn = new System.Windows.Forms.ColumnHeader();
            this.motiveDeltaColumn = new System.Windows.Forms.ColumnHeader();
            this.motiveStopColumn = new System.Windows.Forms.ColumnHeader();
            this.queueList = new System.Windows.Forms.ListView();
            this.queueNullHeader = new System.Windows.Forms.ColumnHeader();
            this.queueUIDHeader = new System.Windows.Forms.ColumnHeader();
            this.queueCallerHeader = new System.Windows.Forms.ColumnHeader();
            this.queueTargetHeader = new System.Windows.Forms.ColumnHeader();
            this.queueIconHeader = new System.Windows.Forms.ColumnHeader();
            this.queueTTAHeader = new System.Windows.Forms.ColumnHeader();
            this.queueArgsHeader = new System.Windows.Forms.ColumnHeader();
            this.queuePriorityHeader = new System.Windows.Forms.ColumnHeader();
            this.queueTreeHeader = new System.Windows.Forms.ColumnHeader();
            this.queueAttenuationHeader = new System.Windows.Forms.ColumnHeader();
            this.queueFlagsHeader = new System.Windows.Forms.ColumnHeader();
            this.floatsLabel = new System.Windows.Forms.Label();
            this.floatsList = new System.Windows.Forms.ListBox();
            this.updatedLabel = new System.Windows.Forms.Label();
            this.menuStrip1.SuspendLayout();
            this.personBox.SuspendLayout();
            this.SuspendLayout();
            // 
            // objectList
            // 
            this.objectList.FormattingEnabled = true;
            this.objectList.Location = new System.Drawing.Point(12, 49);
            this.objectList.Name = "objectList";
            this.objectList.Size = new System.Drawing.Size(137, 108);
            this.objectList.TabIndex = 0;
            this.objectList.SelectedIndexChanged += new System.EventHandler(this.objectList_SelectedIndexChanged);
            // 
            // peopleLabel
            // 
            this.peopleLabel.AutoSize = true;
            this.peopleLabel.Location = new System.Drawing.Point(12, 33);
            this.peopleLabel.Name = "peopleLabel";
            this.peopleLabel.Size = new System.Drawing.Size(40, 13);
            this.peopleLabel.TabIndex = 1;
            this.peopleLabel.Text = "People";
            // 
            // menuStrip1
            // 
            this.menuStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.fileToolStripMenuItem});
            this.menuStrip1.Location = new System.Drawing.Point(0, 0);
            this.menuStrip1.Name = "menuStrip1";
            this.menuStrip1.Size = new System.Drawing.Size(756, 24);
            this.menuStrip1.TabIndex = 2;
            this.menuStrip1.Text = "menuStrip1";
            // 
            // fileToolStripMenuItem
            // 
            this.fileToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.openToolStripMenuItem});
            this.fileToolStripMenuItem.Name = "fileToolStripMenuItem";
            this.fileToolStripMenuItem.Size = new System.Drawing.Size(37, 20);
            this.fileToolStripMenuItem.Text = "File";
            // 
            // openToolStripMenuItem
            // 
            this.openToolStripMenuItem.Name = "openToolStripMenuItem";
            this.openToolStripMenuItem.Size = new System.Drawing.Size(103, 22);
            this.openToolStripMenuItem.Text = "Open";
            this.openToolStripMenuItem.Click += new System.EventHandler(this.openToolStripMenuItem_Click);
            // 
            // personBox
            // 
            this.personBox.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.personBox.Controls.Add(this.positionLabel);
            this.personBox.Controls.Add(this.unknownsLabel);
            this.personBox.Controls.Add(this.animationLabel);
            this.personBox.Controls.Add(this.accessoriesLabel);
            this.personBox.Controls.Add(this.accessoriesList);
            this.personBox.Controls.Add(this.useCountLabel);
            this.personBox.Controls.Add(this.useCountList);
            this.personBox.Controls.Add(this.motiveChangeLabel);
            this.personBox.Controls.Add(this.motiveChangeList);
            this.personBox.Controls.Add(this.queueList);
            this.personBox.Controls.Add(this.floatsLabel);
            this.personBox.Controls.Add(this.floatsList);
            this.personBox.Location = new System.Drawing.Point(155, 33);
            this.personBox.Name = "personBox";
            this.personBox.Size = new System.Drawing.Size(589, 356);
            this.personBox.TabIndex = 3;
            this.personBox.TabStop = false;
            this.personBox.Text = "Person";
            // 
            // positionLabel
            // 
            this.positionLabel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.positionLabel.Location = new System.Drawing.Point(477, 16);
            this.positionLabel.Name = "positionLabel";
            this.positionLabel.Size = new System.Drawing.Size(106, 18);
            this.positionLabel.TabIndex = 0;
            this.positionLabel.Text = "X: 0, Y: 0";
            this.positionLabel.TextAlign = System.Drawing.ContentAlignment.TopRight;
            // 
            // unknownsLabel
            // 
            this.unknownsLabel.Location = new System.Drawing.Point(106, 69);
            this.unknownsLabel.Name = "unknownsLabel";
            this.unknownsLabel.Size = new System.Drawing.Size(373, 32);
            this.unknownsLabel.TabIndex = 4;
            this.unknownsLabel.Text = "Unknown1: 65536, Unknown2: 65536, UnknownValue: 65536\r\nRoutingFrameCount: 1";
            // 
            // animationLabel
            // 
            this.animationLabel.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.animationLabel.Location = new System.Drawing.Point(106, 16);
            this.animationLabel.Name = "animationLabel";
            this.animationLabel.Size = new System.Drawing.Size(420, 42);
            this.animationLabel.TabIndex = 11;
            this.animationLabel.Text = "Animation: a2o-idle-neutral-lhips-look-1c;1;1000;240;1000;0;1;1\r\nBase: a2o-standi" +
    "ng-loop;-10;1000;70;1000;1;1;1\r\nCarry: a2o-rarm-carry-loop;10;0;1000;1000;0;1;1";
            // 
            // accessoriesLabel
            // 
            this.accessoriesLabel.AutoSize = true;
            this.accessoriesLabel.Location = new System.Drawing.Point(477, 110);
            this.accessoriesLabel.Name = "accessoriesLabel";
            this.accessoriesLabel.Size = new System.Drawing.Size(64, 13);
            this.accessoriesLabel.TabIndex = 10;
            this.accessoriesLabel.Text = "Accessories";
            // 
            // accessoriesList
            // 
            this.accessoriesList.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.accessoriesList.FormattingEnabled = true;
            this.accessoriesList.Location = new System.Drawing.Point(477, 128);
            this.accessoriesList.Name = "accessoriesList";
            this.accessoriesList.Size = new System.Drawing.Size(106, 95);
            this.accessoriesList.TabIndex = 9;
            // 
            // useCountLabel
            // 
            this.useCountLabel.AutoSize = true;
            this.useCountLabel.Location = new System.Drawing.Point(104, 110);
            this.useCountLabel.Name = "useCountLabel";
            this.useCountLabel.Size = new System.Drawing.Size(62, 13);
            this.useCountLabel.TabIndex = 8;
            this.useCountLabel.Text = "Use Counts";
            // 
            // useCountList
            // 
            this.useCountList.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
            this.useIDColumn,
            this.useStackColumn,
            this.useFlagColumn});
            this.useCountList.Location = new System.Drawing.Point(104, 126);
            this.useCountList.Name = "useCountList";
            this.useCountList.Size = new System.Drawing.Size(199, 97);
            this.useCountList.TabIndex = 7;
            this.useCountList.UseCompatibleStateImageBehavior = false;
            this.useCountList.View = System.Windows.Forms.View.Details;
            // 
            // useIDColumn
            // 
            this.useIDColumn.Text = "Object";
            this.useIDColumn.Width = 100;
            // 
            // useStackColumn
            // 
            this.useStackColumn.Text = "Stack";
            this.useStackColumn.Width = 43;
            // 
            // useFlagColumn
            // 
            this.useFlagColumn.Text = "Flag";
            this.useFlagColumn.Width = 43;
            // 
            // motiveChangeLabel
            // 
            this.motiveChangeLabel.AutoSize = true;
            this.motiveChangeLabel.Location = new System.Drawing.Point(309, 112);
            this.motiveChangeLabel.Name = "motiveChangeLabel";
            this.motiveChangeLabel.Size = new System.Drawing.Size(84, 13);
            this.motiveChangeLabel.TabIndex = 6;
            this.motiveChangeLabel.Text = "Motive Changes";
            // 
            // motiveChangeList
            // 
            this.motiveChangeList.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
            this.motiveIDColumn,
            this.motiveDeltaColumn,
            this.motiveStopColumn});
            this.motiveChangeList.Location = new System.Drawing.Point(309, 128);
            this.motiveChangeList.Name = "motiveChangeList";
            this.motiveChangeList.Size = new System.Drawing.Size(162, 97);
            this.motiveChangeList.TabIndex = 5;
            this.motiveChangeList.UseCompatibleStateImageBehavior = false;
            this.motiveChangeList.View = System.Windows.Forms.View.Details;
            // 
            // motiveIDColumn
            // 
            this.motiveIDColumn.Text = "Motive";
            // 
            // motiveDeltaColumn
            // 
            this.motiveDeltaColumn.Text = "Delta";
            this.motiveDeltaColumn.Width = 40;
            // 
            // motiveStopColumn
            // 
            this.motiveStopColumn.Text = "Stop";
            this.motiveStopColumn.Width = 40;
            // 
            // queueList
            // 
            this.queueList.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.queueList.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
            this.queueNullHeader,
            this.queueUIDHeader,
            this.queueCallerHeader,
            this.queueTargetHeader,
            this.queueIconHeader,
            this.queueTTAHeader,
            this.queueArgsHeader,
            this.queuePriorityHeader,
            this.queueTreeHeader,
            this.queueAttenuationHeader,
            this.queueFlagsHeader});
            this.queueList.Items.AddRange(new System.Windows.Forms.ListViewItem[] {
            listViewItem1});
            this.queueList.Location = new System.Drawing.Point(6, 237);
            this.queueList.Name = "queueList";
            this.queueList.Size = new System.Drawing.Size(577, 113);
            this.queueList.TabIndex = 3;
            this.queueList.UseCompatibleStateImageBehavior = false;
            this.queueList.View = System.Windows.Forms.View.Details;
            // 
            // queueNullHeader
            // 
            this.queueNullHeader.Text = "?";
            this.queueNullHeader.Width = 20;
            // 
            // queueUIDHeader
            // 
            this.queueUIDHeader.Text = "UID";
            this.queueUIDHeader.Width = 43;
            // 
            // queueCallerHeader
            // 
            this.queueCallerHeader.Text = "Caller";
            this.queueCallerHeader.Width = 43;
            // 
            // queueTargetHeader
            // 
            this.queueTargetHeader.Text = "Target";
            this.queueTargetHeader.Width = 110;
            // 
            // queueIconHeader
            // 
            this.queueIconHeader.Text = "Icon";
            this.queueIconHeader.Width = 43;
            // 
            // queueTTAHeader
            // 
            this.queueTTAHeader.Text = "TTA#";
            this.queueTTAHeader.Width = 43;
            // 
            // queueArgsHeader
            // 
            this.queueArgsHeader.Text = "Args";
            // 
            // queuePriorityHeader
            // 
            this.queuePriorityHeader.Text = "Priority";
            this.queuePriorityHeader.Width = 43;
            // 
            // queueTreeHeader
            // 
            this.queueTreeHeader.Text = "Tree#";
            this.queueTreeHeader.Width = 43;
            // 
            // queueAttenuationHeader
            // 
            this.queueAttenuationHeader.Text = "Attenuation";
            this.queueAttenuationHeader.Width = 66;
            // 
            // queueFlagsHeader
            // 
            this.queueFlagsHeader.Text = "Flags";
            this.queueFlagsHeader.Width = 43;
            // 
            // floatsLabel
            // 
            this.floatsLabel.AutoSize = true;
            this.floatsLabel.Location = new System.Drawing.Point(65, 21);
            this.floatsLabel.Name = "floatsLabel";
            this.floatsLabel.Size = new System.Drawing.Size(35, 13);
            this.floatsLabel.TabIndex = 2;
            this.floatsLabel.Text = "Floats";
            // 
            // floatsList
            // 
            this.floatsList.FormattingEnabled = true;
            this.floatsList.Location = new System.Drawing.Point(6, 37);
            this.floatsList.Name = "floatsList";
            this.floatsList.Size = new System.Drawing.Size(92, 186);
            this.floatsList.TabIndex = 1;
            // 
            // updatedLabel
            // 
            this.updatedLabel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.updatedLabel.AutoSize = true;
            this.updatedLabel.Location = new System.Drawing.Point(12, 379);
            this.updatedLabel.Name = "updatedLabel";
            this.updatedLabel.Size = new System.Drawing.Size(119, 13);
            this.updatedLabel.TabIndex = 4;
            this.updatedLabel.Text = "Last Updated: 21:14:00";
            // 
            // HouseSpy
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(756, 401);
            this.Controls.Add(this.updatedLabel);
            this.Controls.Add(this.personBox);
            this.Controls.Add(this.peopleLabel);
            this.Controls.Add(this.objectList);
            this.Controls.Add(this.menuStrip1);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.MainMenuStrip = this.menuStrip1;
            this.Name = "HouseSpy";
            this.Text = "House Spy";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.HouseSpy_FormClosing);
            this.menuStrip1.ResumeLayout(false);
            this.menuStrip1.PerformLayout();
            this.personBox.ResumeLayout(false);
            this.personBox.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.ListBox objectList;
        private System.Windows.Forms.Label peopleLabel;
        private System.Windows.Forms.MenuStrip menuStrip1;
        private System.Windows.Forms.ToolStripMenuItem fileToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem openToolStripMenuItem;
        private System.Windows.Forms.GroupBox personBox;
        private System.Windows.Forms.Label positionLabel;
        private System.Windows.Forms.Label floatsLabel;
        private System.Windows.Forms.ListBox floatsList;
        private System.Windows.Forms.ListView queueList;
        private System.Windows.Forms.ColumnHeader queueNullHeader;
        private System.Windows.Forms.ColumnHeader queueUIDHeader;
        private System.Windows.Forms.ColumnHeader queueCallerHeader;
        private System.Windows.Forms.ColumnHeader queueTargetHeader;
        private System.Windows.Forms.ColumnHeader queueIconHeader;
        private System.Windows.Forms.ColumnHeader queueTTAHeader;
        private System.Windows.Forms.ColumnHeader queueArgsHeader;
        private System.Windows.Forms.ColumnHeader queuePriorityHeader;
        private System.Windows.Forms.ColumnHeader queueTreeHeader;
        private System.Windows.Forms.ColumnHeader queueAttenuationHeader;
        private System.Windows.Forms.ColumnHeader queueFlagsHeader;
        private System.Windows.Forms.Label unknownsLabel;
        private System.Windows.Forms.ListView useCountList;
        private System.Windows.Forms.Label motiveChangeLabel;
        private System.Windows.Forms.ListView motiveChangeList;
        private System.Windows.Forms.ColumnHeader motiveIDColumn;
        private System.Windows.Forms.ColumnHeader motiveDeltaColumn;
        private System.Windows.Forms.ColumnHeader motiveStopColumn;
        private System.Windows.Forms.ColumnHeader useIDColumn;
        private System.Windows.Forms.ColumnHeader useStackColumn;
        private System.Windows.Forms.ColumnHeader useFlagColumn;
        private System.Windows.Forms.Label animationLabel;
        private System.Windows.Forms.Label accessoriesLabel;
        private System.Windows.Forms.ListBox accessoriesList;
        private System.Windows.Forms.Label useCountLabel;
        private System.Windows.Forms.Label updatedLabel;
    }
}