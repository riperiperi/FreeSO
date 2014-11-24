namespace PDChat
{
    partial class ChatFrm
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
            this.PictureBox1 = new System.Windows.Forms.PictureBox();
            this.LblName = new System.Windows.Forms.Label();
            this.LstParticipants = new System.Windows.Forms.ListBox();
            this.LblParticipants = new System.Windows.Forms.Label();
            this.TxtChat = new System.Windows.Forms.RichTextBox();
            this.TxtMessage = new System.Windows.Forms.TextBox();
            this.BtnSend = new System.Windows.Forms.Button();
            ((System.ComponentModel.ISupportInitialize)(this.PictureBox1)).BeginInit();
            this.SuspendLayout();
            // 
            // PictureBox1
            // 
            this.PictureBox1.Location = new System.Drawing.Point(12, 38);
            this.PictureBox1.Name = "PictureBox1";
            this.PictureBox1.Size = new System.Drawing.Size(46, 39);
            this.PictureBox1.TabIndex = 0;
            this.PictureBox1.TabStop = false;
            // 
            // LblName
            // 
            this.LblName.AutoSize = true;
            this.LblName.Location = new System.Drawing.Point(9, 90);
            this.LblName.Name = "LblName";
            this.LblName.Size = new System.Drawing.Size(35, 13);
            this.LblName.TabIndex = 1;
            this.LblName.Text = "label1";
            // 
            // LstParticipants
            // 
            this.LstParticipants.FormattingEnabled = true;
            this.LstParticipants.Location = new System.Drawing.Point(778, 38);
            this.LstParticipants.Name = "LstParticipants";
            this.LstParticipants.Size = new System.Drawing.Size(151, 446);
            this.LstParticipants.TabIndex = 2;
            // 
            // LblParticipants
            // 
            this.LblParticipants.AutoSize = true;
            this.LblParticipants.Location = new System.Drawing.Point(818, 9);
            this.LblParticipants.Name = "LblParticipants";
            this.LblParticipants.Size = new System.Drawing.Size(62, 13);
            this.LblParticipants.TabIndex = 1;
            this.LblParticipants.Text = "Participants";
            // 
            // TxtChat
            // 
            this.TxtChat.Location = new System.Drawing.Point(121, 38);
            this.TxtChat.Name = "TxtChat";
            this.TxtChat.Size = new System.Drawing.Size(629, 395);
            this.TxtChat.TabIndex = 3;
            this.TxtChat.Text = "";
            // 
            // TxtMessage
            // 
            this.TxtMessage.Location = new System.Drawing.Point(159, 450);
            this.TxtMessage.Name = "TxtMessage";
            this.TxtMessage.Size = new System.Drawing.Size(559, 20);
            this.TxtMessage.TabIndex = 4;
            // 
            // BtnSend
            // 
            this.BtnSend.Location = new System.Drawing.Point(427, 477);
            this.BtnSend.Name = "BtnSend";
            this.BtnSend.Size = new System.Drawing.Size(75, 23);
            this.BtnSend.TabIndex = 5;
            this.BtnSend.Text = "Send";
            this.BtnSend.UseVisualStyleBackColor = true;
            // 
            // ChatFrm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(941, 503);
            this.Controls.Add(this.BtnSend);
            this.Controls.Add(this.TxtMessage);
            this.Controls.Add(this.TxtChat);
            this.Controls.Add(this.LstParticipants);
            this.Controls.Add(this.LblParticipants);
            this.Controls.Add(this.LblName);
            this.Controls.Add(this.PictureBox1);
            this.Name = "ChatFrm";
            this.Text = "ChatFrm";
            ((System.ComponentModel.ISupportInitialize)(this.PictureBox1)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.PictureBox PictureBox1;
        private System.Windows.Forms.Label LblName;
        private System.Windows.Forms.ListBox LstParticipants;
        private System.Windows.Forms.Label LblParticipants;
        private System.Windows.Forms.RichTextBox TxtChat;
        private System.Windows.Forms.TextBox TxtMessage;
        private System.Windows.Forms.Button BtnSend;
    }
}