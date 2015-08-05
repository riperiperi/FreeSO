namespace FSO.Client.Debug
{
    partial class TSOSceneInspector
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
            this.toolStrip1 = new System.Windows.Forms.ToolStrip();
            this.refreshBtn = new System.Windows.Forms.ToolStripButton();
            this.uiTree = new System.Windows.Forms.TreeView();
            this.propertyGrid1 = new System.Windows.Forms.PropertyGrid();
            this.toolStrip1.SuspendLayout();
            this.SuspendLayout();
            // 
            // toolStrip1
            // 
            this.toolStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.refreshBtn});
            this.toolStrip1.Location = new System.Drawing.Point(0, 0);
            this.toolStrip1.Name = "toolStrip1";
            this.toolStrip1.Size = new System.Drawing.Size(227, 25);
            this.toolStrip1.TabIndex = 1;
            this.toolStrip1.Text = "toolStrip1";
            // 
            // refreshBtn
            // 
            this.refreshBtn.Image = global::FSO.Client.Properties.Resources.arrow_circle;
            this.refreshBtn.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.refreshBtn.Name = "refreshBtn";
            this.refreshBtn.Size = new System.Drawing.Size(66, 22);
            this.refreshBtn.Text = "Refresh";
            this.refreshBtn.ToolTipText = "Refresh UI Tree";
            this.refreshBtn.Click += new System.EventHandler(this.refreshBtn_Click);
            // 
            // uiTree
            // 
            this.uiTree.BackColor = System.Drawing.Color.AliceBlue;
            this.uiTree.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this.uiTree.Dock = System.Windows.Forms.DockStyle.Top;
            this.uiTree.Location = new System.Drawing.Point(0, 25);
            this.uiTree.Name = "uiTree";
            this.uiTree.Size = new System.Drawing.Size(227, 165);
            this.uiTree.TabIndex = 2;
            this.uiTree.AfterSelect += new System.Windows.Forms.TreeViewEventHandler(this.uiTree_AfterSelect);
            // 
            // propertyGrid1
            // 
            this.propertyGrid1.Location = new System.Drawing.Point(0, 196);
            this.propertyGrid1.Name = "propertyGrid1";
            this.propertyGrid1.Size = new System.Drawing.Size(227, 292);
            this.propertyGrid1.TabIndex = 6;
            // 
            // TSOSceneInspector
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.Color.AliceBlue;
            this.ClientSize = new System.Drawing.Size(227, 488);
            this.Controls.Add(this.propertyGrid1);
            this.Controls.Add(this.uiTree);
            this.Controls.Add(this.toolStrip1);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.SizableToolWindow;
            this.Name = "TSOSceneInspector";
            this.StartPosition = System.Windows.Forms.FormStartPosition.Manual;
            this.Text = "Scene Inspetor";
            this.toolStrip1.ResumeLayout(false);
            this.toolStrip1.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.ToolStrip toolStrip1;
        private System.Windows.Forms.TreeView uiTree;
        private System.Windows.Forms.ToolStripButton refreshBtn;
        private System.Windows.Forms.PropertyGrid propertyGrid1;

    }
}