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
            this.SelectSpriteBox = new System.Windows.Forms.GroupBox();
            this.SPRLabel = new System.Windows.Forms.Label();
            this.label11 = new System.Windows.Forms.Label();
            this.ChangeSPR = new System.Windows.Forms.Button();
            this.RotationCombo = new System.Windows.Forms.ComboBox();
            this.label4 = new System.Windows.Forms.Label();
            this.FlipCheck = new System.Windows.Forms.CheckBox();
            this.label10 = new System.Windows.Forms.Label();
            this.zPhys = new System.Windows.Forms.TextBox();
            this.label8 = new System.Windows.Forms.Label();
            this.yPhys = new System.Windows.Forms.TextBox();
            this.label9 = new System.Windows.Forms.Label();
            this.xPhys = new System.Windows.Forms.TextBox();
            this.label7 = new System.Windows.Forms.Label();
            this.yPx = new System.Windows.Forms.NumericUpDown();
            this.label6 = new System.Windows.Forms.Label();
            this.xPx = new System.Windows.Forms.NumericUpDown();
            this.label5 = new System.Windows.Forms.Label();
            this.label3 = new System.Windows.Forms.Label();
            this.DGRPBox = new System.Windows.Forms.GroupBox();
            this.DGRPDown = new System.Windows.Forms.Button();
            this.RemoveDGRP = new System.Windows.Forms.Button();
            this.AddDGRP = new System.Windows.Forms.Button();
            this.DGRPUp = new System.Windows.Forms.Button();
            this.RenameDGRP = new System.Windows.Forms.Button();
            this.FirstDGRP = new System.Windows.Forms.Button();
            this.LastDGRP = new System.Windows.Forms.Button();
            this.DGRPList = new System.Windows.Forms.ListView();
            this.IndexColumn = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.NameColumn = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.groupBox3 = new System.Windows.Forms.GroupBox();
            this.MoveUpSPR = new System.Windows.Forms.Button();
            this.AddSPR = new System.Windows.Forms.Button();
            this.RemoveSPR = new System.Windows.Forms.Button();
            this.SpriteList = new System.Windows.Forms.ListBox();
            this.MoveDownSPR = new System.Windows.Forms.Button();
            this.RotationTrack = new System.Windows.Forms.TrackBar();
            this.ZoomTrack = new System.Windows.Forms.TrackBar();
            this.label1 = new System.Windows.Forms.Label();
            this.label2 = new System.Windows.Forms.Label();
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this.LastDynLabel = new System.Windows.Forms.Label();
            this.label15 = new System.Windows.Forms.Label();
            this.LastDynButton = new System.Windows.Forms.Button();
            this.FirstDynLabel = new System.Windows.Forms.Label();
            this.label13 = new System.Windows.Forms.Label();
            this.FirstDynButton = new System.Windows.Forms.Button();
            this.AutoZoom = new System.Windows.Forms.CheckBox();
            this.DGRPEdit = new FSO.IDE.Common.InteractiveDGRPControl();
            this.SelectSpriteBox.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.yPx)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.xPx)).BeginInit();
            this.DGRPBox.SuspendLayout();
            this.groupBox3.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.RotationTrack)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.ZoomTrack)).BeginInit();
            this.groupBox1.SuspendLayout();
            this.SuspendLayout();
            // 
            // SelectSpriteBox
            // 
            this.SelectSpriteBox.Controls.Add(this.SPRLabel);
            this.SelectSpriteBox.Controls.Add(this.label11);
            this.SelectSpriteBox.Controls.Add(this.ChangeSPR);
            this.SelectSpriteBox.Controls.Add(this.RotationCombo);
            this.SelectSpriteBox.Controls.Add(this.label4);
            this.SelectSpriteBox.Controls.Add(this.FlipCheck);
            this.SelectSpriteBox.Controls.Add(this.label10);
            this.SelectSpriteBox.Controls.Add(this.zPhys);
            this.SelectSpriteBox.Controls.Add(this.label8);
            this.SelectSpriteBox.Controls.Add(this.yPhys);
            this.SelectSpriteBox.Controls.Add(this.label9);
            this.SelectSpriteBox.Controls.Add(this.xPhys);
            this.SelectSpriteBox.Controls.Add(this.label7);
            this.SelectSpriteBox.Controls.Add(this.yPx);
            this.SelectSpriteBox.Controls.Add(this.label6);
            this.SelectSpriteBox.Controls.Add(this.xPx);
            this.SelectSpriteBox.Controls.Add(this.label5);
            this.SelectSpriteBox.Controls.Add(this.label3);
            this.SelectSpriteBox.Location = new System.Drawing.Point(559, 77);
            this.SelectSpriteBox.Name = "SelectSpriteBox";
            this.SelectSpriteBox.Size = new System.Drawing.Size(200, 200);
            this.SelectSpriteBox.TabIndex = 8;
            this.SelectSpriteBox.TabStop = false;
            this.SelectSpriteBox.Text = "Selected Sprite";
            // 
            // SPRLabel
            // 
            this.SPRLabel.AutoEllipsis = true;
            this.SPRLabel.Location = new System.Drawing.Point(13, 29);
            this.SPRLabel.Name = "SPRLabel";
            this.SPRLabel.Size = new System.Drawing.Size(126, 28);
            this.SPRLabel.TabIndex = 17;
            this.SPRLabel.Text = "None Selected";
            this.SPRLabel.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // label11
            // 
            this.label11.AutoSize = true;
            this.label11.Location = new System.Drawing.Point(6, 16);
            this.label11.Name = "label11";
            this.label11.Size = new System.Drawing.Size(69, 13);
            this.label11.TabIndex = 16;
            this.label11.Text = "SPR2 Chunk";
            // 
            // ChangeSPR
            // 
            this.ChangeSPR.Location = new System.Drawing.Point(142, 29);
            this.ChangeSPR.Name = "ChangeSPR";
            this.ChangeSPR.Size = new System.Drawing.Size(52, 28);
            this.ChangeSPR.TabIndex = 15;
            this.ChangeSPR.Text = "Change";
            this.ChangeSPR.UseVisualStyleBackColor = true;
            this.ChangeSPR.Click += new System.EventHandler(this.ChangeSPR_Click);
            // 
            // RotationCombo
            // 
            this.RotationCombo.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.RotationCombo.FormattingEnabled = true;
            this.RotationCombo.Location = new System.Drawing.Point(61, 148);
            this.RotationCombo.Name = "RotationCombo";
            this.RotationCombo.Size = new System.Drawing.Size(132, 21);
            this.RotationCombo.TabIndex = 14;
            this.RotationCombo.SelectedIndexChanged += new System.EventHandler(this.RotationCombo_SelectedIndexChanged);
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Location = new System.Drawing.Point(5, 151);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(50, 13);
            this.label4.TabIndex = 13;
            this.label4.Text = "Rotation:";
            // 
            // FlipCheck
            // 
            this.FlipCheck.AutoSize = true;
            this.FlipCheck.Location = new System.Drawing.Point(8, 175);
            this.FlipCheck.Name = "FlipCheck";
            this.FlipCheck.Size = new System.Drawing.Size(99, 17);
            this.FlipCheck.TabIndex = 12;
            this.FlipCheck.Text = "Flip Horizontally";
            this.FlipCheck.UseVisualStyleBackColor = true;
            this.FlipCheck.CheckedChanged += new System.EventHandler(this.FlipCheck_CheckedChanged);
            // 
            // label10
            // 
            this.label10.AutoSize = true;
            this.label10.Location = new System.Drawing.Point(138, 122);
            this.label10.Name = "label10";
            this.label10.Size = new System.Drawing.Size(15, 13);
            this.label10.TabIndex = 11;
            this.label10.Text = "z:";
            // 
            // zPhys
            // 
            this.zPhys.Location = new System.Drawing.Point(153, 119);
            this.zPhys.Name = "zPhys";
            this.zPhys.Size = new System.Drawing.Size(40, 20);
            this.zPhys.TabIndex = 10;
            this.zPhys.TextChanged += new System.EventHandler(this.zPhys_TextChanged);
            // 
            // label8
            // 
            this.label8.AutoSize = true;
            this.label8.Location = new System.Drawing.Point(76, 122);
            this.label8.Name = "label8";
            this.label8.Size = new System.Drawing.Size(15, 13);
            this.label8.TabIndex = 9;
            this.label8.Text = "y:";
            // 
            // yPhys
            // 
            this.yPhys.Location = new System.Drawing.Point(91, 119);
            this.yPhys.Name = "yPhys";
            this.yPhys.Size = new System.Drawing.Size(40, 20);
            this.yPhys.TabIndex = 8;
            this.yPhys.TextChanged += new System.EventHandler(this.yPhys_TextChanged);
            // 
            // label9
            // 
            this.label9.AutoSize = true;
            this.label9.Location = new System.Drawing.Point(13, 122);
            this.label9.Name = "label9";
            this.label9.Size = new System.Drawing.Size(15, 13);
            this.label9.TabIndex = 7;
            this.label9.Text = "x:";
            // 
            // xPhys
            // 
            this.xPhys.Location = new System.Drawing.Point(28, 119);
            this.xPhys.Name = "xPhys";
            this.xPhys.Size = new System.Drawing.Size(40, 20);
            this.xPhys.TabIndex = 6;
            this.xPhys.TextChanged += new System.EventHandler(this.xPhys_TextChanged);
            // 
            // label7
            // 
            this.label7.AutoSize = true;
            this.label7.Location = new System.Drawing.Point(83, 78);
            this.label7.Name = "label7";
            this.label7.Size = new System.Drawing.Size(15, 13);
            this.label7.TabIndex = 5;
            this.label7.Text = "y:";
            // 
            // yPx
            // 
            this.yPx.Location = new System.Drawing.Point(98, 76);
            this.yPx.Maximum = new decimal(new int[] {
            32767,
            0,
            0,
            0});
            this.yPx.Minimum = new decimal(new int[] {
            32768,
            0,
            0,
            -2147483648});
            this.yPx.Name = "yPx";
            this.yPx.Size = new System.Drawing.Size(50, 20);
            this.yPx.TabIndex = 4;
            this.yPx.ValueChanged += new System.EventHandler(this.yPx_ValueChanged);
            // 
            // label6
            // 
            this.label6.AutoSize = true;
            this.label6.Location = new System.Drawing.Point(13, 78);
            this.label6.Name = "label6";
            this.label6.Size = new System.Drawing.Size(15, 13);
            this.label6.TabIndex = 3;
            this.label6.Text = "x:";
            // 
            // xPx
            // 
            this.xPx.Location = new System.Drawing.Point(28, 76);
            this.xPx.Maximum = new decimal(new int[] {
            32767,
            0,
            0,
            0});
            this.xPx.Minimum = new decimal(new int[] {
            32768,
            0,
            0,
            -2147483648});
            this.xPx.Name = "xPx";
            this.xPx.Size = new System.Drawing.Size(50, 20);
            this.xPx.TabIndex = 2;
            this.xPx.ValueChanged += new System.EventHandler(this.xPx_ValueChanged);
            // 
            // label5
            // 
            this.label5.AutoSize = true;
            this.label5.Location = new System.Drawing.Point(6, 103);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(77, 13);
            this.label5.TabIndex = 1;
            this.label5.Text = "Physical Offset";
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(5, 60);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(60, 13);
            this.label3.TabIndex = 0;
            this.label3.Text = "Pixel Offset";
            // 
            // DGRPBox
            // 
            this.DGRPBox.Controls.Add(this.DGRPDown);
            this.DGRPBox.Controls.Add(this.RemoveDGRP);
            this.DGRPBox.Controls.Add(this.AddDGRP);
            this.DGRPBox.Controls.Add(this.DGRPUp);
            this.DGRPBox.Controls.Add(this.RenameDGRP);
            this.DGRPBox.Controls.Add(this.FirstDGRP);
            this.DGRPBox.Controls.Add(this.LastDGRP);
            this.DGRPBox.Controls.Add(this.DGRPList);
            this.DGRPBox.Location = new System.Drawing.Point(3, 119);
            this.DGRPBox.Name = "DGRPBox";
            this.DGRPBox.Size = new System.Drawing.Size(200, 334);
            this.DGRPBox.TabIndex = 6;
            this.DGRPBox.TabStop = false;
            this.DGRPBox.Text = "Drawgroups";
            // 
            // DGRPDown
            // 
            this.DGRPDown.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.DGRPDown.Font = new System.Drawing.Font("Microsoft Sans Serif", 11.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.DGRPDown.Location = new System.Drawing.Point(167, 271);
            this.DGRPDown.Margin = new System.Windows.Forms.Padding(0);
            this.DGRPDown.Name = "DGRPDown";
            this.DGRPDown.Size = new System.Drawing.Size(26, 26);
            this.DGRPDown.TabIndex = 19;
            this.DGRPDown.Text = "↓";
            this.DGRPDown.UseVisualStyleBackColor = true;
            this.DGRPDown.Click += new System.EventHandler(this.DGRPDown_Click);
            // 
            // RemoveDGRP
            // 
            this.RemoveDGRP.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.RemoveDGRP.Font = new System.Drawing.Font("Microsoft Sans Serif", 11.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.RemoveDGRP.Location = new System.Drawing.Point(138, 271);
            this.RemoveDGRP.Margin = new System.Windows.Forms.Padding(0);
            this.RemoveDGRP.Name = "RemoveDGRP";
            this.RemoveDGRP.Size = new System.Drawing.Size(26, 26);
            this.RemoveDGRP.TabIndex = 20;
            this.RemoveDGRP.Text = "-";
            this.RemoveDGRP.UseVisualStyleBackColor = true;
            this.RemoveDGRP.Click += new System.EventHandler(this.RemoveDGRP_Click);
            // 
            // AddDGRP
            // 
            this.AddDGRP.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.AddDGRP.Font = new System.Drawing.Font("Microsoft Sans Serif", 11.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.AddDGRP.Location = new System.Drawing.Point(36, 271);
            this.AddDGRP.Margin = new System.Windows.Forms.Padding(0);
            this.AddDGRP.Name = "AddDGRP";
            this.AddDGRP.Size = new System.Drawing.Size(26, 26);
            this.AddDGRP.TabIndex = 21;
            this.AddDGRP.Text = "+";
            this.AddDGRP.UseVisualStyleBackColor = true;
            this.AddDGRP.Click += new System.EventHandler(this.AddDGRP_Click);
            // 
            // DGRPUp
            // 
            this.DGRPUp.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.DGRPUp.Font = new System.Drawing.Font("Microsoft Sans Serif", 11.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.DGRPUp.Location = new System.Drawing.Point(7, 271);
            this.DGRPUp.Margin = new System.Windows.Forms.Padding(0);
            this.DGRPUp.Name = "DGRPUp";
            this.DGRPUp.Size = new System.Drawing.Size(26, 26);
            this.DGRPUp.TabIndex = 22;
            this.DGRPUp.Text = "↑";
            this.DGRPUp.UseVisualStyleBackColor = true;
            this.DGRPUp.Click += new System.EventHandler(this.DGRPUp_Click);
            // 
            // RenameDGRP
            // 
            this.RenameDGRP.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.RenameDGRP.Location = new System.Drawing.Point(65, 271);
            this.RenameDGRP.Name = "RenameDGRP";
            this.RenameDGRP.Size = new System.Drawing.Size(70, 26);
            this.RenameDGRP.TabIndex = 5;
            this.RenameDGRP.Text = "Rename";
            this.RenameDGRP.UseVisualStyleBackColor = true;
            this.RenameDGRP.Click += new System.EventHandler(this.RenameDGRP_Click);
            // 
            // FirstDGRP
            // 
            this.FirstDGRP.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.FirstDGRP.Location = new System.Drawing.Point(6, 302);
            this.FirstDGRP.Name = "FirstDGRP";
            this.FirstDGRP.Size = new System.Drawing.Size(91, 23);
            this.FirstDGRP.TabIndex = 3;
            this.FirstDGRP.Text = "Set as First";
            this.FirstDGRP.UseVisualStyleBackColor = true;
            this.FirstDGRP.Click += new System.EventHandler(this.FirstDGRP_Click);
            // 
            // LastDGRP
            // 
            this.LastDGRP.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.LastDGRP.Location = new System.Drawing.Point(103, 302);
            this.LastDGRP.Name = "LastDGRP";
            this.LastDGRP.Size = new System.Drawing.Size(91, 23);
            this.LastDGRP.TabIndex = 2;
            this.LastDGRP.Text = "Set as Last";
            this.LastDGRP.UseVisualStyleBackColor = true;
            this.LastDGRP.Click += new System.EventHandler(this.LastDGRP_Click);
            // 
            // DGRPList
            // 
            this.DGRPList.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.DGRPList.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
            this.IndexColumn,
            this.NameColumn});
            this.DGRPList.FullRowSelect = true;
            this.DGRPList.HideSelection = false;
            this.DGRPList.Location = new System.Drawing.Point(6, 19);
            this.DGRPList.MultiSelect = false;
            this.DGRPList.Name = "DGRPList";
            this.DGRPList.Size = new System.Drawing.Size(188, 248);
            this.DGRPList.TabIndex = 1;
            this.DGRPList.UseCompatibleStateImageBehavior = false;
            this.DGRPList.View = System.Windows.Forms.View.Details;
            this.DGRPList.SelectedIndexChanged += new System.EventHandler(this.DGRPList_SelectedIndexChanged);
            // 
            // IndexColumn
            // 
            this.IndexColumn.Text = "#";
            this.IndexColumn.Width = 35;
            // 
            // NameColumn
            // 
            this.NameColumn.Text = "Name";
            this.NameColumn.Width = 128;
            // 
            // groupBox3
            // 
            this.groupBox3.Controls.Add(this.MoveUpSPR);
            this.groupBox3.Controls.Add(this.AddSPR);
            this.groupBox3.Controls.Add(this.RemoveSPR);
            this.groupBox3.Controls.Add(this.SpriteList);
            this.groupBox3.Controls.Add(this.MoveDownSPR);
            this.groupBox3.Location = new System.Drawing.Point(559, 283);
            this.groupBox3.Name = "groupBox3";
            this.groupBox3.Size = new System.Drawing.Size(200, 170);
            this.groupBox3.TabIndex = 5;
            this.groupBox3.TabStop = false;
            this.groupBox3.Text = "Sprites";
            // 
            // MoveUpSPR
            // 
            this.MoveUpSPR.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.MoveUpSPR.Font = new System.Drawing.Font("Microsoft Sans Serif", 11.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.MoveUpSPR.Location = new System.Drawing.Point(169, 19);
            this.MoveUpSPR.Margin = new System.Windows.Forms.Padding(0);
            this.MoveUpSPR.Name = "MoveUpSPR";
            this.MoveUpSPR.Size = new System.Drawing.Size(26, 26);
            this.MoveUpSPR.TabIndex = 18;
            this.MoveUpSPR.Text = "↑";
            this.MoveUpSPR.UseVisualStyleBackColor = true;
            this.MoveUpSPR.Click += new System.EventHandler(this.MoveUpSPR_Click);
            // 
            // AddSPR
            // 
            this.AddSPR.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.AddSPR.Font = new System.Drawing.Font("Microsoft Sans Serif", 11.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.AddSPR.Location = new System.Drawing.Point(169, 63);
            this.AddSPR.Margin = new System.Windows.Forms.Padding(0);
            this.AddSPR.Name = "AddSPR";
            this.AddSPR.Size = new System.Drawing.Size(26, 26);
            this.AddSPR.TabIndex = 17;
            this.AddSPR.Text = "+";
            this.AddSPR.UseVisualStyleBackColor = true;
            this.AddSPR.Click += new System.EventHandler(this.AddSPR_Click);
            // 
            // RemoveSPR
            // 
            this.RemoveSPR.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.RemoveSPR.Font = new System.Drawing.Font("Microsoft Sans Serif", 11.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.RemoveSPR.Location = new System.Drawing.Point(169, 93);
            this.RemoveSPR.Margin = new System.Windows.Forms.Padding(0);
            this.RemoveSPR.Name = "RemoveSPR";
            this.RemoveSPR.Size = new System.Drawing.Size(26, 26);
            this.RemoveSPR.TabIndex = 16;
            this.RemoveSPR.Text = "-";
            this.RemoveSPR.UseVisualStyleBackColor = true;
            this.RemoveSPR.Click += new System.EventHandler(this.RemoveSPR_Click);
            // 
            // SpriteList
            // 
            this.SpriteList.FormattingEnabled = true;
            this.SpriteList.IntegralHeight = false;
            this.SpriteList.Location = new System.Drawing.Point(6, 19);
            this.SpriteList.Name = "SpriteList";
            this.SpriteList.Size = new System.Drawing.Size(159, 145);
            this.SpriteList.TabIndex = 0;
            this.SpriteList.SelectedIndexChanged += new System.EventHandler(this.SpriteList_SelectedIndexChanged);
            // 
            // MoveDownSPR
            // 
            this.MoveDownSPR.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.MoveDownSPR.Font = new System.Drawing.Font("Microsoft Sans Serif", 11.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.MoveDownSPR.Location = new System.Drawing.Point(169, 138);
            this.MoveDownSPR.Margin = new System.Windows.Forms.Padding(0);
            this.MoveDownSPR.Name = "MoveDownSPR";
            this.MoveDownSPR.Size = new System.Drawing.Size(26, 26);
            this.MoveDownSPR.TabIndex = 15;
            this.MoveDownSPR.Text = "↓";
            this.MoveDownSPR.UseVisualStyleBackColor = true;
            this.MoveDownSPR.Click += new System.EventHandler(this.MoveDownSPR_Click);
            // 
            // RotationTrack
            // 
            this.RotationTrack.BackColor = System.Drawing.SystemColors.ControlLightLight;
            this.RotationTrack.LargeChange = 4;
            this.RotationTrack.Location = new System.Drawing.Point(559, 25);
            this.RotationTrack.Maximum = 3;
            this.RotationTrack.Name = "RotationTrack";
            this.RotationTrack.Size = new System.Drawing.Size(104, 45);
            this.RotationTrack.TabIndex = 9;
            this.RotationTrack.Scroll += new System.EventHandler(this.trackBar1_Scroll);
            // 
            // ZoomTrack
            // 
            this.ZoomTrack.BackColor = System.Drawing.SystemColors.ControlLightLight;
            this.ZoomTrack.Location = new System.Drawing.Point(669, 25);
            this.ZoomTrack.Maximum = 2;
            this.ZoomTrack.Name = "ZoomTrack";
            this.ZoomTrack.Size = new System.Drawing.Size(90, 45);
            this.ZoomTrack.TabIndex = 10;
            this.ZoomTrack.Scroll += new System.EventHandler(this.ZoomTrack_Scroll);
            // 
            // label1
            // 
            this.label1.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.label1.Location = new System.Drawing.Point(559, 9);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(104, 17);
            this.label1.TabIndex = 11;
            this.label1.Text = "Rotation";
            this.label1.TextAlign = System.Drawing.ContentAlignment.TopCenter;
            // 
            // label2
            // 
            this.label2.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.label2.Location = new System.Drawing.Point(669, 9);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(90, 17);
            this.label2.TabIndex = 12;
            this.label2.Text = "Zoom";
            this.label2.TextAlign = System.Drawing.ContentAlignment.TopCenter;
            // 
            // groupBox1
            // 
            this.groupBox1.Controls.Add(this.LastDynLabel);
            this.groupBox1.Controls.Add(this.label15);
            this.groupBox1.Controls.Add(this.LastDynButton);
            this.groupBox1.Controls.Add(this.FirstDynLabel);
            this.groupBox1.Controls.Add(this.label13);
            this.groupBox1.Controls.Add(this.FirstDynButton);
            this.groupBox1.Location = new System.Drawing.Point(3, 3);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.Size = new System.Drawing.Size(200, 110);
            this.groupBox1.TabIndex = 13;
            this.groupBox1.TabStop = false;
            this.groupBox1.Text = "Dynamic Sprites";
            // 
            // LastDynLabel
            // 
            this.LastDynLabel.AutoEllipsis = true;
            this.LastDynLabel.Location = new System.Drawing.Point(12, 74);
            this.LastDynLabel.Name = "LastDynLabel";
            this.LastDynLabel.Size = new System.Drawing.Size(126, 28);
            this.LastDynLabel.TabIndex = 23;
            this.LastDynLabel.Text = "None Selected";
            this.LastDynLabel.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // label15
            // 
            this.label15.AutoSize = true;
            this.label15.Location = new System.Drawing.Point(5, 61);
            this.label15.Name = "label15";
            this.label15.Size = new System.Drawing.Size(101, 13);
            this.label15.TabIndex = 22;
            this.label15.Text = "Last Dynamic Sprite";
            // 
            // LastDynButton
            // 
            this.LastDynButton.Location = new System.Drawing.Point(141, 74);
            this.LastDynButton.Name = "LastDynButton";
            this.LastDynButton.Size = new System.Drawing.Size(52, 28);
            this.LastDynButton.TabIndex = 21;
            this.LastDynButton.Text = "Change";
            this.LastDynButton.UseVisualStyleBackColor = true;
            this.LastDynButton.Click += new System.EventHandler(this.LastDynButton_Click);
            // 
            // FirstDynLabel
            // 
            this.FirstDynLabel.AutoEllipsis = true;
            this.FirstDynLabel.Location = new System.Drawing.Point(12, 30);
            this.FirstDynLabel.Name = "FirstDynLabel";
            this.FirstDynLabel.Size = new System.Drawing.Size(126, 28);
            this.FirstDynLabel.TabIndex = 20;
            this.FirstDynLabel.Text = "None Selected";
            this.FirstDynLabel.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // label13
            // 
            this.label13.AutoSize = true;
            this.label13.Location = new System.Drawing.Point(5, 17);
            this.label13.Name = "label13";
            this.label13.Size = new System.Drawing.Size(100, 13);
            this.label13.TabIndex = 19;
            this.label13.Text = "First Dynamic Sprite";
            // 
            // FirstDynButton
            // 
            this.FirstDynButton.Location = new System.Drawing.Point(141, 30);
            this.FirstDynButton.Name = "FirstDynButton";
            this.FirstDynButton.Size = new System.Drawing.Size(52, 28);
            this.FirstDynButton.TabIndex = 18;
            this.FirstDynButton.Text = "Change";
            this.FirstDynButton.UseVisualStyleBackColor = true;
            this.FirstDynButton.Click += new System.EventHandler(this.FirstDynButton_Click);
            // 
            // AutoZoom
            // 
            this.AutoZoom.AutoSize = true;
            this.AutoZoom.Checked = true;
            this.AutoZoom.CheckState = System.Windows.Forms.CheckState.Checked;
            this.AutoZoom.Location = new System.Drawing.Point(583, 58);
            this.AutoZoom.Name = "AutoZoom";
            this.AutoZoom.Size = new System.Drawing.Size(148, 17);
            this.AutoZoom.TabIndex = 14;
            this.AutoZoom.Text = "Auto-Generate Far Zooms";
            this.AutoZoom.UseVisualStyleBackColor = true;
            this.AutoZoom.CheckedChanged += new System.EventHandler(this.AutoZoom_CheckedChanged);
            // 
            // DGRPEdit
            // 
            this.DGRPEdit.Location = new System.Drawing.Point(206, 9);
            this.DGRPEdit.Name = "DGRPEdit";
            this.DGRPEdit.Size = new System.Drawing.Size(350, 447);
            this.DGRPEdit.TabIndex = 1;
            // 
            // DGRPEditor
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.AutoZoom);
            this.Controls.Add(this.groupBox1);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.ZoomTrack);
            this.Controls.Add(this.RotationTrack);
            this.Controls.Add(this.DGRPEdit);
            this.Controls.Add(this.SelectSpriteBox);
            this.Controls.Add(this.DGRPBox);
            this.Controls.Add(this.groupBox3);
            this.Name = "DGRPEditor";
            this.Size = new System.Drawing.Size(762, 459);
            this.SelectSpriteBox.ResumeLayout(false);
            this.SelectSpriteBox.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.yPx)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.xPx)).EndInit();
            this.DGRPBox.ResumeLayout(false);
            this.groupBox3.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.RotationTrack)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.ZoomTrack)).EndInit();
            this.groupBox1.ResumeLayout(false);
            this.groupBox1.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

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
    }
}
