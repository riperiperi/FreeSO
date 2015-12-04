namespace FSO.IDE
{
    partial class ObjectWindow
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(ObjectWindow));
            this.dataTab = new System.Windows.Forms.TabControl();
            this.tabPage1 = new System.Windows.Forms.TabPage();
            this.TreeList = new System.Windows.Forms.ListBox();
            this.tabPage2 = new System.Windows.Forms.TabPage();
            this.tabPage3 = new System.Windows.Forms.TabPage();
            this.tabPage4 = new System.Windows.Forms.TabPage();
            this.dataTab.SuspendLayout();
            this.tabPage1.SuspendLayout();
            this.SuspendLayout();
            // 
            // dataTab
            // 
            this.dataTab.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.dataTab.Controls.Add(this.tabPage1);
            this.dataTab.Controls.Add(this.tabPage2);
            this.dataTab.Controls.Add(this.tabPage3);
            this.dataTab.Controls.Add(this.tabPage4);
            this.dataTab.Location = new System.Drawing.Point(9, 6);
            this.dataTab.Name = "dataTab";
            this.dataTab.SelectedIndex = 0;
            this.dataTab.Size = new System.Drawing.Size(284, 423);
            this.dataTab.TabIndex = 0;
            // 
            // tabPage1
            // 
            this.tabPage1.Controls.Add(this.TreeList);
            this.tabPage1.Location = new System.Drawing.Point(4, 22);
            this.tabPage1.Name = "tabPage1";
            this.tabPage1.Padding = new System.Windows.Forms.Padding(3);
            this.tabPage1.Size = new System.Drawing.Size(276, 397);
            this.tabPage1.TabIndex = 0;
            this.tabPage1.Text = "Trees";
            this.tabPage1.UseVisualStyleBackColor = true;
            // 
            // TreeList
            // 
            this.TreeList.Dock = System.Windows.Forms.DockStyle.Fill;
            this.TreeList.FormattingEnabled = true;
            this.TreeList.Location = new System.Drawing.Point(3, 3);
            this.TreeList.Name = "TreeList";
            this.TreeList.Size = new System.Drawing.Size(270, 391);
            this.TreeList.TabIndex = 0;
            this.TreeList.DoubleClick += new System.EventHandler(this.TreeList_DoubleClick);
            // 
            // tabPage2
            // 
            this.tabPage2.Location = new System.Drawing.Point(4, 22);
            this.tabPage2.Name = "tabPage2";
            this.tabPage2.Padding = new System.Windows.Forms.Padding(3);
            this.tabPage2.Size = new System.Drawing.Size(276, 397);
            this.tabPage2.TabIndex = 1;
            this.tabPage2.Text = "Tree Tables";
            this.tabPage2.UseVisualStyleBackColor = true;
            // 
            // tabPage3
            // 
            this.tabPage3.Location = new System.Drawing.Point(4, 22);
            this.tabPage3.Name = "tabPage3";
            this.tabPage3.Padding = new System.Windows.Forms.Padding(3);
            this.tabPage3.Size = new System.Drawing.Size(276, 397);
            this.tabPage3.TabIndex = 2;
            this.tabPage3.Text = "Strings";
            this.tabPage3.UseVisualStyleBackColor = true;
            // 
            // tabPage4
            // 
            this.tabPage4.Location = new System.Drawing.Point(4, 22);
            this.tabPage4.Name = "tabPage4";
            this.tabPage4.Padding = new System.Windows.Forms.Padding(3);
            this.tabPage4.Size = new System.Drawing.Size(276, 397);
            this.tabPage4.TabIndex = 3;
            this.tabPage4.Text = "Constants";
            this.tabPage4.UseVisualStyleBackColor = true;
            // 
            // ObjectWindow
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(301, 437);
            this.Controls.Add(this.dataTab);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.Name = "ObjectWindow";
            this.Text = "Edit Object - accessoryrack";
            this.dataTab.ResumeLayout(false);
            this.tabPage1.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.TabControl dataTab;
        private System.Windows.Forms.TabPage tabPage1;
        private System.Windows.Forms.ListBox TreeList;
        private System.Windows.Forms.TabPage tabPage2;
        private System.Windows.Forms.TabPage tabPage3;
        private System.Windows.Forms.TabPage tabPage4;
    }
}