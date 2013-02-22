namespace Dressup
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
            this.panel1 = new System.Windows.Forms.Panel();
            this.LstBodies = new System.Windows.Forms.ListBox();
            this.LstAppearances = new System.Windows.Forms.ListBox();
            this.LstHeads = new System.Windows.Forms.ListBox();
            this.LblAppearances = new System.Windows.Forms.Label();
            this.label1 = new System.Windows.Forms.Label();
            this.label2 = new System.Windows.Forms.Label();
            this.menuStrip1 = new System.Windows.Forms.MenuStrip();
            this.fileToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.exitToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.helpToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.aboutTSODressUpToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.BtnSkeleton = new System.Windows.Forms.Button();
            this.panel1.SuspendLayout();
            this.menuStrip1.SuspendLayout();
            this.SuspendLayout();
            // 
            // panelViewport
            // 
            this.panelViewport.Anchor = System.Windows.Forms.AnchorStyles.Left;
            this.panelViewport.BackColor = System.Drawing.Color.SteelBlue;
            this.panelViewport.Dock = System.Windows.Forms.DockStyle.None;
            this.panelViewport.Location = new System.Drawing.Point(12, 67);
            this.panelViewport.Size = new System.Drawing.Size(605, 624);
            // 
            // panel1
            // 
            this.panel1.Controls.Add(this.LstBodies);
            this.panel1.Controls.Add(this.LstAppearances);
            this.panel1.Controls.Add(this.LstHeads);
            this.panel1.Controls.Add(this.LblAppearances);
            this.panel1.Controls.Add(this.label1);
            this.panel1.Controls.Add(this.label2);
            this.panel1.Dock = System.Windows.Forms.DockStyle.Right;
            this.panel1.Location = new System.Drawing.Point(623, 24);
            this.panel1.Name = "panel1";
            this.panel1.Size = new System.Drawing.Size(294, 679);
            this.panel1.TabIndex = 3;
            // 
            // LstBodies
            // 
            this.LstBodies.FormattingEnabled = true;
            this.LstBodies.Location = new System.Drawing.Point(13, 263);
            this.LstBodies.Name = "LstBodies";
            this.LstBodies.Size = new System.Drawing.Size(269, 212);
            this.LstBodies.TabIndex = 5;
            this.LstBodies.SelectedIndexChanged += new System.EventHandler(this.LstBodies_SelectedIndexChanged);
            // 
            // LstAppearances
            // 
            this.LstAppearances.FormattingEnabled = true;
            this.LstAppearances.Location = new System.Drawing.Point(13, 520);
            this.LstAppearances.Name = "LstAppearances";
            this.LstAppearances.Size = new System.Drawing.Size(269, 147);
            this.LstAppearances.TabIndex = 5;
            // 
            // LstHeads
            // 
            this.LstHeads.FormattingEnabled = true;
            this.LstHeads.Location = new System.Drawing.Point(13, 43);
            this.LstHeads.Name = "LstHeads";
            this.LstHeads.Size = new System.Drawing.Size(269, 173);
            this.LstHeads.TabIndex = 5;
            // 
            // LblAppearances
            // 
            this.LblAppearances.AutoSize = true;
            this.LblAppearances.Location = new System.Drawing.Point(10, 493);
            this.LblAppearances.Name = "LblAppearances";
            this.LblAppearances.Size = new System.Drawing.Size(115, 13);
            this.LblAppearances.TabIndex = 4;
            this.LblAppearances.Text = "Available appearances";
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(10, 16);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(82, 13);
            this.label1.TabIndex = 4;
            this.label1.Text = "Available heads";
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(10, 234);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(84, 13);
            this.label2.TabIndex = 4;
            this.label2.Text = "Available bodies";
            // 
            // menuStrip1
            // 
            this.menuStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.fileToolStripMenuItem,
            this.helpToolStripMenuItem});
            this.menuStrip1.Location = new System.Drawing.Point(0, 0);
            this.menuStrip1.Name = "menuStrip1";
            this.menuStrip1.Size = new System.Drawing.Size(917, 24);
            this.menuStrip1.TabIndex = 4;
            this.menuStrip1.Text = "menuStrip1";
            // 
            // fileToolStripMenuItem
            // 
            this.fileToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.exitToolStripMenuItem});
            this.fileToolStripMenuItem.Name = "fileToolStripMenuItem";
            this.fileToolStripMenuItem.Size = new System.Drawing.Size(37, 20);
            this.fileToolStripMenuItem.Text = "File";
            // 
            // exitToolStripMenuItem
            // 
            this.exitToolStripMenuItem.Name = "exitToolStripMenuItem";
            this.exitToolStripMenuItem.Size = new System.Drawing.Size(152, 22);
            this.exitToolStripMenuItem.Text = "Exit";
            this.exitToolStripMenuItem.Click += new System.EventHandler(this.exitToolStripMenuItem_Click);
            // 
            // helpToolStripMenuItem
            // 
            this.helpToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.aboutTSODressUpToolStripMenuItem});
            this.helpToolStripMenuItem.Name = "helpToolStripMenuItem";
            this.helpToolStripMenuItem.Size = new System.Drawing.Size(44, 20);
            this.helpToolStripMenuItem.Text = "Help";
            // 
            // aboutTSODressUpToolStripMenuItem
            // 
            this.aboutTSODressUpToolStripMenuItem.Name = "aboutTSODressUpToolStripMenuItem";
            this.aboutTSODressUpToolStripMenuItem.Size = new System.Drawing.Size(178, 22);
            this.aboutTSODressUpToolStripMenuItem.Text = "About TSO DressUp";
            this.aboutTSODressUpToolStripMenuItem.Click += new System.EventHandler(this.aboutTSODressUpToolStripMenuItem_Click);
            // 
            // BtnSkeleton
            // 
            this.BtnSkeleton.Location = new System.Drawing.Point(510, 35);
            this.BtnSkeleton.Name = "BtnSkeleton";
            this.BtnSkeleton.Size = new System.Drawing.Size(95, 23);
            this.BtnSkeleton.TabIndex = 5;
            this.BtnSkeleton.Text = "Show skeleton";
            this.BtnSkeleton.UseVisualStyleBackColor = true;
            this.BtnSkeleton.Click += new System.EventHandler(this.BtnSkeleton_Click);
            // 
            // Form1
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(917, 703);
            this.Controls.Add(this.BtnSkeleton);
            this.Controls.Add(this.panel1);
            this.Controls.Add(this.menuStrip1);
            this.MainMenuStrip = this.menuStrip1;
            this.Name = "Form1";
            this.Text = "TSO DressUp";
            this.Controls.SetChildIndex(this.menuStrip1, 0);
            this.Controls.SetChildIndex(this.panelViewport, 0);
            this.Controls.SetChildIndex(this.panel1, 0);
            this.Controls.SetChildIndex(this.BtnSkeleton, 0);
            this.panel1.ResumeLayout(false);
            this.panel1.PerformLayout();
            this.menuStrip1.ResumeLayout(false);
            this.menuStrip1.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Panel panel1;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.MenuStrip menuStrip1;
        private System.Windows.Forms.ToolStripMenuItem fileToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem exitToolStripMenuItem;
        private System.Windows.Forms.ListBox LstHeads;
        private System.Windows.Forms.Label LblAppearances;
        private System.Windows.Forms.ListBox LstAppearances;
        private System.Windows.Forms.ToolStripMenuItem helpToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem aboutTSODressUpToolStripMenuItem;
        private System.Windows.Forms.Button BtnSkeleton;
        private System.Windows.Forms.ListBox LstBodies;
        private System.Windows.Forms.Label label1;
    }
}