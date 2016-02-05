namespace FSO.IDE.ResourceBrowser
{
    partial class IFFResComponent
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

        #region Component Designer generated code

        /// <summary> 
        /// Required method for Designer support - do not modify 
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            this.ResTypeCombo = new System.Windows.Forms.ComboBox();
            this.RenameRes = new System.Windows.Forms.Button();
            this.NewRes = new System.Windows.Forms.Button();
            this.CopyRes = new System.Windows.Forms.Button();
            this.PasteRes = new System.Windows.Forms.Button();
            this.DeleteRes = new System.Windows.Forms.Button();
            this.ResList = new System.Windows.Forms.ListBox();
            this.ListOrderContext = new System.Windows.Forms.ContextMenuStrip(this.components);
            this.alphabeticalOrderToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.iDOrderToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.ResControlPanel = new System.Windows.Forms.Panel();
            this.ListOrderContext.SuspendLayout();
            this.SuspendLayout();
            // 
            // ResTypeCombo
            // 
            this.ResTypeCombo.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.ResTypeCombo.FormattingEnabled = true;
            this.ResTypeCombo.Items.AddRange(new object[] {
            "Trees",
            "Tree Tables",
            "Strings",
            "Constants",
            "SLOTs"});
            this.ResTypeCombo.Location = new System.Drawing.Point(2, 2);
            this.ResTypeCombo.Name = "ResTypeCombo";
            this.ResTypeCombo.Size = new System.Drawing.Size(250, 21);
            this.ResTypeCombo.TabIndex = 14;
            this.ResTypeCombo.SelectedIndexChanged += new System.EventHandler(this.ResTypeCombo_SelectedIndexChanged);
            // 
            // RenameRes
            // 
            this.RenameRes.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.RenameRes.Location = new System.Drawing.Point(46, 435);
            this.RenameRes.Name = "RenameRes";
            this.RenameRes.Size = new System.Drawing.Size(55, 23);
            this.RenameRes.TabIndex = 13;
            this.RenameRes.Text = "Rename";
            this.RenameRes.UseVisualStyleBackColor = true;
            this.RenameRes.Click += new System.EventHandler(this.RenameRes_Click);
            // 
            // NewRes
            // 
            this.NewRes.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.NewRes.Location = new System.Drawing.Point(1, 435);
            this.NewRes.Name = "NewRes";
            this.NewRes.Size = new System.Drawing.Size(41, 23);
            this.NewRes.TabIndex = 12;
            this.NewRes.Text = "New";
            this.NewRes.UseVisualStyleBackColor = true;
            this.NewRes.Click += new System.EventHandler(this.NewRes_Click);
            // 
            // CopyRes
            // 
            this.CopyRes.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.CopyRes.Location = new System.Drawing.Point(105, 435);
            this.CopyRes.Name = "CopyRes";
            this.CopyRes.Size = new System.Drawing.Size(40, 23);
            this.CopyRes.TabIndex = 11;
            this.CopyRes.Text = "Copy";
            this.CopyRes.UseVisualStyleBackColor = true;
            // 
            // PasteRes
            // 
            this.PasteRes.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.PasteRes.Location = new System.Drawing.Point(149, 435);
            this.PasteRes.Name = "PasteRes";
            this.PasteRes.Size = new System.Drawing.Size(50, 23);
            this.PasteRes.TabIndex = 10;
            this.PasteRes.Text = "Paste";
            this.PasteRes.UseVisualStyleBackColor = true;
            // 
            // DeleteRes
            // 
            this.DeleteRes.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.DeleteRes.Location = new System.Drawing.Point(203, 435);
            this.DeleteRes.Name = "DeleteRes";
            this.DeleteRes.Size = new System.Drawing.Size(50, 23);
            this.DeleteRes.TabIndex = 9;
            this.DeleteRes.Text = "Delete";
            this.DeleteRes.UseVisualStyleBackColor = true;
            // 
            // ResList
            // 
            this.ResList.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left)));
            this.ResList.FormattingEnabled = true;
            this.ResList.Location = new System.Drawing.Point(2, 27);
            this.ResList.Name = "ResList";
            this.ResList.Size = new System.Drawing.Size(250, 407);
            this.ResList.TabIndex = 8;
            this.ResList.SelectedIndexChanged += new System.EventHandler(this.ResList_SelectedIndexChanged);
            this.ResList.DoubleClick += new System.EventHandler(this.ResList_DoubleClick);
            // 
            // ListOrderContext
            // 
            this.ListOrderContext.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.alphabeticalOrderToolStripMenuItem,
            this.iDOrderToolStripMenuItem});
            this.ListOrderContext.Name = "ListOrderContext";
            this.ListOrderContext.Size = new System.Drawing.Size(174, 48);
            // 
            // alphabeticalOrderToolStripMenuItem
            // 
            this.alphabeticalOrderToolStripMenuItem.Checked = true;
            this.alphabeticalOrderToolStripMenuItem.CheckState = System.Windows.Forms.CheckState.Checked;
            this.alphabeticalOrderToolStripMenuItem.Name = "alphabeticalOrderToolStripMenuItem";
            this.alphabeticalOrderToolStripMenuItem.Size = new System.Drawing.Size(173, 22);
            this.alphabeticalOrderToolStripMenuItem.Text = "Alphabetical Order";
            // 
            // iDOrderToolStripMenuItem
            // 
            this.iDOrderToolStripMenuItem.Name = "iDOrderToolStripMenuItem";
            this.iDOrderToolStripMenuItem.Size = new System.Drawing.Size(173, 22);
            this.iDOrderToolStripMenuItem.Text = "ID Order";
            // 
            // ResControlPanel
            // 
            this.ResControlPanel.Location = new System.Drawing.Point(257, 2);
            this.ResControlPanel.Margin = new System.Windows.Forms.Padding(0);
            this.ResControlPanel.Name = "ResControlPanel";
            this.ResControlPanel.Size = new System.Drawing.Size(502, 455);
            this.ResControlPanel.TabIndex = 15;
            // 
            // IFFResComponent
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.ResControlPanel);
            this.Controls.Add(this.ResTypeCombo);
            this.Controls.Add(this.RenameRes);
            this.Controls.Add(this.NewRes);
            this.Controls.Add(this.CopyRes);
            this.Controls.Add(this.PasteRes);
            this.Controls.Add(this.DeleteRes);
            this.Controls.Add(this.ResList);
            this.Name = "IFFResComponent";
            this.Size = new System.Drawing.Size(762, 459);
            this.ListOrderContext.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.ComboBox ResTypeCombo;
        private System.Windows.Forms.Button RenameRes;
        private System.Windows.Forms.Button NewRes;
        private System.Windows.Forms.Button CopyRes;
        private System.Windows.Forms.Button PasteRes;
        private System.Windows.Forms.Button DeleteRes;
        private System.Windows.Forms.ListBox ResList;
        private System.Windows.Forms.ContextMenuStrip ListOrderContext;
        private System.Windows.Forms.ToolStripMenuItem alphabeticalOrderToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem iDOrderToolStripMenuItem;
        private System.Windows.Forms.Panel ResControlPanel;
    }
}
