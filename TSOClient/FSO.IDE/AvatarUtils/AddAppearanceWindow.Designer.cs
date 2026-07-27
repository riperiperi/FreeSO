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
            AddAsLabel = new Label();
            InfoLabel = new Label();
            NameEntry = new TextBox();
            AppearanceRadio = new RadioButton();
            OutfitRadio = new RadioButton();
            HandgroupRadio = new RadioButton();
            NameLabel = new Label();
            HandgroupCombo = new ComboBox();
            HandgroupLabel = new Label();
            SummaryText = new TextBox();
            ImportButton = new Button();
            HeadRadio = new RadioButton();
            SuspendLayout();
            // 
            // AddAsLabel
            // 
            AddAsLabel.AutoSize = true;
            AddAsLabel.Location = new Point(12, 89);
            AddAsLabel.Name = "AddAsLabel";
            AddAsLabel.Size = new Size(45, 13);
            AddAsLabel.TabIndex = 0;
            AddAsLabel.Text = "Add as:";
            // 
            // InfoLabel
            // 
            InfoLabel.Location = new Point(12, 9);
            InfoLabel.Name = "InfoLabel";
            InfoLabel.Size = new Size(396, 75);
            InfoLabel.TabIndex = 1;
            InfoLabel.Text = resources.GetString("InfoLabel.Text");
            // 
            // NameEntry
            // 
            NameEntry.Location = new Point(15, 251);
            NameEntry.Name = "NameEntry";
            NameEntry.Size = new Size(213, 22);
            NameEntry.TabIndex = 2;
            NameEntry.TextChanged += NameEntry_TextChanged;
            // 
            // AppearanceRadio
            // 
            AppearanceRadio.AutoSize = true;
            AppearanceRadio.Checked = true;
            AppearanceRadio.Location = new Point(64, 87);
            AppearanceRadio.Name = "AppearanceRadio";
            AppearanceRadio.Size = new Size(86, 17);
            AppearanceRadio.TabIndex = 3;
            AppearanceRadio.TabStop = true;
            AppearanceRadio.Text = "Appearance";
            AppearanceRadio.UseVisualStyleBackColor = true;
            AppearanceRadio.CheckedChanged += AppearanceRadio_CheckedChanged;
            // 
            // OutfitRadio
            // 
            OutfitRadio.AutoSize = true;
            OutfitRadio.Location = new Point(153, 87);
            OutfitRadio.Name = "OutfitRadio";
            OutfitRadio.Size = new Size(56, 17);
            OutfitRadio.TabIndex = 4;
            OutfitRadio.Text = "Outfit";
            OutfitRadio.UseVisualStyleBackColor = true;
            OutfitRadio.CheckedChanged += OutfitRadio_CheckedChanged;
            // 
            // HandgroupRadio
            // 
            HandgroupRadio.AutoSize = true;
            HandgroupRadio.Location = new Point(266, 87);
            HandgroupRadio.Name = "HandgroupRadio";
            HandgroupRadio.Size = new Size(85, 17);
            HandgroupRadio.TabIndex = 5;
            HandgroupRadio.Text = "Handgroup";
            HandgroupRadio.UseVisualStyleBackColor = true;
            HandgroupRadio.CheckedChanged += HandgroupRadio_CheckedChanged;
            // 
            // NameLabel
            // 
            NameLabel.AutoSize = true;
            NameLabel.Location = new Point(12, 235);
            NameLabel.Name = "NameLabel";
            NameLabel.Size = new Size(39, 13);
            NameLabel.TabIndex = 6;
            NameLabel.Text = "Name:";
            // 
            // HandgroupCombo
            // 
            HandgroupCombo.FormattingEnabled = true;
            HandgroupCombo.Location = new Point(248, 250);
            HandgroupCombo.Name = "HandgroupCombo";
            HandgroupCombo.Size = new Size(160, 21);
            HandgroupCombo.TabIndex = 7;
            // 
            // HandgroupLabel
            // 
            HandgroupLabel.AutoSize = true;
            HandgroupLabel.Location = new Point(245, 234);
            HandgroupLabel.Name = "HandgroupLabel";
            HandgroupLabel.Size = new Size(70, 13);
            HandgroupLabel.TabIndex = 8;
            HandgroupLabel.Text = "Handgroup:";
            // 
            // SummaryText
            // 
            SummaryText.Location = new Point(15, 110);
            SummaryText.Multiline = true;
            SummaryText.Name = "SummaryText";
            SummaryText.ReadOnly = true;
            SummaryText.ScrollBars = ScrollBars.Vertical;
            SummaryText.Size = new Size(393, 117);
            SummaryText.TabIndex = 9;
            // 
            // ImportButton
            // 
            ImportButton.Location = new Point(333, 277);
            ImportButton.Name = "ImportButton";
            ImportButton.Size = new Size(75, 23);
            ImportButton.TabIndex = 10;
            ImportButton.Text = "Import";
            ImportButton.UseVisualStyleBackColor = true;
            ImportButton.Click += ImportButton_Click;
            // 
            // HeadRadio
            // 
            HeadRadio.AutoSize = true;
            HeadRadio.Location = new Point(209, 87);
            HeadRadio.Name = "HeadRadio";
            HeadRadio.Size = new Size(52, 17);
            HeadRadio.TabIndex = 11;
            HeadRadio.TabStop = true;
            HeadRadio.Text = "Head";
            HeadRadio.UseVisualStyleBackColor = true;
            HeadRadio.CheckedChanged += HeadRadio_CheckedChanged;
            // 
            // AddAppearanceWindow
            // 
            AutoScaleDimensions = new SizeF(96F, 96F);
            AutoScaleMode = AutoScaleMode.Dpi;
            ClientSize = new Size(420, 308);
            Controls.Add(HeadRadio);
            Controls.Add(ImportButton);
            Controls.Add(SummaryText);
            Controls.Add(HandgroupLabel);
            Controls.Add(HandgroupCombo);
            Controls.Add(NameLabel);
            Controls.Add(HandgroupRadio);
            Controls.Add(OutfitRadio);
            Controls.Add(AppearanceRadio);
            Controls.Add(NameEntry);
            Controls.Add(InfoLabel);
            Controls.Add(AddAsLabel);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            Icon = (Icon)resources.GetObject("$this.Icon");
            MaximizeBox = false;
            MinimizeBox = false;
            Name = "AddAppearanceWindow";
            Text = "Import Meshes...";
            ResumeLayout(false);
            PerformLayout();

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