namespace PDChat
{
    partial class Form1
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
            this.BtnLogin = new System.Windows.Forms.Button();
            this.TxtUsername = new System.Windows.Forms.TextBox();
            this.TxtPassword = new System.Windows.Forms.TextBox();
            this.LblUsername = new System.Windows.Forms.Label();
            this.LblPassword = new System.Windows.Forms.Label();
            this.PictureBox1 = new System.Windows.Forms.PictureBox();
            this.PictureBox2 = new System.Windows.Forms.PictureBox();
            this.PictureBox3 = new System.Windows.Forms.PictureBox();
            this.LblName1 = new System.Windows.Forms.Label();
            this.LblName2 = new System.Windows.Forms.Label();
            this.LblName3 = new System.Windows.Forms.Label();
            this.LblStatus = new System.Windows.Forms.Label();
            this.BtnChat1 = new System.Windows.Forms.Button();
            this.BtnChat2 = new System.Windows.Forms.Button();
            this.BtnChat3 = new System.Windows.Forms.Button();
            ((System.ComponentModel.ISupportInitialize)(this.PictureBox1)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.PictureBox2)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.PictureBox3)).BeginInit();
            this.SuspendLayout();
            // 
            // BtnLogin
            // 
            this.BtnLogin.Location = new System.Drawing.Point(460, 534);
            this.BtnLogin.Name = "BtnLogin";
            this.BtnLogin.Size = new System.Drawing.Size(75, 23);
            this.BtnLogin.TabIndex = 0;
            this.BtnLogin.Text = "Login";
            this.BtnLogin.UseVisualStyleBackColor = true;
            this.BtnLogin.Click += new System.EventHandler(this.BtnLogin_Click);
            // 
            // TxtUsername
            // 
            this.TxtUsername.Location = new System.Drawing.Point(293, 471);
            this.TxtUsername.Name = "TxtUsername";
            this.TxtUsername.Size = new System.Drawing.Size(100, 20);
            this.TxtUsername.TabIndex = 1;
            // 
            // TxtPassword
            // 
            this.TxtPassword.Location = new System.Drawing.Point(588, 471);
            this.TxtPassword.Name = "TxtPassword";
            this.TxtPassword.Size = new System.Drawing.Size(100, 20);
            this.TxtPassword.TabIndex = 1;
            // 
            // LblUsername
            // 
            this.LblUsername.AutoSize = true;
            this.LblUsername.Location = new System.Drawing.Point(310, 434);
            this.LblUsername.Name = "LblUsername";
            this.LblUsername.Size = new System.Drawing.Size(58, 13);
            this.LblUsername.TabIndex = 2;
            this.LblUsername.Text = "Username:";
            // 
            // LblPassword
            // 
            this.LblPassword.AutoSize = true;
            this.LblPassword.Location = new System.Drawing.Point(609, 434);
            this.LblPassword.Name = "LblPassword";
            this.LblPassword.Size = new System.Drawing.Size(56, 13);
            this.LblPassword.TabIndex = 2;
            this.LblPassword.Text = "Password:";
            // 
            // PictureBox1
            // 
            this.PictureBox1.Location = new System.Drawing.Point(115, 159);
            this.PictureBox1.Name = "PictureBox1";
            this.PictureBox1.Size = new System.Drawing.Size(100, 50);
            this.PictureBox1.TabIndex = 3;
            this.PictureBox1.TabStop = false;
            this.PictureBox1.Visible = false;
            // 
            // PictureBox2
            // 
            this.PictureBox2.Location = new System.Drawing.Point(444, 159);
            this.PictureBox2.Name = "PictureBox2";
            this.PictureBox2.Size = new System.Drawing.Size(100, 50);
            this.PictureBox2.TabIndex = 3;
            this.PictureBox2.TabStop = false;
            this.PictureBox2.Visible = false;
            // 
            // PictureBox3
            // 
            this.PictureBox3.Location = new System.Drawing.Point(760, 159);
            this.PictureBox3.Name = "PictureBox3";
            this.PictureBox3.Size = new System.Drawing.Size(100, 50);
            this.PictureBox3.TabIndex = 3;
            this.PictureBox3.TabStop = false;
            this.PictureBox3.Visible = false;
            // 
            // LblName1
            // 
            this.LblName1.AutoSize = true;
            this.LblName1.Location = new System.Drawing.Point(140, 232);
            this.LblName1.Name = "LblName1";
            this.LblName1.Size = new System.Drawing.Size(58, 13);
            this.LblName1.TabIndex = 2;
            this.LblName1.Text = "Username:";
            this.LblName1.Visible = false;
            // 
            // LblName2
            // 
            this.LblName2.AutoSize = true;
            this.LblName2.Location = new System.Drawing.Point(466, 232);
            this.LblName2.Name = "LblName2";
            this.LblName2.Size = new System.Drawing.Size(58, 13);
            this.LblName2.TabIndex = 2;
            this.LblName2.Text = "Username:";
            this.LblName2.Visible = false;
            // 
            // LblName3
            // 
            this.LblName3.AutoSize = true;
            this.LblName3.Location = new System.Drawing.Point(779, 232);
            this.LblName3.Name = "LblName3";
            this.LblName3.Size = new System.Drawing.Size(58, 13);
            this.LblName3.TabIndex = 2;
            this.LblName3.Text = "Username:";
            this.LblName3.Visible = false;
            // 
            // LblStatus
            // 
            this.LblStatus.AutoSize = true;
            this.LblStatus.Location = new System.Drawing.Point(466, 500);
            this.LblStatus.Name = "LblStatus";
            this.LblStatus.Size = new System.Drawing.Size(70, 13);
            this.LblStatus.TabIndex = 2;
            this.LblStatus.Text = "Connecting...";
            this.LblStatus.Visible = false;
            // 
            // BtnChat1
            // 
            this.BtnChat1.Location = new System.Drawing.Point(143, 272);
            this.BtnChat1.Name = "BtnChat1";
            this.BtnChat1.Size = new System.Drawing.Size(55, 23);
            this.BtnChat1.TabIndex = 4;
            this.BtnChat1.Text = "Chat!";
            this.BtnChat1.UseVisualStyleBackColor = true;
            this.BtnChat1.Visible = false;
            // 
            // BtnChat2
            // 
            this.BtnChat2.Location = new System.Drawing.Point(469, 272);
            this.BtnChat2.Name = "BtnChat2";
            this.BtnChat2.Size = new System.Drawing.Size(55, 23);
            this.BtnChat2.TabIndex = 4;
            this.BtnChat2.Text = "Chat!";
            this.BtnChat2.UseVisualStyleBackColor = true;
            this.BtnChat2.Visible = false;
            // 
            // BtnChat3
            // 
            this.BtnChat3.Location = new System.Drawing.Point(782, 272);
            this.BtnChat3.Name = "BtnChat3";
            this.BtnChat3.Size = new System.Drawing.Size(55, 23);
            this.BtnChat3.TabIndex = 4;
            this.BtnChat3.Text = "Chat!";
            this.BtnChat3.UseVisualStyleBackColor = true;
            this.BtnChat3.Visible = false;
            // 
            // Form1
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(964, 569);
            this.Controls.Add(this.BtnChat3);
            this.Controls.Add(this.BtnChat2);
            this.Controls.Add(this.BtnChat1);
            this.Controls.Add(this.PictureBox3);
            this.Controls.Add(this.PictureBox2);
            this.Controls.Add(this.PictureBox1);
            this.Controls.Add(this.LblPassword);
            this.Controls.Add(this.LblName3);
            this.Controls.Add(this.LblName2);
            this.Controls.Add(this.LblName1);
            this.Controls.Add(this.LblStatus);
            this.Controls.Add(this.LblUsername);
            this.Controls.Add(this.TxtPassword);
            this.Controls.Add(this.TxtUsername);
            this.Controls.Add(this.BtnLogin);
            this.Name = "Form1";
            this.Text = "Project Dollhouse Chat";
            ((System.ComponentModel.ISupportInitialize)(this.PictureBox1)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.PictureBox2)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.PictureBox3)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Button BtnLogin;
        private System.Windows.Forms.TextBox TxtUsername;
        private System.Windows.Forms.TextBox TxtPassword;
        private System.Windows.Forms.Label LblUsername;
        private System.Windows.Forms.Label LblPassword;
        private System.Windows.Forms.PictureBox PictureBox1;
        private System.Windows.Forms.PictureBox PictureBox2;
        private System.Windows.Forms.PictureBox PictureBox3;
        private System.Windows.Forms.Label LblName1;
        private System.Windows.Forms.Label LblName2;
        private System.Windows.Forms.Label LblName3;
        private System.Windows.Forms.Label LblStatus;
        private System.Windows.Forms.Button BtnChat1;
        private System.Windows.Forms.Button BtnChat2;
        private System.Windows.Forms.Button BtnChat3;
    }
}

