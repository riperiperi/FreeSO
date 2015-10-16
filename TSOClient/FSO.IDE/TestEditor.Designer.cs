namespace FSO.IDE
{
    partial class TestEditor
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
            this.panel1 = new System.Windows.Forms.Panel();
            this.bhavViewComponent1 = new FSO.IDE.EditorComponent.BHAVViewComponent();
            this.panel1.SuspendLayout();
            this.SuspendLayout();
            // 
            // panel1
            // 
            this.panel1.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
            this.panel1.Controls.Add(this.bhavViewComponent1);
            this.panel1.Location = new System.Drawing.Point(12, 35);
            this.panel1.Name = "panel1";
            this.panel1.Size = new System.Drawing.Size(646, 486);
            this.panel1.TabIndex = 1;
            // 
            // bhavViewComponent1
            // 
            this.bhavViewComponent1.BackColor = System.Drawing.Color.Black;
            this.bhavViewComponent1.Location = new System.Drawing.Point(3, 3);
            this.bhavViewComponent1.Name = "bhavViewComponent1";
            this.bhavViewComponent1.Size = new System.Drawing.Size(640, 480);
            this.bhavViewComponent1.SuspendOnFormInactive = true;
            this.bhavViewComponent1.TabIndex = 0;
            this.bhavViewComponent1.VSync = false;
            // 
            // TestEditor
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(670, 533);
            this.Controls.Add(this.panel1);
            this.Name = "TestEditor";
            this.Text = "Editor Test";
            this.panel1.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        #endregion

        private EditorComponent.BHAVViewComponent bhavViewComponent1;
        private System.Windows.Forms.Panel panel1;
    }
}

