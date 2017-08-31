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
            this.ObjCombo = new System.Windows.Forms.ComboBox();
            this.SemiGlobalButton = new System.Windows.Forms.Button();
            this.ObjMultitileLabel = new System.Windows.Forms.Label();
            this.ObjDescLabel = new System.Windows.Forms.Label();
            this.ObjNameLabel = new System.Windows.Forms.Label();
            this.GlobalButton = new System.Windows.Forms.Button();
            this.SGChangeButton = new System.Windows.Forms.Button();
            this.AppearanceTab = new System.Windows.Forms.TabPage();
            this.DrawgroupEdit = new FSO.IDE.ResourceBrowser.DGRPEditor();
            this.tabPage3 = new System.Windows.Forms.TabPage();
            this.FuncEditor = new FSO.IDE.ResourceBrowser.OBJfEditor();
            this.tabPage2 = new System.Windows.Forms.TabPage();
            this.IffResView = new FSO.IDE.ResourceBrowser.IFFResComponent();
            this.DefinitionTab = new System.Windows.Forms.TabPage();
            this.DefinitionEditor = new FSO.IDE.ResourceBrowser.OBJDEditor();
            this.objPages = new System.Windows.Forms.TabControl();
            this.Debug3D = new System.Windows.Forms.TabPage();
            this.NewOBJD = new System.Windows.Forms.Button();
            this.DeleteOBJD = new System.Windows.Forms.Button();
            this.ObjThumb = new FSO.IDE.Common.ObjThumbnailControl();
            this.FSOMEdit = new FSO.IDE.ResourceBrowser.FSOMEditor();
            this.AppearanceTab.SuspendLayout();
            this.tabPage3.SuspendLayout();
            this.tabPage2.SuspendLayout();
            this.DefinitionTab.SuspendLayout();
            this.objPages.SuspendLayout();
            this.Debug3D.SuspendLayout();
            this.SuspendLayout();
            // 
            // ObjCombo
            // 
            this.ObjCombo.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.ObjCombo.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.ObjCombo.FormattingEnabled = true;
            this.ObjCombo.Location = new System.Drawing.Point(469, 12);
            this.ObjCombo.Name = "ObjCombo";
            this.ObjCombo.Size = new System.Drawing.Size(304, 21);
            this.ObjCombo.TabIndex = 2;
            this.ObjCombo.SelectedIndexChanged += new System.EventHandler(this.ObjCombo_SelectedIndexChanged);
            // 
            // SemiGlobalButton
            // 
            this.SemiGlobalButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.SemiGlobalButton.Location = new System.Drawing.Point(468, 37);
            this.SemiGlobalButton.Name = "SemiGlobalButton";
            this.SemiGlobalButton.Size = new System.Drawing.Size(171, 23);
            this.SemiGlobalButton.TabIndex = 3;
            this.SemiGlobalButton.Text = "Semi-Global (doorglobals)";
            this.SemiGlobalButton.UseVisualStyleBackColor = true;
            this.SemiGlobalButton.Click += new System.EventHandler(this.SemiGlobalButton_Click);
            // 
            // ObjMultitileLabel
            // 
            this.ObjMultitileLabel.Location = new System.Drawing.Point(61, 45);
            this.ObjMultitileLabel.Name = "ObjMultitileLabel";
            this.ObjMultitileLabel.Size = new System.Drawing.Size(186, 17);
            this.ObjMultitileLabel.TabIndex = 20;
            this.ObjMultitileLabel.Text = "Multitile Master Object";
            // 
            // ObjDescLabel
            // 
            this.ObjDescLabel.Location = new System.Drawing.Point(61, 30);
            this.ObjDescLabel.Name = "ObjDescLabel";
            this.ObjDescLabel.Size = new System.Drawing.Size(186, 17);
            this.ObjDescLabel.TabIndex = 19;
            this.ObjDescLabel.Text = "§2000 - Job Object";
            // 
            // ObjNameLabel
            // 
            this.ObjNameLabel.AutoEllipsis = true;
            this.ObjNameLabel.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.ObjNameLabel.Location = new System.Drawing.Point(61, 12);
            this.ObjNameLabel.Name = "ObjNameLabel";
            this.ObjNameLabel.Size = new System.Drawing.Size(288, 17);
            this.ObjNameLabel.TabIndex = 18;
            this.ObjNameLabel.Text = "Accessory Rack - Cheap";
            // 
            // GlobalButton
            // 
            this.GlobalButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.GlobalButton.Location = new System.Drawing.Point(698, 37);
            this.GlobalButton.Name = "GlobalButton";
            this.GlobalButton.Size = new System.Drawing.Size(75, 23);
            this.GlobalButton.TabIndex = 21;
            this.GlobalButton.Text = "Global";
            this.GlobalButton.UseVisualStyleBackColor = true;
            this.GlobalButton.Click += new System.EventHandler(this.GlobalButton_Click);
            // 
            // SGChangeButton
            // 
            this.SGChangeButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.SGChangeButton.Location = new System.Drawing.Point(640, 37);
            this.SGChangeButton.Name = "SGChangeButton";
            this.SGChangeButton.Size = new System.Drawing.Size(52, 23);
            this.SGChangeButton.TabIndex = 22;
            this.SGChangeButton.Text = "Change";
            this.SGChangeButton.UseVisualStyleBackColor = true;
            this.SGChangeButton.Click += new System.EventHandler(this.SGChangeButton_Click);
            // 
            // AppearanceTab
            // 
            this.AppearanceTab.Controls.Add(this.DrawgroupEdit);
            this.AppearanceTab.Location = new System.Drawing.Point(4, 22);
            this.AppearanceTab.Name = "AppearanceTab";
            this.AppearanceTab.Size = new System.Drawing.Size(762, 459);
            this.AppearanceTab.TabIndex = 4;
            this.AppearanceTab.Text = "Appearance";
            this.AppearanceTab.UseVisualStyleBackColor = true;
            // 
            // DrawgroupEdit
            // 
            this.DrawgroupEdit.Dock = System.Windows.Forms.DockStyle.Fill;
            this.DrawgroupEdit.Location = new System.Drawing.Point(0, 0);
            this.DrawgroupEdit.Name = "DrawgroupEdit";
            this.DrawgroupEdit.Size = new System.Drawing.Size(762, 459);
            this.DrawgroupEdit.TabIndex = 0;
            // 
            // tabPage3
            // 
            this.tabPage3.Controls.Add(this.FuncEditor);
            this.tabPage3.Location = new System.Drawing.Point(4, 22);
            this.tabPage3.Name = "tabPage3";
            this.tabPage3.Size = new System.Drawing.Size(762, 459);
            this.tabPage3.TabIndex = 2;
            this.tabPage3.Text = "Entry Points";
            this.tabPage3.UseVisualStyleBackColor = true;
            // 
            // FuncEditor
            // 
            this.FuncEditor.Dock = System.Windows.Forms.DockStyle.Fill;
            this.FuncEditor.Location = new System.Drawing.Point(0, 0);
            this.FuncEditor.Margin = new System.Windows.Forms.Padding(0);
            this.FuncEditor.Name = "FuncEditor";
            this.FuncEditor.Size = new System.Drawing.Size(762, 459);
            this.FuncEditor.TabIndex = 0;
            // 
            // tabPage2
            // 
            this.tabPage2.Controls.Add(this.IffResView);
            this.tabPage2.Location = new System.Drawing.Point(4, 22);
            this.tabPage2.Name = "tabPage2";
            this.tabPage2.Size = new System.Drawing.Size(762, 459);
            this.tabPage2.TabIndex = 1;
            this.tabPage2.Text = "Trees and Resources";
            this.tabPage2.UseVisualStyleBackColor = true;
            // 
            // IffResView
            // 
            this.IffResView.Dock = System.Windows.Forms.DockStyle.Fill;
            this.IffResView.Location = new System.Drawing.Point(0, 0);
            this.IffResView.Margin = new System.Windows.Forms.Padding(0);
            this.IffResView.Name = "IffResView";
            this.IffResView.Size = new System.Drawing.Size(762, 459);
            this.IffResView.TabIndex = 0;
            // 
            // DefinitionTab
            // 
            this.DefinitionTab.Controls.Add(this.DefinitionEditor);
            this.DefinitionTab.Location = new System.Drawing.Point(4, 22);
            this.DefinitionTab.Name = "DefinitionTab";
            this.DefinitionTab.Padding = new System.Windows.Forms.Padding(3);
            this.DefinitionTab.Size = new System.Drawing.Size(762, 459);
            this.DefinitionTab.TabIndex = 0;
            this.DefinitionTab.Text = "Object";
            this.DefinitionTab.UseVisualStyleBackColor = true;
            // 
            // DefinitionEditor
            // 
            this.DefinitionEditor.Location = new System.Drawing.Point(0, 0);
            this.DefinitionEditor.Name = "DefinitionEditor";
            this.DefinitionEditor.Size = new System.Drawing.Size(762, 459);
            this.DefinitionEditor.TabIndex = 0;
            // 
            // objPages
            // 
            this.objPages.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.objPages.Controls.Add(this.DefinitionTab);
            this.objPages.Controls.Add(this.tabPage2);
            this.objPages.Controls.Add(this.tabPage3);
            this.objPages.Controls.Add(this.AppearanceTab);
            this.objPages.Controls.Add(this.Debug3D);
            this.objPages.Location = new System.Drawing.Point(7, 68);
            this.objPages.Name = "objPages";
            this.objPages.SelectedIndex = 0;
            this.objPages.Size = new System.Drawing.Size(770, 485);
            this.objPages.TabIndex = 0;
            // 
            // Debug3D
            // 
            this.Debug3D.Controls.Add(this.FSOMEdit);
            this.Debug3D.Location = new System.Drawing.Point(4, 22);
            this.Debug3D.Name = "Debug3D";
            this.Debug3D.Padding = new System.Windows.Forms.Padding(3);
            this.Debug3D.Size = new System.Drawing.Size(762, 459);
            this.Debug3D.TabIndex = 5;
            this.Debug3D.Text = "3D Debug";
            this.Debug3D.UseVisualStyleBackColor = true;
            // 
            // NewOBJD
            // 
            this.NewOBJD.Location = new System.Drawing.Point(416, 11);
            this.NewOBJD.Name = "NewOBJD";
            this.NewOBJD.Size = new System.Drawing.Size(47, 23);
            this.NewOBJD.TabIndex = 24;
            this.NewOBJD.Text = "New";
            this.NewOBJD.UseVisualStyleBackColor = true;
            this.NewOBJD.Click += new System.EventHandler(this.NewOBJD_Click);
            // 
            // DeleteOBJD
            // 
            this.DeleteOBJD.Location = new System.Drawing.Point(355, 11);
            this.DeleteOBJD.Name = "DeleteOBJD";
            this.DeleteOBJD.Size = new System.Drawing.Size(55, 23);
            this.DeleteOBJD.TabIndex = 25;
            this.DeleteOBJD.Text = "Delete";
            this.DeleteOBJD.UseVisualStyleBackColor = true;
            this.DeleteOBJD.Click += new System.EventHandler(this.DeleteOBJD_Click);
            // 
            // ObjThumb
            // 
            this.ObjThumb.Location = new System.Drawing.Point(7, 12);
            this.ObjThumb.Name = "ObjThumb";
            this.ObjThumb.Size = new System.Drawing.Size(48, 48);
            this.ObjThumb.TabIndex = 23;
            // 
            // FSOMEdit
            // 
            this.FSOMEdit.Location = new System.Drawing.Point(0, 0);
            this.FSOMEdit.Margin = new System.Windows.Forms.Padding(0);
            this.FSOMEdit.Name = "FSOMEdit";
            this.FSOMEdit.Size = new System.Drawing.Size(762, 459);
            this.FSOMEdit.TabIndex = 0;
            // 
            // ObjectWindow
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(784, 561);
            this.Controls.Add(this.DeleteOBJD);
            this.Controls.Add(this.NewOBJD);
            this.Controls.Add(this.ObjThumb);
            this.Controls.Add(this.SGChangeButton);
            this.Controls.Add(this.GlobalButton);
            this.Controls.Add(this.ObjMultitileLabel);
            this.Controls.Add(this.ObjDescLabel);
            this.Controls.Add(this.ObjNameLabel);
            this.Controls.Add(this.SemiGlobalButton);
            this.Controls.Add(this.ObjCombo);
            this.Controls.Add(this.objPages);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.MaximizeBox = false;
            this.Name = "ObjectWindow";
            this.Text = "Edit Object - accessoryrack";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.ObjectWindow_FormClosing);
            this.AppearanceTab.ResumeLayout(false);
            this.tabPage3.ResumeLayout(false);
            this.tabPage2.ResumeLayout(false);
            this.DefinitionTab.ResumeLayout(false);
            this.objPages.ResumeLayout(false);
            this.Debug3D.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        #endregion
        private System.Windows.Forms.ComboBox ObjCombo;
        private System.Windows.Forms.Button SemiGlobalButton;
        private System.Windows.Forms.Label ObjMultitileLabel;
        private System.Windows.Forms.Label ObjDescLabel;
        private System.Windows.Forms.Label ObjNameLabel;
        private System.Windows.Forms.Button GlobalButton;
        private System.Windows.Forms.Button SGChangeButton;
        private Common.ObjThumbnailControl ObjThumb;
        private System.Windows.Forms.TabPage AppearanceTab;
        private ResourceBrowser.DGRPEditor DrawgroupEdit;
        private System.Windows.Forms.TabPage tabPage3;
        private ResourceBrowser.OBJfEditor FuncEditor;
        private System.Windows.Forms.TabPage tabPage2;
        private ResourceBrowser.IFFResComponent IffResView;
        private System.Windows.Forms.TabPage DefinitionTab;
        private ResourceBrowser.OBJDEditor DefinitionEditor;
        private System.Windows.Forms.TabControl objPages;
        private System.Windows.Forms.Button NewOBJD;
        private System.Windows.Forms.Button DeleteOBJD;
        private System.Windows.Forms.TabPage Debug3D;
        private ResourceBrowser.FSOMEditor FSOMEdit;
    }
}