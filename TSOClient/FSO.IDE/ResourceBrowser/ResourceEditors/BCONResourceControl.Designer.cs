namespace FSO.IDE.ResourceBrowser.ResourceEditors
{
    partial class BCONResourceControl
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
            this.NameHeader = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.ValueHeader = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.CommentHeader = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.StringBox = new System.Windows.Forms.TextBox();
            this.label1 = new System.Windows.Forms.Label();
            this.NewButton = new System.Windows.Forms.Button();
            this.RemoveButton = new System.Windows.Forms.Button();
            this.SaveButton = new System.Windows.Forms.Button();
            this.UpButton = new System.Windows.Forms.Button();
            this.DownButton = new System.Windows.Forms.Button();
            this.Selector = new FSO.IDE.ResourceBrowser.OBJDSelectorControl();
            this.NameBox = new System.Windows.Forms.TextBox();
            this.ValueBox = new System.Windows.Forms.NumericUpDown();
            this.NameLabel = new System.Windows.Forms.Label();
            this.ValueLabel = new System.Windows.Forms.Label();
            ((System.ComponentModel.ISupportInitialize)(this.ValueBox)).BeginInit();
            this.SuspendLayout();
            // 
            // StringList
            // 
            this.StringList.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
            this.IDHeader,
            this.NameHeader,
            this.ValueHeader,
            this.CommentHeader});
            this.StringList.FullRowSelect = true;
            this.StringList.HideSelection = false;
            this.StringList.Location = new System.Drawing.Point(3, 3);
            this.StringList.MultiSelect = false;
            this.StringList.Name = "StringList";
            this.StringList.Size = new System.Drawing.Size(496, 290);
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
            // NameHeader
            // 
            this.NameHeader.Text = "Name";
            this.NameHeader.Width = 100;
            // 
            // ValueHeader
            // 
            this.ValueHeader.Text = "Value";
            // 
            // CommentHeader
            // 
            this.CommentHeader.Text = "Comment";
            this.CommentHeader.Width = 285;
            // 
            // StringBox
            // 
            this.StringBox.AcceptsReturn = true;
            this.StringBox.Location = new System.Drawing.Point(175, 312);
            this.StringBox.Multiline = true;
            this.StringBox.Name = "StringBox";
            this.StringBox.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
            this.StringBox.Size = new System.Drawing.Size(324, 95);
            this.StringBox.TabIndex = 3;
            this.StringBox.TextChanged += new System.EventHandler(this.StringBox_TextChanged);
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(175, 296);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(51, 13);
            this.label1.TabIndex = 4;
            this.label1.Text = "Comment";
            // 
            // NewButton
            // 
            this.NewButton.Location = new System.Drawing.Point(3, 299);
            this.NewButton.Name = "NewButton";
            this.NewButton.Size = new System.Drawing.Size(166, 22);
            this.NewButton.TabIndex = 5;
            this.NewButton.Text = "New Constant";
            this.NewButton.UseVisualStyleBackColor = true;
            this.NewButton.Click += new System.EventHandler(this.NewButton_Click);
            // 
            // RemoveButton
            // 
            this.RemoveButton.Location = new System.Drawing.Point(152, 428);
            this.RemoveButton.Name = "RemoveButton";
            this.RemoveButton.Size = new System.Drawing.Size(56, 23);
            this.RemoveButton.TabIndex = 7;
            this.RemoveButton.Text = "Remove";
            this.RemoveButton.UseVisualStyleBackColor = true;
            this.RemoveButton.Click += new System.EventHandler(this.RemoveButton_Click);
            // 
            // SaveButton
            // 
            this.SaveButton.Location = new System.Drawing.Point(3, 428);
            this.SaveButton.Name = "SaveButton";
            this.SaveButton.Size = new System.Drawing.Size(48, 23);
            this.SaveButton.TabIndex = 9;
            this.SaveButton.Text = "Save";
            this.SaveButton.UseVisualStyleBackColor = true;
            this.SaveButton.Click += new System.EventHandler(this.SaveButton_Click);
            // 
            // UpButton
            // 
            this.UpButton.Location = new System.Drawing.Point(57, 428);
            this.UpButton.Name = "UpButton";
            this.UpButton.Size = new System.Drawing.Size(32, 23);
            this.UpButton.TabIndex = 10;
            this.UpButton.Text = "Up";
            this.UpButton.UseVisualStyleBackColor = true;
            this.UpButton.Click += new System.EventHandler(this.UpButton_Click);
            // 
            // DownButton
            // 
            this.DownButton.Location = new System.Drawing.Point(95, 428);
            this.DownButton.Name = "DownButton";
            this.DownButton.Size = new System.Drawing.Size(51, 23);
            this.DownButton.TabIndex = 11;
            this.DownButton.Text = "Down";
            this.DownButton.UseVisualStyleBackColor = true;
            this.DownButton.Click += new System.EventHandler(this.DownButton_Click);
            // 
            // Selector
            // 
            this.Selector.Location = new System.Drawing.Point(259, 413);
            this.Selector.Name = "Selector";
            this.Selector.Size = new System.Drawing.Size(240, 38);
            this.Selector.TabIndex = 12;
            // 
            // NameBox
            // 
            this.NameBox.Location = new System.Drawing.Point(3, 339);
            this.NameBox.Name = "NameBox";
            this.NameBox.Size = new System.Drawing.Size(166, 20);
            this.NameBox.TabIndex = 13;
            // 
            // ValueBox
            // 
            this.ValueBox.Location = new System.Drawing.Point(3, 387);
            this.ValueBox.Maximum = new decimal(new int[] {
            32767,
            0,
            0,
            0});
            this.ValueBox.Minimum = new decimal(new int[] {
            32768,
            0,
            0,
            -2147483648});
            this.ValueBox.Name = "ValueBox";
            this.ValueBox.Size = new System.Drawing.Size(166, 20);
            this.ValueBox.TabIndex = 14;
            // 
            // NameLabel
            // 
            this.NameLabel.AutoSize = true;
            this.NameLabel.Location = new System.Drawing.Point(3, 324);
            this.NameLabel.Name = "NameLabel";
            this.NameLabel.Size = new System.Drawing.Size(35, 13);
            this.NameLabel.TabIndex = 15;
            this.NameLabel.Text = "Name";
            // 
            // ValueLabel
            // 
            this.ValueLabel.AutoSize = true;
            this.ValueLabel.Location = new System.Drawing.Point(4, 371);
            this.ValueLabel.Name = "ValueLabel";
            this.ValueLabel.Size = new System.Drawing.Size(34, 13);
            this.ValueLabel.TabIndex = 16;
            this.ValueLabel.Text = "Value";
            // 
            // BCONResourceControl
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.ValueLabel);
            this.Controls.Add(this.NameLabel);
            this.Controls.Add(this.ValueBox);
            this.Controls.Add(this.NameBox);
            this.Controls.Add(this.Selector);
            this.Controls.Add(this.DownButton);
            this.Controls.Add(this.UpButton);
            this.Controls.Add(this.SaveButton);
            this.Controls.Add(this.RemoveButton);
            this.Controls.Add(this.NewButton);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.StringBox);
            this.Controls.Add(this.StringList);
            this.Margin = new System.Windows.Forms.Padding(0);
            this.Name = "BCONResourceControl";
            this.Size = new System.Drawing.Size(502, 455);
            ((System.ComponentModel.ISupportInitialize)(this.ValueBox)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.ListView StringList;
        private System.Windows.Forms.ColumnHeader IDHeader;
        private System.Windows.Forms.ColumnHeader NameHeader;
        private System.Windows.Forms.TextBox StringBox;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Button NewButton;
        private System.Windows.Forms.Button RemoveButton;
        private System.Windows.Forms.Button SaveButton;
        private System.Windows.Forms.Button UpButton;
        private System.Windows.Forms.Button DownButton;
        private OBJDSelectorControl Selector;
        private System.Windows.Forms.ColumnHeader ValueHeader;
        private System.Windows.Forms.ColumnHeader CommentHeader;
        private System.Windows.Forms.TextBox NameBox;
        private System.Windows.Forms.NumericUpDown ValueBox;
        private System.Windows.Forms.Label NameLabel;
        private System.Windows.Forms.Label ValueLabel;
    }
}
