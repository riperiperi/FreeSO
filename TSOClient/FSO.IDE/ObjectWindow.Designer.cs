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
            ObjCombo = new ComboBox();
            SemiGlobalButton = new Button();
            ObjMultitileLabel = new Label();
            ObjDescLabel = new Label();
            ObjNameLabel = new Label();
            GlobalButton = new Button();
            SGChangeButton = new Button();
            AppearanceTab = new TabPage();
            DrawgroupEdit = new FSO.IDE.ResourceBrowser.DGRPEditor();
            tabPage3 = new TabPage();
            FuncEditor = new FSO.IDE.ResourceBrowser.OBJfEditor();
            tabPage2 = new TabPage();
            IffResView = new FSO.IDE.ResourceBrowser.IFFResComponent();
            DefinitionTab = new TabPage();
            DefinitionEditor = new FSO.IDE.ResourceBrowser.OBJDEditor();
            objPages = new TabControl();
            Debug3D = new TabPage();
            FSOMEdit = new FSO.IDE.ResourceBrowser.FSOMEditor();
            XMLEntryTab = new TabPage();
            XMLEdit = new FSO.IDE.ResourceBrowser.XMLEntryEditor();
            UpgradeTab = new TabPage();
            UpgradeEditor = new FSO.IDE.ResourceBrowser.UpgradeEditor();
            PatchTab = new TabPage();
            PIFFEditor = new FSO.IDE.ResourceBrowser.PIFFEditor();
            NewOBJD = new Button();
            DeleteOBJD = new Button();
            ObjThumb = new FSO.IDE.Common.ObjThumbnailControl();
            AppearanceTab.SuspendLayout();
            tabPage3.SuspendLayout();
            tabPage2.SuspendLayout();
            DefinitionTab.SuspendLayout();
            objPages.SuspendLayout();
            Debug3D.SuspendLayout();
            XMLEntryTab.SuspendLayout();
            UpgradeTab.SuspendLayout();
            PatchTab.SuspendLayout();
            SuspendLayout();
            // 
            // ObjCombo
            // 
            ObjCombo.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            ObjCombo.DropDownStyle = ComboBoxStyle.DropDownList;
            ObjCombo.FormattingEnabled = true;
            ObjCombo.Location = new Point(469, 12);
            ObjCombo.Name = "ObjCombo";
            ObjCombo.Size = new Size(304, 21);
            ObjCombo.TabIndex = 2;
            ObjCombo.SelectedIndexChanged += ObjCombo_SelectedIndexChanged;
            // 
            // SemiGlobalButton
            // 
            SemiGlobalButton.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            SemiGlobalButton.Location = new Point(468, 37);
            SemiGlobalButton.Name = "SemiGlobalButton";
            SemiGlobalButton.Size = new Size(171, 23);
            SemiGlobalButton.TabIndex = 3;
            SemiGlobalButton.Text = "Semi-Global (doorglobals)";
            SemiGlobalButton.UseVisualStyleBackColor = true;
            SemiGlobalButton.Click += SemiGlobalButton_Click;
            // 
            // ObjMultitileLabel
            // 
            ObjMultitileLabel.Location = new Point(61, 45);
            ObjMultitileLabel.Name = "ObjMultitileLabel";
            ObjMultitileLabel.Size = new Size(186, 17);
            ObjMultitileLabel.TabIndex = 20;
            ObjMultitileLabel.Text = "Multitile Master Object";
            // 
            // ObjDescLabel
            // 
            ObjDescLabel.Location = new Point(61, 30);
            ObjDescLabel.Name = "ObjDescLabel";
            ObjDescLabel.Size = new Size(186, 17);
            ObjDescLabel.TabIndex = 19;
            ObjDescLabel.Text = "§2000 - Job Object";
            // 
            // ObjNameLabel
            // 
            ObjNameLabel.AutoEllipsis = true;
            ObjNameLabel.Font = new Font("Microsoft Sans Serif", 9.75F, FontStyle.Regular, GraphicsUnit.Point, 0);
            ObjNameLabel.Location = new Point(61, 12);
            ObjNameLabel.Name = "ObjNameLabel";
            ObjNameLabel.Size = new Size(288, 17);
            ObjNameLabel.TabIndex = 18;
            ObjNameLabel.Text = "Accessory Rack - Cheap";
            // 
            // GlobalButton
            // 
            GlobalButton.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            GlobalButton.Location = new Point(698, 37);
            GlobalButton.Name = "GlobalButton";
            GlobalButton.Size = new Size(75, 23);
            GlobalButton.TabIndex = 21;
            GlobalButton.Text = "Global";
            GlobalButton.UseVisualStyleBackColor = true;
            GlobalButton.Click += GlobalButton_Click;
            // 
            // SGChangeButton
            // 
            SGChangeButton.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            SGChangeButton.Location = new Point(640, 37);
            SGChangeButton.Name = "SGChangeButton";
            SGChangeButton.Size = new Size(52, 23);
            SGChangeButton.TabIndex = 22;
            SGChangeButton.Text = "Change";
            SGChangeButton.UseVisualStyleBackColor = true;
            SGChangeButton.Click += SGChangeButton_Click;
            // 
            // AppearanceTab
            // 
            AppearanceTab.Controls.Add(DrawgroupEdit);
            AppearanceTab.Location = new Point(4, 24);
            AppearanceTab.Name = "AppearanceTab";
            AppearanceTab.Size = new Size(192, 72);
            AppearanceTab.TabIndex = 4;
            AppearanceTab.Text = "Appearance";
            AppearanceTab.UseVisualStyleBackColor = true;
            // 
            // DrawgroupEdit
            // 
            DrawgroupEdit.Dock = DockStyle.Fill;
            DrawgroupEdit.Location = new Point(0, 0);
            DrawgroupEdit.Margin = new Padding(4, 3, 4, 3);
            DrawgroupEdit.Name = "DrawgroupEdit";
            DrawgroupEdit.Size = new Size(192, 72);
            DrawgroupEdit.TabIndex = 0;
            // 
            // tabPage3
            // 
            tabPage3.Controls.Add(FuncEditor);
            tabPage3.Location = new Point(4, 24);
            tabPage3.Name = "tabPage3";
            tabPage3.Size = new Size(192, 72);
            tabPage3.TabIndex = 2;
            tabPage3.Text = "Entry Points";
            tabPage3.UseVisualStyleBackColor = true;
            // 
            // FuncEditor
            // 
            FuncEditor.Dock = DockStyle.Fill;
            FuncEditor.Location = new Point(0, 0);
            FuncEditor.Margin = new Padding(0);
            FuncEditor.Name = "FuncEditor";
            FuncEditor.Size = new Size(192, 72);
            FuncEditor.TabIndex = 0;
            // 
            // tabPage2
            // 
            tabPage2.Controls.Add(IffResView);
            tabPage2.Location = new Point(4, 24);
            tabPage2.Name = "tabPage2";
            tabPage2.Size = new Size(192, 72);
            tabPage2.TabIndex = 1;
            tabPage2.Text = "Trees and Resources";
            tabPage2.UseVisualStyleBackColor = true;
            // 
            // IffResView
            // 
            IffResView.Dock = DockStyle.Fill;
            IffResView.Location = new Point(0, 0);
            IffResView.Margin = new Padding(0);
            IffResView.Name = "IffResView";
            IffResView.Size = new Size(192, 72);
            IffResView.TabIndex = 0;
            // 
            // DefinitionTab
            // 
            DefinitionTab.Controls.Add(DefinitionEditor);
            DefinitionTab.Location = new Point(4, 22);
            DefinitionTab.Name = "DefinitionTab";
            DefinitionTab.Padding = new Padding(3);
            DefinitionTab.Size = new Size(762, 459);
            DefinitionTab.TabIndex = 0;
            DefinitionTab.Text = "Object";
            DefinitionTab.UseVisualStyleBackColor = true;
            // 
            // DefinitionEditor
            // 
            DefinitionEditor.Location = new Point(0, 0);
            DefinitionEditor.Margin = new Padding(4, 3, 4, 3);
            DefinitionEditor.Name = "DefinitionEditor";
            DefinitionEditor.Size = new Size(762, 459);
            DefinitionEditor.TabIndex = 0;
            // 
            // objPages
            // 
            objPages.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            objPages.Controls.Add(DefinitionTab);
            objPages.Controls.Add(tabPage2);
            objPages.Controls.Add(tabPage3);
            objPages.Controls.Add(AppearanceTab);
            objPages.Controls.Add(Debug3D);
            objPages.Controls.Add(XMLEntryTab);
            objPages.Controls.Add(UpgradeTab);
            objPages.Controls.Add(PatchTab);
            objPages.Location = new Point(7, 68);
            objPages.Name = "objPages";
            objPages.SelectedIndex = 0;
            objPages.Size = new Size(770, 485);
            objPages.TabIndex = 0;
            // 
            // Debug3D
            // 
            Debug3D.Controls.Add(FSOMEdit);
            Debug3D.Location = new Point(4, 24);
            Debug3D.Name = "Debug3D";
            Debug3D.Padding = new Padding(3);
            Debug3D.Size = new Size(192, 72);
            Debug3D.TabIndex = 5;
            Debug3D.Text = "3D Mode";
            Debug3D.UseVisualStyleBackColor = true;
            // 
            // FSOMEdit
            // 
            FSOMEdit.Location = new Point(0, 0);
            FSOMEdit.Margin = new Padding(0);
            FSOMEdit.Name = "FSOMEdit";
            FSOMEdit.Size = new Size(762, 459);
            FSOMEdit.TabIndex = 0;
            // 
            // XMLEntryTab
            // 
            XMLEntryTab.Controls.Add(XMLEdit);
            XMLEntryTab.Location = new Point(4, 24);
            XMLEntryTab.Name = "XMLEntryTab";
            XMLEntryTab.Size = new Size(192, 72);
            XMLEntryTab.TabIndex = 6;
            XMLEntryTab.Text = "XML Entry";
            XMLEntryTab.UseVisualStyleBackColor = true;
            // 
            // XMLEdit
            // 
            XMLEdit.Location = new Point(-1, 0);
            XMLEdit.Margin = new Padding(4, 3, 4, 3);
            XMLEdit.Name = "XMLEdit";
            XMLEdit.Size = new Size(762, 459);
            XMLEdit.TabIndex = 0;
            // 
            // UpgradeTab
            // 
            UpgradeTab.Controls.Add(UpgradeEditor);
            UpgradeTab.Location = new Point(4, 24);
            UpgradeTab.Name = "UpgradeTab";
            UpgradeTab.Size = new Size(192, 72);
            UpgradeTab.TabIndex = 8;
            UpgradeTab.Text = "Upgrades";
            UpgradeTab.UseVisualStyleBackColor = true;
            // 
            // UpgradeEditor
            // 
            UpgradeEditor.Dock = DockStyle.Fill;
            UpgradeEditor.Location = new Point(0, 0);
            UpgradeEditor.Margin = new Padding(4, 3, 4, 3);
            UpgradeEditor.Name = "UpgradeEditor";
            UpgradeEditor.Size = new Size(192, 72);
            UpgradeEditor.TabIndex = 0;
            // 
            // PatchTab
            // 
            PatchTab.Controls.Add(PIFFEditor);
            PatchTab.Location = new Point(4, 24);
            PatchTab.Name = "PatchTab";
            PatchTab.Size = new Size(192, 72);
            PatchTab.TabIndex = 7;
            PatchTab.Text = "Patch Info";
            PatchTab.UseVisualStyleBackColor = true;
            // 
            // PIFFEditor
            // 
            PIFFEditor.Dock = DockStyle.Fill;
            PIFFEditor.Location = new Point(0, 0);
            PIFFEditor.Margin = new Padding(4, 3, 4, 3);
            PIFFEditor.Name = "PIFFEditor";
            PIFFEditor.Size = new Size(192, 72);
            PIFFEditor.TabIndex = 0;
            // 
            // NewOBJD
            // 
            NewOBJD.Location = new Point(416, 11);
            NewOBJD.Name = "NewOBJD";
            NewOBJD.Size = new Size(47, 23);
            NewOBJD.TabIndex = 24;
            NewOBJD.Text = "New";
            NewOBJD.UseVisualStyleBackColor = true;
            NewOBJD.Click += NewOBJD_Click;
            // 
            // DeleteOBJD
            // 
            DeleteOBJD.Location = new Point(355, 11);
            DeleteOBJD.Name = "DeleteOBJD";
            DeleteOBJD.Size = new Size(55, 23);
            DeleteOBJD.TabIndex = 25;
            DeleteOBJD.Text = "Delete";
            DeleteOBJD.UseVisualStyleBackColor = true;
            DeleteOBJD.Click += DeleteOBJD_Click;
            // 
            // ObjThumb
            // 
            ObjThumb.Location = new Point(7, 12);
            ObjThumb.Name = "ObjThumb";
            ObjThumb.Size = new Size(48, 48);
            ObjThumb.TabIndex = 23;
            // 
            // ObjectWindow
            // 
            AutoScaleDimensions = new SizeF(96F, 96F);
            AutoScaleMode = AutoScaleMode.Dpi;
            ClientSize = new Size(784, 561);
            Controls.Add(DeleteOBJD);
            Controls.Add(NewOBJD);
            Controls.Add(ObjThumb);
            Controls.Add(SGChangeButton);
            Controls.Add(GlobalButton);
            Controls.Add(ObjMultitileLabel);
            Controls.Add(ObjDescLabel);
            Controls.Add(ObjNameLabel);
            Controls.Add(SemiGlobalButton);
            Controls.Add(ObjCombo);
            Controls.Add(objPages);
            FormBorderStyle = FormBorderStyle.FixedSingle;
            Icon = (Icon)resources.GetObject("$this.Icon");
            MaximizeBox = false;
            Name = "ObjectWindow";
            Text = "Edit Object - accessoryrack";
            FormClosing += ObjectWindow_FormClosing;
            AppearanceTab.ResumeLayout(false);
            tabPage3.ResumeLayout(false);
            tabPage2.ResumeLayout(false);
            DefinitionTab.ResumeLayout(false);
            objPages.ResumeLayout(false);
            Debug3D.ResumeLayout(false);
            XMLEntryTab.ResumeLayout(false);
            UpgradeTab.ResumeLayout(false);
            PatchTab.ResumeLayout(false);
            ResumeLayout(false);

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
        private System.Windows.Forms.TabPage XMLEntryTab;
        private ResourceBrowser.XMLEntryEditor XMLEdit;
        private System.Windows.Forms.TabPage PatchTab;
        private ResourceBrowser.PIFFEditor PIFFEditor;
        private System.Windows.Forms.TabPage UpgradeTab;
        private ResourceBrowser.UpgradeEditor UpgradeEditor;
    }
}