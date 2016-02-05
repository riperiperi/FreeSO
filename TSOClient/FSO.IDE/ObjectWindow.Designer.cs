namespace FSO.IDE
{
    partial class ObjectWindow
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(ObjectWindow));
            this.objPages = new System.Windows.Forms.TabControl();
            this.tabPage1 = new System.Windows.Forms.TabPage();
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this.label5 = new System.Windows.Forms.Label();
            this.textBox2 = new System.Windows.Forms.TextBox();
            this.label4 = new System.Windows.Forms.Label();
            this.label2 = new System.Windows.Forms.Label();
            this.textBox1 = new System.Windows.Forms.TextBox();
            this.pictureBox1 = new System.Windows.Forms.PictureBox();
            this.tabPage2 = new System.Windows.Forms.TabPage();
            this.IffResView = new FSO.IDE.ResourceBrowser.IFFResComponent();
            this.tabPage3 = new System.Windows.Forms.TabPage();
            this.tabPage4 = new System.Windows.Forms.TabPage();
            this.iffButton = new System.Windows.Forms.Button();
            this.button1 = new System.Windows.Forms.Button();
            this.label1 = new System.Windows.Forms.Label();
            this.tabPage5 = new System.Windows.Forms.TabPage();
            this.label3 = new System.Windows.Forms.Label();
            this.ObjThumb = new System.Windows.Forms.PictureBox();
            this.ObjCombo = new System.Windows.Forms.ComboBox();
            this.SemiGlobalButton = new System.Windows.Forms.Button();
            this.ObjMultitileLabel = new System.Windows.Forms.Label();
            this.ObjDescLabel = new System.Windows.Forms.Label();
            this.ObjNameLabel = new System.Windows.Forms.Label();
            this.GlobalButton = new System.Windows.Forms.Button();
            this.SGChangeButton = new System.Windows.Forms.Button();
            this.label6 = new System.Windows.Forms.Label();
            this.YOffset = new System.Windows.Forms.NumericUpDown();
            this.LevelOffset = new System.Windows.Forms.NumericUpDown();
            this.label7 = new System.Windows.Forms.Label();
            this.checkBox1 = new System.Windows.Forms.CheckBox();
            this.checkBox2 = new System.Windows.Forms.CheckBox();
            this.XOffset = new System.Windows.Forms.NumericUpDown();
            this.label8 = new System.Windows.Forms.Label();
            this.groupBox2 = new System.Windows.Forms.GroupBox();
            this.FootprintWest = new System.Windows.Forms.NumericUpDown();
            this.label9 = new System.Windows.Forms.Label();
            this.numericUpDown2 = new System.Windows.Forms.NumericUpDown();
            this.label10 = new System.Windows.Forms.Label();
            this.FootprintEast = new System.Windows.Forms.NumericUpDown();
            this.FootprintNorth = new System.Windows.Forms.NumericUpDown();
            this.FootprintSouth = new System.Windows.Forms.NumericUpDown();
            this.comboBox1 = new System.Windows.Forms.ComboBox();
            this.label11 = new System.Windows.Forms.Label();
            this.objPages.SuspendLayout();
            this.tabPage1.SuspendLayout();
            this.groupBox1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox1)).BeginInit();
            this.tabPage2.SuspendLayout();
            this.tabPage4.SuspendLayout();
            this.tabPage5.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.ObjThumb)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.YOffset)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.LevelOffset)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.XOffset)).BeginInit();
            this.groupBox2.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.FootprintWest)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.numericUpDown2)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.FootprintEast)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.FootprintNorth)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.FootprintSouth)).BeginInit();
            this.SuspendLayout();
            // 
            // objPages
            // 
            this.objPages.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.objPages.Controls.Add(this.tabPage1);
            this.objPages.Controls.Add(this.tabPage2);
            this.objPages.Controls.Add(this.tabPage3);
            this.objPages.Controls.Add(this.tabPage4);
            this.objPages.Controls.Add(this.tabPage5);
            this.objPages.Location = new System.Drawing.Point(7, 68);
            this.objPages.Name = "objPages";
            this.objPages.SelectedIndex = 0;
            this.objPages.Size = new System.Drawing.Size(770, 485);
            this.objPages.TabIndex = 0;
            // 
            // tabPage1
            // 
            this.tabPage1.Controls.Add(this.groupBox2);
            this.tabPage1.Controls.Add(this.groupBox1);
            this.tabPage1.Controls.Add(this.label5);
            this.tabPage1.Controls.Add(this.textBox2);
            this.tabPage1.Controls.Add(this.label4);
            this.tabPage1.Controls.Add(this.label2);
            this.tabPage1.Controls.Add(this.textBox1);
            this.tabPage1.Controls.Add(this.pictureBox1);
            this.tabPage1.Location = new System.Drawing.Point(4, 22);
            this.tabPage1.Name = "tabPage1";
            this.tabPage1.Padding = new System.Windows.Forms.Padding(3);
            this.tabPage1.Size = new System.Drawing.Size(762, 459);
            this.tabPage1.TabIndex = 0;
            this.tabPage1.Text = "Object";
            this.tabPage1.UseVisualStyleBackColor = true;
            // 
            // groupBox1
            // 
            this.groupBox1.Controls.Add(this.XOffset);
            this.groupBox1.Controls.Add(this.label8);
            this.groupBox1.Controls.Add(this.checkBox2);
            this.groupBox1.Controls.Add(this.checkBox1);
            this.groupBox1.Controls.Add(this.LevelOffset);
            this.groupBox1.Controls.Add(this.label7);
            this.groupBox1.Controls.Add(this.YOffset);
            this.groupBox1.Controls.Add(this.label6);
            this.groupBox1.Location = new System.Drawing.Point(175, 48);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.Size = new System.Drawing.Size(206, 76);
            this.groupBox1.TabIndex = 6;
            this.groupBox1.TabStop = false;
            this.groupBox1.Text = "Multitile";
            // 
            // label5
            // 
            this.label5.AutoSize = true;
            this.label5.Location = new System.Drawing.Point(402, 6);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(34, 13);
            this.label5.TabIndex = 5;
            this.label5.Text = "GUID";
            // 
            // textBox2
            // 
            this.textBox2.Location = new System.Drawing.Point(405, 22);
            this.textBox2.Name = "textBox2";
            this.textBox2.Size = new System.Drawing.Size(100, 20);
            this.textBox2.TabIndex = 4;
            this.textBox2.Text = "0xDEADBEEF";
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Location = new System.Drawing.Point(389, 25);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(10, 13);
            this.label4.TabIndex = 3;
            this.label4.Text = ":";
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(172, 6);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(35, 13);
            this.label2.TabIndex = 2;
            this.label2.Text = "Name";
            // 
            // textBox1
            // 
            this.textBox1.Location = new System.Drawing.Point(175, 22);
            this.textBox1.Name = "textBox1";
            this.textBox1.Size = new System.Drawing.Size(208, 20);
            this.textBox1.TabIndex = 1;
            this.textBox1.Text = "Accessory Rack - Cheap";
            // 
            // pictureBox1
            // 
            this.pictureBox1.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.pictureBox1.Location = new System.Drawing.Point(6, 6);
            this.pictureBox1.Name = "pictureBox1";
            this.pictureBox1.Size = new System.Drawing.Size(160, 447);
            this.pictureBox1.TabIndex = 0;
            this.pictureBox1.TabStop = false;
            // 
            // tabPage2
            // 
            this.tabPage2.Controls.Add(this.IffResView);
            this.tabPage2.Location = new System.Drawing.Point(4, 22);
            this.tabPage2.Name = "tabPage2";
            this.tabPage2.Size = new System.Drawing.Size(762, 459);
            this.tabPage2.TabIndex = 1;
            this.tabPage2.Text = "Trees and Resources";
            this.tabPage2.UseVisualStyleBackColor = true;
            // 
            // IffResView
            // 
            this.IffResView.Dock = System.Windows.Forms.DockStyle.Fill;
            this.IffResView.Location = new System.Drawing.Point(0, 0);
            this.IffResView.Margin = new System.Windows.Forms.Padding(0);
            this.IffResView.Name = "IffResView";
            this.IffResView.Size = new System.Drawing.Size(762, 459);
            this.IffResView.TabIndex = 0;
            // 
            // tabPage3
            // 
            this.tabPage3.Location = new System.Drawing.Point(4, 22);
            this.tabPage3.Name = "tabPage3";
            this.tabPage3.Padding = new System.Windows.Forms.Padding(3);
            this.tabPage3.Size = new System.Drawing.Size(762, 459);
            this.tabPage3.TabIndex = 2;
            this.tabPage3.Text = "Entry Points";
            this.tabPage3.UseVisualStyleBackColor = true;
            // 
            // tabPage4
            // 
            this.tabPage4.Controls.Add(this.iffButton);
            this.tabPage4.Controls.Add(this.button1);
            this.tabPage4.Controls.Add(this.label1);
            this.tabPage4.Location = new System.Drawing.Point(4, 22);
            this.tabPage4.Name = "tabPage4";
            this.tabPage4.Padding = new System.Windows.Forms.Padding(3);
            this.tabPage4.Size = new System.Drawing.Size(762, 459);
            this.tabPage4.TabIndex = 3;
            this.tabPage4.Text = "File Options";
            this.tabPage4.UseVisualStyleBackColor = true;
            // 
            // iffButton
            // 
            this.iffButton.Location = new System.Drawing.Point(6, 72);
            this.iffButton.Name = "iffButton";
            this.iffButton.Size = new System.Drawing.Size(300, 29);
            this.iffButton.TabIndex = 5;
            this.iffButton.Text = "Export .iff";
            this.iffButton.UseVisualStyleBackColor = true;
            this.iffButton.Click += new System.EventHandler(this.iffButton_Click);
            // 
            // button1
            // 
            this.button1.Location = new System.Drawing.Point(6, 37);
            this.button1.Name = "button1";
            this.button1.Size = new System.Drawing.Size(300, 29);
            this.button1.TabIndex = 4;
            this.button1.Text = "Save .piff (test)";
            this.button1.UseVisualStyleBackColor = true;
            this.button1.Click += new System.EventHandler(this.button1_Click);
            // 
            // label1
            // 
            this.label1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.label1.Location = new System.Drawing.Point(3, 3);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(756, 453);
            this.label1.TabIndex = 0;
            this.label1.Text = resources.GetString("label1.Text");
            // 
            // tabPage5
            // 
            this.tabPage5.Controls.Add(this.label3);
            this.tabPage5.Location = new System.Drawing.Point(4, 22);
            this.tabPage5.Name = "tabPage5";
            this.tabPage5.Padding = new System.Windows.Forms.Padding(3);
            this.tabPage5.Size = new System.Drawing.Size(762, 459);
            this.tabPage5.TabIndex = 4;
            this.tabPage5.Text = "Appearance";
            this.tabPage5.UseVisualStyleBackColor = true;
            // 
            // label3
            // 
            this.label3.Dock = System.Windows.Forms.DockStyle.Fill;
            this.label3.Location = new System.Drawing.Point(3, 3);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(756, 453);
            this.label3.TabIndex = 2;
            this.label3.Text = "This page will feature a transmogrifier style DGRP editor with live preview. It w" +
    "ill also allow import of SPR2.\r\n";
            // 
            // ObjThumb
            // 
            this.ObjThumb.Location = new System.Drawing.Point(7, 12);
            this.ObjThumb.Name = "ObjThumb";
            this.ObjThumb.Size = new System.Drawing.Size(48, 48);
            this.ObjThumb.TabIndex = 1;
            this.ObjThumb.TabStop = false;
            // 
            // ObjCombo
            // 
            this.ObjCombo.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.ObjCombo.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.ObjCombo.FormattingEnabled = true;
            this.ObjCombo.Location = new System.Drawing.Point(469, 12);
            this.ObjCombo.Name = "ObjCombo";
            this.ObjCombo.Size = new System.Drawing.Size(304, 21);
            this.ObjCombo.TabIndex = 2;
            this.ObjCombo.SelectedIndexChanged += new System.EventHandler(this.ObjCombo_SelectedIndexChanged);
            // 
            // SemiGlobalButton
            // 
            this.SemiGlobalButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.SemiGlobalButton.Location = new System.Drawing.Point(469, 37);
            this.SemiGlobalButton.Name = "SemiGlobalButton";
            this.SemiGlobalButton.Size = new System.Drawing.Size(171, 23);
            this.SemiGlobalButton.TabIndex = 3;
            this.SemiGlobalButton.Text = "Semi-Global (doorglobals)";
            this.SemiGlobalButton.UseVisualStyleBackColor = true;
            this.SemiGlobalButton.Click += new System.EventHandler(this.SemiGlobalButton_Click);
            // 
            // ObjMultitileLabel
            // 
            this.ObjMultitileLabel.Location = new System.Drawing.Point(61, 45);
            this.ObjMultitileLabel.Name = "ObjMultitileLabel";
            this.ObjMultitileLabel.Size = new System.Drawing.Size(186, 17);
            this.ObjMultitileLabel.TabIndex = 20;
            this.ObjMultitileLabel.Text = "Multitile Master Object";
            // 
            // ObjDescLabel
            // 
            this.ObjDescLabel.Location = new System.Drawing.Point(61, 30);
            this.ObjDescLabel.Name = "ObjDescLabel";
            this.ObjDescLabel.Size = new System.Drawing.Size(186, 17);
            this.ObjDescLabel.TabIndex = 19;
            this.ObjDescLabel.Text = "§2000 - Job Object";
            // 
            // ObjNameLabel
            // 
            this.ObjNameLabel.AutoEllipsis = true;
            this.ObjNameLabel.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.ObjNameLabel.Location = new System.Drawing.Point(61, 12);
            this.ObjNameLabel.Name = "ObjNameLabel";
            this.ObjNameLabel.Size = new System.Drawing.Size(288, 17);
            this.ObjNameLabel.TabIndex = 18;
            this.ObjNameLabel.Text = "Accessory Rack - Cheap";
            // 
            // GlobalButton
            // 
            this.GlobalButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.GlobalButton.Location = new System.Drawing.Point(698, 37);
            this.GlobalButton.Name = "GlobalButton";
            this.GlobalButton.Size = new System.Drawing.Size(75, 23);
            this.GlobalButton.TabIndex = 21;
            this.GlobalButton.Text = "Global";
            this.GlobalButton.UseVisualStyleBackColor = true;
            this.GlobalButton.Click += new System.EventHandler(this.GlobalButton_Click);
            // 
            // SGChangeButton
            // 
            this.SGChangeButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.SGChangeButton.Location = new System.Drawing.Point(640, 37);
            this.SGChangeButton.Name = "SGChangeButton";
            this.SGChangeButton.Size = new System.Drawing.Size(52, 23);
            this.SGChangeButton.TabIndex = 22;
            this.SGChangeButton.Text = "Change";
            this.SGChangeButton.UseVisualStyleBackColor = true;
            // 
            // label6
            // 
            this.label6.AutoSize = true;
            this.label6.Location = new System.Drawing.Point(76, 14);
            this.label6.Name = "label6";
            this.label6.Size = new System.Drawing.Size(48, 13);
            this.label6.TabIndex = 0;
            this.label6.Text = "Offest Y:";
            // 
            // YOffset
            // 
            this.YOffset.Location = new System.Drawing.Point(76, 30);
            this.YOffset.Name = "YOffset";
            this.YOffset.Size = new System.Drawing.Size(55, 20);
            this.YOffset.TabIndex = 1;
            // 
            // LevelOffset
            // 
            this.LevelOffset.Location = new System.Drawing.Point(145, 30);
            this.LevelOffset.Name = "LevelOffset";
            this.LevelOffset.Size = new System.Drawing.Size(55, 20);
            this.LevelOffset.TabIndex = 3;
            // 
            // label7
            // 
            this.label7.AutoSize = true;
            this.label7.Location = new System.Drawing.Point(145, 14);
            this.label7.Name = "label7";
            this.label7.Size = new System.Drawing.Size(36, 13);
            this.label7.TabIndex = 2;
            this.label7.Text = "Level:";
            // 
            // checkBox1
            // 
            this.checkBox1.AutoSize = true;
            this.checkBox1.Location = new System.Drawing.Point(84, 53);
            this.checkBox1.Margin = new System.Windows.Forms.Padding(3, 0, 3, 0);
            this.checkBox1.Name = "checkBox1";
            this.checkBox1.Size = new System.Drawing.Size(58, 17);
            this.checkBox1.TabIndex = 23;
            this.checkBox1.Text = "Master";
            this.checkBox1.UseVisualStyleBackColor = true;
            // 
            // checkBox2
            // 
            this.checkBox2.AutoSize = true;
            this.checkBox2.Location = new System.Drawing.Point(8, 53);
            this.checkBox2.Margin = new System.Windows.Forms.Padding(3, 0, 3, 0);
            this.checkBox2.Name = "checkBox2";
            this.checkBox2.Size = new System.Drawing.Size(70, 17);
            this.checkBox2.TabIndex = 24;
            this.checkBox2.Text = "Lead Tile";
            this.checkBox2.UseVisualStyleBackColor = true;
            // 
            // XOffset
            // 
            this.XOffset.Location = new System.Drawing.Point(8, 30);
            this.XOffset.Name = "XOffset";
            this.XOffset.Size = new System.Drawing.Size(55, 20);
            this.XOffset.TabIndex = 26;
            // 
            // label8
            // 
            this.label8.AutoSize = true;
            this.label8.Location = new System.Drawing.Point(8, 14);
            this.label8.Name = "label8";
            this.label8.Size = new System.Drawing.Size(48, 13);
            this.label8.TabIndex = 25;
            this.label8.Text = "Offest X:";
            // 
            // groupBox2
            // 
            this.groupBox2.Controls.Add(this.label11);
            this.groupBox2.Controls.Add(this.comboBox1);
            this.groupBox2.Controls.Add(this.FootprintSouth);
            this.groupBox2.Controls.Add(this.FootprintNorth);
            this.groupBox2.Controls.Add(this.FootprintWest);
            this.groupBox2.Controls.Add(this.label9);
            this.groupBox2.Controls.Add(this.numericUpDown2);
            this.groupBox2.Controls.Add(this.label10);
            this.groupBox2.Controls.Add(this.FootprintEast);
            this.groupBox2.Location = new System.Drawing.Point(175, 130);
            this.groupBox2.Name = "groupBox2";
            this.groupBox2.Size = new System.Drawing.Size(206, 118);
            this.groupBox2.TabIndex = 7;
            this.groupBox2.TabStop = false;
            this.groupBox2.Text = "Physical";
            // 
            // FootprintWest
            // 
            this.FootprintWest.Location = new System.Drawing.Point(8, 63);
            this.FootprintWest.Maximum = new decimal(new int[] {
            15,
            0,
            0,
            0});
            this.FootprintWest.Name = "FootprintWest";
            this.FootprintWest.Size = new System.Drawing.Size(55, 20);
            this.FootprintWest.TabIndex = 26;
            // 
            // label9
            // 
            this.label9.AutoSize = true;
            this.label9.Location = new System.Drawing.Point(11, 19);
            this.label9.Name = "label9";
            this.label9.Size = new System.Drawing.Size(118, 13);
            this.label9.TabIndex = 25;
            this.label9.Text = "Collision Footprint Inset:";
            // 
            // numericUpDown2
            // 
            this.numericUpDown2.Location = new System.Drawing.Point(145, 38);
            this.numericUpDown2.Name = "numericUpDown2";
            this.numericUpDown2.Size = new System.Drawing.Size(55, 20);
            this.numericUpDown2.TabIndex = 3;
            // 
            // label10
            // 
            this.label10.AutoSize = true;
            this.label10.Location = new System.Drawing.Point(142, 22);
            this.label10.Name = "label10";
            this.label10.Size = new System.Drawing.Size(58, 13);
            this.label10.TabIndex = 2;
            this.label10.Text = "Tile Width:";
            // 
            // FootprintEast
            // 
            this.FootprintEast.Location = new System.Drawing.Point(76, 63);
            this.FootprintEast.Maximum = new decimal(new int[] {
            15,
            0,
            0,
            0});
            this.FootprintEast.Name = "FootprintEast";
            this.FootprintEast.Size = new System.Drawing.Size(55, 20);
            this.FootprintEast.TabIndex = 1;
            // 
            // FootprintNorth
            // 
            this.FootprintNorth.Location = new System.Drawing.Point(42, 36);
            this.FootprintNorth.Maximum = new decimal(new int[] {
            15,
            0,
            0,
            0});
            this.FootprintNorth.Name = "FootprintNorth";
            this.FootprintNorth.Size = new System.Drawing.Size(55, 20);
            this.FootprintNorth.TabIndex = 27;
            // 
            // FootprintSouth
            // 
            this.FootprintSouth.Location = new System.Drawing.Point(42, 90);
            this.FootprintSouth.Maximum = new decimal(new int[] {
            15,
            0,
            0,
            0});
            this.FootprintSouth.Name = "FootprintSouth";
            this.FootprintSouth.Size = new System.Drawing.Size(55, 20);
            this.FootprintSouth.TabIndex = 28;
            // 
            // comboBox1
            // 
            this.comboBox1.FormattingEnabled = true;
            this.comboBox1.Location = new System.Drawing.Point(145, 82);
            this.comboBox1.Name = "comboBox1";
            this.comboBox1.Size = new System.Drawing.Size(55, 21);
            this.comboBox1.TabIndex = 29;
            this.comboBox1.Text = "North";
            // 
            // label11
            // 
            this.label11.AutoSize = true;
            this.label11.Location = new System.Drawing.Point(142, 66);
            this.label11.Name = "label11";
            this.label11.Size = new System.Drawing.Size(50, 13);
            this.label11.TabIndex = 30;
            this.label11.Text = "Front Dir:";
            // 
            // ObjectWindow
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(784, 561);
            this.Controls.Add(this.SGChangeButton);
            this.Controls.Add(this.GlobalButton);
            this.Controls.Add(this.ObjMultitileLabel);
            this.Controls.Add(this.ObjDescLabel);
            this.Controls.Add(this.ObjNameLabel);
            this.Controls.Add(this.SemiGlobalButton);
            this.Controls.Add(this.ObjCombo);
            this.Controls.Add(this.ObjThumb);
            this.Controls.Add(this.objPages);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.Name = "ObjectWindow";
            this.Text = "Edit Object - accessoryrack";
            this.objPages.ResumeLayout(false);
            this.tabPage1.ResumeLayout(false);
            this.tabPage1.PerformLayout();
            this.groupBox1.ResumeLayout(false);
            this.groupBox1.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox1)).EndInit();
            this.tabPage2.ResumeLayout(false);
            this.tabPage4.ResumeLayout(false);
            this.tabPage5.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.ObjThumb)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.YOffset)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.LevelOffset)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.XOffset)).EndInit();
            this.groupBox2.ResumeLayout(false);
            this.groupBox2.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.FootprintWest)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.numericUpDown2)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.FootprintEast)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.FootprintNorth)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.FootprintSouth)).EndInit();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.TabControl objPages;
        private System.Windows.Forms.TabPage tabPage1;
        private System.Windows.Forms.TabPage tabPage2;
        private System.Windows.Forms.TabPage tabPage3;
        private System.Windows.Forms.TabPage tabPage4;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.TabPage tabPage5;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.PictureBox ObjThumb;
        private System.Windows.Forms.ComboBox ObjCombo;
        private System.Windows.Forms.Button SemiGlobalButton;
        private System.Windows.Forms.Label ObjMultitileLabel;
        private System.Windows.Forms.Label ObjDescLabel;
        private System.Windows.Forms.Label ObjNameLabel;
        private System.Windows.Forms.Button GlobalButton;
        private ResourceBrowser.IFFResComponent IffResView;
        private System.Windows.Forms.Button SGChangeButton;
        private System.Windows.Forms.GroupBox groupBox1;
        private System.Windows.Forms.Label label5;
        private System.Windows.Forms.TextBox textBox2;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.TextBox textBox1;
        private System.Windows.Forms.PictureBox pictureBox1;
        private System.Windows.Forms.Button iffButton;
        private System.Windows.Forms.Button button1;
        private System.Windows.Forms.GroupBox groupBox2;
        private System.Windows.Forms.Label label11;
        private System.Windows.Forms.ComboBox comboBox1;
        private System.Windows.Forms.NumericUpDown FootprintSouth;
        private System.Windows.Forms.NumericUpDown FootprintNorth;
        private System.Windows.Forms.NumericUpDown FootprintWest;
        private System.Windows.Forms.Label label9;
        private System.Windows.Forms.NumericUpDown numericUpDown2;
        private System.Windows.Forms.Label label10;
        private System.Windows.Forms.NumericUpDown FootprintEast;
        private System.Windows.Forms.NumericUpDown XOffset;
        private System.Windows.Forms.Label label8;
        private System.Windows.Forms.CheckBox checkBox2;
        private System.Windows.Forms.CheckBox checkBox1;
        private System.Windows.Forms.NumericUpDown LevelOffset;
        private System.Windows.Forms.Label label7;
        private System.Windows.Forms.NumericUpDown YOffset;
        private System.Windows.Forms.Label label6;
    }
}