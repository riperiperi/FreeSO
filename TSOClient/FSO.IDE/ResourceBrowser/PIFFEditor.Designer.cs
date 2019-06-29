namespace FSO.IDE.ResourceBrowser
{
    partial class PIFFEditor
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
            this.SaveIff = new System.Windows.Forms.Button();
            this.EntryList = new System.Windows.Forms.ListBox();
            this.ChunksLabel = new System.Windows.Forms.Label();
            this.CommentLabel = new System.Windows.Forms.Label();
            this.EntryComment = new System.Windows.Forms.TextBox();
            this.SummaryLabel = new System.Windows.Forms.Label();
            this.EntrySummary = new System.Windows.Forms.TextBox();
            this.PIFFLabel = new System.Windows.Forms.Label();
            this.PIFFName = new System.Windows.Forms.TextBox();
            this.PIFFButton = new System.Windows.Forms.Button();
            this.SPFButton = new System.Windows.Forms.Button();
            this.STRButton = new System.Windows.Forms.Button();
            this.FileCommentsLabel = new System.Windows.Forms.Label();
            this.PIFFComment = new System.Windows.Forms.TextBox();
            this.PIFFBox = new System.Windows.Forms.GroupBox();
            this.EntryDelete = new System.Windows.Forms.Button();
            this.PIFFBox.SuspendLayout();
            this.SuspendLayout();
            // 
            // SaveIff
            // 
            this.SaveIff.Location = new System.Drawing.Point(87, 53);
            this.SaveIff.Name = "SaveIff";
            this.SaveIff.Size = new System.Drawing.Size(150, 23);
            this.SaveIff.TabIndex = 0;
            this.SaveIff.Text = "Save Complete Iff...";
            this.SaveIff.UseVisualStyleBackColor = true;
            // 
            // EntryList
            // 
            this.EntryList.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left)));
            this.EntryList.FormattingEnabled = true;
            this.EntryList.Location = new System.Drawing.Point(6, 32);
            this.EntryList.Name = "EntryList";
            this.EntryList.Size = new System.Drawing.Size(272, 329);
            this.EntryList.TabIndex = 1;
            this.EntryList.SelectedIndexChanged += new System.EventHandler(this.EntryList_SelectedIndexChanged);
            // 
            // ChunksLabel
            // 
            this.ChunksLabel.AutoSize = true;
            this.ChunksLabel.Location = new System.Drawing.Point(6, 16);
            this.ChunksLabel.Name = "ChunksLabel";
            this.ChunksLabel.Size = new System.Drawing.Size(89, 13);
            this.ChunksLabel.TabIndex = 2;
            this.ChunksLabel.Text = "Patched Chunks:";
            // 
            // CommentLabel
            // 
            this.CommentLabel.AutoSize = true;
            this.CommentLabel.Location = new System.Drawing.Point(284, 16);
            this.CommentLabel.Name = "CommentLabel";
            this.CommentLabel.Size = new System.Drawing.Size(54, 13);
            this.CommentLabel.TabIndex = 3;
            this.CommentLabel.Text = "Comment:";
            // 
            // EntryComment
            // 
            this.EntryComment.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.EntryComment.Location = new System.Drawing.Point(284, 32);
            this.EntryComment.Multiline = true;
            this.EntryComment.Name = "EntryComment";
            this.EntryComment.Size = new System.Drawing.Size(466, 67);
            this.EntryComment.TabIndex = 4;
            this.EntryComment.TextChanged += new System.EventHandler(this.EntryComment_TextChanged);
            // 
            // SummaryLabel
            // 
            this.SummaryLabel.AutoSize = true;
            this.SummaryLabel.Location = new System.Drawing.Point(284, 102);
            this.SummaryLabel.Name = "SummaryLabel";
            this.SummaryLabel.Size = new System.Drawing.Size(53, 13);
            this.SummaryLabel.TabIndex = 5;
            this.SummaryLabel.Text = "Summary:";
            // 
            // EntrySummary
            // 
            this.EntrySummary.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.EntrySummary.Location = new System.Drawing.Point(284, 118);
            this.EntrySummary.Multiline = true;
            this.EntrySummary.Name = "EntrySummary";
            this.EntrySummary.ReadOnly = true;
            this.EntrySummary.Size = new System.Drawing.Size(466, 214);
            this.EntrySummary.TabIndex = 6;
            this.EntrySummary.Text = "No Summary Avaiable.";
            // 
            // PIFFLabel
            // 
            this.PIFFLabel.AutoSize = true;
            this.PIFFLabel.Location = new System.Drawing.Point(84, 3);
            this.PIFFLabel.Name = "PIFFLabel";
            this.PIFFLabel.Size = new System.Drawing.Size(63, 13);
            this.PIFFLabel.TabIndex = 7;
            this.PIFFLabel.Text = "PIFF Name:";
            // 
            // PIFFName
            // 
            this.PIFFName.Location = new System.Drawing.Point(87, 19);
            this.PIFFName.Name = "PIFFName";
            this.PIFFName.Size = new System.Drawing.Size(150, 20);
            this.PIFFName.TabIndex = 8;
            this.PIFFName.Text = "SampleName.piff";
            this.PIFFName.TextChanged += new System.EventHandler(this.PIFFName_TextChanged);
            // 
            // PIFFButton
            // 
            this.PIFFButton.Location = new System.Drawing.Point(3, 3);
            this.PIFFButton.Name = "PIFFButton";
            this.PIFFButton.Size = new System.Drawing.Size(75, 23);
            this.PIFFButton.TabIndex = 13;
            this.PIFFButton.Text = "PIFF";
            this.PIFFButton.UseVisualStyleBackColor = true;
            this.PIFFButton.Click += new System.EventHandler(this.PIFFButton_Click);
            // 
            // SPFButton
            // 
            this.SPFButton.Location = new System.Drawing.Point(3, 28);
            this.SPFButton.Name = "SPFButton";
            this.SPFButton.Size = new System.Drawing.Size(75, 23);
            this.SPFButton.TabIndex = 14;
            this.SPFButton.Text = "SPF";
            this.SPFButton.UseVisualStyleBackColor = true;
            this.SPFButton.Click += new System.EventHandler(this.SPFButton_Click);
            // 
            // STRButton
            // 
            this.STRButton.Location = new System.Drawing.Point(3, 53);
            this.STRButton.Name = "STRButton";
            this.STRButton.Size = new System.Drawing.Size(75, 23);
            this.STRButton.TabIndex = 15;
            this.STRButton.Text = "STR (old)";
            this.STRButton.UseVisualStyleBackColor = true;
            this.STRButton.Click += new System.EventHandler(this.STRButton_Click);
            // 
            // FileCommentsLabel
            // 
            this.FileCommentsLabel.AutoSize = true;
            this.FileCommentsLabel.Location = new System.Drawing.Point(240, 3);
            this.FileCommentsLabel.Name = "FileCommentsLabel";
            this.FileCommentsLabel.Size = new System.Drawing.Size(78, 13);
            this.FileCommentsLabel.TabIndex = 16;
            this.FileCommentsLabel.Text = "File Comments:";
            // 
            // PIFFComment
            // 
            this.PIFFComment.Location = new System.Drawing.Point(243, 19);
            this.PIFFComment.Multiline = true;
            this.PIFFComment.Name = "PIFFComment";
            this.PIFFComment.Size = new System.Drawing.Size(516, 57);
            this.PIFFComment.TabIndex = 17;
            this.PIFFComment.TextChanged += new System.EventHandler(this.PIFFComment_TextChanged);
            // 
            // PIFFBox
            // 
            this.PIFFBox.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.PIFFBox.Controls.Add(this.EntryDelete);
            this.PIFFBox.Controls.Add(this.ChunksLabel);
            this.PIFFBox.Controls.Add(this.EntryList);
            this.PIFFBox.Controls.Add(this.CommentLabel);
            this.PIFFBox.Controls.Add(this.EntryComment);
            this.PIFFBox.Controls.Add(this.SummaryLabel);
            this.PIFFBox.Controls.Add(this.EntrySummary);
            this.PIFFBox.Location = new System.Drawing.Point(3, 89);
            this.PIFFBox.Name = "PIFFBox";
            this.PIFFBox.Size = new System.Drawing.Size(756, 367);
            this.PIFFBox.TabIndex = 18;
            this.PIFFBox.TabStop = false;
            this.PIFFBox.Text = "PIFF";
            // 
            // EntryDelete
            // 
            this.EntryDelete.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.EntryDelete.Location = new System.Drawing.Point(675, 338);
            this.EntryDelete.Name = "EntryDelete";
            this.EntryDelete.Size = new System.Drawing.Size(75, 23);
            this.EntryDelete.TabIndex = 7;
            this.EntryDelete.Text = "Delete";
            this.EntryDelete.UseVisualStyleBackColor = true;
            // 
            // PIFFEditor
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.PIFFBox);
            this.Controls.Add(this.PIFFComment);
            this.Controls.Add(this.FileCommentsLabel);
            this.Controls.Add(this.STRButton);
            this.Controls.Add(this.SPFButton);
            this.Controls.Add(this.PIFFButton);
            this.Controls.Add(this.PIFFName);
            this.Controls.Add(this.PIFFLabel);
            this.Controls.Add(this.SaveIff);
            this.Name = "PIFFEditor";
            this.Size = new System.Drawing.Size(762, 459);
            this.PIFFBox.ResumeLayout(false);
            this.PIFFBox.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Button SaveIff;
        private System.Windows.Forms.ListBox EntryList;
        private System.Windows.Forms.Label ChunksLabel;
        private System.Windows.Forms.Label CommentLabel;
        private System.Windows.Forms.TextBox EntryComment;
        private System.Windows.Forms.Label SummaryLabel;
        private System.Windows.Forms.TextBox EntrySummary;
        private System.Windows.Forms.Label PIFFLabel;
        private System.Windows.Forms.TextBox PIFFName;
        private System.Windows.Forms.Button PIFFButton;
        private System.Windows.Forms.Button SPFButton;
        private System.Windows.Forms.Button STRButton;
        private System.Windows.Forms.Label FileCommentsLabel;
        private System.Windows.Forms.TextBox PIFFComment;
        private System.Windows.Forms.GroupBox PIFFBox;
        private System.Windows.Forms.Button EntryDelete;
    }
}
