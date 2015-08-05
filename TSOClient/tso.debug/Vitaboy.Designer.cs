namespace FSO.Debug
{
    partial class Vitaboy
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
            this.menu = new System.Windows.Forms.ToolStrip();
            this.tabControl1 = new System.Windows.Forms.TabControl();
            this.bindings = new System.Windows.Forms.TabPage();
            this.bindingsLoad = new System.Windows.Forms.Button();
            this.bindingsList = new System.Windows.Forms.ListBox();
            this.avatarTab = new System.Windows.Forms.TabPage();
            this.outfitLoadBtn = new System.Windows.Forms.Button();
            this.outfitList = new System.Windows.Forms.ListBox();
            this.animationTab = new System.Windows.Forms.TabPage();
            this.canvas = new FSO.Common.Rendering.Framework.winforms.WinFormsGameWindow();
            this.animationLoadBtn = new System.Windows.Forms.Button();
            this.animationsList = new System.Windows.Forms.ListBox();
            this.tabControl1.SuspendLayout();
            this.bindings.SuspendLayout();
            this.avatarTab.SuspendLayout();
            this.animationTab.SuspendLayout();
            this.SuspendLayout();
            // 
            // menu
            // 
            this.menu.Location = new System.Drawing.Point(0, 0);
            this.menu.Name = "menu";
            this.menu.Size = new System.Drawing.Size(984, 25);
            this.menu.TabIndex = 1;
            this.menu.Text = "toolStrip1";
            this.menu.ItemClicked += new System.Windows.Forms.ToolStripItemClickedEventHandler(this.menu_ItemClicked);
            // 
            // tabControl1
            // 
            this.tabControl1.Controls.Add(this.bindings);
            this.tabControl1.Controls.Add(this.avatarTab);
            this.tabControl1.Controls.Add(this.animationTab);
            this.tabControl1.Location = new System.Drawing.Point(519, 28);
            this.tabControl1.Name = "tabControl1";
            this.tabControl1.SelectedIndex = 0;
            this.tabControl1.Size = new System.Drawing.Size(465, 425);
            this.tabControl1.TabIndex = 2;
            // 
            // bindings
            // 
            this.bindings.Controls.Add(this.bindingsLoad);
            this.bindings.Controls.Add(this.bindingsList);
            this.bindings.Location = new System.Drawing.Point(4, 22);
            this.bindings.Name = "bindings";
            this.bindings.Size = new System.Drawing.Size(457, 399);
            this.bindings.TabIndex = 2;
            this.bindings.Text = "Bindings";
            this.bindings.UseVisualStyleBackColor = true;
            // 
            // bindingsLoad
            // 
            this.bindingsLoad.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.bindingsLoad.Location = new System.Drawing.Point(3, 373);
            this.bindingsLoad.Name = "bindingsLoad";
            this.bindingsLoad.Size = new System.Drawing.Size(151, 23);
            this.bindingsLoad.TabIndex = 1;
            this.bindingsLoad.Text = "Load Binding";
            this.bindingsLoad.UseVisualStyleBackColor = true;
            this.bindingsLoad.Click += new System.EventHandler(this.bindingsLoad_Click);
            // 
            // bindingsList
            // 
            this.bindingsList.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
                        | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.bindingsList.FormattingEnabled = true;
            this.bindingsList.Location = new System.Drawing.Point(3, 3);
            this.bindingsList.Name = "bindingsList";
            this.bindingsList.Size = new System.Drawing.Size(451, 368);
            this.bindingsList.TabIndex = 0;
            // 
            // avatarTab
            // 
            this.avatarTab.Controls.Add(this.outfitLoadBtn);
            this.avatarTab.Controls.Add(this.outfitList);
            this.avatarTab.Location = new System.Drawing.Point(4, 22);
            this.avatarTab.Name = "avatarTab";
            this.avatarTab.Padding = new System.Windows.Forms.Padding(3);
            this.avatarTab.Size = new System.Drawing.Size(457, 399);
            this.avatarTab.TabIndex = 0;
            this.avatarTab.Text = "Outfits";
            this.avatarTab.UseVisualStyleBackColor = true;
            // 
            // outfitLoadBtn
            // 
            this.outfitLoadBtn.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.outfitLoadBtn.Location = new System.Drawing.Point(3, 373);
            this.outfitLoadBtn.Name = "outfitLoadBtn";
            this.outfitLoadBtn.Size = new System.Drawing.Size(151, 23);
            this.outfitLoadBtn.TabIndex = 3;
            this.outfitLoadBtn.Text = "Load Outfit";
            this.outfitLoadBtn.UseVisualStyleBackColor = true;
            this.outfitLoadBtn.Click += new System.EventHandler(this.outfitLoadBtn_Click);
            // 
            // outfitList
            // 
            this.outfitList.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
                        | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.outfitList.FormattingEnabled = true;
            this.outfitList.Location = new System.Drawing.Point(3, 3);
            this.outfitList.Name = "outfitList";
            this.outfitList.Size = new System.Drawing.Size(451, 368);
            this.outfitList.TabIndex = 2;
            // 
            // animationTab
            // 
            this.animationTab.Controls.Add(this.animationLoadBtn);
            this.animationTab.Controls.Add(this.animationsList);
            this.animationTab.Location = new System.Drawing.Point(4, 22);
            this.animationTab.Name = "animationTab";
            this.animationTab.Padding = new System.Windows.Forms.Padding(3);
            this.animationTab.Size = new System.Drawing.Size(457, 399);
            this.animationTab.TabIndex = 1;
            this.animationTab.Text = "Animation";
            this.animationTab.UseVisualStyleBackColor = true;
            // 
            // canvas
            // 
            this.canvas.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
                        | System.Windows.Forms.AnchorStyles.Left)));
            this.canvas.Location = new System.Drawing.Point(0, 28);
            this.canvas.Name = "canvas";
            this.canvas.Size = new System.Drawing.Size(513, 438);
            this.canvas.TabIndex = 0;
            this.canvas.Text = "winFormsGameWindow1";
            // 
            // animationLoadBtn
            // 
            this.animationLoadBtn.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.animationLoadBtn.Location = new System.Drawing.Point(3, 373);
            this.animationLoadBtn.Name = "animationLoadBtn";
            this.animationLoadBtn.Size = new System.Drawing.Size(151, 23);
            this.animationLoadBtn.TabIndex = 5;
            this.animationLoadBtn.Text = "Load Animation";
            this.animationLoadBtn.UseVisualStyleBackColor = true;
            this.animationLoadBtn.Click += new System.EventHandler(this.animationLoadBtn_Click);
            // 
            // animationsList
            // 
            this.animationsList.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
                        | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.animationsList.FormattingEnabled = true;
            this.animationsList.Location = new System.Drawing.Point(3, 3);
            this.animationsList.Name = "animationsList";
            this.animationsList.Size = new System.Drawing.Size(451, 368);
            this.animationsList.TabIndex = 4;
            // 
            // Vitaboy
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(984, 465);
            this.Controls.Add(this.tabControl1);
            this.Controls.Add(this.menu);
            this.Controls.Add(this.canvas);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedToolWindow;
            this.Name = "Vitaboy";
            this.Text = "Vitaboy";
            this.Load += new System.EventHandler(this.Vitaboy_Load);
            this.tabControl1.ResumeLayout(false);
            this.bindings.ResumeLayout(false);
            this.avatarTab.ResumeLayout(false);
            this.animationTab.ResumeLayout(false);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private FSO.Common.Rendering.Framework.winforms.WinFormsGameWindow canvas;
        private System.Windows.Forms.ToolStrip menu;
        private System.Windows.Forms.TabControl tabControl1;
        private System.Windows.Forms.TabPage avatarTab;
        private System.Windows.Forms.TabPage animationTab;
        private System.Windows.Forms.TabPage bindings;
        private System.Windows.Forms.ListBox bindingsList;
        private System.Windows.Forms.Button bindingsLoad;
        private System.Windows.Forms.Button outfitLoadBtn;
        private System.Windows.Forms.ListBox outfitList;
        private System.Windows.Forms.Button animationLoadBtn;
        private System.Windows.Forms.ListBox animationsList;
    }
}