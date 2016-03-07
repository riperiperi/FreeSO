namespace FSO.IDE.ResourceBrowser
{
    partial class SPR2SelectorDialog
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(SPR2SelectorDialog));
            this.iffRes = new FSO.IDE.ResourceBrowser.IFFResComponent();
            this.SuspendLayout();
            // 
            // iffRes
            // 
            this.iffRes.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.iffRes.Location = new System.Drawing.Point(3, 3);
            this.iffRes.Name = "iffRes";
            this.iffRes.Size = new System.Drawing.Size(762, 459);
            this.iffRes.TabIndex = 0;
            // 
            // SPR2SelectorDialog
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(768, 465);
            this.Controls.Add(this.iffRes);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.MaximizeBox = false;
            this.MinimumSize = new System.Drawing.Size(784, 504);
            this.Name = "SPR2SelectorDialog";
            this.Text = "Select SPR2...";
            this.ResumeLayout(false);

        }

        #endregion

        private IFFResComponent iffRes;
    }
}