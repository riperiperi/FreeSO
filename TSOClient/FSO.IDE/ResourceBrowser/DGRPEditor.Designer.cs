namespace FSO.IDE.ResourceBrowser
{
    partial class DGRPEditor
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
            SelectSpriteBox = new GroupBox();
            SPRLabel = new Label();
            label11 = new Label();
            ChangeSPR = new Button();
            RotationCombo = new ComboBox();
            label4 = new Label();
            FlipCheck = new CheckBox();
            label10 = new Label();
            zPhys = new TextBox();
            label8 = new Label();
            yPhys = new TextBox();
            label9 = new Label();
            xPhys = new TextBox();
            label7 = new Label();
            yPx = new NumericUpDown();
            label6 = new Label();
            xPx = new NumericUpDown();
            label5 = new Label();
            label3 = new Label();
            DGRPBox = new GroupBox();
            CopyButton = new Button();
            DGRPDown = new Button();
            RemoveDGRP = new Button();
            AddDGRP = new Button();
            DGRPUp = new Button();
            RenameDGRP = new Button();
            FirstDGRP = new Button();
            LastDGRP = new Button();
            DGRPList = new ListView();
            IndexColumn = new ColumnHeader();
            NameColumn = new ColumnHeader();
            groupBox3 = new GroupBox();
            MoveUpSPR = new Button();
            AddSPR = new Button();
            RemoveSPR = new Button();
            SpriteList = new ListBox();
            MoveDownSPR = new Button();
            RotationTrack = new TrackBar();
            ZoomTrack = new TrackBar();
            label1 = new Label();
            label2 = new Label();
            groupBox1 = new GroupBox();
            LastDynLabel = new Label();
            label15 = new Label();
            LastDynButton = new Button();
            FirstDynLabel = new Label();
            label13 = new Label();
            FirstDynButton = new Button();
            AutoZoom = new CheckBox();
            DGRPEdit = new FSO.IDE.Common.InteractiveDGRPControl();
            AutoRot = new CheckBox();
            SelectSpriteBox.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)yPx).BeginInit();
            ((System.ComponentModel.ISupportInitialize)xPx).BeginInit();
            DGRPBox.SuspendLayout();
            groupBox3.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)RotationTrack).BeginInit();
            ((System.ComponentModel.ISupportInitialize)ZoomTrack).BeginInit();
            groupBox1.SuspendLayout();
            SuspendLayout();
            // 
            // SelectSpriteBox
            // 
            SelectSpriteBox.Controls.Add(SPRLabel);
            SelectSpriteBox.Controls.Add(label11);
            SelectSpriteBox.Controls.Add(ChangeSPR);
            SelectSpriteBox.Controls.Add(RotationCombo);
            SelectSpriteBox.Controls.Add(label4);
            SelectSpriteBox.Controls.Add(FlipCheck);
            SelectSpriteBox.Controls.Add(label10);
            SelectSpriteBox.Controls.Add(zPhys);
            SelectSpriteBox.Controls.Add(label8);
            SelectSpriteBox.Controls.Add(yPhys);
            SelectSpriteBox.Controls.Add(label9);
            SelectSpriteBox.Controls.Add(xPhys);
            SelectSpriteBox.Controls.Add(label7);
            SelectSpriteBox.Controls.Add(yPx);
            SelectSpriteBox.Controls.Add(label6);
            SelectSpriteBox.Controls.Add(xPx);
            SelectSpriteBox.Controls.Add(label5);
            SelectSpriteBox.Controls.Add(label3);
            SelectSpriteBox.Location = new Point(559, 77);
            SelectSpriteBox.Name = "SelectSpriteBox";
            SelectSpriteBox.Size = new Size(200, 200);
            SelectSpriteBox.TabIndex = 8;
            SelectSpriteBox.TabStop = false;
            SelectSpriteBox.Text = "Selected Sprite";
            // 
            // SPRLabel
            // 
            SPRLabel.AutoEllipsis = true;
            SPRLabel.Location = new Point(13, 29);
            SPRLabel.Name = "SPRLabel";
            SPRLabel.Size = new Size(126, 28);
            SPRLabel.TabIndex = 17;
            SPRLabel.Text = "None Selected";
            SPRLabel.TextAlign = ContentAlignment.MiddleRight;
            // 
            // label11
            // 
            label11.AutoSize = true;
            label11.Location = new Point(6, 16);
            label11.Name = "label11";
            label11.Size = new Size(69, 13);
            label11.TabIndex = 16;
            label11.Text = "SPR2 Chunk";
            // 
            // ChangeSPR
            // 
            ChangeSPR.Location = new Point(142, 29);
            ChangeSPR.Name = "ChangeSPR";
            ChangeSPR.Size = new Size(52, 28);
            ChangeSPR.TabIndex = 15;
            ChangeSPR.Text = "Change";
            ChangeSPR.UseVisualStyleBackColor = true;
            ChangeSPR.Click += ChangeSPR_Click;
            // 
            // RotationCombo
            // 
            RotationCombo.DropDownStyle = ComboBoxStyle.DropDownList;
            RotationCombo.FormattingEnabled = true;
            RotationCombo.Location = new Point(61, 148);
            RotationCombo.Name = "RotationCombo";
            RotationCombo.Size = new Size(132, 21);
            RotationCombo.TabIndex = 14;
            RotationCombo.SelectedIndexChanged += RotationCombo_SelectedIndexChanged;
            // 
            // label4
            // 
            label4.AutoSize = true;
            label4.Location = new Point(5, 151);
            label4.Name = "label4";
            label4.Size = new Size(55, 13);
            label4.TabIndex = 13;
            label4.Text = "Rotation:";
            // 
            // FlipCheck
            // 
            FlipCheck.AutoSize = true;
            FlipCheck.Location = new Point(8, 175);
            FlipCheck.Name = "FlipCheck";
            FlipCheck.Size = new Size(110, 17);
            FlipCheck.TabIndex = 12;
            FlipCheck.Text = "Flip Horizontally";
            FlipCheck.UseVisualStyleBackColor = true;
            FlipCheck.CheckedChanged += FlipCheck_CheckedChanged;
            // 
            // label10
            // 
            label10.AutoSize = true;
            label10.Location = new Point(138, 122);
            label10.Name = "label10";
            label10.Size = new Size(15, 13);
            label10.TabIndex = 11;
            label10.Text = "z:";
            // 
            // zPhys
            // 
            zPhys.Location = new Point(153, 119);
            zPhys.Name = "zPhys";
            zPhys.Size = new Size(40, 22);
            zPhys.TabIndex = 10;
            zPhys.TextChanged += zPhys_TextChanged;
            // 
            // label8
            // 
            label8.AutoSize = true;
            label8.Location = new Point(76, 122);
            label8.Name = "label8";
            label8.Size = new Size(15, 13);
            label8.TabIndex = 9;
            label8.Text = "y:";
            // 
            // yPhys
            // 
            yPhys.Location = new Point(91, 119);
            yPhys.Name = "yPhys";
            yPhys.Size = new Size(40, 22);
            yPhys.TabIndex = 8;
            yPhys.TextChanged += yPhys_TextChanged;
            // 
            // label9
            // 
            label9.AutoSize = true;
            label9.Location = new Point(13, 122);
            label9.Name = "label9";
            label9.Size = new Size(15, 13);
            label9.TabIndex = 7;
            label9.Text = "x:";
            // 
            // xPhys
            // 
            xPhys.Location = new Point(28, 119);
            xPhys.Name = "xPhys";
            xPhys.Size = new Size(40, 22);
            xPhys.TabIndex = 6;
            xPhys.TextChanged += xPhys_TextChanged;
            // 
            // label7
            // 
            label7.AutoSize = true;
            label7.Location = new Point(83, 78);
            label7.Name = "label7";
            label7.Size = new Size(15, 13);
            label7.TabIndex = 5;
            label7.Text = "y:";
            // 
            // yPx
            // 
            yPx.Location = new Point(98, 76);
            yPx.Maximum = new decimal(new int[] { 32767, 0, 0, 0 });
            yPx.Minimum = new decimal(new int[] { 32768, 0, 0, int.MinValue });
            yPx.Name = "yPx";
            yPx.Size = new Size(50, 22);
            yPx.TabIndex = 4;
            yPx.ValueChanged += yPx_ValueChanged;
            // 
            // label6
            // 
            label6.AutoSize = true;
            label6.Location = new Point(13, 78);
            label6.Name = "label6";
            label6.Size = new Size(15, 13);
            label6.TabIndex = 3;
            label6.Text = "x:";
            // 
            // xPx
            // 
            xPx.Location = new Point(28, 76);
            xPx.Maximum = new decimal(new int[] { 32767, 0, 0, 0 });
            xPx.Minimum = new decimal(new int[] { 32768, 0, 0, int.MinValue });
            xPx.Name = "xPx";
            xPx.Size = new Size(50, 22);
            xPx.TabIndex = 2;
            xPx.ValueChanged += xPx_ValueChanged;
            // 
            // label5
            // 
            label5.AutoSize = true;
            label5.Location = new Point(6, 103);
            label5.Name = "label5";
            label5.Size = new Size(82, 13);
            label5.TabIndex = 1;
            label5.Text = "Physical Offset";
            // 
            // label3
            // 
            label3.AutoSize = true;
            label3.Location = new Point(5, 60);
            label3.Name = "label3";
            label3.Size = new Size(65, 13);
            label3.TabIndex = 0;
            label3.Text = "Pixel Offset";
            // 
            // DGRPBox
            // 
            DGRPBox.Controls.Add(CopyButton);
            DGRPBox.Controls.Add(DGRPDown);
            DGRPBox.Controls.Add(RemoveDGRP);
            DGRPBox.Controls.Add(AddDGRP);
            DGRPBox.Controls.Add(DGRPUp);
            DGRPBox.Controls.Add(RenameDGRP);
            DGRPBox.Controls.Add(FirstDGRP);
            DGRPBox.Controls.Add(LastDGRP);
            DGRPBox.Controls.Add(DGRPList);
            DGRPBox.Location = new Point(3, 119);
            DGRPBox.Name = "DGRPBox";
            DGRPBox.Size = new Size(200, 334);
            DGRPBox.TabIndex = 6;
            DGRPBox.TabStop = false;
            DGRPBox.Text = "Drawgroups";
            // 
            // CopyButton
            // 
            CopyButton.Location = new Point(79, 302);
            CopyButton.Name = "CopyButton";
            CopyButton.Size = new Size(41, 23);
            CopyButton.TabIndex = 23;
            CopyButton.Text = "Copy";
            CopyButton.UseVisualStyleBackColor = true;
            CopyButton.Click += CopyButton_Click;
            // 
            // DGRPDown
            // 
            DGRPDown.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
            DGRPDown.Font = new Font("Microsoft Sans Serif", 11.25F, FontStyle.Regular, GraphicsUnit.Point, 0);
            DGRPDown.Location = new Point(167, 271);
            DGRPDown.Margin = new Padding(0);
            DGRPDown.Name = "DGRPDown";
            DGRPDown.Size = new Size(26, 26);
            DGRPDown.TabIndex = 19;
            DGRPDown.Text = "↓";
            DGRPDown.UseVisualStyleBackColor = true;
            DGRPDown.Click += DGRPDown_Click;
            // 
            // RemoveDGRP
            // 
            RemoveDGRP.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
            RemoveDGRP.Font = new Font("Microsoft Sans Serif", 11.25F, FontStyle.Regular, GraphicsUnit.Point, 0);
            RemoveDGRP.Location = new Point(138, 271);
            RemoveDGRP.Margin = new Padding(0);
            RemoveDGRP.Name = "RemoveDGRP";
            RemoveDGRP.Size = new Size(26, 26);
            RemoveDGRP.TabIndex = 20;
            RemoveDGRP.Text = "-";
            RemoveDGRP.UseVisualStyleBackColor = true;
            RemoveDGRP.Click += RemoveDGRP_Click;
            // 
            // AddDGRP
            // 
            AddDGRP.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
            AddDGRP.Font = new Font("Microsoft Sans Serif", 11.25F, FontStyle.Regular, GraphicsUnit.Point, 0);
            AddDGRP.Location = new Point(36, 271);
            AddDGRP.Margin = new Padding(0);
            AddDGRP.Name = "AddDGRP";
            AddDGRP.Size = new Size(26, 26);
            AddDGRP.TabIndex = 21;
            AddDGRP.Text = "+";
            AddDGRP.UseVisualStyleBackColor = true;
            AddDGRP.Click += AddDGRP_Click;
            // 
            // DGRPUp
            // 
            DGRPUp.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
            DGRPUp.Font = new Font("Microsoft Sans Serif", 11.25F, FontStyle.Regular, GraphicsUnit.Point, 0);
            DGRPUp.Location = new Point(7, 271);
            DGRPUp.Margin = new Padding(0);
            DGRPUp.Name = "DGRPUp";
            DGRPUp.Size = new Size(26, 26);
            DGRPUp.TabIndex = 22;
            DGRPUp.Text = "↑";
            DGRPUp.UseVisualStyleBackColor = true;
            DGRPUp.Click += DGRPUp_Click;
            // 
            // RenameDGRP
            // 
            RenameDGRP.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
            RenameDGRP.Location = new Point(65, 271);
            RenameDGRP.Name = "RenameDGRP";
            RenameDGRP.Size = new Size(70, 26);
            RenameDGRP.TabIndex = 5;
            RenameDGRP.Text = "Rename";
            RenameDGRP.UseVisualStyleBackColor = true;
            RenameDGRP.Click += RenameDGRP_Click;
            // 
            // FirstDGRP
            // 
            FirstDGRP.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
            FirstDGRP.Location = new Point(6, 302);
            FirstDGRP.Name = "FirstDGRP";
            FirstDGRP.Size = new Size(70, 23);
            FirstDGRP.TabIndex = 3;
            FirstDGRP.Text = "Set as First";
            FirstDGRP.UseVisualStyleBackColor = true;
            FirstDGRP.Click += FirstDGRP_Click;
            // 
            // LastDGRP
            // 
            LastDGRP.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
            LastDGRP.Location = new Point(123, 302);
            LastDGRP.Name = "LastDGRP";
            LastDGRP.Size = new Size(70, 23);
            LastDGRP.TabIndex = 2;
            LastDGRP.Text = "Set as Last";
            LastDGRP.UseVisualStyleBackColor = true;
            LastDGRP.Click += LastDGRP_Click;
            // 
            // DGRPList
            // 
            DGRPList.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            DGRPList.Columns.AddRange(new ColumnHeader[] { IndexColumn, NameColumn });
            DGRPList.FullRowSelect = true;
            DGRPList.Location = new Point(6, 19);
            DGRPList.MultiSelect = false;
            DGRPList.Name = "DGRPList";
            DGRPList.Size = new Size(188, 248);
            DGRPList.TabIndex = 1;
            DGRPList.UseCompatibleStateImageBehavior = false;
            DGRPList.View = View.Details;
            DGRPList.SelectedIndexChanged += DGRPList_SelectedIndexChanged;
            // 
            // IndexColumn
            // 
            IndexColumn.Text = "#";
            IndexColumn.Width = 35;
            // 
            // NameColumn
            // 
            NameColumn.Text = "Name";
            NameColumn.Width = 128;
            // 
            // groupBox3
            // 
            groupBox3.Controls.Add(MoveUpSPR);
            groupBox3.Controls.Add(AddSPR);
            groupBox3.Controls.Add(RemoveSPR);
            groupBox3.Controls.Add(SpriteList);
            groupBox3.Controls.Add(MoveDownSPR);
            groupBox3.Location = new Point(559, 283);
            groupBox3.Name = "groupBox3";
            groupBox3.Size = new Size(200, 170);
            groupBox3.TabIndex = 5;
            groupBox3.TabStop = false;
            groupBox3.Text = "Sprites";
            // 
            // MoveUpSPR
            // 
            MoveUpSPR.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            MoveUpSPR.Font = new Font("Microsoft Sans Serif", 11.25F, FontStyle.Regular, GraphicsUnit.Point, 0);
            MoveUpSPR.Location = new Point(169, 19);
            MoveUpSPR.Margin = new Padding(0);
            MoveUpSPR.Name = "MoveUpSPR";
            MoveUpSPR.Size = new Size(26, 26);
            MoveUpSPR.TabIndex = 18;
            MoveUpSPR.Text = "↑";
            MoveUpSPR.UseVisualStyleBackColor = true;
            MoveUpSPR.Click += MoveUpSPR_Click;
            // 
            // AddSPR
            // 
            AddSPR.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            AddSPR.Font = new Font("Microsoft Sans Serif", 11.25F, FontStyle.Regular, GraphicsUnit.Point, 0);
            AddSPR.Location = new Point(169, 63);
            AddSPR.Margin = new Padding(0);
            AddSPR.Name = "AddSPR";
            AddSPR.Size = new Size(26, 26);
            AddSPR.TabIndex = 17;
            AddSPR.Text = "+";
            AddSPR.UseVisualStyleBackColor = true;
            AddSPR.Click += AddSPR_Click;
            // 
            // RemoveSPR
            // 
            RemoveSPR.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            RemoveSPR.Font = new Font("Microsoft Sans Serif", 11.25F, FontStyle.Regular, GraphicsUnit.Point, 0);
            RemoveSPR.Location = new Point(169, 93);
            RemoveSPR.Margin = new Padding(0);
            RemoveSPR.Name = "RemoveSPR";
            RemoveSPR.Size = new Size(26, 26);
            RemoveSPR.TabIndex = 16;
            RemoveSPR.Text = "-";
            RemoveSPR.UseVisualStyleBackColor = true;
            RemoveSPR.Click += RemoveSPR_Click;
            // 
            // SpriteList
            // 
            SpriteList.FormattingEnabled = true;
            SpriteList.IntegralHeight = false;
            SpriteList.Location = new Point(6, 19);
            SpriteList.Name = "SpriteList";
            SpriteList.Size = new Size(159, 145);
            SpriteList.TabIndex = 0;
            SpriteList.SelectedIndexChanged += SpriteList_SelectedIndexChanged;
            // 
            // MoveDownSPR
            // 
            MoveDownSPR.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            MoveDownSPR.Font = new Font("Microsoft Sans Serif", 11.25F, FontStyle.Regular, GraphicsUnit.Point, 0);
            MoveDownSPR.Location = new Point(169, 138);
            MoveDownSPR.Margin = new Padding(0);
            MoveDownSPR.Name = "MoveDownSPR";
            MoveDownSPR.Size = new Size(26, 26);
            MoveDownSPR.TabIndex = 15;
            MoveDownSPR.Text = "↓";
            MoveDownSPR.UseVisualStyleBackColor = true;
            MoveDownSPR.Click += MoveDownSPR_Click;
            // 
            // RotationTrack
            // 
            RotationTrack.BackColor = Color.FromArgb(249, 249, 249);
            RotationTrack.LargeChange = 4;
            RotationTrack.Location = new Point(559, 25);
            RotationTrack.Maximum = 3;
            RotationTrack.Name = "RotationTrack";
            RotationTrack.Size = new Size(104, 45);
            RotationTrack.TabIndex = 9;
            RotationTrack.Scroll += trackBar1_Scroll;
            // 
            // ZoomTrack
            // 
            ZoomTrack.BackColor = Color.FromArgb(249, 249, 249);
            ZoomTrack.Location = new Point(669, 25);
            ZoomTrack.Maximum = 2;
            ZoomTrack.Name = "ZoomTrack";
            ZoomTrack.Size = new Size(90, 45);
            ZoomTrack.TabIndex = 10;
            ZoomTrack.Scroll += ZoomTrack_Scroll;
            // 
            // label1
            // 
            label1.Location = new Point(559, 9);
            label1.Name = "label1";
            label1.Size = new Size(104, 17);
            label1.TabIndex = 11;
            label1.Text = "Rotation";
            label1.TextAlign = ContentAlignment.TopCenter;
            // 
            // label2
            // 
            label2.Location = new Point(669, 9);
            label2.Name = "label2";
            label2.Size = new Size(90, 17);
            label2.TabIndex = 12;
            label2.Text = "Zoom";
            label2.TextAlign = ContentAlignment.TopCenter;
            // 
            // groupBox1
            // 
            groupBox1.Controls.Add(LastDynLabel);
            groupBox1.Controls.Add(label15);
            groupBox1.Controls.Add(LastDynButton);
            groupBox1.Controls.Add(FirstDynLabel);
            groupBox1.Controls.Add(label13);
            groupBox1.Controls.Add(FirstDynButton);
            groupBox1.Location = new Point(3, 3);
            groupBox1.Name = "groupBox1";
            groupBox1.Size = new Size(200, 110);
            groupBox1.TabIndex = 13;
            groupBox1.TabStop = false;
            groupBox1.Text = "Dynamic Sprites";
            // 
            // LastDynLabel
            // 
            LastDynLabel.AutoEllipsis = true;
            LastDynLabel.Location = new Point(12, 74);
            LastDynLabel.Name = "LastDynLabel";
            LastDynLabel.Size = new Size(126, 28);
            LastDynLabel.TabIndex = 23;
            LastDynLabel.Text = "None Selected";
            LastDynLabel.TextAlign = ContentAlignment.MiddleRight;
            // 
            // label15
            // 
            label15.AutoSize = true;
            label15.Location = new Point(5, 61);
            label15.Name = "label15";
            label15.Size = new Size(106, 13);
            label15.TabIndex = 22;
            label15.Text = "Last Dynamic Sprite";
            // 
            // LastDynButton
            // 
            LastDynButton.Location = new Point(141, 74);
            LastDynButton.Name = "LastDynButton";
            LastDynButton.Size = new Size(52, 28);
            LastDynButton.TabIndex = 21;
            LastDynButton.Text = "Change";
            LastDynButton.UseVisualStyleBackColor = true;
            LastDynButton.Click += LastDynButton_Click;
            // 
            // FirstDynLabel
            // 
            FirstDynLabel.AutoEllipsis = true;
            FirstDynLabel.Location = new Point(12, 30);
            FirstDynLabel.Name = "FirstDynLabel";
            FirstDynLabel.Size = new Size(126, 28);
            FirstDynLabel.TabIndex = 20;
            FirstDynLabel.Text = "None Selected";
            FirstDynLabel.TextAlign = ContentAlignment.MiddleRight;
            // 
            // label13
            // 
            label13.AutoSize = true;
            label13.Location = new Point(5, 17);
            label13.Name = "label13";
            label13.Size = new Size(108, 13);
            label13.TabIndex = 19;
            label13.Text = "First Dynamic Sprite";
            // 
            // FirstDynButton
            // 
            FirstDynButton.Location = new Point(141, 30);
            FirstDynButton.Name = "FirstDynButton";
            FirstDynButton.Size = new Size(52, 28);
            FirstDynButton.TabIndex = 18;
            FirstDynButton.Text = "Change";
            FirstDynButton.UseVisualStyleBackColor = true;
            FirstDynButton.Click += FirstDynButton_Click;
            // 
            // AutoZoom
            // 
            AutoZoom.AutoSize = true;
            AutoZoom.Checked = true;
            AutoZoom.CheckState = CheckState.Checked;
            AutoZoom.Location = new Point(583, 58);
            AutoZoom.Name = "AutoZoom";
            AutoZoom.Size = new Size(158, 17);
            AutoZoom.TabIndex = 14;
            AutoZoom.Text = "Auto-Generate Far Zooms";
            AutoZoom.UseVisualStyleBackColor = true;
            AutoZoom.CheckedChanged += AutoZoom_CheckedChanged;
            // 
            // DGRPEdit
            // 
            DGRPEdit.Location = new Point(206, 9);
            DGRPEdit.Name = "DGRPEdit";
            DGRPEdit.Size = new Size(350, 447);
            DGRPEdit.TabIndex = 1;
            // 
            // AutoRot
            // 
            AutoRot.AutoSize = true;
            AutoRot.Location = new Point(633, 3);
            AutoRot.Name = "AutoRot";
            AutoRot.Size = new Size(69, 17);
            AutoRot.TabIndex = 18;
            AutoRot.Text = "AutoRot";
            AutoRot.UseVisualStyleBackColor = true;
            AutoRot.CheckedChanged += AutoRot_CheckedChanged;
            // 
            // DGRPEditor
            // 
            AutoScaleDimensions = new SizeF(96F, 96F);
            AutoScaleMode = AutoScaleMode.Dpi;
            Controls.Add(AutoRot);
            Controls.Add(AutoZoom);
            Controls.Add(groupBox1);
            Controls.Add(label2);
            Controls.Add(label1);
            Controls.Add(ZoomTrack);
            Controls.Add(RotationTrack);
            Controls.Add(DGRPEdit);
            Controls.Add(SelectSpriteBox);
            Controls.Add(DGRPBox);
            Controls.Add(groupBox3);
            Name = "DGRPEditor";
            Size = new Size(762, 459);
            SelectSpriteBox.ResumeLayout(false);
            SelectSpriteBox.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)yPx).EndInit();
            ((System.ComponentModel.ISupportInitialize)xPx).EndInit();
            DGRPBox.ResumeLayout(false);
            groupBox3.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)RotationTrack).EndInit();
            ((System.ComponentModel.ISupportInitialize)ZoomTrack).EndInit();
            groupBox1.ResumeLayout(false);
            groupBox1.PerformLayout();
            ResumeLayout(false);
            PerformLayout();

        }

        #endregion

        private System.Windows.Forms.GroupBox SelectSpriteBox;
        private System.Windows.Forms.GroupBox DGRPBox;
        private System.Windows.Forms.GroupBox groupBox3;
        private Common.InteractiveDGRPControl DGRPEdit;
        private System.Windows.Forms.TrackBar RotationTrack;
        private System.Windows.Forms.TrackBar ZoomTrack;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Label label7;
        private System.Windows.Forms.NumericUpDown yPx;
        private System.Windows.Forms.Label label6;
        private System.Windows.Forms.NumericUpDown xPx;
        private System.Windows.Forms.Label label5;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.CheckBox FlipCheck;
        private System.Windows.Forms.Label label10;
        private System.Windows.Forms.TextBox zPhys;
        private System.Windows.Forms.Label label8;
        private System.Windows.Forms.TextBox yPhys;
        private System.Windows.Forms.Label label9;
        private System.Windows.Forms.TextBox xPhys;
        private System.Windows.Forms.ComboBox RotationCombo;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.Button FirstDGRP;
        private System.Windows.Forms.Button LastDGRP;
        private System.Windows.Forms.ListView DGRPList;
        private System.Windows.Forms.ColumnHeader IndexColumn;
        private System.Windows.Forms.ColumnHeader NameColumn;
        private System.Windows.Forms.ListBox SpriteList;
        private System.Windows.Forms.Label SPRLabel;
        private System.Windows.Forms.Label label11;
        private System.Windows.Forms.Button ChangeSPR;
        private System.Windows.Forms.Button MoveUpSPR;
        private System.Windows.Forms.Button AddSPR;
        private System.Windows.Forms.Button RemoveSPR;
        private System.Windows.Forms.Button MoveDownSPR;
        private System.Windows.Forms.Button RenameDGRP;
        private System.Windows.Forms.GroupBox groupBox1;
        private System.Windows.Forms.Label FirstDynLabel;
        private System.Windows.Forms.Label label13;
        private System.Windows.Forms.Button FirstDynButton;
        private System.Windows.Forms.CheckBox AutoZoom;
        private System.Windows.Forms.Button DGRPDown;
        private System.Windows.Forms.Button RemoveDGRP;
        private System.Windows.Forms.Button AddDGRP;
        private System.Windows.Forms.Button DGRPUp;
        private System.Windows.Forms.Label LastDynLabel;
        private System.Windows.Forms.Label label15;
        private System.Windows.Forms.Button LastDynButton;
        private System.Windows.Forms.Button CopyButton;
        private System.Windows.Forms.CheckBox AutoRot;
    }
}
