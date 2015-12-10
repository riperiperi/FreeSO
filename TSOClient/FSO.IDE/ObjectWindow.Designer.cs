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
            this.objPages = new System.Windows.Forms.TabControl();
            this.tabPage1 = new System.Windows.Forms.TabPage();
            this.label2 = new System.Windows.Forms.Label();
            this.tabPage2 = new System.Windows.Forms.TabPage();
            this.IffResView = new FSO.IDE.ResourceBrowser.IFFResComponent();
            this.tabPage3 = new System.Windows.Forms.TabPage();
            this.tabPage4 = new System.Windows.Forms.TabPage();
            this.label1 = new System.Windows.Forms.Label();
            this.tabPage5 = new System.Windows.Forms.TabPage();
            this.label3 = new System.Windows.Forms.Label();
            this.ObjThumb = new System.Windows.Forms.PictureBox();
            this.ObjCombo = new System.Windows.Forms.ComboBox();
            this.button1 = new System.Windows.Forms.Button();
            this.ObjMultitileLabel = new System.Windows.Forms.Label();
            this.ObjDescLabel = new System.Windows.Forms.Label();
            this.ObjNameLabel = new System.Windows.Forms.Label();
            this.button2 = new System.Windows.Forms.Button();
            this.objPages.SuspendLayout();
            this.tabPage1.SuspendLayout();
            this.tabPage2.SuspendLayout();
            this.tabPage4.SuspendLayout();
            this.tabPage5.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.ObjThumb)).BeginInit();
            this.SuspendLayout();
            // 
            // objPages
            // 
            this.objPages.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.objPages.Controls.Add(this.tabPage1);
            this.objPages.Controls.Add(this.tabPage2);
            this.objPages.Controls.Add(this.tabPage3);
            this.objPages.Controls.Add(this.tabPage4);
            this.objPages.Controls.Add(this.tabPage5);
            this.objPages.Location = new System.Drawing.Point(7, 68);
            this.objPages.Name = "objPages";
            this.objPages.SelectedIndex = 0;
            this.objPages.Size = new System.Drawing.Size(770, 485);
            this.objPages.TabIndex = 0;
            // 
            // tabPage1
            // 
            this.tabPage1.Controls.Add(this.label2);
            this.tabPage1.Location = new System.Drawing.Point(4, 22);
            this.tabPage1.Name = "tabPage1";
            this.tabPage1.Padding = new System.Windows.Forms.Padding(3);
            this.tabPage1.Size = new System.Drawing.Size(762, 459);
            this.tabPage1.TabIndex = 0;
            this.tabPage1.Text = "Object";
            this.tabPage1.UseVisualStyleBackColor = true;
            // 
            // label2
            // 
            this.label2.Dock = System.Windows.Forms.DockStyle.Fill;
            this.label2.Location = new System.Drawing.Point(3, 3);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(756, 453);
            this.label2.TabIndex = 1;
            this.label2.Text = "This page will include the OBJD editor with live preview for collision bounds/sel" +
    "ected base sprite.";
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
            // tabPage3
            // 
            this.tabPage3.Location = new System.Drawing.Point(4, 22);
            this.tabPage3.Name = "tabPage3";
            this.tabPage3.Padding = new System.Windows.Forms.Padding(3);
            this.tabPage3.Size = new System.Drawing.Size(762, 459);
            this.tabPage3.TabIndex = 2;
            this.tabPage3.Text = "Entry Points";
            this.tabPage3.UseVisualStyleBackColor = true;
            // 
            // tabPage4
            // 
            this.tabPage4.Controls.Add(this.label1);
            this.tabPage4.Location = new System.Drawing.Point(4, 22);
            this.tabPage4.Name = "tabPage4";
            this.tabPage4.Padding = new System.Windows.Forms.Padding(3);
            this.tabPage4.Size = new System.Drawing.Size(762, 459);
            this.tabPage4.TabIndex = 3;
            this.tabPage4.Text = "File Options";
            this.tabPage4.UseVisualStyleBackColor = true;
            // 
            // label1
            // 
            this.label1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.label1.Location = new System.Drawing.Point(3, 3);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(756, 453);
            this.label1.TabIndex = 0;
            this.label1.Text = resources.GetString("label1.Text");
            // 
            // tabPage5
            // 
            this.tabPage5.Controls.Add(this.label3);
            this.tabPage5.Location = new System.Drawing.Point(4, 22);
            this.tabPage5.Name = "tabPage5";
            this.tabPage5.Padding = new System.Windows.Forms.Padding(3);
            this.tabPage5.Size = new System.Drawing.Size(762, 459);
            this.tabPage5.TabIndex = 4;
            this.tabPage5.Text = "Appearance";
            this.tabPage5.UseVisualStyleBackColor = true;
            // 
            // label3
            // 
            this.label3.Dock = System.Windows.Forms.DockStyle.Fill;
            this.label3.Location = new System.Drawing.Point(3, 3);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(756, 453);
            this.label3.TabIndex = 2;
            this.label3.Text = "This page will feature a transmogrifier style DGRP editor with live preview. It w" +
    "ill also allow import of SPR2.\r\n";
            // 
            // ObjThumb
            // 
            this.ObjThumb.Location = new System.Drawing.Point(7, 12);
            this.ObjThumb.Name = "ObjThumb";
            this.ObjThumb.Size = new System.Drawing.Size(48, 48);
            this.ObjThumb.TabIndex = 1;
            this.ObjThumb.TabStop = false;
            // 
            // ObjCombo
            // 
            this.ObjCombo.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.ObjCombo.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.ObjCombo.FormattingEnabled = true;
            this.ObjCombo.Location = new System.Drawing.Point(564, 12);
            this.ObjCombo.Name = "ObjCombo";
            this.ObjCombo.Size = new System.Drawing.Size(209, 21);
            this.ObjCombo.TabIndex = 2;
            this.ObjCombo.SelectedIndexChanged += new System.EventHandler(this.ObjCombo_SelectedIndexChanged);
            // 
            // button1
            // 
            this.button1.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.button1.Location = new System.Drawing.Point(617, 37);
            this.button1.Name = "button1";
            this.button1.Size = new System.Drawing.Size(75, 23);
            this.button1.TabIndex = 3;
            this.button1.Text = "Semi-Global";
            this.button1.UseVisualStyleBackColor = true;
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
            // button2
            // 
            this.button2.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.button2.Location = new System.Drawing.Point(698, 37);
            this.button2.Name = "button2";
            this.button2.Size = new System.Drawing.Size(75, 23);
            this.button2.TabIndex = 21;
            this.button2.Text = "Global";
            this.button2.UseVisualStyleBackColor = true;
            // 
            // ObjectWindow
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(784, 561);
            this.Controls.Add(this.button2);
            this.Controls.Add(this.ObjMultitileLabel);
            this.Controls.Add(this.ObjDescLabel);
            this.Controls.Add(this.ObjNameLabel);
            this.Controls.Add(this.button1);
            this.Controls.Add(this.ObjCombo);
            this.Controls.Add(this.ObjThumb);
            this.Controls.Add(this.objPages);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.Name = "ObjectWindow";
            this.Text = "Edit Object - accessoryrack";
            this.objPages.ResumeLayout(false);
            this.tabPage1.ResumeLayout(false);
            this.tabPage2.ResumeLayout(false);
            this.tabPage4.ResumeLayout(false);
            this.tabPage5.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.ObjThumb)).EndInit();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.TabControl objPages;
        private System.Windows.Forms.TabPage tabPage1;
        private System.Windows.Forms.TabPage tabPage2;
        private System.Windows.Forms.TabPage tabPage3;
        private System.Windows.Forms.TabPage tabPage4;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.TabPage tabPage5;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.PictureBox ObjThumb;
        private System.Windows.Forms.ComboBox ObjCombo;
        private System.Windows.Forms.Button button1;
        private System.Windows.Forms.Label ObjMultitileLabel;
        private System.Windows.Forms.Label ObjDescLabel;
        private System.Windows.Forms.Label ObjNameLabel;
        private System.Windows.Forms.Button button2;
        private ResourceBrowser.IFFResComponent IffResView;
    }
}