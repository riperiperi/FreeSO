namespace PDPatcher
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(Form1));
            this.BtnExit = new System.Windows.Forms.PictureBox();
            this.BtnMinimize = new System.Windows.Forms.PictureBox();
            this.LblApplicationName = new System.Windows.Forms.Label();
            this.TSOLogo = new System.Windows.Forms.PictureBox();
            this.TxtProgressDescription = new System.Windows.Forms.TextBox();
            this.TxtProgressLeft = new System.Windows.Forms.PictureBox();
            this.TxtProgressRight = new System.Windows.Forms.PictureBox();
            this.LblProgressDescription = new System.Windows.Forms.Label();
            this.TxtOverallProgress = new System.Windows.Forms.TextBox();
            this.TxtOverallProgressRight = new System.Windows.Forms.PictureBox();
            this.TxtOverallProgressLeft = new System.Windows.Forms.PictureBox();
            this.BtnQuit = new System.Windows.Forms.Button();
            this.BtnAbout = new System.Windows.Forms.Button();
            ((System.ComponentModel.ISupportInitialize)(this.BtnExit)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.BtnMinimize)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.TSOLogo)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.TxtProgressLeft)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.TxtProgressRight)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.TxtOverallProgressRight)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.TxtOverallProgressLeft)).BeginInit();
            this.SuspendLayout();
            // 
            // BtnExit
            // 
            this.BtnExit.Location = new System.Drawing.Point(533, 22);
            this.BtnExit.Name = "BtnExit";
            this.BtnExit.Size = new System.Drawing.Size(20, 21);
            this.BtnExit.TabIndex = 0;
            this.BtnExit.TabStop = false;
            // 
            // BtnMinimize
            // 
            this.BtnMinimize.Location = new System.Drawing.Point(511, 13);
            this.BtnMinimize.Name = "BtnMinimize";
            this.BtnMinimize.Size = new System.Drawing.Size(17, 16);
            this.BtnMinimize.TabIndex = 1;
            this.BtnMinimize.TabStop = false;
            // 
            // LblApplicationName
            // 
            this.LblApplicationName.AutoSize = true;
            this.LblApplicationName.Font = new System.Drawing.Font("Comic Sans MS", 10F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.LblApplicationName.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(255)))), ((int)(((byte)(255)))), ((int)(((byte)(128)))));
            this.LblApplicationName.Location = new System.Drawing.Point(174, 22);
            this.LblApplicationName.Name = "LblApplicationName";
            this.LblApplicationName.Size = new System.Drawing.Size(230, 19);
            this.LblApplicationName.TabIndex = 2;
            this.LblApplicationName.Text = "Project Dollhouse Update Utility";
            // 
            // TSOLogo
            // 
            this.TSOLogo.Image = global::PDPatcher.Properties.Resources._4a552ba5_TSOLogo;
            this.TSOLogo.Location = new System.Drawing.Point(23, 52);
            this.TSOLogo.Name = "TSOLogo";
            this.TSOLogo.Size = new System.Drawing.Size(77, 63);
            this.TSOLogo.TabIndex = 4;
            this.TSOLogo.TabStop = false;
            // 
            // TxtProgressDescription
            // 
            this.TxtProgressDescription.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this.TxtProgressDescription.Font = new System.Drawing.Font("Comic Sans MS", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.TxtProgressDescription.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(255)))), ((int)(((byte)(255)))), ((int)(((byte)(128)))));
            this.TxtProgressDescription.Location = new System.Drawing.Point(23, 138);
            this.TxtProgressDescription.Name = "TxtProgressDescription";
            this.TxtProgressDescription.ReadOnly = true;
            this.TxtProgressDescription.Size = new System.Drawing.Size(519, 16);
            this.TxtProgressDescription.TabIndex = 5;
            // 
            // TxtProgressLeft
            // 
            this.TxtProgressLeft.Image = ((System.Drawing.Image)(resources.GetObject("TxtProgressLeft.Image")));
            this.TxtProgressLeft.Location = new System.Drawing.Point(23, 138);
            this.TxtProgressLeft.Name = "TxtProgressLeft";
            this.TxtProgressLeft.Size = new System.Drawing.Size(10, 17);
            this.TxtProgressLeft.TabIndex = 6;
            this.TxtProgressLeft.TabStop = false;
            // 
            // TxtProgressRight
            // 
            this.TxtProgressRight.Image = global::PDPatcher.Properties.Resources.TxtRight;
            this.TxtProgressRight.Location = new System.Drawing.Point(533, 138);
            this.TxtProgressRight.Name = "TxtProgressRight";
            this.TxtProgressRight.Size = new System.Drawing.Size(9, 17);
            this.TxtProgressRight.TabIndex = 7;
            this.TxtProgressRight.TabStop = false;
            // 
            // LblProgressDescription
            // 
            this.LblProgressDescription.AutoSize = true;
            this.LblProgressDescription.Font = new System.Drawing.Font("Comic Sans MS", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.LblProgressDescription.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(255)))), ((int)(((byte)(255)))), ((int)(((byte)(128)))));
            this.LblProgressDescription.Location = new System.Drawing.Point(20, 167);
            this.LblProgressDescription.Name = "LblProgressDescription";
            this.LblProgressDescription.Size = new System.Drawing.Size(84, 16);
            this.LblProgressDescription.TabIndex = 8;
            this.LblProgressDescription.Text = "Starting up...";
            // 
            // TxtOverallProgress
            // 
            this.TxtOverallProgress.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this.TxtOverallProgress.Font = new System.Drawing.Font("Comic Sans MS", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.TxtOverallProgress.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(255)))), ((int)(((byte)(255)))), ((int)(((byte)(128)))));
            this.TxtOverallProgress.Location = new System.Drawing.Point(31, 200);
            this.TxtOverallProgress.Name = "TxtOverallProgress";
            this.TxtOverallProgress.ReadOnly = true;
            this.TxtOverallProgress.Size = new System.Drawing.Size(511, 16);
            this.TxtOverallProgress.TabIndex = 5;
            // 
            // TxtOverallProgressRight
            // 
            this.TxtOverallProgressRight.Image = global::PDPatcher.Properties.Resources.TxtRight;
            this.TxtOverallProgressRight.Location = new System.Drawing.Point(533, 200);
            this.TxtOverallProgressRight.Name = "TxtOverallProgressRight";
            this.TxtOverallProgressRight.Size = new System.Drawing.Size(9, 17);
            this.TxtOverallProgressRight.TabIndex = 7;
            this.TxtOverallProgressRight.TabStop = false;
            // 
            // TxtOverallProgressLeft
            // 
            this.TxtOverallProgressLeft.Image = ((System.Drawing.Image)(resources.GetObject("TxtOverallProgressLeft.Image")));
            this.TxtOverallProgressLeft.Location = new System.Drawing.Point(23, 200);
            this.TxtOverallProgressLeft.Name = "TxtOverallProgressLeft";
            this.TxtOverallProgressLeft.Size = new System.Drawing.Size(10, 17);
            this.TxtOverallProgressLeft.TabIndex = 6;
            this.TxtOverallProgressLeft.TabStop = false;
            // 
            // BtnQuit
            // 
            this.BtnQuit.Font = new System.Drawing.Font("Comic Sans MS", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.BtnQuit.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(255)))), ((int)(((byte)(255)))), ((int)(((byte)(128)))));
            this.BtnQuit.Location = new System.Drawing.Point(453, 221);
            this.BtnQuit.Name = "BtnQuit";
            this.BtnQuit.Size = new System.Drawing.Size(64, 34);
            this.BtnQuit.TabIndex = 9;
            this.BtnQuit.Text = "Quit";
            this.BtnQuit.UseVisualStyleBackColor = true;
            // 
            // BtnAbout
            // 
            this.BtnAbout.Font = new System.Drawing.Font("Comic Sans MS", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.BtnAbout.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(255)))), ((int)(((byte)(255)))), ((int)(((byte)(128)))));
            this.BtnAbout.Location = new System.Drawing.Point(40, 221);
            this.BtnAbout.Name = "BtnAbout";
            this.BtnAbout.Size = new System.Drawing.Size(64, 34);
            this.BtnAbout.TabIndex = 9;
            this.BtnAbout.Text = "About";
            this.BtnAbout.UseVisualStyleBackColor = true;
            // 
            // Form1
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackgroundImage = global::PDPatcher.Properties.Resources.DialogBackMain;
            this.ClientSize = new System.Drawing.Size(558, 267);
            this.Controls.Add(this.BtnAbout);
            this.Controls.Add(this.BtnQuit);
            this.Controls.Add(this.LblProgressDescription);
            this.Controls.Add(this.TxtOverallProgressRight);
            this.Controls.Add(this.TxtProgressRight);
            this.Controls.Add(this.TxtOverallProgressLeft);
            this.Controls.Add(this.TxtProgressLeft);
            this.Controls.Add(this.TxtOverallProgress);
            this.Controls.Add(this.TxtProgressDescription);
            this.Controls.Add(this.TSOLogo);
            this.Controls.Add(this.LblApplicationName);
            this.Controls.Add(this.BtnMinimize);
            this.Controls.Add(this.BtnExit);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.None;
            this.Name = "Form1";
            this.Text = "Form1";
            ((System.ComponentModel.ISupportInitialize)(this.BtnExit)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.BtnMinimize)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.TSOLogo)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.TxtProgressLeft)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.TxtProgressRight)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.TxtOverallProgressRight)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.TxtOverallProgressLeft)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.PictureBox BtnExit;
        private System.Windows.Forms.PictureBox BtnMinimize;
        private System.Windows.Forms.Label LblApplicationName;
        private System.Windows.Forms.PictureBox TSOLogo;
        public System.Windows.Forms.TextBox TxtProgressDescription;
        private System.Windows.Forms.PictureBox TxtProgressLeft;
        private System.Windows.Forms.PictureBox TxtProgressRight;
        private System.Windows.Forms.Label LblProgressDescription;
        public System.Windows.Forms.TextBox TxtOverallProgress;
        private System.Windows.Forms.PictureBox TxtOverallProgressRight;
        private System.Windows.Forms.PictureBox TxtOverallProgressLeft;
        private System.Windows.Forms.Button BtnQuit;
        private System.Windows.Forms.Button BtnAbout;


    }
}

