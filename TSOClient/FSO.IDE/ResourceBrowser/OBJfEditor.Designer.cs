namespace FSO.IDE.ResourceBrowser
{
    partial class OBJfEditor
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(OBJfEditor));
            this.FunctionList = new System.Windows.Forms.ListView();
            this.FunctionCol = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.ActionCol = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.CheckCol = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.TableLabel = new System.Windows.Forms.Label();
            this.TableCombo = new System.Windows.Forms.ComboBox();
            this.CheckButton = new System.Windows.Forms.Button();
            this.ActionButton = new System.Windows.Forms.Button();
            this.DescTitle = new System.Windows.Forms.Label();
            this.FilterCheck = new System.Windows.Forms.CheckBox();
            this.DescLabel = new System.Windows.Forms.Label();
            this.SuspendLayout();
            // 
            // FunctionList
            // 
            this.FunctionList.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
            this.FunctionCol,
            this.ActionCol,
            this.CheckCol});
            this.FunctionList.FullRowSelect = true;
            this.FunctionList.Location = new System.Drawing.Point(3, 3);
            this.FunctionList.MultiSelect = false;
            this.FunctionList.Name = "FunctionList";
            this.FunctionList.Size = new System.Drawing.Size(561, 453);
            this.FunctionList.TabIndex = 0;
            this.FunctionList.UseCompatibleStateImageBehavior = false;
            this.FunctionList.View = System.Windows.Forms.View.Details;
            this.FunctionList.SelectedIndexChanged += new System.EventHandler(this.FunctionList_SelectedIndexChanged);
            // 
            // FunctionCol
            // 
            this.FunctionCol.Text = "Function";
            this.FunctionCol.Width = 130;
            // 
            // ActionCol
            // 
            this.ActionCol.Text = "Action Tree";
            this.ActionCol.Width = 191;
            // 
            // CheckCol
            // 
            this.CheckCol.Text = "Check Tree";
            this.CheckCol.Width = 201;
            // 
            // TableLabel
            // 
            this.TableLabel.AutoSize = true;
            this.TableLabel.Location = new System.Drawing.Point(567, 6);
            this.TableLabel.Name = "TableLabel";
            this.TableLabel.Size = new System.Drawing.Size(118, 13);
            this.TableLabel.TabIndex = 1;
            this.TableLabel.Text = "Function Table Source:";
            // 
            // TableCombo
            // 
            this.TableCombo.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.TableCombo.Enabled = false;
            this.TableCombo.FormattingEnabled = true;
            this.TableCombo.Items.AddRange(new object[] {
            "OBJD",
            "Function Table (OBJf)"});
            this.TableCombo.Location = new System.Drawing.Point(570, 22);
            this.TableCombo.Name = "TableCombo";
            this.TableCombo.Size = new System.Drawing.Size(189, 21);
            this.TableCombo.TabIndex = 2;
            this.TableCombo.SelectedIndexChanged += new System.EventHandler(this.TableCombo_SelectedIndexChanged);
            // 
            // CheckButton
            // 
            this.CheckButton.Location = new System.Drawing.Point(570, 433);
            this.CheckButton.Name = "CheckButton";
            this.CheckButton.Size = new System.Drawing.Size(189, 23);
            this.CheckButton.TabIndex = 5;
            this.CheckButton.Text = "Set Check Tree...";
            this.CheckButton.UseVisualStyleBackColor = true;
            this.CheckButton.Click += new System.EventHandler(this.CheckButton_Click);
            // 
            // ActionButton
            // 
            this.ActionButton.Location = new System.Drawing.Point(570, 404);
            this.ActionButton.Name = "ActionButton";
            this.ActionButton.Size = new System.Drawing.Size(189, 23);
            this.ActionButton.TabIndex = 6;
            this.ActionButton.Text = "Set Action Tree...";
            this.ActionButton.UseVisualStyleBackColor = true;
            this.ActionButton.Click += new System.EventHandler(this.ActionButton_Click);
            // 
            // DescTitle
            // 
            this.DescTitle.AutoSize = true;
            this.DescTitle.Location = new System.Drawing.Point(567, 57);
            this.DescTitle.Name = "DescTitle";
            this.DescTitle.Size = new System.Drawing.Size(152, 13);
            this.DescTitle.TabIndex = 7;
            this.DescTitle.Text = "Selected Function Description:";
            // 
            // FilterCheck
            // 
            this.FilterCheck.AutoSize = true;
            this.FilterCheck.Location = new System.Drawing.Point(570, 381);
            this.FilterCheck.Name = "FilterCheck";
            this.FilterCheck.Size = new System.Drawing.Size(166, 17);
            this.FilterCheck.TabIndex = 8;
            this.FilterCheck.Text = "Only show assigned functions";
            this.FilterCheck.UseVisualStyleBackColor = true;
            this.FilterCheck.CheckedChanged += new System.EventHandler(this.FilterCheck_CheckedChanged);
            // 
            // DescLabel
            // 
            this.DescLabel.Location = new System.Drawing.Point(571, 74);
            this.DescLabel.Name = "DescLabel";
            this.DescLabel.Size = new System.Drawing.Size(188, 260);
            this.DescLabel.TabIndex = 9;
            this.DescLabel.Text = resources.GetString("DescLabel.Text");
            // 
            // OBJfEditor
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.DescLabel);
            this.Controls.Add(this.FilterCheck);
            this.Controls.Add(this.DescTitle);
            this.Controls.Add(this.ActionButton);
            this.Controls.Add(this.CheckButton);
            this.Controls.Add(this.TableCombo);
            this.Controls.Add(this.TableLabel);
            this.Controls.Add(this.FunctionList);
            this.Name = "OBJfEditor";
            this.Size = new System.Drawing.Size(762, 459);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.ListView FunctionList;
        private System.Windows.Forms.ColumnHeader FunctionCol;
        private System.Windows.Forms.ColumnHeader ActionCol;
        private System.Windows.Forms.ColumnHeader CheckCol;
        private System.Windows.Forms.Label TableLabel;
        private System.Windows.Forms.ComboBox TableCombo;
        private System.Windows.Forms.Button CheckButton;
        private System.Windows.Forms.Button ActionButton;
        private System.Windows.Forms.Label DescTitle;
        private System.Windows.Forms.CheckBox FilterCheck;
        private System.Windows.Forms.Label DescLabel;
    }
}
