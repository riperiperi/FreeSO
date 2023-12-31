namespace FSO.IDE.ResourceBrowser.ResourceEditors
{
    partial class SPRResourceControl
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
            this.ModeCombo = new System.Windows.Forms.ComboBox();
            this.PreviewLabel = new System.Windows.Forms.Label();
            this.SPRBox3 = new System.Windows.Forms.PictureBox();
            this.SPRBox2 = new System.Windows.Forms.PictureBox();
            this.SPRBox1 = new System.Windows.Forms.PictureBox();
            this.FrameList = new System.Windows.Forms.ListBox();
            this.FramesLabel = new System.Windows.Forms.Label();
            this.NewButton = new System.Windows.Forms.Button();
            this.ImportButton = new System.Windows.Forms.Button();
            this.ExportButton = new System.Windows.Forms.Button();
            this.DeleteButton = new System.Windows.Forms.Button();
            this.ExportAll = new System.Windows.Forms.Button();
            this.ImportAll = new System.Windows.Forms.Button();
            this.AutoZooms = new System.Windows.Forms.CheckBox();
            this.SheetImport = new System.Windows.Forms.Button();
            this.FramesButton = new System.Windows.Forms.Button();
            this.SPRSelector = new FSO.IDE.ResourceBrowser.OBJDSelectorControl();
            ((System.ComponentModel.ISupportInitialize)(this.SPRBox3)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.SPRBox2)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.SPRBox1)).BeginInit();
            this.SuspendLayout();
            // 
            // ModeCombo
            // 
            this.ModeCombo.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.ModeCombo.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.ModeCombo.FormattingEnabled = true;
            this.ModeCombo.Items.AddRange(new object[] {
            "Color",
            "Alpha",
            "Z-Buffer"});
            this.ModeCombo.Location = new System.Drawing.Point(3, 431);
            this.ModeCombo.Name = "ModeCombo";
            this.ModeCombo.Size = new System.Drawing.Size(250, 21);
            this.ModeCombo.TabIndex = 3;
            this.ModeCombo.SelectedIndexChanged += new System.EventHandler(this.comboBox1_SelectedIndexChanged);
            // 
            // PreviewLabel
            // 
            this.PreviewLabel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.PreviewLabel.AutoSize = true;
            this.PreviewLabel.Location = new System.Drawing.Point(3, 21);
            this.PreviewLabel.Name = "PreviewLabel";
            this.PreviewLabel.Size = new System.Drawing.Size(48, 13);
            this.PreviewLabel.TabIndex = 4;
            this.PreviewLabel.Text = "Preview:";
            // 
            // SPRBox3
            // 
            this.SPRBox3.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.SPRBox3.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.SPRBox3.Location = new System.Drawing.Point(219, 325);
            this.SPRBox3.Name = "SPRBox3";
            this.SPRBox3.Size = new System.Drawing.Size(34, 100);
            this.SPRBox3.TabIndex = 2;
            this.SPRBox3.TabStop = false;
            // 
            // SPRBox2
            // 
            this.SPRBox2.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.SPRBox2.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.SPRBox2.Location = new System.Drawing.Point(145, 229);
            this.SPRBox2.Name = "SPRBox2";
            this.SPRBox2.Size = new System.Drawing.Size(68, 196);
            this.SPRBox2.TabIndex = 1;
            this.SPRBox2.TabStop = false;
            // 
            // SPRBox1
            // 
            this.SPRBox1.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.SPRBox1.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.SPRBox1.Location = new System.Drawing.Point(3, 37);
            this.SPRBox1.Name = "SPRBox1";
            this.SPRBox1.Size = new System.Drawing.Size(136, 388);
            this.SPRBox1.TabIndex = 0;
            this.SPRBox1.TabStop = false;
            // 
            // FrameList
            // 
            this.FrameList.FormattingEnabled = true;
            this.FrameList.IntegralHeight = false;
            this.FrameList.Location = new System.Drawing.Point(269, 37);
            this.FrameList.Name = "FrameList";
            this.FrameList.Size = new System.Drawing.Size(149, 305);
            this.FrameList.TabIndex = 5;
            this.FrameList.SelectedIndexChanged += new System.EventHandler(this.FrameList_SelectedIndexChanged);
            // 
            // FramesLabel
            // 
            this.FramesLabel.AutoSize = true;
            this.FramesLabel.Location = new System.Drawing.Point(266, 21);
            this.FramesLabel.Name = "FramesLabel";
            this.FramesLabel.Size = new System.Drawing.Size(55, 13);
            this.FramesLabel.TabIndex = 6;
            this.FramesLabel.Text = "Rotations:";
            // 
            // NewButton
            // 
            this.NewButton.Enabled = false;
            this.NewButton.Location = new System.Drawing.Point(424, 37);
            this.NewButton.Name = "NewButton";
            this.NewButton.Size = new System.Drawing.Size(75, 23);
            this.NewButton.TabIndex = 7;
            this.NewButton.Text = "New";
            this.NewButton.UseVisualStyleBackColor = true;
            this.NewButton.Click += new System.EventHandler(this.NewButton_Click);
            // 
            // ImportButton
            // 
            this.ImportButton.Enabled = false;
            this.ImportButton.Location = new System.Drawing.Point(424, 66);
            this.ImportButton.Name = "ImportButton";
            this.ImportButton.Size = new System.Drawing.Size(75, 23);
            this.ImportButton.TabIndex = 8;
            this.ImportButton.Text = "Import";
            this.ImportButton.UseVisualStyleBackColor = true;
            this.ImportButton.Click += new System.EventHandler(this.ImportButton_Click);
            // 
            // ExportButton
            // 
            this.ExportButton.Location = new System.Drawing.Point(424, 95);
            this.ExportButton.Name = "ExportButton";
            this.ExportButton.Size = new System.Drawing.Size(75, 23);
            this.ExportButton.TabIndex = 9;
            this.ExportButton.Text = "Export";
            this.ExportButton.UseVisualStyleBackColor = true;
            this.ExportButton.Click += new System.EventHandler(this.ExportButton_Click);
            // 
            // DeleteButton
            // 
            this.DeleteButton.Enabled = false;
            this.DeleteButton.Location = new System.Drawing.Point(424, 124);
            this.DeleteButton.Name = "DeleteButton";
            this.DeleteButton.Size = new System.Drawing.Size(75, 23);
            this.DeleteButton.TabIndex = 10;
            this.DeleteButton.Text = "Delete";
            this.DeleteButton.UseVisualStyleBackColor = true;
            this.DeleteButton.Click += new System.EventHandler(this.DeleteButton_Click);
            // 
            // ExportAll
            // 
            this.ExportAll.Location = new System.Drawing.Point(269, 377);
            this.ExportAll.Name = "ExportAll";
            this.ExportAll.Size = new System.Drawing.Size(149, 23);
            this.ExportAll.TabIndex = 11;
            this.ExportAll.Text = "Export All";
            this.ExportAll.UseVisualStyleBackColor = true;
            this.ExportAll.Click += new System.EventHandler(this.ExportAll_Click);
            // 
            // ImportAll
            // 
            this.ImportAll.Enabled = false;
            this.ImportAll.Location = new System.Drawing.Point(269, 348);
            this.ImportAll.Name = "ImportAll";
            this.ImportAll.Size = new System.Drawing.Size(149, 23);
            this.ImportAll.TabIndex = 12;
            this.ImportAll.Text = "Import All";
            this.ImportAll.UseVisualStyleBackColor = true;
            this.ImportAll.Click += new System.EventHandler(this.ImportAll_Click);
            // 
            // AutoZooms
            // 
            this.AutoZooms.AutoSize = true;
            this.AutoZooms.Checked = true;
            this.AutoZooms.CheckState = System.Windows.Forms.CheckState.Checked;
            this.AutoZooms.Location = new System.Drawing.Point(250, 0);
            this.AutoZooms.Name = "AutoZooms";
            this.AutoZooms.Size = new System.Drawing.Size(249, 17);
            this.AutoZooms.TabIndex = 13;
            this.AutoZooms.Text = "Automatically Generate Medium and Far Zooms";
            this.AutoZooms.UseVisualStyleBackColor = true;
            this.AutoZooms.CheckedChanged += new System.EventHandler(this.AutoZooms_CheckedChanged);
            // 
            // SheetImport
            // 
            this.SheetImport.Enabled = false;
            this.SheetImport.Location = new System.Drawing.Point(424, 348);
            this.SheetImport.Name = "SheetImport";
            this.SheetImport.Size = new System.Drawing.Size(75, 52);
            this.SheetImport.TabIndex = 15;
            this.SheetImport.Text = "Import from TGA Sheet";
            this.SheetImport.UseVisualStyleBackColor = true;
            this.SheetImport.Click += new System.EventHandler(this.SheetImport_Click);
            // 
            // FramesButton
            // 
            this.FramesButton.Location = new System.Drawing.Point(424, 153);
            this.FramesButton.Name = "FramesButton";
            this.FramesButton.Size = new System.Drawing.Size(75, 23);
            this.FramesButton.TabIndex = 16;
            this.FramesButton.Text = "Frames";
            this.FramesButton.UseVisualStyleBackColor = true;
            this.FramesButton.Click += new System.EventHandler(this.FramesButton_Click);
            // 
            // SPRSelector
            // 
            this.SPRSelector.Location = new System.Drawing.Point(269, 406);
            this.SPRSelector.Name = "SPRSelector";
            this.SPRSelector.Size = new System.Drawing.Size(230, 46);
            this.SPRSelector.TabIndex = 14;
            // 
            // SPRResourceControl
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.FramesButton);
            this.Controls.Add(this.SheetImport);
            this.Controls.Add(this.SPRSelector);
            this.Controls.Add(this.AutoZooms);
            this.Controls.Add(this.ImportAll);
            this.Controls.Add(this.ExportAll);
            this.Controls.Add(this.DeleteButton);
            this.Controls.Add(this.ExportButton);
            this.Controls.Add(this.ImportButton);
            this.Controls.Add(this.NewButton);
            this.Controls.Add(this.FramesLabel);
            this.Controls.Add(this.FrameList);
            this.Controls.Add(this.PreviewLabel);
            this.Controls.Add(this.ModeCombo);
            this.Controls.Add(this.SPRBox3);
            this.Controls.Add(this.SPRBox2);
            this.Controls.Add(this.SPRBox1);
            this.Name = "SPRResourceControl";
            this.Size = new System.Drawing.Size(502, 455);
            ((System.ComponentModel.ISupportInitialize)(this.SPRBox3)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.SPRBox2)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.SPRBox1)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

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
        private System.Windows.Forms.Button FramesButton;
    }
}
