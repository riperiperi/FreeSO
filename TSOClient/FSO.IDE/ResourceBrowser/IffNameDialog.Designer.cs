namespace FSO.IDE.ResourceBrowser
{
    partial class IffNameDialog
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
            this.OKButton = new System.Windows.Forms.Button();
            this.CancelButton = new System.Windows.Forms.Button();
            this.ChunkLabelText = new System.Windows.Forms.Label();
            this.ChunkLabelEntry = new System.Windows.Forms.TextBox();
            this.ChunkIDText = new System.Windows.Forms.Label();
            this.ChunkIDEntry = new System.Windows.Forms.NumericUpDown();
            ((System.ComponentModel.ISupportInitialize)(this.ChunkIDEntry)).BeginInit();
            this.SuspendLayout();
            // 
            // OKButton
            // 
            this.OKButton.Location = new System.Drawing.Point(233, 70);
            this.OKButton.Name = "OKButton";
            this.OKButton.Size = new System.Drawing.Size(75, 23);
            this.OKButton.TabIndex = 0;
            this.OKButton.Text = "OK";
            this.OKButton.UseVisualStyleBackColor = true;
            this.OKButton.Click += new System.EventHandler(this.OKButton_Click);
            // 
            // CancelButton
            // 
            this.CancelButton.Location = new System.Drawing.Point(152, 70);
            this.CancelButton.Name = "CancelButton";
            this.CancelButton.Size = new System.Drawing.Size(75, 23);
            this.CancelButton.TabIndex = 1;
            this.CancelButton.Text = "Cancel";
            this.CancelButton.UseVisualStyleBackColor = true;
            this.CancelButton.Click += new System.EventHandler(this.CancelButton_Click);
            // 
            // ChunkLabelText
            // 
            this.ChunkLabelText.AutoSize = true;
            this.ChunkLabelText.Location = new System.Drawing.Point(12, 9);
            this.ChunkLabelText.Name = "ChunkLabelText";
            this.ChunkLabelText.Size = new System.Drawing.Size(70, 13);
            this.ChunkLabelText.TabIndex = 2;
            this.ChunkLabelText.Text = "Chunk Label:";
            // 
            // ChunkLabelEntry
            // 
            this.ChunkLabelEntry.Location = new System.Drawing.Point(12, 25);
            this.ChunkLabelEntry.Name = "ChunkLabelEntry";
            this.ChunkLabelEntry.Size = new System.Drawing.Size(296, 20);
            this.ChunkLabelEntry.TabIndex = 3;
            // 
            // ChunkIDText
            // 
            this.ChunkIDText.AutoSize = true;
            this.ChunkIDText.Location = new System.Drawing.Point(12, 57);
            this.ChunkIDText.Name = "ChunkIDText";
            this.ChunkIDText.Size = new System.Drawing.Size(52, 13);
            this.ChunkIDText.TabIndex = 4;
            this.ChunkIDText.Text = "Chunk ID";
            // 
            // ChunkIDEntry
            // 
            this.ChunkIDEntry.Location = new System.Drawing.Point(12, 73);
            this.ChunkIDEntry.Maximum = new decimal(new int[] {
            65535,
            0,
            0,
            0});
            this.ChunkIDEntry.Minimum = new decimal(new int[] {
            1,
            0,
            0,
            0});
            this.ChunkIDEntry.Name = "ChunkIDEntry";
            this.ChunkIDEntry.Size = new System.Drawing.Size(120, 20);
            this.ChunkIDEntry.TabIndex = 5;
            this.ChunkIDEntry.Value = new decimal(new int[] {
            1,
            0,
            0,
            0});
            // 
            // IffNameDialog
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(320, 108);
            this.ControlBox = false;
            this.Controls.Add(this.ChunkIDEntry);
            this.Controls.Add(this.ChunkIDText);
            this.Controls.Add(this.ChunkLabelEntry);
            this.Controls.Add(this.ChunkLabelText);
            this.Controls.Add(this.CancelButton);
            this.Controls.Add(this.OKButton);
            this.Name = "IffNameDialog";
            this.SizeGripStyle = System.Windows.Forms.SizeGripStyle.Hide;
            this.Text = "Specify a Name and ID";
            this.TopMost = true;
            ((System.ComponentModel.ISupportInitialize)(this.ChunkIDEntry)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Button OKButton;
        private System.Windows.Forms.Button CancelButton;
        private System.Windows.Forms.Label ChunkLabelText;
        private System.Windows.Forms.TextBox ChunkLabelEntry;
        private System.Windows.Forms.Label ChunkIDText;
        private System.Windows.Forms.NumericUpDown ChunkIDEntry;
    }
}