namespace XNAWinForms
{
    partial class XNAWinForm
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
            this.panelViewport = new System.Windows.Forms.Panel();
            this.SuspendLayout();
            // 
            // panelViewport
            // 
            this.panelViewport.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.panelViewport.Dock = System.Windows.Forms.DockStyle.Fill;
            this.panelViewport.Location = new System.Drawing.Point(0, 0);
            this.panelViewport.Name = "panelViewport";
            this.panelViewport.Size = new System.Drawing.Size(518, 377);
            this.panelViewport.TabIndex = 1;
            this.panelViewport.Resize += new System.EventHandler(this.OnViewportResize);
            this.panelViewport.BackColorChanged += new System.EventHandler(this.panelViewport_BackColorChanged);
            this.panelViewport.Paint += new System.Windows.Forms.PaintEventHandler(this.OnVieweportPaint);
            // 
            // XNAWinForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(518, 377);
            this.Controls.Add(this.panelViewport);
            this.DoubleBuffered = true;
            this.Name = "XNAWinForm";
            this.Text = "XNA WinForms";
            this.ResumeLayout(false);

        }

        #endregion

        protected System.Windows.Forms.Panel panelViewport;

    }
}

