namespace FSO.IDE.ResourceBrowser
{
    partial class XMLEntryEditor
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
            this.CategoryComboBox = new System.Windows.Forms.ComboBox();
            this.CategoryLabel = new System.Windows.Forms.Label();
            this.XMLEntryTextBox = new System.Windows.Forms.TextBox();
            this.XMLEntryLabel = new System.Windows.Forms.Label();
            this.CopyButton = new System.Windows.Forms.Button();
            this.SalePriceLabel = new System.Windows.Forms.Label();
            this.GUIDLabel = new System.Windows.Forms.Label();
            this.GUIDTextBox = new System.Windows.Forms.TextBox();
            this.IFFFilenameLabel = new System.Windows.Forms.Label();
            this.IFFFilenameTextBox = new System.Windows.Forms.TextBox();
            this.NameLabel = new System.Windows.Forms.Label();
            this.NameTextBox = new System.Windows.Forms.TextBox();
            this.SalePriceUpDown = new System.Windows.Forms.NumericUpDown();
            this.CopiedLabel = new System.Windows.Forms.Label();
            this.CommentCheckbox = new System.Windows.Forms.CheckBox();
            ((System.ComponentModel.ISupportInitialize)(this.SalePriceUpDown)).BeginInit();
            this.SuspendLayout();
            // 
            // CategoryComboBox
            // 
            this.CategoryComboBox.FormattingEnabled = true;
            this.CategoryComboBox.Location = new System.Drawing.Point(7, 99);
            this.CategoryComboBox.Name = "CategoryComboBox";
            this.CategoryComboBox.Size = new System.Drawing.Size(223, 21);
            this.CategoryComboBox.TabIndex = 0;
            this.CategoryComboBox.SelectedIndexChanged += new System.EventHandler(this.UpdateXMLTextBox);
            // 
            // CategoryLabel
            // 
            this.CategoryLabel.AutoSize = true;
            this.CategoryLabel.Location = new System.Drawing.Point(4, 83);
            this.CategoryLabel.Name = "CategoryLabel";
            this.CategoryLabel.Size = new System.Drawing.Size(128, 13);
            this.CategoryLabel.TabIndex = 1;
            this.CategoryLabel.Text = "Buy/Build Mode Category";
            // 
            // XMLEntryTextBox
            // 
            this.XMLEntryTextBox.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.XMLEntryTextBox.Location = new System.Drawing.Point(7, 257);
            this.XMLEntryTextBox.Multiline = true;
            this.XMLEntryTextBox.Name = "XMLEntryTextBox";
            this.XMLEntryTextBox.ReadOnly = true;
            this.XMLEntryTextBox.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
            this.XMLEntryTextBox.Size = new System.Drawing.Size(752, 84);
            this.XMLEntryTextBox.TabIndex = 2;
            // 
            // XMLEntryLabel
            // 
            this.XMLEntryLabel.AutoSize = true;
            this.XMLEntryLabel.Location = new System.Drawing.Point(4, 241);
            this.XMLEntryLabel.Name = "XMLEntryLabel";
            this.XMLEntryLabel.Size = new System.Drawing.Size(56, 13);
            this.XMLEntryLabel.TabIndex = 3;
            this.XMLEntryLabel.Text = "XML Entry";
            // 
            // CopyButton
            // 
            this.CopyButton.Location = new System.Drawing.Point(7, 347);
            this.CopyButton.Name = "CopyButton";
            this.CopyButton.Size = new System.Drawing.Size(120, 23);
            this.CopyButton.TabIndex = 4;
            this.CopyButton.Text = "Copy To Clipboard";
            this.CopyButton.UseVisualStyleBackColor = true;
            this.CopyButton.Click += new System.EventHandler(this.CopyButton_Click);
            // 
            // SalePriceLabel
            // 
            this.SalePriceLabel.AutoSize = true;
            this.SalePriceLabel.Location = new System.Drawing.Point(4, 123);
            this.SalePriceLabel.Name = "SalePriceLabel";
            this.SalePriceLabel.Size = new System.Drawing.Size(55, 13);
            this.SalePriceLabel.TabIndex = 5;
            this.SalePriceLabel.Text = "Sale Price";
            // 
            // GUIDLabel
            // 
            this.GUIDLabel.AutoSize = true;
            this.GUIDLabel.Location = new System.Drawing.Point(4, 44);
            this.GUIDLabel.Name = "GUIDLabel";
            this.GUIDLabel.Size = new System.Drawing.Size(34, 13);
            this.GUIDLabel.TabIndex = 7;
            this.GUIDLabel.Text = "GUID";
            // 
            // GUIDTextBox
            // 
            this.GUIDTextBox.Location = new System.Drawing.Point(7, 60);
            this.GUIDTextBox.Name = "GUIDTextBox";
            this.GUIDTextBox.ReadOnly = true;
            this.GUIDTextBox.Size = new System.Drawing.Size(97, 20);
            this.GUIDTextBox.TabIndex = 8;
            // 
            // IFFFilenameLabel
            // 
            this.IFFFilenameLabel.AutoSize = true;
            this.IFFFilenameLabel.Location = new System.Drawing.Point(4, 4);
            this.IFFFilenameLabel.Name = "IFFFilenameLabel";
            this.IFFFilenameLabel.Size = new System.Drawing.Size(78, 13);
            this.IFFFilenameLabel.TabIndex = 9;
            this.IFFFilenameLabel.Text = "Comment Entry";
            // 
            // IFFFilenameTextBox
            // 
            this.IFFFilenameTextBox.Enabled = false;
            this.IFFFilenameTextBox.Location = new System.Drawing.Point(7, 21);
            this.IFFFilenameTextBox.Name = "IFFFilenameTextBox";
            this.IFFFilenameTextBox.Size = new System.Drawing.Size(223, 20);
            this.IFFFilenameTextBox.TabIndex = 10;
            this.IFFFilenameTextBox.TextChanged += new System.EventHandler(this.UpdateXMLTextBox);
            // 
            // NameLabel
            // 
            this.NameLabel.AutoSize = true;
            this.NameLabel.Location = new System.Drawing.Point(4, 162);
            this.NameLabel.Name = "NameLabel";
            this.NameLabel.Size = new System.Drawing.Size(35, 13);
            this.NameLabel.TabIndex = 11;
            this.NameLabel.Text = "Name";
            // 
            // NameTextBox
            // 
            this.NameTextBox.Location = new System.Drawing.Point(7, 178);
            this.NameTextBox.Name = "NameTextBox";
            this.NameTextBox.Size = new System.Drawing.Size(223, 20);
            this.NameTextBox.TabIndex = 12;
            this.NameTextBox.TextChanged += new System.EventHandler(this.UpdateXMLTextBox);
            // 
            // SalePriceUpDown
            // 
            this.SalePriceUpDown.Location = new System.Drawing.Point(7, 139);
            this.SalePriceUpDown.Maximum = new decimal(new int[] {
            65535,
            0,
            0,
            0});
            this.SalePriceUpDown.Name = "SalePriceUpDown";
            this.SalePriceUpDown.Size = new System.Drawing.Size(97, 20);
            this.SalePriceUpDown.TabIndex = 13;
            this.SalePriceUpDown.ValueChanged += new System.EventHandler(this.UpdateXMLTextBox);
            // 
            // CopiedLabel
            // 
            this.CopiedLabel.AutoSize = true;
            this.CopiedLabel.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Italic, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.CopiedLabel.Location = new System.Drawing.Point(133, 352);
            this.CopiedLabel.Name = "CopiedLabel";
            this.CopiedLabel.Size = new System.Drawing.Size(68, 13);
            this.CopiedLabel.TabIndex = 14;
            this.CopiedLabel.Text = "XML Copied!";
            // 
            // CommentCheckbox
            // 
            this.CommentCheckbox.AutoSize = true;
            this.CommentCheckbox.Location = new System.Drawing.Point(236, 23);
            this.CommentCheckbox.Name = "CommentCheckbox";
            this.CommentCheckbox.Size = new System.Drawing.Size(108, 17);
            this.CommentCheckbox.TabIndex = 15;
            this.CommentCheckbox.Text = "Include Comment";
            this.CommentCheckbox.UseVisualStyleBackColor = true;
            this.CommentCheckbox.CheckedChanged += new System.EventHandler(this.CommentCheckbox_CheckedChanged);
            // 
            // XMLEntryEditor
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.CommentCheckbox);
            this.Controls.Add(this.CopiedLabel);
            this.Controls.Add(this.SalePriceUpDown);
            this.Controls.Add(this.NameTextBox);
            this.Controls.Add(this.NameLabel);
            this.Controls.Add(this.IFFFilenameTextBox);
            this.Controls.Add(this.IFFFilenameLabel);
            this.Controls.Add(this.GUIDTextBox);
            this.Controls.Add(this.GUIDLabel);
            this.Controls.Add(this.SalePriceLabel);
            this.Controls.Add(this.CopyButton);
            this.Controls.Add(this.XMLEntryLabel);
            this.Controls.Add(this.XMLEntryTextBox);
            this.Controls.Add(this.CategoryLabel);
            this.Controls.Add(this.CategoryComboBox);
            this.Name = "XMLEntryEditor";
            this.Size = new System.Drawing.Size(762, 459);
            ((System.ComponentModel.ISupportInitialize)(this.SalePriceUpDown)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.ComboBox CategoryComboBox;
        private System.Windows.Forms.Label CategoryLabel;
        private System.Windows.Forms.TextBox XMLEntryTextBox;
        private System.Windows.Forms.Label XMLEntryLabel;
        private System.Windows.Forms.Button CopyButton;
        private System.Windows.Forms.Label SalePriceLabel;
        private System.Windows.Forms.Label GUIDLabel;
        private System.Windows.Forms.TextBox GUIDTextBox;
        private System.Windows.Forms.Label IFFFilenameLabel;
        private System.Windows.Forms.TextBox IFFFilenameTextBox;
        private System.Windows.Forms.Label NameLabel;
        private System.Windows.Forms.TextBox NameTextBox;
        private System.Windows.Forms.NumericUpDown SalePriceUpDown;
        private System.Windows.Forms.Label CopiedLabel;
        private System.Windows.Forms.CheckBox CommentCheckbox;
    }
}
