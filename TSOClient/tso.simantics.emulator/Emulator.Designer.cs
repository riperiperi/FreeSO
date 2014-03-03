namespace TSO.Simantics.emulator
{
    partial class Emulator
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
            this.console1 = new TSO.Simantics.emulator.Console();
            this.SuspendLayout();
            // 
            // console1
            // 
            this.console1.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(27)))), ((int)(((byte)(29)))), ((int)(((byte)(30)))));
            this.console1.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this.console1.Location = new System.Drawing.Point(12, 12);
            this.console1.Name = "console1";
            this.console1.Size = new System.Drawing.Size(504, 375);
            this.console1.TabIndex = 0;
            this.console1.Text = "";
            // 
            // Emulator
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.SystemColors.Control;
            this.ClientSize = new System.Drawing.Size(793, 399);
            this.Controls.Add(this.console1);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.SizableToolWindow;
            this.Name = "Emulator";
            this.Text = "Form1";
            this.ResumeLayout(false);

        }

        #endregion

        private Console console1;
    }
}

