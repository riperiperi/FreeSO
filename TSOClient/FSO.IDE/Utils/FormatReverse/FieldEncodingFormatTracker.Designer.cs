namespace FSO.IDE.Utils.FormatReverse
{
    partial class FieldEncodingFormatTracker
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
            this.DataLabel = new System.Windows.Forms.Label();
            this.DataList = new System.Windows.Forms.ListBox();
            this.NextLabel = new System.Windows.Forms.Label();
            this.UndoButton = new System.Windows.Forms.Button();
            this.ShortButton = new System.Windows.Forms.Button();
            this.IntButton = new System.Windows.Forms.Button();
            this.ShortPreview = new System.Windows.Forms.Label();
            this.IntPreview = new System.Windows.Forms.Label();
            this.RepeatBase = new System.Windows.Forms.NumericUpDown();
            this.RepeatBox = new System.Windows.Forms.GroupBox();
            this.RepeatLabel = new System.Windows.Forms.Label();
            this.label5 = new System.Windows.Forms.Label();
            this.SaveButton = new System.Windows.Forms.Button();
            this.ShortErrors = new System.Windows.Forms.Label();
            this.IntErrors = new System.Windows.Forms.Label();
            this.LoadButton = new System.Windows.Forms.Button();
            this.LabelTextBox = new System.Windows.Forms.TextBox();
            this.LabelButton = new System.Windows.Forms.Button();
            this.BitView = new System.Windows.Forms.Label();
            this.UnknownButton = new System.Windows.Forms.Button();
            ((System.ComponentModel.ISupportInitialize)(this.RepeatBase)).BeginInit();
            this.RepeatBox.SuspendLayout();
            this.SuspendLayout();
            // 
            // DataLabel
            // 
            this.DataLabel.AutoSize = true;
            this.DataLabel.Location = new System.Drawing.Point(12, 11);
            this.DataLabel.Name = "DataLabel";
            this.DataLabel.Size = new System.Drawing.Size(62, 13);
            this.DataLabel.TabIndex = 0;
            this.DataLabel.Text = "Data so far:";
            // 
            // DataList
            // 
            this.DataList.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.DataList.FormattingEnabled = true;
            this.DataList.IntegralHeight = false;
            this.DataList.Location = new System.Drawing.Point(12, 27);
            this.DataList.Name = "DataList";
            this.DataList.Size = new System.Drawing.Size(175, 260);
            this.DataList.TabIndex = 1;
            this.DataList.SelectedIndexChanged += new System.EventHandler(this.DataList_SelectedIndexChanged);
            // 
            // NextLabel
            // 
            this.NextLabel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.NextLabel.AutoSize = true;
            this.NextLabel.Location = new System.Drawing.Point(206, 26);
            this.NextLabel.Name = "NextLabel";
            this.NextLabel.Size = new System.Drawing.Size(62, 13);
            this.NextLabel.TabIndex = 2;
            this.NextLabel.Text = "Next Value:";
            // 
            // UndoButton
            // 
            this.UndoButton.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.UndoButton.Location = new System.Drawing.Point(12, 321);
            this.UndoButton.Name = "UndoButton";
            this.UndoButton.Size = new System.Drawing.Size(175, 23);
            this.UndoButton.TabIndex = 3;
            this.UndoButton.Text = "Undo to Before Selected";
            this.UndoButton.UseVisualStyleBackColor = true;
            this.UndoButton.Click += new System.EventHandler(this.UndoButton_Click);
            // 
            // ShortButton
            // 
            this.ShortButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.ShortButton.Location = new System.Drawing.Point(209, 42);
            this.ShortButton.Name = "ShortButton";
            this.ShortButton.Size = new System.Drawing.Size(120, 23);
            this.ShortButton.TabIndex = 4;
            this.ShortButton.Text = "Short";
            this.ShortButton.UseVisualStyleBackColor = true;
            this.ShortButton.Click += new System.EventHandler(this.ShortButton_Click);
            // 
            // IntButton
            // 
            this.IntButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.IntButton.Location = new System.Drawing.Point(335, 42);
            this.IntButton.Name = "IntButton";
            this.IntButton.Size = new System.Drawing.Size(120, 23);
            this.IntButton.TabIndex = 5;
            this.IntButton.Text = "Int";
            this.IntButton.UseVisualStyleBackColor = true;
            this.IntButton.Click += new System.EventHandler(this.IntButton_Click);
            // 
            // ShortPreview
            // 
            this.ShortPreview.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.ShortPreview.Location = new System.Drawing.Point(209, 68);
            this.ShortPreview.Name = "ShortPreview";
            this.ShortPreview.Size = new System.Drawing.Size(120, 23);
            this.ShortPreview.TabIndex = 6;
            this.ShortPreview.Text = "65535";
            this.ShortPreview.TextAlign = System.Drawing.ContentAlignment.TopCenter;
            // 
            // IntPreview
            // 
            this.IntPreview.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.IntPreview.Location = new System.Drawing.Point(335, 68);
            this.IntPreview.Name = "IntPreview";
            this.IntPreview.Size = new System.Drawing.Size(120, 23);
            this.IntPreview.TabIndex = 7;
            this.IntPreview.Text = "2147483647";
            this.IntPreview.TextAlign = System.Drawing.ContentAlignment.TopCenter;
            // 
            // RepeatBase
            // 
            this.RepeatBase.Location = new System.Drawing.Point(9, 35);
            this.RepeatBase.Name = "RepeatBase";
            this.RepeatBase.Size = new System.Drawing.Size(95, 20);
            this.RepeatBase.TabIndex = 9;
            this.RepeatBase.ValueChanged += new System.EventHandler(this.RepeatBase_ValueChanged);
            // 
            // RepeatBox
            // 
            this.RepeatBox.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.RepeatBox.Controls.Add(this.RepeatLabel);
            this.RepeatBox.Controls.Add(this.label5);
            this.RepeatBox.Controls.Add(this.RepeatBase);
            this.RepeatBox.Location = new System.Drawing.Point(209, 247);
            this.RepeatBox.Name = "RepeatBox";
            this.RepeatBox.Size = new System.Drawing.Size(246, 64);
            this.RepeatBox.TabIndex = 10;
            this.RepeatBox.TabStop = false;
            this.RepeatBox.Text = "Repeat Testing";
            // 
            // RepeatLabel
            // 
            this.RepeatLabel.Location = new System.Drawing.Point(110, 16);
            this.RepeatLabel.Name = "RepeatLabel";
            this.RepeatLabel.Size = new System.Drawing.Size(130, 39);
            this.RepeatLabel.TabIndex = 11;
            this.RepeatLabel.Text = "Repeated 23 times!";
            this.RepeatLabel.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // label5
            // 
            this.label5.AutoSize = true;
            this.label5.Location = new System.Drawing.Point(6, 19);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(98, 13);
            this.label5.TabIndex = 10;
            this.label5.Text = "Repeat Base Index";
            // 
            // SaveButton
            // 
            this.SaveButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.SaveButton.Location = new System.Drawing.Point(209, 321);
            this.SaveButton.Name = "SaveButton";
            this.SaveButton.Size = new System.Drawing.Size(120, 23);
            this.SaveButton.TabIndex = 11;
            this.SaveButton.Text = "Save Format";
            this.SaveButton.UseVisualStyleBackColor = true;
            this.SaveButton.Click += new System.EventHandler(this.SaveButton_Click);
            // 
            // ShortErrors
            // 
            this.ShortErrors.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.ShortErrors.Location = new System.Drawing.Point(209, 91);
            this.ShortErrors.Name = "ShortErrors";
            this.ShortErrors.Size = new System.Drawing.Size(120, 120);
            this.ShortErrors.TabIndex = 12;
            this.ShortErrors.Text = "Detected issues:\r\n\r\nIncorrect bit size\r\nEmpty non-zero bit size\r\nShort term impos" +
    "sible";
            this.ShortErrors.TextAlign = System.Drawing.ContentAlignment.TopCenter;
            // 
            // IntErrors
            // 
            this.IntErrors.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.IntErrors.Location = new System.Drawing.Point(336, 91);
            this.IntErrors.Name = "IntErrors";
            this.IntErrors.Size = new System.Drawing.Size(120, 120);
            this.IntErrors.TabIndex = 13;
            this.IntErrors.Text = "Detected issues:\r\n\r\nIncorrect bit size\r\nEmpty non-zero bit size";
            this.IntErrors.TextAlign = System.Drawing.ContentAlignment.TopCenter;
            // 
            // LoadButton
            // 
            this.LoadButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.LoadButton.Location = new System.Drawing.Point(335, 321);
            this.LoadButton.Name = "LoadButton";
            this.LoadButton.Size = new System.Drawing.Size(120, 23);
            this.LoadButton.TabIndex = 14;
            this.LoadButton.Text = "Load Format";
            this.LoadButton.UseVisualStyleBackColor = true;
            this.LoadButton.Click += new System.EventHandler(this.LoadButton_Click);
            // 
            // LabelTextBox
            // 
            this.LabelTextBox.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.LabelTextBox.Location = new System.Drawing.Point(12, 295);
            this.LabelTextBox.Name = "LabelTextBox";
            this.LabelTextBox.Size = new System.Drawing.Size(123, 20);
            this.LabelTextBox.TabIndex = 15;
            // 
            // LabelButton
            // 
            this.LabelButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.LabelButton.Location = new System.Drawing.Point(141, 293);
            this.LabelButton.Name = "LabelButton";
            this.LabelButton.Size = new System.Drawing.Size(46, 23);
            this.LabelButton.TabIndex = 16;
            this.LabelButton.Text = "Label";
            this.LabelButton.UseVisualStyleBackColor = true;
            this.LabelButton.Click += new System.EventHandler(this.LabelButton_Click);
            // 
            // BitView
            // 
            this.BitView.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.BitView.Font = new System.Drawing.Font("Consolas", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.BitView.Location = new System.Drawing.Point(212, 221);
            this.BitView.Name = "BitView";
            this.BitView.Size = new System.Drawing.Size(243, 23);
            this.BitView.TabIndex = 17;
            this.BitView.Text = ">111010111|00110110]10110";
            // 
            // UnknownButton
            // 
            this.UnknownButton.Location = new System.Drawing.Point(427, 11);
            this.UnknownButton.Name = "UnknownButton";
            this.UnknownButton.Size = new System.Drawing.Size(28, 23);
            this.UnknownButton.TabIndex = 18;
            this.UnknownButton.Text = "?";
            this.UnknownButton.UseVisualStyleBackColor = true;
            this.UnknownButton.Click += new System.EventHandler(this.UnknownButton_Click);
            // 
            // FieldEncodingFormatTracker
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(468, 356);
            this.Controls.Add(this.UnknownButton);
            this.Controls.Add(this.BitView);
            this.Controls.Add(this.LabelButton);
            this.Controls.Add(this.LabelTextBox);
            this.Controls.Add(this.LoadButton);
            this.Controls.Add(this.IntErrors);
            this.Controls.Add(this.ShortErrors);
            this.Controls.Add(this.SaveButton);
            this.Controls.Add(this.RepeatBox);
            this.Controls.Add(this.IntPreview);
            this.Controls.Add(this.ShortPreview);
            this.Controls.Add(this.IntButton);
            this.Controls.Add(this.ShortButton);
            this.Controls.Add(this.UndoButton);
            this.Controls.Add(this.NextLabel);
            this.Controls.Add(this.DataList);
            this.Controls.Add(this.DataLabel);
            this.Name = "FieldEncodingFormatTracker";
            this.Text = "Field Encoding Format Tracker (OBJM)";
            ((System.ComponentModel.ISupportInitialize)(this.RepeatBase)).EndInit();
            this.RepeatBox.ResumeLayout(false);
            this.RepeatBox.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Label DataLabel;
        private System.Windows.Forms.ListBox DataList;
        private System.Windows.Forms.Label NextLabel;
        private System.Windows.Forms.Button UndoButton;
        private System.Windows.Forms.Button ShortButton;
        private System.Windows.Forms.Button IntButton;
        private System.Windows.Forms.Label ShortPreview;
        private System.Windows.Forms.Label IntPreview;
        private System.Windows.Forms.NumericUpDown RepeatBase;
        private System.Windows.Forms.GroupBox RepeatBox;
        private System.Windows.Forms.Label RepeatLabel;
        private System.Windows.Forms.Label label5;
        private System.Windows.Forms.Button SaveButton;
        private System.Windows.Forms.Label ShortErrors;
        private System.Windows.Forms.Label IntErrors;
        private System.Windows.Forms.Button LoadButton;
        private System.Windows.Forms.TextBox LabelTextBox;
        private System.Windows.Forms.Button LabelButton;
        private System.Windows.Forms.Label BitView;
        private System.Windows.Forms.Button UnknownButton;
    }
}