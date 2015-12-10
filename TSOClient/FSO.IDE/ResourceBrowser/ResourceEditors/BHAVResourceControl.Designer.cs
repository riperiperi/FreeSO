namespace FSO.IDE.ResourceBrowser.ResourceEditors
{
    partial class BHAVResourceControl
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
            this.EditButton = new System.Windows.Forms.Button();
            this.ParamLocalTable = new System.Windows.Forms.TableLayoutPanel();
            this.LocalBox = new System.Windows.Forms.GroupBox();
            this.tableLayoutPanel3 = new System.Windows.Forms.TableLayoutPanel();
            this.LocalAddBtn = new System.Windows.Forms.Button();
            this.LocalRemoveBtn = new System.Windows.Forms.Button();
            this.LocalRenameBtn = new System.Windows.Forms.Button();
            this.LocalList = new System.Windows.Forms.ListBox();
            this.ParamBox = new System.Windows.Forms.GroupBox();
            this.tableLayoutPanel2 = new System.Windows.Forms.TableLayoutPanel();
            this.ParamAddBtn = new System.Windows.Forms.Button();
            this.ParamRemoveBtn = new System.Windows.Forms.Button();
            this.ParamRenameBtn = new System.Windows.Forms.Button();
            this.ParamList = new System.Windows.Forms.ListBox();
            this.EntryPointBox = new System.Windows.Forms.ComboBox();
            this.label1 = new System.Windows.Forms.Label();
            this.TPRPButton = new System.Windows.Forms.Button();
            this.DescriptionBox = new System.Windows.Forms.TextBox();
            this.label2 = new System.Windows.Forms.Label();
            this.label3 = new System.Windows.Forms.Label();
            this.StackChangeBtn = new System.Windows.Forms.Button();
            this.StackObjName = new System.Windows.Forms.Label();
            this.ParamLocalTable.SuspendLayout();
            this.LocalBox.SuspendLayout();
            this.tableLayoutPanel3.SuspendLayout();
            this.ParamBox.SuspendLayout();
            this.tableLayoutPanel2.SuspendLayout();
            this.SuspendLayout();
            // 
            // EditButton
            // 
            this.EditButton.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.EditButton.Location = new System.Drawing.Point(3, 413);
            this.EditButton.Name = "EditButton";
            this.EditButton.Size = new System.Drawing.Size(496, 39);
            this.EditButton.TabIndex = 0;
            this.EditButton.Text = "Edit Tree";
            this.EditButton.UseVisualStyleBackColor = true;
            this.EditButton.Click += new System.EventHandler(this.EditButton_Click);
            // 
            // ParamLocalTable
            // 
            this.ParamLocalTable.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.ParamLocalTable.ColumnCount = 2;
            this.ParamLocalTable.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.ParamLocalTable.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.ParamLocalTable.Controls.Add(this.LocalBox, 1, 0);
            this.ParamLocalTable.Controls.Add(this.ParamBox, 0, 0);
            this.ParamLocalTable.Location = new System.Drawing.Point(3, 107);
            this.ParamLocalTable.Name = "ParamLocalTable";
            this.ParamLocalTable.RowCount = 1;
            this.ParamLocalTable.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 74.76923F));
            this.ParamLocalTable.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 300F));
            this.ParamLocalTable.Size = new System.Drawing.Size(495, 300);
            this.ParamLocalTable.TabIndex = 1;
            // 
            // LocalBox
            // 
            this.LocalBox.Controls.Add(this.tableLayoutPanel3);
            this.LocalBox.Controls.Add(this.LocalList);
            this.LocalBox.Dock = System.Windows.Forms.DockStyle.Fill;
            this.LocalBox.Location = new System.Drawing.Point(250, 3);
            this.LocalBox.Name = "LocalBox";
            this.LocalBox.Size = new System.Drawing.Size(242, 294);
            this.LocalBox.TabIndex = 0;
            this.LocalBox.TabStop = false;
            this.LocalBox.Text = "Locals";
            // 
            // tableLayoutPanel3
            // 
            this.tableLayoutPanel3.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.tableLayoutPanel3.ColumnCount = 3;
            this.tableLayoutPanel3.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 33.33333F));
            this.tableLayoutPanel3.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 33.33333F));
            this.tableLayoutPanel3.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 33.33333F));
            this.tableLayoutPanel3.Controls.Add(this.LocalAddBtn, 0, 0);
            this.tableLayoutPanel3.Controls.Add(this.LocalRemoveBtn, 1, 0);
            this.tableLayoutPanel3.Controls.Add(this.LocalRenameBtn, 2, 0);
            this.tableLayoutPanel3.Location = new System.Drawing.Point(7, 16);
            this.tableLayoutPanel3.Name = "tableLayoutPanel3";
            this.tableLayoutPanel3.RowCount = 1;
            this.tableLayoutPanel3.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tableLayoutPanel3.Size = new System.Drawing.Size(229, 30);
            this.tableLayoutPanel3.TabIndex = 3;
            // 
            // LocalAddBtn
            // 
            this.LocalAddBtn.Dock = System.Windows.Forms.DockStyle.Fill;
            this.LocalAddBtn.Location = new System.Drawing.Point(3, 3);
            this.LocalAddBtn.Name = "LocalAddBtn";
            this.LocalAddBtn.Size = new System.Drawing.Size(70, 24);
            this.LocalAddBtn.TabIndex = 0;
            this.LocalAddBtn.Text = "Add";
            this.LocalAddBtn.UseVisualStyleBackColor = true;
            // 
            // LocalRemoveBtn
            // 
            this.LocalRemoveBtn.Dock = System.Windows.Forms.DockStyle.Fill;
            this.LocalRemoveBtn.Location = new System.Drawing.Point(79, 3);
            this.LocalRemoveBtn.Name = "LocalRemoveBtn";
            this.LocalRemoveBtn.Size = new System.Drawing.Size(70, 24);
            this.LocalRemoveBtn.TabIndex = 1;
            this.LocalRemoveBtn.Text = "Remove";
            this.LocalRemoveBtn.UseVisualStyleBackColor = true;
            // 
            // LocalRenameBtn
            // 
            this.LocalRenameBtn.Dock = System.Windows.Forms.DockStyle.Fill;
            this.LocalRenameBtn.Location = new System.Drawing.Point(155, 3);
            this.LocalRenameBtn.Name = "LocalRenameBtn";
            this.LocalRenameBtn.Size = new System.Drawing.Size(71, 24);
            this.LocalRenameBtn.TabIndex = 2;
            this.LocalRenameBtn.Text = "Rename";
            this.LocalRenameBtn.UseVisualStyleBackColor = true;
            // 
            // LocalList
            // 
            this.LocalList.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.LocalList.FormattingEnabled = true;
            this.LocalList.IntegralHeight = false;
            this.LocalList.Location = new System.Drawing.Point(7, 47);
            this.LocalList.Name = "LocalList";
            this.LocalList.Size = new System.Drawing.Size(229, 239);
            this.LocalList.TabIndex = 2;
            // 
            // ParamBox
            // 
            this.ParamBox.Controls.Add(this.tableLayoutPanel2);
            this.ParamBox.Controls.Add(this.ParamList);
            this.ParamBox.Dock = System.Windows.Forms.DockStyle.Fill;
            this.ParamBox.Location = new System.Drawing.Point(3, 3);
            this.ParamBox.Name = "ParamBox";
            this.ParamBox.Size = new System.Drawing.Size(241, 294);
            this.ParamBox.TabIndex = 1;
            this.ParamBox.TabStop = false;
            this.ParamBox.Text = "Parameters";
            // 
            // tableLayoutPanel2
            // 
            this.tableLayoutPanel2.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.tableLayoutPanel2.ColumnCount = 3;
            this.tableLayoutPanel2.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 33.33333F));
            this.tableLayoutPanel2.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 33.33333F));
            this.tableLayoutPanel2.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 33.33333F));
            this.tableLayoutPanel2.Controls.Add(this.ParamAddBtn, 0, 0);
            this.tableLayoutPanel2.Controls.Add(this.ParamRemoveBtn, 1, 0);
            this.tableLayoutPanel2.Controls.Add(this.ParamRenameBtn, 2, 0);
            this.tableLayoutPanel2.Location = new System.Drawing.Point(6, 16);
            this.tableLayoutPanel2.Name = "tableLayoutPanel2";
            this.tableLayoutPanel2.RowCount = 1;
            this.tableLayoutPanel2.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tableLayoutPanel2.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 30F));
            this.tableLayoutPanel2.Size = new System.Drawing.Size(229, 30);
            this.tableLayoutPanel2.TabIndex = 1;
            // 
            // ParamAddBtn
            // 
            this.ParamAddBtn.Dock = System.Windows.Forms.DockStyle.Fill;
            this.ParamAddBtn.Location = new System.Drawing.Point(3, 3);
            this.ParamAddBtn.Name = "ParamAddBtn";
            this.ParamAddBtn.Size = new System.Drawing.Size(70, 24);
            this.ParamAddBtn.TabIndex = 0;
            this.ParamAddBtn.Text = "Add";
            this.ParamAddBtn.UseVisualStyleBackColor = true;
            // 
            // ParamRemoveBtn
            // 
            this.ParamRemoveBtn.Dock = System.Windows.Forms.DockStyle.Fill;
            this.ParamRemoveBtn.Location = new System.Drawing.Point(79, 3);
            this.ParamRemoveBtn.Name = "ParamRemoveBtn";
            this.ParamRemoveBtn.Size = new System.Drawing.Size(70, 24);
            this.ParamRemoveBtn.TabIndex = 1;
            this.ParamRemoveBtn.Text = "Remove";
            this.ParamRemoveBtn.UseVisualStyleBackColor = true;
            // 
            // ParamRenameBtn
            // 
            this.ParamRenameBtn.Dock = System.Windows.Forms.DockStyle.Fill;
            this.ParamRenameBtn.Location = new System.Drawing.Point(155, 3);
            this.ParamRenameBtn.Name = "ParamRenameBtn";
            this.ParamRenameBtn.Size = new System.Drawing.Size(71, 24);
            this.ParamRenameBtn.TabIndex = 2;
            this.ParamRenameBtn.Text = "Rename";
            this.ParamRenameBtn.UseVisualStyleBackColor = true;
            // 
            // ParamList
            // 
            this.ParamList.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.ParamList.FormattingEnabled = true;
            this.ParamList.IntegralHeight = false;
            this.ParamList.Location = new System.Drawing.Point(6, 47);
            this.ParamList.Name = "ParamList";
            this.ParamList.Size = new System.Drawing.Size(229, 239);
            this.ParamList.TabIndex = 0;
            // 
            // EntryPointBox
            // 
            this.EntryPointBox.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.EntryPointBox.FormattingEnabled = true;
            this.EntryPointBox.Items.AddRange(new object[] {
            "Unknown",
            "Interaction",
            "Check Tree",
            "Functional",
            "Callback",
            "Subroutine"});
            this.EntryPointBox.Location = new System.Drawing.Point(67, 3);
            this.EntryPointBox.Name = "EntryPointBox";
            this.EntryPointBox.Size = new System.Drawing.Size(129, 21);
            this.EntryPointBox.TabIndex = 2;
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(0, 6);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(61, 13);
            this.label1.TabIndex = 3;
            this.label1.Text = "Entry Point:";
            // 
            // TPRPButton
            // 
            this.TPRPButton.Location = new System.Drawing.Point(3, 81);
            this.TPRPButton.Name = "TPRPButton";
            this.TPRPButton.Size = new System.Drawing.Size(244, 23);
            this.TPRPButton.TabIndex = 4;
            this.TPRPButton.Text = "Generate TPRP (metadata)";
            this.TPRPButton.UseVisualStyleBackColor = true;
            // 
            // DescriptionBox
            // 
            this.DescriptionBox.AcceptsReturn = true;
            this.DescriptionBox.Location = new System.Drawing.Point(253, 21);
            this.DescriptionBox.Multiline = true;
            this.DescriptionBox.Name = "DescriptionBox";
            this.DescriptionBox.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
            this.DescriptionBox.Size = new System.Drawing.Size(245, 83);
            this.DescriptionBox.TabIndex = 5;
            // 
            // label2
            // 
            this.label2.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.label2.Location = new System.Drawing.Point(250, 6);
            this.label2.Margin = new System.Windows.Forms.Padding(0);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(200, 15);
            this.label2.TabIndex = 6;
            this.label2.Text = "Description:";
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(0, 31);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(120, 13);
            this.label3.TabIndex = 8;
            this.label3.Text = "Expected Stack Object:";
            // 
            // StackChangeBtn
            // 
            this.StackChangeBtn.Location = new System.Drawing.Point(3, 45);
            this.StackChangeBtn.Name = "StackChangeBtn";
            this.StackChangeBtn.Size = new System.Drawing.Size(58, 23);
            this.StackChangeBtn.TabIndex = 9;
            this.StackChangeBtn.Text = "Change";
            this.StackChangeBtn.UseVisualStyleBackColor = true;
            // 
            // StackObjName
            // 
            this.StackObjName.AutoEllipsis = true;
            this.StackObjName.Location = new System.Drawing.Point(67, 50);
            this.StackObjName.Name = "StackObjName";
            this.StackObjName.Size = new System.Drawing.Size(180, 18);
            this.StackObjName.TabIndex = 10;
            this.StackObjName.Text = "Default";
            // 
            // BHAVResourceControl
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.StackObjName);
            this.Controls.Add(this.StackChangeBtn);
            this.Controls.Add(this.label3);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.DescriptionBox);
            this.Controls.Add(this.TPRPButton);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.EntryPointBox);
            this.Controls.Add(this.ParamLocalTable);
            this.Controls.Add(this.EditButton);
            this.Margin = new System.Windows.Forms.Padding(0);
            this.Name = "BHAVResourceControl";
            this.Size = new System.Drawing.Size(502, 455);
            this.ParamLocalTable.ResumeLayout(false);
            this.LocalBox.ResumeLayout(false);
            this.tableLayoutPanel3.ResumeLayout(false);
            this.ParamBox.ResumeLayout(false);
            this.tableLayoutPanel2.ResumeLayout(false);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Button EditButton;
        private System.Windows.Forms.TableLayoutPanel ParamLocalTable;
        private System.Windows.Forms.GroupBox LocalBox;
        private System.Windows.Forms.GroupBox ParamBox;
        private System.Windows.Forms.TableLayoutPanel tableLayoutPanel3;
        private System.Windows.Forms.Button LocalAddBtn;
        private System.Windows.Forms.Button LocalRemoveBtn;
        private System.Windows.Forms.Button LocalRenameBtn;
        private System.Windows.Forms.ListBox LocalList;
        private System.Windows.Forms.TableLayoutPanel tableLayoutPanel2;
        private System.Windows.Forms.Button ParamAddBtn;
        private System.Windows.Forms.Button ParamRemoveBtn;
        private System.Windows.Forms.Button ParamRenameBtn;
        private System.Windows.Forms.ListBox ParamList;
        private System.Windows.Forms.ComboBox EntryPointBox;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Button TPRPButton;
        private System.Windows.Forms.TextBox DescriptionBox;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.Button StackChangeBtn;
        private System.Windows.Forms.Label StackObjName;
    }
}
