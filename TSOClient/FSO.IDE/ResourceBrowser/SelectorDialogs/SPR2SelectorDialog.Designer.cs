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
            iffRes = new IFFResComponent();
            SuspendLayout();
            // 
            // iffRes
            // 
            iffRes.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            iffRes.Location = new Point(3, 3);
            iffRes.Margin = new Padding(4, 3, 4, 3);
            iffRes.Name = "iffRes";
            iffRes.Size = new Size(762, 459);
            iffRes.TabIndex = 0;
            // 
            // SPR2SelectorDialog
            // 
            AutoScaleDimensions = new SizeF(96F, 96F);
            AutoScaleMode = AutoScaleMode.Dpi;
            ClientSize = new Size(768, 465);
            Controls.Add(iffRes);
            FormBorderStyle = FormBorderStyle.FixedSingle;
            Icon = (Icon)resources.GetObject("$this.Icon");
            MaximizeBox = false;
            MinimumSize = new Size(784, 504);
            Name = "SPR2SelectorDialog";
            Text = "Select SPR2...";
            ResumeLayout(false);

        }

        #endregion

        private IFFResComponent iffRes;
    }
}