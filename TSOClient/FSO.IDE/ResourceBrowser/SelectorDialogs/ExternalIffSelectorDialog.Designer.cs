namespace FSO.IDE.ResourceBrowser.SelectorDialogs
{
    partial class ExternalIffSelectorDialog
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
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this.label1 = new System.Windows.Forms.Label();
            this.label2 = new System.Windows.Forms.Label();
            this.IffLinkLabel = new System.Windows.Forms.LinkLabel();
            this.SPFLinkLabel = new System.Windows.Forms.LinkLabel();
            this.IffEditorButton = new System.Windows.Forms.Button();
            this.tableLayoutPanel1 = new System.Windows.Forms.TableLayoutPanel();
            this.OBJDEditorButton = new System.Windows.Forms.Button();
            this.ExitButton = new System.Windows.Forms.Button();
            this.groupBox1.SuspendLayout();
            this.tableLayoutPanel1.SuspendLayout();
            this.SuspendLayout();
            // 
            // groupBox1
            // 
            this.groupBox1.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.groupBox1.Controls.Add(this.SPFLinkLabel);
            this.groupBox1.Controls.Add(this.IffLinkLabel);
            this.groupBox1.Controls.Add(this.label2);
            this.groupBox1.Controls.Add(this.label1);
            this.groupBox1.Location = new System.Drawing.Point(13, 13);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.Size = new System.Drawing.Size(316, 103);
            this.groupBox1.TabIndex = 0;
            this.groupBox1.TabStop = false;
            this.groupBox1.Text = "File";
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label1.Location = new System.Drawing.Point(7, 20);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(43, 13);
            this.label1.TabIndex = 0;
            this.label1.Text = "Iff File";
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label2.Location = new System.Drawing.Point(7, 59);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(50, 13);
            this.label2.TabIndex = 1;
            this.label2.Text = "Spf File";
            // 
            // IffLinkLabel
            // 
            this.IffLinkLabel.AutoSize = true;
            this.IffLinkLabel.Location = new System.Drawing.Point(10, 39);
            this.IffLinkLabel.Name = "IffLinkLabel";
            this.IffLinkLabel.Size = new System.Drawing.Size(69, 13);
            this.IffLinkLabel.TabIndex = 2;
            this.IffLinkLabel.TabStop = true;
            this.IffLinkLabel.Text = "Not Selected";
            this.IffLinkLabel.LinkClicked += new System.Windows.Forms.LinkLabelLinkClickedEventHandler(this.IffLinkLabel_LinkClicked);
            // 
            // SPFLinkLabel
            // 
            this.SPFLinkLabel.AutoSize = true;
            this.SPFLinkLabel.Location = new System.Drawing.Point(10, 76);
            this.SPFLinkLabel.Name = "SPFLinkLabel";
            this.SPFLinkLabel.Size = new System.Drawing.Size(69, 13);
            this.SPFLinkLabel.TabIndex = 3;
            this.SPFLinkLabel.TabStop = true;
            this.SPFLinkLabel.Text = "Not Selected";
            this.SPFLinkLabel.LinkClicked += new System.Windows.Forms.LinkLabelLinkClickedEventHandler(this.SPFLinkLabel_LinkClicked);
            // 
            // IffEditorButton
            // 
            this.IffEditorButton.Location = new System.Drawing.Point(3, 3);
            this.IffEditorButton.Name = "IffEditorButton";
            this.IffEditorButton.Size = new System.Drawing.Size(99, 46);
            this.IffEditorButton.TabIndex = 5;
            this.IffEditorButton.Text = "Open IFF Editor";
            this.IffEditorButton.UseVisualStyleBackColor = true;
            this.IffEditorButton.Click += new System.EventHandler(this.IffEditorButton_Click);
            // 
            // tableLayoutPanel1
            // 
            this.tableLayoutPanel1.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.tableLayoutPanel1.ColumnCount = 3;
            this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 33.33333F));
            this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 33.33333F));
            this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 33.33333F));
            this.tableLayoutPanel1.Controls.Add(this.ExitButton, 2, 0);
            this.tableLayoutPanel1.Controls.Add(this.OBJDEditorButton, 1, 0);
            this.tableLayoutPanel1.Controls.Add(this.IffEditorButton, 0, 0);
            this.tableLayoutPanel1.Location = new System.Drawing.Point(14, 121);
            this.tableLayoutPanel1.Name = "tableLayoutPanel1";
            this.tableLayoutPanel1.RowCount = 1;
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tableLayoutPanel1.Size = new System.Drawing.Size(315, 52);
            this.tableLayoutPanel1.TabIndex = 6;
            // 
            // OBJDEditorButton
            // 
            this.OBJDEditorButton.Location = new System.Drawing.Point(108, 3);
            this.OBJDEditorButton.Name = "OBJDEditorButton";
            this.OBJDEditorButton.Size = new System.Drawing.Size(99, 46);
            this.OBJDEditorButton.TabIndex = 6;
            this.OBJDEditorButton.Text = "Open OBJD Editor";
            this.OBJDEditorButton.UseVisualStyleBackColor = true;
            this.OBJDEditorButton.Click += new System.EventHandler(this.OBJDEditorButton_Click);
            // 
            // ExitButton
            // 
            this.ExitButton.Location = new System.Drawing.Point(213, 3);
            this.ExitButton.Name = "ExitButton";
            this.ExitButton.Size = new System.Drawing.Size(99, 46);
            this.ExitButton.TabIndex = 7;
            this.ExitButton.Text = "Cancel";
            this.ExitButton.UseVisualStyleBackColor = true;
            this.ExitButton.Click += new System.EventHandler(this.ExitButton_Click);
            // 
            // ExternalIffSelectorDialog
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(341, 185);
            this.Controls.Add(this.tableLayoutPanel1);
            this.Controls.Add(this.groupBox1);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedToolWindow;
            this.Name = "ExternalIffSelectorDialog";
            this.ShowIcon = false;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Open External Iff";
            this.groupBox1.ResumeLayout(false);
            this.groupBox1.PerformLayout();
            this.tableLayoutPanel1.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.GroupBox groupBox1;
        private System.Windows.Forms.LinkLabel SPFLinkLabel;
        private System.Windows.Forms.LinkLabel IffLinkLabel;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Button IffEditorButton;
        private System.Windows.Forms.TableLayoutPanel tableLayoutPanel1;
        private System.Windows.Forms.Button ExitButton;
        private System.Windows.Forms.Button OBJDEditorButton;
    }
}