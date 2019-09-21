namespace FSO.IDE.AvatarUtils
{
    partial class AddAppearanceWindow
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(AddAppearanceWindow));
            this.AddAsLabel = new System.Windows.Forms.Label();
            this.InfoLabel = new System.Windows.Forms.Label();
            this.NameEntry = new System.Windows.Forms.TextBox();
            this.AppearanceRadio = new System.Windows.Forms.RadioButton();
            this.OutfitRadio = new System.Windows.Forms.RadioButton();
            this.HandgroupRadio = new System.Windows.Forms.RadioButton();
            this.NameLabel = new System.Windows.Forms.Label();
            this.HandgroupCombo = new System.Windows.Forms.ComboBox();
            this.HandgroupLabel = new System.Windows.Forms.Label();
            this.SummaryText = new System.Windows.Forms.TextBox();
            this.ImportButton = new System.Windows.Forms.Button();
            this.HeadRadio = new System.Windows.Forms.RadioButton();
            this.SuspendLayout();
            // 
            // AddAsLabel
            // 
            this.AddAsLabel.AutoSize = true;
            this.AddAsLabel.Location = new System.Drawing.Point(12, 89);
            this.AddAsLabel.Name = "AddAsLabel";
            this.AddAsLabel.Size = new System.Drawing.Size(43, 13);
            this.AddAsLabel.TabIndex = 0;
            this.AddAsLabel.Text = "Add as:";
            // 
            // InfoLabel
            // 
            this.InfoLabel.Location = new System.Drawing.Point(12, 9);
            this.InfoLabel.Name = "InfoLabel";
            this.InfoLabel.Size = new System.Drawing.Size(396, 75);
            this.InfoLabel.TabIndex = 1;
            this.InfoLabel.Text = resources.GetString("InfoLabel.Text");
            // 
            // NameEntry
            // 
            this.NameEntry.Location = new System.Drawing.Point(15, 251);
            this.NameEntry.Name = "NameEntry";
            this.NameEntry.Size = new System.Drawing.Size(213, 20);
            this.NameEntry.TabIndex = 2;
            this.NameEntry.TextChanged += new System.EventHandler(this.NameEntry_TextChanged);
            // 
            // AppearanceRadio
            // 
            this.AppearanceRadio.AutoSize = true;
            this.AppearanceRadio.Checked = true;
            this.AppearanceRadio.Location = new System.Drawing.Point(64, 87);
            this.AppearanceRadio.Name = "AppearanceRadio";
            this.AppearanceRadio.Size = new System.Drawing.Size(83, 17);
            this.AppearanceRadio.TabIndex = 3;
            this.AppearanceRadio.TabStop = true;
            this.AppearanceRadio.Text = "Appearance";
            this.AppearanceRadio.UseVisualStyleBackColor = true;
            this.AppearanceRadio.CheckedChanged += new System.EventHandler(this.AppearanceRadio_CheckedChanged);
            // 
            // OutfitRadio
            // 
            this.OutfitRadio.AutoSize = true;
            this.OutfitRadio.Location = new System.Drawing.Point(153, 87);
            this.OutfitRadio.Name = "OutfitRadio";
            this.OutfitRadio.Size = new System.Drawing.Size(50, 17);
            this.OutfitRadio.TabIndex = 4;
            this.OutfitRadio.Text = "Outfit";
            this.OutfitRadio.UseVisualStyleBackColor = true;
            this.OutfitRadio.CheckedChanged += new System.EventHandler(this.OutfitRadio_CheckedChanged);
            // 
            // HandgroupRadio
            // 
            this.HandgroupRadio.AutoSize = true;
            this.HandgroupRadio.Location = new System.Drawing.Point(266, 87);
            this.HandgroupRadio.Name = "HandgroupRadio";
            this.HandgroupRadio.Size = new System.Drawing.Size(78, 17);
            this.HandgroupRadio.TabIndex = 5;
            this.HandgroupRadio.Text = "Handgroup";
            this.HandgroupRadio.UseVisualStyleBackColor = true;
            this.HandgroupRadio.CheckedChanged += new System.EventHandler(this.HandgroupRadio_CheckedChanged);
            // 
            // NameLabel
            // 
            this.NameLabel.AutoSize = true;
            this.NameLabel.Location = new System.Drawing.Point(12, 235);
            this.NameLabel.Name = "NameLabel";
            this.NameLabel.Size = new System.Drawing.Size(38, 13);
            this.NameLabel.TabIndex = 6;
            this.NameLabel.Text = "Name:";
            // 
            // HandgroupCombo
            // 
            this.HandgroupCombo.FormattingEnabled = true;
            this.HandgroupCombo.Location = new System.Drawing.Point(248, 250);
            this.HandgroupCombo.Name = "HandgroupCombo";
            this.HandgroupCombo.Size = new System.Drawing.Size(160, 21);
            this.HandgroupCombo.TabIndex = 7;
            // 
            // HandgroupLabel
            // 
            this.HandgroupLabel.AutoSize = true;
            this.HandgroupLabel.Location = new System.Drawing.Point(245, 234);
            this.HandgroupLabel.Name = "HandgroupLabel";
            this.HandgroupLabel.Size = new System.Drawing.Size(63, 13);
            this.HandgroupLabel.TabIndex = 8;
            this.HandgroupLabel.Text = "Handgroup:";
            // 
            // SummaryText
            // 
            this.SummaryText.Location = new System.Drawing.Point(15, 110);
            this.SummaryText.Multiline = true;
            this.SummaryText.Name = "SummaryText";
            this.SummaryText.ReadOnly = true;
            this.SummaryText.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
            this.SummaryText.Size = new System.Drawing.Size(393, 117);
            this.SummaryText.TabIndex = 9;
            // 
            // ImportButton
            // 
            this.ImportButton.Location = new System.Drawing.Point(333, 277);
            this.ImportButton.Name = "ImportButton";
            this.ImportButton.Size = new System.Drawing.Size(75, 23);
            this.ImportButton.TabIndex = 10;
            this.ImportButton.Text = "Import";
            this.ImportButton.UseVisualStyleBackColor = true;
            this.ImportButton.Click += new System.EventHandler(this.ImportButton_Click);
            // 
            // HeadRadio
            // 
            this.HeadRadio.AutoSize = true;
            this.HeadRadio.Location = new System.Drawing.Point(209, 87);
            this.HeadRadio.Name = "HeadRadio";
            this.HeadRadio.Size = new System.Drawing.Size(51, 17);
            this.HeadRadio.TabIndex = 11;
            this.HeadRadio.TabStop = true;
            this.HeadRadio.Text = "Head";
            this.HeadRadio.UseVisualStyleBackColor = true;
            this.HeadRadio.CheckedChanged += new System.EventHandler(this.HeadRadio_CheckedChanged);
            // 
            // AddAppearanceWindow
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(420, 308);
            this.Controls.Add(this.HeadRadio);
            this.Controls.Add(this.ImportButton);
            this.Controls.Add(this.SummaryText);
            this.Controls.Add(this.HandgroupLabel);
            this.Controls.Add(this.HandgroupCombo);
            this.Controls.Add(this.NameLabel);
            this.Controls.Add(this.HandgroupRadio);
            this.Controls.Add(this.OutfitRadio);
            this.Controls.Add(this.AppearanceRadio);
            this.Controls.Add(this.NameEntry);
            this.Controls.Add(this.InfoLabel);
            this.Controls.Add(this.AddAsLabel);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "AddAppearanceWindow";
            this.Text = "Import Meshes...";
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Label AddAsLabel;
        private System.Windows.Forms.Label InfoLabel;
        private System.Windows.Forms.TextBox NameEntry;
        private System.Windows.Forms.RadioButton AppearanceRadio;
        private System.Windows.Forms.RadioButton OutfitRadio;
        private System.Windows.Forms.RadioButton HandgroupRadio;
        private System.Windows.Forms.Label NameLabel;
        private System.Windows.Forms.ComboBox HandgroupCombo;
        private System.Windows.Forms.Label HandgroupLabel;
        private System.Windows.Forms.TextBox SummaryText;
        private System.Windows.Forms.Button ImportButton;
        private System.Windows.Forms.RadioButton HeadRadio;
    }
}