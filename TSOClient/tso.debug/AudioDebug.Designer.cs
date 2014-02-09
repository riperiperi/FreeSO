namespace tso.debug
{
    partial class AudioDebug
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(AudioDebug));
            this.toolStrip1 = new System.Windows.Forms.ToolStrip();
            this.tabControl1 = new System.Windows.Forms.TabControl();
            this.tabPage1 = new System.Windows.Forms.TabPage();
            this.toolStrip2 = new System.Windows.Forms.ToolStrip();
            this.toolStripDropDownButton1 = new System.Windows.Forms.ToolStripDropDownButton();
            this.radioStationsToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.trackGrid = new System.Windows.Forms.DataGridView();
            this.btnPlayTrack = new System.Windows.Forms.ToolStripButton();
            this.ID = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.Type = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.FilePath = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.tabControl1.SuspendLayout();
            this.tabPage1.SuspendLayout();
            this.toolStrip2.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.trackGrid)).BeginInit();
            this.SuspendLayout();
            // 
            // toolStrip1
            // 
            this.toolStrip1.Location = new System.Drawing.Point(0, 0);
            this.toolStrip1.Name = "toolStrip1";
            this.toolStrip1.Size = new System.Drawing.Size(976, 25);
            this.toolStrip1.TabIndex = 0;
            this.toolStrip1.Text = "toolStrip1";
            // 
            // tabControl1
            // 
            this.tabControl1.Controls.Add(this.tabPage1);
            this.tabControl1.Location = new System.Drawing.Point(12, 28);
            this.tabControl1.Name = "tabControl1";
            this.tabControl1.SelectedIndex = 0;
            this.tabControl1.Size = new System.Drawing.Size(547, 314);
            this.tabControl1.TabIndex = 1;
            // 
            // tabPage1
            // 
            this.tabPage1.Controls.Add(this.trackGrid);
            this.tabPage1.Controls.Add(this.toolStrip2);
            this.tabPage1.Location = new System.Drawing.Point(4, 22);
            this.tabPage1.Name = "tabPage1";
            this.tabPage1.Padding = new System.Windows.Forms.Padding(3);
            this.tabPage1.Size = new System.Drawing.Size(539, 288);
            this.tabPage1.TabIndex = 0;
            this.tabPage1.Text = "Audio Tracks";
            this.tabPage1.UseVisualStyleBackColor = true;
            // 
            // toolStrip2
            // 
            this.toolStrip2.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.toolStripDropDownButton1,
            this.btnPlayTrack});
            this.toolStrip2.Location = new System.Drawing.Point(3, 3);
            this.toolStrip2.Name = "toolStrip2";
            this.toolStrip2.Size = new System.Drawing.Size(533, 25);
            this.toolStrip2.TabIndex = 0;
            this.toolStrip2.Text = "toolStrip2";
            // 
            // toolStripDropDownButton1
            // 
            this.toolStripDropDownButton1.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.radioStationsToolStripMenuItem});
            this.toolStripDropDownButton1.Image = global::tso.debug.Properties.Resources.filter;
            this.toolStripDropDownButton1.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.toolStripDropDownButton1.Name = "toolStripDropDownButton1";
            this.toolStripDropDownButton1.Size = new System.Drawing.Size(62, 22);
            this.toolStripDropDownButton1.Text = "Filter";
            // 
            // radioStationsToolStripMenuItem
            // 
            this.radioStationsToolStripMenuItem.Checked = true;
            this.radioStationsToolStripMenuItem.CheckOnClick = true;
            this.radioStationsToolStripMenuItem.CheckState = System.Windows.Forms.CheckState.Checked;
            this.radioStationsToolStripMenuItem.Name = "radioStationsToolStripMenuItem";
            this.radioStationsToolStripMenuItem.Size = new System.Drawing.Size(152, 22);
            this.radioStationsToolStripMenuItem.Text = "Radio Stations";
            this.radioStationsToolStripMenuItem.Click += new System.EventHandler(this.radioStationsToolStripMenuItem_Click);
            // 
            // trackGrid
            // 
            this.trackGrid.AllowUserToAddRows = false;
            this.trackGrid.AllowUserToDeleteRows = false;
            this.trackGrid.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.trackGrid.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
            this.ID,
            this.Type,
            this.FilePath});
            this.trackGrid.Dock = System.Windows.Forms.DockStyle.Fill;
            this.trackGrid.Location = new System.Drawing.Point(3, 28);
            this.trackGrid.Name = "trackGrid";
            this.trackGrid.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
            this.trackGrid.Size = new System.Drawing.Size(533, 257);
            this.trackGrid.TabIndex = 1;
            // 
            // btnPlayTrack
            // 
            this.btnPlayTrack.Image = ((System.Drawing.Image)(resources.GetObject("btnPlayTrack.Image")));
            this.btnPlayTrack.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.btnPlayTrack.Name = "btnPlayTrack";
            this.btnPlayTrack.Size = new System.Drawing.Size(49, 22);
            this.btnPlayTrack.Text = "Play";
            this.btnPlayTrack.Click += new System.EventHandler(this.btnPlayTrack_Click);
            // 
            // ID
            // 
            this.ID.DataPropertyName = "ID";
            this.ID.HeaderText = "ID";
            this.ID.Name = "ID";
            this.ID.Width = 90;
            // 
            // Type
            // 
            this.Type.DataPropertyName = "Type";
            this.Type.HeaderText = "Type";
            this.Type.Name = "Type";
            // 
            // FilePath
            // 
            this.FilePath.DataPropertyName = "Name";
            this.FilePath.HeaderText = "Name";
            this.FilePath.Name = "FilePath";
            this.FilePath.Width = 300;
            // 
            // AudioDebug
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(976, 354);
            this.Controls.Add(this.tabControl1);
            this.Controls.Add(this.toolStrip1);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.SizableToolWindow;
            this.Name = "AudioDebug";
            this.Text = "Audio Engine";
            this.Load += new System.EventHandler(this.AudioDebug_Load);
            this.tabControl1.ResumeLayout(false);
            this.tabPage1.ResumeLayout(false);
            this.tabPage1.PerformLayout();
            this.toolStrip2.ResumeLayout(false);
            this.toolStrip2.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.trackGrid)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.ToolStrip toolStrip1;
        private System.Windows.Forms.TabControl tabControl1;
        private System.Windows.Forms.TabPage tabPage1;
        private System.Windows.Forms.ToolStrip toolStrip2;
        private System.Windows.Forms.ToolStripDropDownButton toolStripDropDownButton1;
        private System.Windows.Forms.ToolStripMenuItem radioStationsToolStripMenuItem;
        private System.Windows.Forms.DataGridView trackGrid;
        private System.Windows.Forms.ToolStripButton btnPlayTrack;
        private System.Windows.Forms.DataGridViewTextBoxColumn ID;
        private System.Windows.Forms.DataGridViewTextBoxColumn Type;
        private System.Windows.Forms.DataGridViewTextBoxColumn FilePath;
    }
}