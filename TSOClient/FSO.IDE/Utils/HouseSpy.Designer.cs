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
            ListViewItem listViewItem1 = new ListViewItem(new string[] { "0", "65536", "63356", "65536", "65536", "99", "0,0,0,0", "100", "8192", "0.3", "99" }, -1);
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(HouseSpy));
            objectList = new ListBox();
            peopleLabel = new Label();
            menuStrip1 = new MenuStrip();
            fileToolStripMenuItem = new ToolStripMenuItem();
            openToolStripMenuItem = new ToolStripMenuItem();
            personBox = new GroupBox();
            positionLabel = new Label();
            unknownsLabel = new Label();
            animationLabel = new Label();
            accessoriesLabel = new Label();
            accessoriesList = new ListBox();
            useCountLabel = new Label();
            useCountList = new ListView();
            useIDColumn = new ColumnHeader();
            useStackColumn = new ColumnHeader();
            useFlagColumn = new ColumnHeader();
            motiveChangeLabel = new Label();
            motiveChangeList = new ListView();
            motiveIDColumn = new ColumnHeader();
            motiveDeltaColumn = new ColumnHeader();
            motiveStopColumn = new ColumnHeader();
            queueList = new ListView();
            queueNullHeader = new ColumnHeader();
            queueUIDHeader = new ColumnHeader();
            queueCallerHeader = new ColumnHeader();
            queueTargetHeader = new ColumnHeader();
            queueIconHeader = new ColumnHeader();
            queueTTAHeader = new ColumnHeader();
            queueArgsHeader = new ColumnHeader();
            queuePriorityHeader = new ColumnHeader();
            queueTreeHeader = new ColumnHeader();
            queueAttenuationHeader = new ColumnHeader();
            queueFlagsHeader = new ColumnHeader();
            floatsLabel = new Label();
            floatsList = new ListBox();
            updatedLabel = new Label();
            menuStrip1.SuspendLayout();
            personBox.SuspendLayout();
            SuspendLayout();
            // 
            // objectList
            // 
            objectList.FormattingEnabled = true;
            objectList.Location = new Point(12, 49);
            objectList.Name = "objectList";
            objectList.Size = new Size(137, 108);
            objectList.TabIndex = 0;
            objectList.SelectedIndexChanged += objectList_SelectedIndexChanged;
            // 
            // peopleLabel
            // 
            peopleLabel.AutoSize = true;
            peopleLabel.Location = new Point(12, 33);
            peopleLabel.Name = "peopleLabel";
            peopleLabel.Size = new Size(42, 13);
            peopleLabel.TabIndex = 1;
            peopleLabel.Text = "People";
            // 
            // menuStrip1
            // 
            menuStrip1.Items.AddRange(new ToolStripItem[] { fileToolStripMenuItem });
            menuStrip1.Location = new Point(0, 0);
            menuStrip1.Name = "menuStrip1";
            menuStrip1.Size = new Size(756, 24);
            menuStrip1.TabIndex = 2;
            menuStrip1.Text = "menuStrip1";
            // 
            // fileToolStripMenuItem
            // 
            fileToolStripMenuItem.DropDownItems.AddRange(new ToolStripItem[] { openToolStripMenuItem });
            fileToolStripMenuItem.Name = "fileToolStripMenuItem";
            fileToolStripMenuItem.Size = new Size(37, 20);
            fileToolStripMenuItem.Text = "File";
            // 
            // openToolStripMenuItem
            // 
            openToolStripMenuItem.Name = "openToolStripMenuItem";
            openToolStripMenuItem.Size = new Size(103, 22);
            openToolStripMenuItem.Text = "Open";
            openToolStripMenuItem.Click += openToolStripMenuItem_Click;
            // 
            // personBox
            // 
            personBox.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            personBox.Controls.Add(positionLabel);
            personBox.Controls.Add(unknownsLabel);
            personBox.Controls.Add(animationLabel);
            personBox.Controls.Add(accessoriesLabel);
            personBox.Controls.Add(accessoriesList);
            personBox.Controls.Add(useCountLabel);
            personBox.Controls.Add(useCountList);
            personBox.Controls.Add(motiveChangeLabel);
            personBox.Controls.Add(motiveChangeList);
            personBox.Controls.Add(queueList);
            personBox.Controls.Add(floatsLabel);
            personBox.Controls.Add(floatsList);
            personBox.Location = new Point(155, 33);
            personBox.Name = "personBox";
            personBox.Size = new Size(589, 356);
            personBox.TabIndex = 3;
            personBox.TabStop = false;
            personBox.Text = "Person";
            // 
            // positionLabel
            // 
            positionLabel.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            positionLabel.Location = new Point(477, 16);
            positionLabel.Name = "positionLabel";
            positionLabel.Size = new Size(106, 18);
            positionLabel.TabIndex = 0;
            positionLabel.Text = "X: 0, Y: 0";
            positionLabel.TextAlign = ContentAlignment.TopRight;
            // 
            // unknownsLabel
            // 
            unknownsLabel.Location = new Point(106, 69);
            unknownsLabel.Name = "unknownsLabel";
            unknownsLabel.Size = new Size(373, 32);
            unknownsLabel.TabIndex = 4;
            unknownsLabel.Text = "Unknown1: 65536, Unknown2: 65536, UnknownValue: 65536\r\nRoutingFrameCount: 1";
            // 
            // animationLabel
            // 
            animationLabel.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            animationLabel.Location = new Point(106, 16);
            animationLabel.Name = "animationLabel";
            animationLabel.Size = new Size(420, 42);
            animationLabel.TabIndex = 11;
            animationLabel.Text = "Animation: a2o-idle-neutral-lhips-look-1c;1;1000;240;1000;0;1;1\r\nBase: a2o-standing-loop;-10;1000;70;1000;1;1;1\r\nCarry: a2o-rarm-carry-loop;10;0;1000;1000;0;1;1";
            // 
            // accessoriesLabel
            // 
            accessoriesLabel.AutoSize = true;
            accessoriesLabel.Location = new Point(477, 110);
            accessoriesLabel.Name = "accessoriesLabel";
            accessoriesLabel.Size = new Size(65, 13);
            accessoriesLabel.TabIndex = 10;
            accessoriesLabel.Text = "Accessories";
            // 
            // accessoriesList
            // 
            accessoriesList.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            accessoriesList.FormattingEnabled = true;
            accessoriesList.Location = new Point(477, 128);
            accessoriesList.Name = "accessoriesList";
            accessoriesList.Size = new Size(106, 95);
            accessoriesList.TabIndex = 9;
            // 
            // useCountLabel
            // 
            useCountLabel.AutoSize = true;
            useCountLabel.Location = new Point(104, 110);
            useCountLabel.Name = "useCountLabel";
            useCountLabel.Size = new Size(66, 13);
            useCountLabel.TabIndex = 8;
            useCountLabel.Text = "Use Counts";
            // 
            // useCountList
            // 
            useCountList.Columns.AddRange(new ColumnHeader[] { useIDColumn, useStackColumn, useFlagColumn });
            useCountList.Location = new Point(104, 126);
            useCountList.Name = "useCountList";
            useCountList.Size = new Size(199, 97);
            useCountList.TabIndex = 7;
            useCountList.UseCompatibleStateImageBehavior = false;
            useCountList.View = View.Details;
            // 
            // useIDColumn
            // 
            useIDColumn.Text = "Object";
            useIDColumn.Width = 100;
            // 
            // useStackColumn
            // 
            useStackColumn.Text = "Stack";
            useStackColumn.Width = 43;
            // 
            // useFlagColumn
            // 
            useFlagColumn.Text = "Flag";
            useFlagColumn.Width = 43;
            // 
            // motiveChangeLabel
            // 
            motiveChangeLabel.AutoSize = true;
            motiveChangeLabel.Location = new Point(309, 112);
            motiveChangeLabel.Name = "motiveChangeLabel";
            motiveChangeLabel.Size = new Size(90, 13);
            motiveChangeLabel.TabIndex = 6;
            motiveChangeLabel.Text = "Motive Changes";
            // 
            // motiveChangeList
            // 
            motiveChangeList.Columns.AddRange(new ColumnHeader[] { motiveIDColumn, motiveDeltaColumn, motiveStopColumn });
            motiveChangeList.Location = new Point(309, 128);
            motiveChangeList.Name = "motiveChangeList";
            motiveChangeList.Size = new Size(162, 97);
            motiveChangeList.TabIndex = 5;
            motiveChangeList.UseCompatibleStateImageBehavior = false;
            motiveChangeList.View = View.Details;
            // 
            // motiveIDColumn
            // 
            motiveIDColumn.Text = "Motive";
            // 
            // motiveDeltaColumn
            // 
            motiveDeltaColumn.Text = "Delta";
            motiveDeltaColumn.Width = 40;
            // 
            // motiveStopColumn
            // 
            motiveStopColumn.Text = "Stop";
            motiveStopColumn.Width = 40;
            // 
            // queueList
            // 
            queueList.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            queueList.Columns.AddRange(new ColumnHeader[] { queueNullHeader, queueUIDHeader, queueCallerHeader, queueTargetHeader, queueIconHeader, queueTTAHeader, queueArgsHeader, queuePriorityHeader, queueTreeHeader, queueAttenuationHeader, queueFlagsHeader });
            queueList.Items.AddRange(new ListViewItem[] { listViewItem1 });
            queueList.Location = new Point(6, 237);
            queueList.Name = "queueList";
            queueList.Size = new Size(577, 113);
            queueList.TabIndex = 3;
            queueList.UseCompatibleStateImageBehavior = false;
            queueList.View = View.Details;
            // 
            // queueNullHeader
            // 
            queueNullHeader.Text = "?";
            queueNullHeader.Width = 20;
            // 
            // queueUIDHeader
            // 
            queueUIDHeader.Text = "UID";
            queueUIDHeader.Width = 43;
            // 
            // queueCallerHeader
            // 
            queueCallerHeader.Text = "Caller";
            queueCallerHeader.Width = 43;
            // 
            // queueTargetHeader
            // 
            queueTargetHeader.Text = "Target";
            queueTargetHeader.Width = 110;
            // 
            // queueIconHeader
            // 
            queueIconHeader.Text = "Icon";
            queueIconHeader.Width = 43;
            // 
            // queueTTAHeader
            // 
            queueTTAHeader.Text = "TTA#";
            queueTTAHeader.Width = 43;
            // 
            // queueArgsHeader
            // 
            queueArgsHeader.Text = "Args";
            // 
            // queuePriorityHeader
            // 
            queuePriorityHeader.Text = "Priority";
            queuePriorityHeader.Width = 43;
            // 
            // queueTreeHeader
            // 
            queueTreeHeader.Text = "Tree#";
            queueTreeHeader.Width = 43;
            // 
            // queueAttenuationHeader
            // 
            queueAttenuationHeader.Text = "Attenuation";
            queueAttenuationHeader.Width = 66;
            // 
            // queueFlagsHeader
            // 
            queueFlagsHeader.Text = "Flags";
            queueFlagsHeader.Width = 43;
            // 
            // floatsLabel
            // 
            floatsLabel.AutoSize = true;
            floatsLabel.Location = new Point(65, 21);
            floatsLabel.Name = "floatsLabel";
            floatsLabel.Size = new Size(38, 13);
            floatsLabel.TabIndex = 2;
            floatsLabel.Text = "Floats";
            // 
            // floatsList
            // 
            floatsList.FormattingEnabled = true;
            floatsList.Location = new Point(6, 37);
            floatsList.Name = "floatsList";
            floatsList.Size = new Size(92, 186);
            floatsList.TabIndex = 1;
            // 
            // updatedLabel
            // 
            updatedLabel.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
            updatedLabel.AutoSize = true;
            updatedLabel.Location = new Point(12, 379);
            updatedLabel.Name = "updatedLabel";
            updatedLabel.Size = new Size(123, 13);
            updatedLabel.TabIndex = 4;
            updatedLabel.Text = "Last Updated: 21:14:00";
            // 
            // HouseSpy
            // 
            AutoScaleDimensions = new SizeF(96F, 96F);
            AutoScaleMode = AutoScaleMode.Dpi;
            ClientSize = new Size(756, 401);
            Controls.Add(updatedLabel);
            Controls.Add(personBox);
            Controls.Add(peopleLabel);
            Controls.Add(objectList);
            Controls.Add(menuStrip1);
            Icon = (Icon)resources.GetObject("$this.Icon");
            MainMenuStrip = menuStrip1;
            Name = "HouseSpy";
            Text = "House Spy";
            FormClosing += HouseSpy_FormClosing;
            menuStrip1.ResumeLayout(false);
            menuStrip1.PerformLayout();
            personBox.ResumeLayout(false);
            personBox.PerformLayout();
            ResumeLayout(false);
            PerformLayout();

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