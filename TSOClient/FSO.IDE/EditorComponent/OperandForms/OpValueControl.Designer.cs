namespace FSO.IDE.EditorComponent.OperandForms
{
    partial class OpValueControl
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
            this.tableLayoutPanel3 = new System.Windows.Forms.TableLayoutPanel();
            this.TitleLabel = new System.Windows.Forms.Label();
            this.ValueEntry = new System.Windows.Forms.NumericUpDown();
            this.tableLayoutPanel3.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.ValueEntry)).BeginInit();
            this.SuspendLayout();
            // 
            // tableLayoutPanel3
            // 
            this.tableLayoutPanel3.AutoSize = true;
            this.tableLayoutPanel3.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.tableLayoutPanel3.ColumnCount = 2;
            this.tableLayoutPanel3.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle());
            this.tableLayoutPanel3.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tableLayoutPanel3.Controls.Add(this.TitleLabel, 0, 0);
            this.tableLayoutPanel3.Controls.Add(this.ValueEntry, 1, 0);
            this.tableLayoutPanel3.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tableLayoutPanel3.Location = new System.Drawing.Point(0, 0);
            this.tableLayoutPanel3.Margin = new System.Windows.Forms.Padding(0);
            this.tableLayoutPanel3.MinimumSize = new System.Drawing.Size(0, 26);
            this.tableLayoutPanel3.Name = "tableLayoutPanel3";
            this.tableLayoutPanel3.RowCount = 1;
            this.tableLayoutPanel3.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tableLayoutPanel3.Size = new System.Drawing.Size(1288, 683);
            this.tableLayoutPanel3.TabIndex = 13;
            // 
            // TitleLabel
            // 
            this.TitleLabel.AutoSize = true;
            this.TitleLabel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.TitleLabel.Location = new System.Drawing.Point(3, 0);
            this.TitleLabel.MinimumSize = new System.Drawing.Size(0, 26);
            this.TitleLabel.Name = "TitleLabel";
            this.TitleLabel.Size = new System.Drawing.Size(91, 683);
            this.TitleLabel.TabIndex = 1;
            this.TitleLabel.Text = "Expected Events:";
            this.TitleLabel.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // ValueEntry
            // 
            this.ValueEntry.Dock = System.Windows.Forms.DockStyle.Fill;
            this.ValueEntry.Location = new System.Drawing.Point(100, 3);
            this.ValueEntry.Maximum = new decimal(new int[] {
            32767,
            0,
            0,
            0});
            this.ValueEntry.Minimum = new decimal(new int[] {
            32768,
            0,
            0,
            -2147483648});
            this.ValueEntry.Name = "ValueEntry";
            this.ValueEntry.Size = new System.Drawing.Size(1185, 20);
            this.ValueEntry.TabIndex = 2;
            this.ValueEntry.ValueChanged += new System.EventHandler(this.ValueEntry_ValueChanged);
            // 
            // OpValueControl
            // 
            this.AutoSize = true;
            this.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.Controls.Add(this.tableLayoutPanel3);
            this.Margin = new System.Windows.Forms.Padding(0);
            this.Name = "OpValueControl";
            this.Size = new System.Drawing.Size(1288, 683);
            this.tableLayoutPanel3.ResumeLayout(false);
            this.tableLayoutPanel3.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.ValueEntry)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.TableLayoutPanel tableLayoutPanel3;
        private System.Windows.Forms.Label TitleLabel;
        private System.Windows.Forms.NumericUpDown ValueEntry;
    }
}
