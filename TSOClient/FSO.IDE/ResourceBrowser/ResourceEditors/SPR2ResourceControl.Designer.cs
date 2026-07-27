namespace FSO.IDE.ResourceBrowser.ResourceEditors
{
    partial class SPR2ResourceControl
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
            ModeCombo = new ComboBox();
            PreviewLabel = new Label();
            SPRBox3 = new PictureBox();
            SPRBox2 = new PictureBox();
            SPRBox1 = new PictureBox();
            FrameList = new ListBox();
            FramesLabel = new Label();
            NewButton = new Button();
            ImportButton = new Button();
            ExportButton = new Button();
            DeleteButton = new Button();
            ExportAll = new Button();
            ImportAll = new Button();
            AutoZooms = new CheckBox();
            SPRSelector = new OBJDSelectorControl();
            SheetImport = new Button();
            ((System.ComponentModel.ISupportInitialize)SPRBox3).BeginInit();
            ((System.ComponentModel.ISupportInitialize)SPRBox2).BeginInit();
            ((System.ComponentModel.ISupportInitialize)SPRBox1).BeginInit();
            SuspendLayout();
            // 
            // ModeCombo
            // 
            ModeCombo.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
            ModeCombo.DropDownStyle = ComboBoxStyle.DropDownList;
            ModeCombo.FormattingEnabled = true;
            ModeCombo.Items.AddRange(new object[] { "Color", "Alpha", "Z-Buffer" });
            ModeCombo.Location = new Point(3, 431);
            ModeCombo.Name = "ModeCombo";
            ModeCombo.Size = new Size(250, 21);
            ModeCombo.TabIndex = 3;
            ModeCombo.SelectedIndexChanged += comboBox1_SelectedIndexChanged;
            // 
            // PreviewLabel
            // 
            PreviewLabel.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
            PreviewLabel.AutoSize = true;
            PreviewLabel.Location = new Point(3, 21);
            PreviewLabel.Name = "PreviewLabel";
            PreviewLabel.Size = new Size(49, 13);
            PreviewLabel.TabIndex = 4;
            PreviewLabel.Text = "Preview:";
            // 
            // SPRBox3
            // 
            SPRBox3.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
            SPRBox3.BorderStyle = BorderStyle.FixedSingle;
            SPRBox3.Location = new Point(219, 325);
            SPRBox3.Name = "SPRBox3";
            SPRBox3.Size = new Size(34, 100);
            SPRBox3.SizeMode = PictureBoxSizeMode.Zoom;
            SPRBox3.TabIndex = 2;
            SPRBox3.TabStop = false;
            // 
            // SPRBox2
            // 
            SPRBox2.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
            SPRBox2.BorderStyle = BorderStyle.FixedSingle;
            SPRBox2.Location = new Point(145, 229);
            SPRBox2.Name = "SPRBox2";
            SPRBox2.Size = new Size(68, 196);
            SPRBox2.SizeMode = PictureBoxSizeMode.Zoom;
            SPRBox2.TabIndex = 1;
            SPRBox2.TabStop = false;
            // 
            // SPRBox1
            // 
            SPRBox1.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
            SPRBox1.BorderStyle = BorderStyle.FixedSingle;
            SPRBox1.Location = new Point(3, 37);
            SPRBox1.Name = "SPRBox1";
            SPRBox1.Size = new Size(136, 388);
            SPRBox1.SizeMode = PictureBoxSizeMode.Zoom;
            SPRBox1.TabIndex = 0;
            SPRBox1.TabStop = false;
            // 
            // FrameList
            // 
            FrameList.FormattingEnabled = true;
            FrameList.IntegralHeight = false;
            FrameList.Location = new Point(269, 37);
            FrameList.Name = "FrameList";
            FrameList.Size = new Size(149, 305);
            FrameList.TabIndex = 5;
            FrameList.SelectedIndexChanged += FrameList_SelectedIndexChanged;
            // 
            // FramesLabel
            // 
            FramesLabel.AutoSize = true;
            FramesLabel.Location = new Point(266, 21);
            FramesLabel.Name = "FramesLabel";
            FramesLabel.Size = new Size(60, 13);
            FramesLabel.TabIndex = 6;
            FramesLabel.Text = "Rotations:";
            // 
            // NewButton
            // 
            NewButton.Location = new Point(424, 37);
            NewButton.Name = "NewButton";
            NewButton.Size = new Size(75, 23);
            NewButton.TabIndex = 7;
            NewButton.Text = "New";
            NewButton.UseVisualStyleBackColor = true;
            NewButton.Click += NewButton_Click;
            // 
            // ImportButton
            // 
            ImportButton.Location = new Point(424, 66);
            ImportButton.Name = "ImportButton";
            ImportButton.Size = new Size(75, 23);
            ImportButton.TabIndex = 8;
            ImportButton.Text = "Import";
            ImportButton.UseVisualStyleBackColor = true;
            ImportButton.Click += ImportButton_Click;
            // 
            // ExportButton
            // 
            ExportButton.Location = new Point(424, 95);
            ExportButton.Name = "ExportButton";
            ExportButton.Size = new Size(75, 23);
            ExportButton.TabIndex = 9;
            ExportButton.Text = "Export";
            ExportButton.UseVisualStyleBackColor = true;
            ExportButton.Click += ExportButton_Click;
            // 
            // DeleteButton
            // 
            DeleteButton.Location = new Point(424, 124);
            DeleteButton.Name = "DeleteButton";
            DeleteButton.Size = new Size(75, 23);
            DeleteButton.TabIndex = 10;
            DeleteButton.Text = "Delete";
            DeleteButton.UseVisualStyleBackColor = true;
            DeleteButton.Click += DeleteButton_Click;
            // 
            // ExportAll
            // 
            ExportAll.Location = new Point(269, 377);
            ExportAll.Name = "ExportAll";
            ExportAll.Size = new Size(149, 23);
            ExportAll.TabIndex = 11;
            ExportAll.Text = "Export All";
            ExportAll.UseVisualStyleBackColor = true;
            ExportAll.Click += ExportAll_Click;
            // 
            // ImportAll
            // 
            ImportAll.Location = new Point(269, 348);
            ImportAll.Name = "ImportAll";
            ImportAll.Size = new Size(149, 23);
            ImportAll.TabIndex = 12;
            ImportAll.Text = "Import All";
            ImportAll.UseVisualStyleBackColor = true;
            ImportAll.Click += ImportAll_Click;
            // 
            // AutoZooms
            // 
            AutoZooms.AutoSize = true;
            AutoZooms.Checked = true;
            AutoZooms.CheckState = CheckState.Checked;
            AutoZooms.Location = new Point(250, 0);
            AutoZooms.Name = "AutoZooms";
            AutoZooms.Size = new Size(269, 17);
            AutoZooms.TabIndex = 13;
            AutoZooms.Text = "Automatically Generate Medium and Far Zooms";
            AutoZooms.UseVisualStyleBackColor = true;
            AutoZooms.CheckedChanged += AutoZooms_CheckedChanged;
            // 
            // SPRSelector
            // 
            SPRSelector.Location = new Point(269, 406);
            SPRSelector.Name = "SPRSelector";
            SPRSelector.Size = new Size(230, 46);
            SPRSelector.TabIndex = 14;
            // 
            // SheetImport
            // 
            SheetImport.Location = new Point(424, 348);
            SheetImport.Name = "SheetImport";
            SheetImport.Size = new Size(75, 52);
            SheetImport.TabIndex = 15;
            SheetImport.Text = "Import from TGA Sheet";
            SheetImport.UseVisualStyleBackColor = true;
            SheetImport.Click += SheetImport_Click;
            // 
            // SPR2ResourceControl
            // 
            AutoScaleDimensions = new SizeF(96F, 96F);
            AutoScaleMode = AutoScaleMode.Dpi;
            Controls.Add(SheetImport);
            Controls.Add(SPRSelector);
            Controls.Add(AutoZooms);
            Controls.Add(ImportAll);
            Controls.Add(ExportAll);
            Controls.Add(DeleteButton);
            Controls.Add(ExportButton);
            Controls.Add(ImportButton);
            Controls.Add(NewButton);
            Controls.Add(FramesLabel);
            Controls.Add(FrameList);
            Controls.Add(PreviewLabel);
            Controls.Add(ModeCombo);
            Controls.Add(SPRBox3);
            Controls.Add(SPRBox2);
            Controls.Add(SPRBox1);
            Name = "SPR2ResourceControl";
            Size = new Size(502, 455);
            ((System.ComponentModel.ISupportInitialize)SPRBox3).EndInit();
            ((System.ComponentModel.ISupportInitialize)SPRBox2).EndInit();
            ((System.ComponentModel.ISupportInitialize)SPRBox1).EndInit();
            ResumeLayout(false);
            PerformLayout();

        }

        #endregion

        private System.Windows.Forms.PictureBox SPRBox1;
        private System.Windows.Forms.PictureBox SPRBox2;
        private System.Windows.Forms.PictureBox SPRBox3;
        private System.Windows.Forms.ComboBox ModeCombo;
        private System.Windows.Forms.Label PreviewLabel;
        private System.Windows.Forms.ListBox FrameList;
        private System.Windows.Forms.Label FramesLabel;
        private System.Windows.Forms.Button NewButton;
        private System.Windows.Forms.Button ImportButton;
        private System.Windows.Forms.Button ExportButton;
        private System.Windows.Forms.Button DeleteButton;
        private System.Windows.Forms.Button ExportAll;
        private System.Windows.Forms.Button ImportAll;
        private System.Windows.Forms.CheckBox AutoZooms;
        private OBJDSelectorControl SPRSelector;
        private System.Windows.Forms.Button SheetImport;
    }
}
