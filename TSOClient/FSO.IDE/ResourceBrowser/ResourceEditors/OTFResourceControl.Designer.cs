namespace FSO.IDE.ResourceBrowser.ResourceEditors
{
    partial class OTFResourceControl
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
            this.XMLDisplay = new System.Windows.Forms.TextBox();
            this.label1 = new System.Windows.Forms.Label();
            this.SuspendLayout();
            // 
            // XMLDisplay
            // 
            this.XMLDisplay.Location = new System.Drawing.Point(3, 24);
            this.XMLDisplay.Multiline = true;
            this.XMLDisplay.Name = "XMLDisplay";
            this.XMLDisplay.ReadOnly = true;
            this.XMLDisplay.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
            this.XMLDisplay.Size = new System.Drawing.Size(496, 428);
            this.XMLDisplay.TabIndex = 0;
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(3, 8);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(69, 13);
            this.label1.TabIndex = 1;
            this.label1.Text = "XML Display:";
            // 
            // OTFResourceControl
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.label1);
            this.Controls.Add(this.XMLDisplay);
            this.Name = "OTFResourceControl";
            this.Size = new System.Drawing.Size(502, 455);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.TextBox XMLDisplay;
        private System.Windows.Forms.Label label1;
    }
}
