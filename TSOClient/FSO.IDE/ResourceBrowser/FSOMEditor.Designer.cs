namespace FSO.IDE.ResourceBrowser
{
    partial class FSOMEditor
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
            this.DGRPBox = new System.Windows.Forms.GroupBox();
            this.DGRPList = new System.Windows.Forms.ListView();
            this.IndexColumn = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.NameColumn = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.ReconBox = new System.Windows.Forms.GroupBox();
            this.BlenderCheck = new System.Windows.Forms.CheckBox();
            this.SimpleCheck = new System.Windows.Forms.CheckBox();
            this.ExportButton2 = new System.Windows.Forms.Button();
            this.ToCustom = new System.Windows.Forms.Button();
            this.DoorCheck = new System.Windows.Forms.CheckBox();
            this.RotLabel = new System.Windows.Forms.Label();
            this.Rot4 = new System.Windows.Forms.CheckBox();
            this.CounterCheck = new System.Windows.Forms.CheckBox();
            this.TweakLabel = new System.Windows.Forms.Label();
            this.Rot1 = new System.Windows.Forms.CheckBox();
            this.Rot2 = new System.Windows.Forms.CheckBox();
            this.Rot3 = new System.Windows.Forms.CheckBox();
            this.CustomBox = new System.Windows.Forms.GroupBox();
            this.EmptyButton = new System.Windows.Forms.Button();
            this.HelpButton = new System.Windows.Forms.Button();
            this.ImportButton = new System.Windows.Forms.Button();
            this.ExportButton = new System.Windows.Forms.Button();
            this.ToRecon = new System.Windows.Forms.Button();
            this.IffCheck = new System.Windows.Forms.CheckBox();
            this.Debug3D = new FSO.IDE.Common.Debug3DControl();
            this.DGRPBox.SuspendLayout();
            this.ReconBox.SuspendLayout();
            this.CustomBox.SuspendLayout();
            this.SuspendLayout();
            // 
            // DGRPBox
            // 
            this.DGRPBox.Controls.Add(this.DGRPList);
            this.DGRPBox.Controls.Add(this.ReconBox);
            this.DGRPBox.Controls.Add(this.CustomBox);
            this.DGRPBox.Location = new System.Drawing.Point(3, 22);
            this.DGRPBox.Name = "DGRPBox";
            this.DGRPBox.Size = new System.Drawing.Size(200, 434);
            this.DGRPBox.TabIndex = 7;
            this.DGRPBox.TabStop = false;
            this.DGRPBox.Text = "Drawgroups";
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
            this.DGRPList.Size = new System.Drawing.Size(188, 253);
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
            // ReconBox
            // 
            this.ReconBox.Controls.Add(this.BlenderCheck);
            this.ReconBox.Controls.Add(this.SimpleCheck);
            this.ReconBox.Controls.Add(this.ExportButton2);
            this.ReconBox.Controls.Add(this.ToCustom);
            this.ReconBox.Controls.Add(this.DoorCheck);
            this.ReconBox.Controls.Add(this.RotLabel);
            this.ReconBox.Controls.Add(this.Rot4);
            this.ReconBox.Controls.Add(this.CounterCheck);
            this.ReconBox.Controls.Add(this.TweakLabel);
            this.ReconBox.Controls.Add(this.Rot1);
            this.ReconBox.Controls.Add(this.Rot2);
            this.ReconBox.Controls.Add(this.Rot3);
            this.ReconBox.Location = new System.Drawing.Point(6, 278);
            this.ReconBox.Name = "ReconBox";
            this.ReconBox.Size = new System.Drawing.Size(188, 150);
            this.ReconBox.TabIndex = 9;
            this.ReconBox.TabStop = false;
            this.ReconBox.Text = "Reconstruction";
            // 
            // BlenderCheck
            // 
            this.BlenderCheck.AutoSize = true;
            this.BlenderCheck.Location = new System.Drawing.Point(14, 102);
            this.BlenderCheck.Name = "BlenderCheck";
            this.BlenderCheck.Size = new System.Drawing.Size(106, 17);
            this.BlenderCheck.TabIndex = 19;
            this.BlenderCheck.Text = "Blender Imported";
            this.BlenderCheck.UseVisualStyleBackColor = true;
            this.BlenderCheck.CheckedChanged += new System.EventHandler(this.UpdateFSOR);
            // 
            // SimpleCheck
            // 
            this.SimpleCheck.AutoSize = true;
            this.SimpleCheck.Checked = true;
            this.SimpleCheck.CheckState = System.Windows.Forms.CheckState.Checked;
            this.SimpleCheck.Location = new System.Drawing.Point(9, 17);
            this.SimpleCheck.Name = "SimpleCheck";
            this.SimpleCheck.Size = new System.Drawing.Size(138, 17);
            this.SimpleCheck.TabIndex = 11;
            this.SimpleCheck.Text = "Use Mesh Simplification";
            this.SimpleCheck.UseVisualStyleBackColor = true;
            this.SimpleCheck.CheckedChanged += new System.EventHandler(this.UpdateFSOR);
            // 
            // ExportButton2
            // 
            this.ExportButton2.Location = new System.Drawing.Point(97, 120);
            this.ExportButton2.Name = "ExportButton2";
            this.ExportButton2.Size = new System.Drawing.Size(85, 23);
            this.ExportButton2.TabIndex = 18;
            this.ExportButton2.Text = "Export .OBJ";
            this.ExportButton2.UseVisualStyleBackColor = true;
            this.ExportButton2.Click += new System.EventHandler(this.ExportButton_Click);
            // 
            // ToCustom
            // 
            this.ToCustom.Location = new System.Drawing.Point(9, 120);
            this.ToCustom.Name = "ToCustom";
            this.ToCustom.Size = new System.Drawing.Size(84, 23);
            this.ToCustom.TabIndex = 18;
            this.ToCustom.Text = "Use Custom...";
            this.ToCustom.UseVisualStyleBackColor = true;
            this.ToCustom.Click += new System.EventHandler(this.ToCustom_Click);
            // 
            // DoorCheck
            // 
            this.DoorCheck.AutoSize = true;
            this.DoorCheck.Location = new System.Drawing.Point(99, 86);
            this.DoorCheck.Name = "DoorCheck";
            this.DoorCheck.Size = new System.Drawing.Size(65, 17);
            this.DoorCheck.TabIndex = 16;
            this.DoorCheck.Text = "Door Fix";
            this.DoorCheck.UseVisualStyleBackColor = true;
            this.DoorCheck.CheckedChanged += new System.EventHandler(this.UpdateFSOR);
            // 
            // RotLabel
            // 
            this.RotLabel.AutoSize = true;
            this.RotLabel.Location = new System.Drawing.Point(6, 35);
            this.RotLabel.Name = "RotLabel";
            this.RotLabel.Size = new System.Drawing.Size(52, 13);
            this.RotLabel.TabIndex = 8;
            this.RotLabel.Text = "Rotations";
            // 
            // Rot4
            // 
            this.Rot4.AutoSize = true;
            this.Rot4.Checked = true;
            this.Rot4.CheckState = System.Windows.Forms.CheckState.Checked;
            this.Rot4.Location = new System.Drawing.Point(128, 50);
            this.Rot4.Name = "Rot4";
            this.Rot4.Size = new System.Drawing.Size(32, 17);
            this.Rot4.TabIndex = 13;
            this.Rot4.Text = "4";
            this.Rot4.UseVisualStyleBackColor = true;
            this.Rot4.CheckedChanged += new System.EventHandler(this.UpdateFSOR);
            // 
            // CounterCheck
            // 
            this.CounterCheck.AutoSize = true;
            this.CounterCheck.Location = new System.Drawing.Point(14, 86);
            this.CounterCheck.Name = "CounterCheck";
            this.CounterCheck.Size = new System.Drawing.Size(79, 17);
            this.CounterCheck.TabIndex = 15;
            this.CounterCheck.Text = "Counter Fix";
            this.CounterCheck.UseVisualStyleBackColor = true;
            this.CounterCheck.CheckedChanged += new System.EventHandler(this.UpdateFSOR);
            // 
            // TweakLabel
            // 
            this.TweakLabel.AutoSize = true;
            this.TweakLabel.Location = new System.Drawing.Point(6, 70);
            this.TweakLabel.Name = "TweakLabel";
            this.TweakLabel.Size = new System.Drawing.Size(45, 13);
            this.TweakLabel.TabIndex = 14;
            this.TweakLabel.Text = "Tweaks";
            // 
            // Rot1
            // 
            this.Rot1.AutoSize = true;
            this.Rot1.Checked = true;
            this.Rot1.CheckState = System.Windows.Forms.CheckState.Checked;
            this.Rot1.Location = new System.Drawing.Point(14, 50);
            this.Rot1.Name = "Rot1";
            this.Rot1.Size = new System.Drawing.Size(32, 17);
            this.Rot1.TabIndex = 8;
            this.Rot1.Text = "1";
            this.Rot1.UseVisualStyleBackColor = true;
            this.Rot1.CheckedChanged += new System.EventHandler(this.UpdateFSOR);
            // 
            // Rot2
            // 
            this.Rot2.AutoSize = true;
            this.Rot2.Checked = true;
            this.Rot2.CheckState = System.Windows.Forms.CheckState.Checked;
            this.Rot2.Location = new System.Drawing.Point(52, 50);
            this.Rot2.Name = "Rot2";
            this.Rot2.Size = new System.Drawing.Size(32, 17);
            this.Rot2.TabIndex = 11;
            this.Rot2.Text = "2";
            this.Rot2.UseVisualStyleBackColor = true;
            this.Rot2.CheckedChanged += new System.EventHandler(this.UpdateFSOR);
            // 
            // Rot3
            // 
            this.Rot3.AutoSize = true;
            this.Rot3.Checked = true;
            this.Rot3.CheckState = System.Windows.Forms.CheckState.Checked;
            this.Rot3.Location = new System.Drawing.Point(90, 50);
            this.Rot3.Name = "Rot3";
            this.Rot3.Size = new System.Drawing.Size(32, 17);
            this.Rot3.TabIndex = 12;
            this.Rot3.Text = "3";
            this.Rot3.UseVisualStyleBackColor = true;
            this.Rot3.CheckedChanged += new System.EventHandler(this.UpdateFSOR);
            // 
            // CustomBox
            // 
            this.CustomBox.Controls.Add(this.EmptyButton);
            this.CustomBox.Controls.Add(this.HelpButton);
            this.CustomBox.Controls.Add(this.ImportButton);
            this.CustomBox.Controls.Add(this.ExportButton);
            this.CustomBox.Controls.Add(this.ToRecon);
            this.CustomBox.Location = new System.Drawing.Point(6, 278);
            this.CustomBox.Name = "CustomBox";
            this.CustomBox.Size = new System.Drawing.Size(188, 150);
            this.CustomBox.TabIndex = 10;
            this.CustomBox.TabStop = false;
            this.CustomBox.Text = "Custom Mesh";
            // 
            // EmptyButton
            // 
            this.EmptyButton.Location = new System.Drawing.Point(6, 44);
            this.EmptyButton.Name = "EmptyButton";
            this.EmptyButton.Size = new System.Drawing.Size(85, 23);
            this.EmptyButton.TabIndex = 18;
            this.EmptyButton.Text = "Make Empty";
            this.EmptyButton.UseVisualStyleBackColor = true;
            this.EmptyButton.Click += new System.EventHandler(this.EmptyButton_Click);
            // 
            // HelpButton
            // 
            this.HelpButton.Location = new System.Drawing.Point(124, 120);
            this.HelpButton.Name = "HelpButton";
            this.HelpButton.Size = new System.Drawing.Size(58, 23);
            this.HelpButton.TabIndex = 17;
            this.HelpButton.Text = "Help";
            this.HelpButton.UseVisualStyleBackColor = true;
            this.HelpButton.Click += new System.EventHandler(this.HelpButton_Click);
            // 
            // ImportButton
            // 
            this.ImportButton.Location = new System.Drawing.Point(6, 17);
            this.ImportButton.Name = "ImportButton";
            this.ImportButton.Size = new System.Drawing.Size(85, 23);
            this.ImportButton.TabIndex = 9;
            this.ImportButton.Text = "Import .OBJ";
            this.ImportButton.UseVisualStyleBackColor = true;
            this.ImportButton.Click += new System.EventHandler(this.ImportButton_Click);
            // 
            // ExportButton
            // 
            this.ExportButton.Location = new System.Drawing.Point(97, 17);
            this.ExportButton.Name = "ExportButton";
            this.ExportButton.Size = new System.Drawing.Size(85, 23);
            this.ExportButton.TabIndex = 8;
            this.ExportButton.Text = "Export .OBJ";
            this.ExportButton.UseVisualStyleBackColor = true;
            this.ExportButton.Click += new System.EventHandler(this.ExportButton_Click);
            // 
            // ToRecon
            // 
            this.ToRecon.Location = new System.Drawing.Point(6, 120);
            this.ToRecon.Name = "ToRecon";
            this.ToRecon.Size = new System.Drawing.Size(112, 23);
            this.ToRecon.TabIndex = 10;
            this.ToRecon.Text = "Use Reconstruction";
            this.ToRecon.UseVisualStyleBackColor = true;
            this.ToRecon.Click += new System.EventHandler(this.ToRecon_Click);
            // 
            // IffCheck
            // 
            this.IffCheck.AutoSize = true;
            this.IffCheck.Location = new System.Drawing.Point(4, 3);
            this.IffCheck.Name = "IffCheck";
            this.IffCheck.Size = new System.Drawing.Size(200, 17);
            this.IffCheck.TabIndex = 10;
            this.IffCheck.Text = "Save changes to IFF (FSOM, MTEX)";
            this.IffCheck.UseVisualStyleBackColor = true;
            this.IffCheck.CheckedChanged += new System.EventHandler(this.UpdateUseIFF);
            // 
            // Debug3D
            // 
            this.Debug3D.Location = new System.Drawing.Point(209, 0);
            this.Debug3D.Name = "Debug3D";
            this.Debug3D.Size = new System.Drawing.Size(553, 459);
            this.Debug3D.TabIndex = 8;
            // 
            // FSOMEditor
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.IffCheck);
            this.Controls.Add(this.Debug3D);
            this.Controls.Add(this.DGRPBox);
            this.Name = "FSOMEditor";
            this.Size = new System.Drawing.Size(762, 459);
            this.DGRPBox.ResumeLayout(false);
            this.ReconBox.ResumeLayout(false);
            this.ReconBox.PerformLayout();
            this.CustomBox.ResumeLayout(false);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.GroupBox DGRPBox;
        private System.Windows.Forms.ListView DGRPList;
        private System.Windows.Forms.ColumnHeader IndexColumn;
        private System.Windows.Forms.ColumnHeader NameColumn;
        private System.Windows.Forms.Button ExportButton;
        private System.Windows.Forms.Button ToRecon;
        private System.Windows.Forms.Button ImportButton;
        private System.Windows.Forms.Label TweakLabel;
        private System.Windows.Forms.CheckBox Rot4;
        private System.Windows.Forms.CheckBox Rot3;
        private System.Windows.Forms.CheckBox Rot2;
        private System.Windows.Forms.CheckBox Rot1;
        private System.Windows.Forms.Label RotLabel;
        private System.Windows.Forms.Button HelpButton;
        private Common.Debug3DControl Debug3D;
        private System.Windows.Forms.GroupBox ReconBox;
        private System.Windows.Forms.CheckBox IffCheck;
        private System.Windows.Forms.GroupBox CustomBox;
        private System.Windows.Forms.CheckBox DoorCheck;
        private System.Windows.Forms.CheckBox CounterCheck;
        private System.Windows.Forms.CheckBox SimpleCheck;
        private System.Windows.Forms.Button ExportButton2;
        private System.Windows.Forms.Button ToCustom;
        private System.Windows.Forms.Button EmptyButton;
        private System.Windows.Forms.CheckBox BlenderCheck;
    }
}
