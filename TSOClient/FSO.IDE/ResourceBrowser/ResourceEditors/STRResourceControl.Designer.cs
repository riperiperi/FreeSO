namespace FSO.IDE.ResourceBrowser.ResourceEditors
{
    partial class STRResourceControl
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
            this.StringList = new System.Windows.Forms.ListView();
            this.IDHeader = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.StringHeader = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.LanguageBox = new System.Windows.Forms.ComboBox();
            this.LanguageLabel = new System.Windows.Forms.Label();
            this.StringBox = new System.Windows.Forms.TextBox();
            this.label1 = new System.Windows.Forms.Label();
            this.NewButton = new System.Windows.Forms.Button();
            this.RemoveButton = new System.Windows.Forms.Button();
            this.SelectButton = new System.Windows.Forms.Button();
            this.SuspendLayout();
            // 
            // StringList
            // 
            this.StringList.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
            this.IDHeader,
            this.StringHeader});
            this.StringList.FullRowSelect = true;
            this.StringList.Location = new System.Drawing.Point(3, 30);
            this.StringList.MultiSelect = false;
            this.StringList.Name = "StringList";
            this.StringList.Size = new System.Drawing.Size(250, 422);
            this.StringList.TabIndex = 0;
            this.StringList.UseCompatibleStateImageBehavior = false;
            this.StringList.View = System.Windows.Forms.View.Details;
            this.StringList.SelectedIndexChanged += new System.EventHandler(this.StringList_SelectedIndexChanged);
            // 
            // IDHeader
            // 
            this.IDHeader.Text = "#";
            this.IDHeader.Width = 30;
            // 
            // StringHeader
            // 
            this.StringHeader.Text = "String";
            this.StringHeader.Width = 195;
            // 
            // LanguageBox
            // 
            this.LanguageBox.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.LanguageBox.FormattingEnabled = true;
            this.LanguageBox.Location = new System.Drawing.Point(111, 3);
            this.LanguageBox.Name = "LanguageBox";
            this.LanguageBox.Size = new System.Drawing.Size(142, 21);
            this.LanguageBox.TabIndex = 1;
            // 
            // LanguageLabel
            // 
            this.LanguageLabel.Location = new System.Drawing.Point(3, 6);
            this.LanguageLabel.Name = "LanguageLabel";
            this.LanguageLabel.Size = new System.Drawing.Size(102, 18);
            this.LanguageLabel.TabIndex = 2;
            this.LanguageLabel.Text = "Language Set:";
            // 
            // StringBox
            // 
            this.StringBox.AcceptsReturn = true;
            this.StringBox.Location = new System.Drawing.Point(259, 46);
            this.StringBox.Multiline = true;
            this.StringBox.Name = "StringBox";
            this.StringBox.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
            this.StringBox.Size = new System.Drawing.Size(240, 333);
            this.StringBox.TabIndex = 3;
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(259, 30);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(37, 13);
            this.label1.TabIndex = 4;
            this.label1.Text = "String:";
            // 
            // NewButton
            // 
            this.NewButton.Location = new System.Drawing.Point(259, 2);
            this.NewButton.Name = "NewButton";
            this.NewButton.Size = new System.Drawing.Size(240, 22);
            this.NewButton.TabIndex = 5;
            this.NewButton.Text = "New String";
            this.NewButton.UseVisualStyleBackColor = true;
            // 
            // RemoveButton
            // 
            this.RemoveButton.Location = new System.Drawing.Point(382, 385);
            this.RemoveButton.Name = "RemoveButton";
            this.RemoveButton.Size = new System.Drawing.Size(117, 21);
            this.RemoveButton.TabIndex = 7;
            this.RemoveButton.Text = "Remove String";
            this.RemoveButton.UseVisualStyleBackColor = true;
            // 
            // SelectButton
            // 
            this.SelectButton.Location = new System.Drawing.Point(259, 412);
            this.SelectButton.Name = "SelectButton";
            this.SelectButton.Size = new System.Drawing.Size(240, 40);
            this.SelectButton.TabIndex = 8;
            this.SelectButton.Text = "Select String";
            this.SelectButton.UseVisualStyleBackColor = true;
            // 
            // STRResourceControl
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.SelectButton);
            this.Controls.Add(this.RemoveButton);
            this.Controls.Add(this.NewButton);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.StringBox);
            this.Controls.Add(this.LanguageLabel);
            this.Controls.Add(this.LanguageBox);
            this.Controls.Add(this.StringList);
            this.Margin = new System.Windows.Forms.Padding(0);
            this.Name = "STRResourceControl";
            this.Size = new System.Drawing.Size(502, 455);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.ListView StringList;
        private System.Windows.Forms.ColumnHeader IDHeader;
        private System.Windows.Forms.ColumnHeader StringHeader;
        private System.Windows.Forms.ComboBox LanguageBox;
        private System.Windows.Forms.Label LanguageLabel;
        private System.Windows.Forms.TextBox StringBox;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Button NewButton;
        private System.Windows.Forms.Button RemoveButton;
        private System.Windows.Forms.Button SelectButton;
    }
}
