namespace FSO.IDE.Common
{
    partial class NewObjectDialog
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(NewObjectDialog));
            this.GUIDText = new System.Windows.Forms.Label();
            this.ChunkLabelEntry = new System.Windows.Forms.TextBox();
            this.NameText = new System.Windows.Forms.Label();
            this.CancelButton = new System.Windows.Forms.Button();
            this.OKButton = new System.Windows.Forms.Button();
            this.GUIDEntry = new System.Windows.Forms.TextBox();
            this.RandomGUID = new System.Windows.Forms.Button();
            this.label1 = new System.Windows.Forms.Label();
            this.SuspendLayout();
            // 
            // GUIDText
            // 
            this.GUIDText.AutoSize = true;
            this.GUIDText.Location = new System.Drawing.Point(12, 96);
            this.GUIDText.Name = "GUIDText";
            this.GUIDText.Size = new System.Drawing.Size(34, 13);
            this.GUIDText.TabIndex = 10;
            this.GUIDText.Text = "GUID";
            // 
            // ChunkLabelEntry
            // 
            this.ChunkLabelEntry.Location = new System.Drawing.Point(12, 73);
            this.ChunkLabelEntry.Name = "ChunkLabelEntry";
            this.ChunkLabelEntry.Size = new System.Drawing.Size(296, 20);
            this.ChunkLabelEntry.TabIndex = 9;
            // 
            // NameText
            // 
            this.NameText.AutoSize = true;
            this.NameText.Location = new System.Drawing.Point(12, 57);
            this.NameText.Name = "NameText";
            this.NameText.Size = new System.Drawing.Size(69, 13);
            this.NameText.TabIndex = 8;
            this.NameText.Text = "Object Name";
            // 
            // CancelButton
            // 
            this.CancelButton.Location = new System.Drawing.Point(152, 138);
            this.CancelButton.Name = "CancelButton";
            this.CancelButton.Size = new System.Drawing.Size(75, 23);
            this.CancelButton.TabIndex = 7;
            this.CancelButton.Text = "Cancel";
            this.CancelButton.UseVisualStyleBackColor = true;
            // 
            // OKButton
            // 
            this.OKButton.Location = new System.Drawing.Point(233, 138);
            this.OKButton.Name = "OKButton";
            this.OKButton.Size = new System.Drawing.Size(75, 23);
            this.OKButton.TabIndex = 6;
            this.OKButton.Text = "OK";
            this.OKButton.UseVisualStyleBackColor = true;
            this.OKButton.Click += new System.EventHandler(this.OKButton_Click);
            // 
            // GUIDEntry
            // 
            this.GUIDEntry.Location = new System.Drawing.Point(12, 112);
            this.GUIDEntry.Name = "GUIDEntry";
            this.GUIDEntry.Size = new System.Drawing.Size(114, 20);
            this.GUIDEntry.TabIndex = 11;
            // 
            // RandomGUID
            // 
            this.RandomGUID.Location = new System.Drawing.Point(12, 138);
            this.RandomGUID.Name = "RandomGUID";
            this.RandomGUID.Size = new System.Drawing.Size(114, 23);
            this.RandomGUID.TabIndex = 12;
            this.RandomGUID.Text = "Random GUID";
            this.RandomGUID.UseVisualStyleBackColor = true;
            this.RandomGUID.Click += new System.EventHandler(this.RandomGUID_Click);
            // 
            // label1
            // 
            this.label1.Location = new System.Drawing.Point(11, 9);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(297, 39);
            this.label1.TabIndex = 13;
            this.label1.Text = "Enter a name and GUID for the new Object. You can define your own GUID, or click " +
    "\"Random GUID\" to get a random one that\'s not taken.";
            // 
            // NewObjectDialog
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(320, 173);
            this.ControlBox = false;
            this.Controls.Add(this.label1);
            this.Controls.Add(this.RandomGUID);
            this.Controls.Add(this.GUIDEntry);
            this.Controls.Add(this.GUIDText);
            this.Controls.Add(this.ChunkLabelEntry);
            this.Controls.Add(this.NameText);
            this.Controls.Add(this.CancelButton);
            this.Controls.Add(this.OKButton);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.Name = "NewObjectDialog";
            this.Text = "Creating a new Object...";
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion
        private System.Windows.Forms.Label GUIDText;
        private System.Windows.Forms.TextBox ChunkLabelEntry;
        private System.Windows.Forms.Label NameText;
        private System.Windows.Forms.Button CancelButton;
        private System.Windows.Forms.Button OKButton;
        private System.Windows.Forms.TextBox GUIDEntry;
        private System.Windows.Forms.Button RandomGUID;
        private System.Windows.Forms.Label label1;
    }
}