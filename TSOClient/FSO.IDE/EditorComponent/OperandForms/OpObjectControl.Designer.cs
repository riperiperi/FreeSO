namespace FSO.IDE.EditorComponent.OperandForms
{
    partial class OpObjectControl
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
            this.ObjectLabel = new System.Windows.Forms.Label();
            this.TitleLabel = new System.Windows.Forms.Label();
            this.EditButton = new System.Windows.Forms.Button();
            this.tableLayoutPanel3.SuspendLayout();
            this.SuspendLayout();
            // 
            // tableLayoutPanel3
            // 
            this.tableLayoutPanel3.AutoSize = true;
            this.tableLayoutPanel3.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.tableLayoutPanel3.ColumnCount = 3;
            this.tableLayoutPanel3.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle());
            this.tableLayoutPanel3.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tableLayoutPanel3.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 40F));
            this.tableLayoutPanel3.Controls.Add(this.ObjectLabel, 1, 0);
            this.tableLayoutPanel3.Controls.Add(this.TitleLabel, 0, 0);
            this.tableLayoutPanel3.Controls.Add(this.EditButton, 2, 0);
            this.tableLayoutPanel3.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tableLayoutPanel3.Location = new System.Drawing.Point(0, 0);
            this.tableLayoutPanel3.Margin = new System.Windows.Forms.Padding(0);
            this.tableLayoutPanel3.MinimumSize = new System.Drawing.Size(0, 26);
            this.tableLayoutPanel3.Name = "tableLayoutPanel3";
            this.tableLayoutPanel3.RowCount = 1;
            this.tableLayoutPanel3.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tableLayoutPanel3.Size = new System.Drawing.Size(222, 29);
            this.tableLayoutPanel3.TabIndex = 13;
            // 
            // ObjectLabel
            // 
            this.ObjectLabel.AutoEllipsis = true;
            this.ObjectLabel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.ObjectLabel.ImageAlign = System.Drawing.ContentAlignment.MiddleRight;
            this.ObjectLabel.Location = new System.Drawing.Point(47, 0);
            this.ObjectLabel.Margin = new System.Windows.Forms.Padding(0);
            this.ObjectLabel.MinimumSize = new System.Drawing.Size(0, 26);
            this.ObjectLabel.Name = "ObjectLabel";
            this.ObjectLabel.Size = new System.Drawing.Size(135, 29);
            this.ObjectLabel.TabIndex = 3;
            this.ObjectLabel.Text = "Restaurant - 4x4 Table Swank";
            this.ObjectLabel.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // TitleLabel
            // 
            this.TitleLabel.AutoSize = true;
            this.TitleLabel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.TitleLabel.Location = new System.Drawing.Point(3, 0);
            this.TitleLabel.MinimumSize = new System.Drawing.Size(0, 26);
            this.TitleLabel.Name = "TitleLabel";
            this.TitleLabel.Size = new System.Drawing.Size(41, 29);
            this.TitleLabel.TabIndex = 1;
            this.TitleLabel.Text = "Object:";
            this.TitleLabel.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // EditButton
            // 
            this.EditButton.Dock = System.Windows.Forms.DockStyle.Fill;
            this.EditButton.Location = new System.Drawing.Point(185, 3);
            this.EditButton.Name = "EditButton";
            this.EditButton.Size = new System.Drawing.Size(34, 23);
            this.EditButton.TabIndex = 2;
            this.EditButton.Text = "Edit";
            this.EditButton.UseVisualStyleBackColor = true;
            this.EditButton.Click += new System.EventHandler(this.EditButton_Click);
            // 
            // OpScopeControl
            // 
            this.AutoSize = true;
            this.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.Controls.Add(this.tableLayoutPanel3);
            this.Margin = new System.Windows.Forms.Padding(0);
            this.Name = "OpScopeControl";
            this.Size = new System.Drawing.Size(222, 29);
            this.tableLayoutPanel3.ResumeLayout(false);
            this.tableLayoutPanel3.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.TableLayoutPanel tableLayoutPanel3;
        private System.Windows.Forms.Label TitleLabel;
        private System.Windows.Forms.Label ObjectLabel;
        private System.Windows.Forms.Button EditButton;
    }
}
