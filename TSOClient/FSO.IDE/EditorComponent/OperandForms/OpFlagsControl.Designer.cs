namespace FSO.IDE.EditorComponent.OperandForms
{
    partial class OpFlagsControl
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
            this.FlagsLabel = new System.Windows.Forms.Label();
            this.FlagsPanel = new System.Windows.Forms.FlowLayoutPanel();
            this.checkBox1 = new System.Windows.Forms.CheckBox();
            this.checkBox2 = new System.Windows.Forms.CheckBox();
            this.checkBox3 = new System.Windows.Forms.CheckBox();
            this.checkBox4 = new System.Windows.Forms.CheckBox();
            this.checkBox5 = new System.Windows.Forms.CheckBox();
            this.checkBox6 = new System.Windows.Forms.CheckBox();
            this.FlagsPanel.SuspendLayout();
            this.SuspendLayout();
            // 
            // MainLayout
            // 

            // 
            // FlagsLabel
            // 
            this.FlagsLabel.AutoSize = true;
            this.FlagsLabel.Location = new System.Drawing.Point(3, 3);
            this.FlagsLabel.Margin = new System.Windows.Forms.Padding(3, 3, 3, 0);
            this.FlagsLabel.Name = "FlagsLabel";
            this.FlagsLabel.Size = new System.Drawing.Size(35, 13);
            this.FlagsLabel.TabIndex = 0;
            this.FlagsLabel.Text = "Flags:";
            // 
            // FlagsPanel
            // 
            this.FlagsPanel.AutoSize = true;
            this.FlagsPanel.Controls.Add(this.checkBox1);
            this.FlagsPanel.Controls.Add(this.checkBox2);
            this.FlagsPanel.Controls.Add(this.checkBox3);
            this.FlagsPanel.Controls.Add(this.checkBox4);
            this.FlagsPanel.Controls.Add(this.checkBox5);
            this.FlagsPanel.Controls.Add(this.checkBox6);
            this.FlagsPanel.Location = new System.Drawing.Point(44, 3);
            this.FlagsPanel.MinimumSize = new System.Drawing.Size(230, 15);
            this.FlagsPanel.Name = "FlagsPanel";
            this.FlagsPanel.Size = new System.Drawing.Size(741, 17);
            this.FlagsPanel.TabIndex = 1;
            // 
            // checkBox1
            // 
            this.checkBox1.AutoSize = true;
            this.checkBox1.Location = new System.Drawing.Point(3, 0);
            this.checkBox1.Margin = new System.Windows.Forms.Padding(3, 0, 3, 0);
            this.checkBox1.Name = "checkBox1";
            this.checkBox1.Size = new System.Drawing.Size(195, 17);
            this.checkBox1.TabIndex = 0;
            this.checkBox1.Text = "Use Stack Parameter 0 to specify id";
            this.checkBox1.UseVisualStyleBackColor = true;
            // 
            // checkBox2
            // 
            this.checkBox2.AutoSize = true;
            this.checkBox2.Location = new System.Drawing.Point(204, 0);
            this.checkBox2.Margin = new System.Windows.Forms.Padding(3, 0, 3, 0);
            this.checkBox2.Name = "checkBox2";
            this.checkBox2.Size = new System.Drawing.Size(151, 17);
            this.checkBox2.TabIndex = 1;
            this.checkBox2.Text = "Play Animation Backwards";
            this.checkBox2.UseVisualStyleBackColor = true;
            // 
            // checkBox3
            // 
            this.checkBox3.AutoSize = true;
            this.checkBox3.Location = new System.Drawing.Point(361, 0);
            this.checkBox3.Margin = new System.Windows.Forms.Padding(3, 0, 3, 0);
            this.checkBox3.Name = "checkBox3";
            this.checkBox3.Size = new System.Drawing.Size(85, 17);
            this.checkBox3.TabIndex = 2;
            this.checkBox3.Text = "Interruptable";
            this.checkBox3.UseVisualStyleBackColor = true;
            // 
            // checkBox4
            // 
            this.checkBox4.AutoSize = true;
            this.checkBox4.Location = new System.Drawing.Point(452, 0);
            this.checkBox4.Margin = new System.Windows.Forms.Padding(3, 0, 3, 0);
            this.checkBox4.Name = "checkBox4";
            this.checkBox4.Size = new System.Drawing.Size(71, 17);
            this.checkBox4.TabIndex = 3;
            this.checkBox4.Text = "Hurryable";
            this.checkBox4.UseVisualStyleBackColor = true;
            // 
            // checkBox5
            // 
            this.checkBox5.AutoSize = true;
            this.checkBox5.Location = new System.Drawing.Point(529, 0);
            this.checkBox5.Margin = new System.Windows.Forms.Padding(3, 0, 3, 0);
            this.checkBox5.Name = "checkBox5";
            this.checkBox5.Size = new System.Drawing.Size(73, 17);
            this.checkBox5.TabIndex = 4;
            this.checkBox5.Text = "Full Reset";
            this.checkBox5.UseVisualStyleBackColor = true;
            // 
            // checkBox6
            // 
            this.checkBox6.AutoSize = true;
            this.checkBox6.Location = new System.Drawing.Point(608, 0);
            this.checkBox6.Margin = new System.Windows.Forms.Padding(3, 0, 3, 0);
            this.checkBox6.Name = "checkBox6";
            this.checkBox6.Size = new System.Drawing.Size(130, 17);
            this.checkBox6.TabIndex = 5;
            this.checkBox6.Text = "Place Events In Local";
            this.checkBox6.UseVisualStyleBackColor = true;
            // 
            // OpFlagsControl
            // 



            this.AutoSize = true;
            this.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.Controls.Add(this.FlagsLabel);
            this.Controls.Add(this.FlagsPanel);
            this.Dock = System.Windows.Forms.DockStyle.Fill;
            this.Margin = new System.Windows.Forms.Padding(0);
            this.MinimumSize = new System.Drawing.Size(0, 10);

            this.Name = "OpFlagsControl";
            this.Size = new System.Drawing.Size(788, 23);
            this.FlagsPanel.ResumeLayout(false);
            this.FlagsPanel.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion
        private System.Windows.Forms.Label FlagsLabel;
        private System.Windows.Forms.FlowLayoutPanel FlagsPanel;
        private System.Windows.Forms.CheckBox checkBox1;
        private System.Windows.Forms.CheckBox checkBox2;
        private System.Windows.Forms.CheckBox checkBox3;
        private System.Windows.Forms.CheckBox checkBox4;
        private System.Windows.Forms.CheckBox checkBox5;
        private System.Windows.Forms.CheckBox checkBox6;
    }
}
