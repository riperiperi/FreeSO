namespace FSO.IDE.ResourceBrowser
{
    partial class OBJDSelectorControl
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
            this.SelectButton = new System.Windows.Forms.Button();
            this.SelectCombo = new System.Windows.Forms.ComboBox();
            this.SuspendLayout();
            // 
            // SelectButton
            // 
            this.SelectButton.BackColor = System.Drawing.SystemColors.Window;
            this.SelectButton.Dock = System.Windows.Forms.DockStyle.Fill;
            this.SelectButton.Location = new System.Drawing.Point(0, 0);
            this.SelectButton.Name = "SelectButton";
            this.SelectButton.Size = new System.Drawing.Size(249, 30);
            this.SelectButton.TabIndex = 0;
            this.SelectButton.Text = "Select";
            this.SelectButton.UseVisualStyleBackColor = false;
            this.SelectButton.Click += new System.EventHandler(this.SelectButton_Click);
            // 
            // SelectCombo
            // 
            this.SelectCombo.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.SelectCombo.FormattingEnabled = true;
            this.SelectCombo.Location = new System.Drawing.Point(3, 5);
            this.SelectCombo.Name = "SelectCombo";
            this.SelectCombo.Size = new System.Drawing.Size(162, 21);
            this.SelectCombo.TabIndex = 1;
            this.SelectCombo.Visible = false;
            this.SelectCombo.SelectedIndexChanged += new System.EventHandler(this.SelectCombo_SelectedIndexChanged);
            // 
            // OBJDSelectorControl
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.SelectCombo);
            this.Controls.Add(this.SelectButton);
            this.Name = "OBJDSelectorControl";
            this.Size = new System.Drawing.Size(249, 30);
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.Button SelectButton;
        private System.Windows.Forms.ComboBox SelectCombo;
    }
}
