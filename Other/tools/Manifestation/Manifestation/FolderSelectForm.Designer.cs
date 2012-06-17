namespace Manifestation
{
    partial class FolderSelectForm
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
            this.LstFiles = new System.Windows.Forms.ListBox();
            this.TxtCurrentFolder = new System.Windows.Forms.TextBox();
            this.label1 = new System.Windows.Forms.Label();
            this.BtnAddFolder = new System.Windows.Forms.Button();
            this.BtnReplace = new System.Windows.Forms.Button();
            this.SuspendLayout();
            // 
            // LstFiles
            // 
            this.LstFiles.FormattingEnabled = true;
            this.LstFiles.Location = new System.Drawing.Point(12, 12);
            this.LstFiles.Name = "LstFiles";
            this.LstFiles.Size = new System.Drawing.Size(159, 290);
            this.LstFiles.TabIndex = 0;
            // 
            // TxtCurrentFolder
            // 
            this.TxtCurrentFolder.Location = new System.Drawing.Point(236, 38);
            this.TxtCurrentFolder.Name = "TxtCurrentFolder";
            this.TxtCurrentFolder.Size = new System.Drawing.Size(156, 20);
            this.TxtCurrentFolder.TabIndex = 1;
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(233, 12);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(73, 13);
            this.label1.TabIndex = 2;
            this.label1.Text = "Current folder:";
            // 
            // BtnAddFolder
            // 
            this.BtnAddFolder.Location = new System.Drawing.Point(236, 89);
            this.BtnAddFolder.Name = "BtnAddFolder";
            this.BtnAddFolder.Size = new System.Drawing.Size(156, 23);
            this.BtnAddFolder.TabIndex = 3;
            this.BtnAddFolder.Text = "Add selected files to folder";
            this.BtnAddFolder.UseVisualStyleBackColor = true;
            this.BtnAddFolder.Click += new System.EventHandler(this.BtnAddFolder_Click);
            // 
            // BtnReplace
            // 
            this.BtnReplace.Location = new System.Drawing.Point(236, 118);
            this.BtnReplace.Name = "BtnReplace";
            this.BtnReplace.Size = new System.Drawing.Size(156, 23);
            this.BtnReplace.TabIndex = 4;
            this.BtnReplace.Text = "Replace";
            this.BtnReplace.UseVisualStyleBackColor = true;
            this.BtnReplace.Click += new System.EventHandler(this.BtnReplace_Click);
            // 
            // FolderSelectForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(404, 319);
            this.Controls.Add(this.BtnReplace);
            this.Controls.Add(this.BtnAddFolder);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.TxtCurrentFolder);
            this.Controls.Add(this.LstFiles);
            this.Name = "FolderSelectForm";
            this.Text = "Folder Selection";
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.ListBox LstFiles;
        private System.Windows.Forms.TextBox TxtCurrentFolder;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Button BtnAddFolder;
        private System.Windows.Forms.Button BtnReplace;
    }
}