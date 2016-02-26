namespace FSO.IDE.EditorComponent
{
    partial class VarAnimSelect
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(VarAnimSelect));
            this.AnimDisplay = new FSO.IDE.Common.AvatarAnimatorControl();
            this.AddButton = new System.Windows.Forms.Button();
            this.CancelBtn = new System.Windows.Forms.Button();
            this.SelectAnim = new System.Windows.Forms.Button();
            this.pictureBox1 = new System.Windows.Forms.PictureBox();
            this.SearchBox = new System.Windows.Forms.TextBox();
            this.label1 = new System.Windows.Forms.Label();
            this.label2 = new System.Windows.Forms.Label();
            this.AllList = new System.Windows.Forms.ListBox();
            this.MyList = new System.Windows.Forms.ListBox();
            this.RemoveButton = new System.Windows.Forms.Button();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox1)).BeginInit();
            this.SuspendLayout();
            // 
            // AnimDisplay
            // 
            this.AnimDisplay.Location = new System.Drawing.Point(214, 41);
            this.AnimDisplay.Name = "AnimDisplay";
            this.AnimDisplay.Size = new System.Drawing.Size(157, 408);
            this.AnimDisplay.TabIndex = 0;
            // 
            // AddButton
            // 
            this.AddButton.Location = new System.Drawing.Point(214, 12);
            this.AddButton.Name = "AddButton";
            this.AddButton.Size = new System.Drawing.Size(157, 23);
            this.AddButton.TabIndex = 1;
            this.AddButton.Text = "Add to Anim Set ->";
            this.AddButton.UseVisualStyleBackColor = true;
            this.AddButton.Click += new System.EventHandler(this.AddButton_Click);
            // 
            // CancelBtn
            // 
            this.CancelBtn.Location = new System.Drawing.Point(377, 397);
            this.CancelBtn.Name = "CancelBtn";
            this.CancelBtn.Size = new System.Drawing.Size(195, 23);
            this.CancelBtn.TabIndex = 2;
            this.CancelBtn.Text = "Cancel";
            this.CancelBtn.UseVisualStyleBackColor = true;
            this.CancelBtn.Click += new System.EventHandler(this.CancelBtn_Click);
            // 
            // SelectAnim
            // 
            this.SelectAnim.Location = new System.Drawing.Point(377, 426);
            this.SelectAnim.Name = "SelectAnim";
            this.SelectAnim.Size = new System.Drawing.Size(195, 23);
            this.SelectAnim.TabIndex = 3;
            this.SelectAnim.Text = "Select This Animation";
            this.SelectAnim.UseVisualStyleBackColor = true;
            this.SelectAnim.Click += new System.EventHandler(this.SelectAnim_Click);
            // 
            // pictureBox1
            // 
            this.pictureBox1.Image = global::FSO.IDE.Properties.Resources.search;
            this.pictureBox1.Location = new System.Drawing.Point(12, 29);
            this.pictureBox1.Name = "pictureBox1";
            this.pictureBox1.Size = new System.Drawing.Size(18, 19);
            this.pictureBox1.TabIndex = 5;
            this.pictureBox1.TabStop = false;
            // 
            // SearchBox
            // 
            this.SearchBox.Location = new System.Drawing.Point(36, 28);
            this.SearchBox.Name = "SearchBox";
            this.SearchBox.Size = new System.Drawing.Size(172, 20);
            this.SearchBox.TabIndex = 6;
            this.SearchBox.TextChanged += new System.EventHandler(this.SearchBox_TextChanged);
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(12, 12);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(75, 13);
            this.label1.TabIndex = 7;
            this.label1.Text = "All Animations:";
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(377, 22);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(63, 13);
            this.label2.TabIndex = 8;
            this.label2.Text = "Current Set:";
            // 
            // AllList
            // 
            this.AllList.FormattingEnabled = true;
            this.AllList.IntegralHeight = false;
            this.AllList.Location = new System.Drawing.Point(12, 54);
            this.AllList.Name = "AllList";
            this.AllList.Size = new System.Drawing.Size(196, 395);
            this.AllList.TabIndex = 9;
            this.AllList.SelectedIndexChanged += new System.EventHandler(this.AllList_SelectedIndexChanged);
            // 
            // MyList
            // 
            this.MyList.FormattingEnabled = true;
            this.MyList.IntegralHeight = false;
            this.MyList.Location = new System.Drawing.Point(377, 41);
            this.MyList.Name = "MyList";
            this.MyList.Size = new System.Drawing.Size(195, 350);
            this.MyList.TabIndex = 10;
            this.MyList.SelectedIndexChanged += new System.EventHandler(this.MyList_SelectedIndexChanged);
            // 
            // RemoveButton
            // 
            this.RemoveButton.Location = new System.Drawing.Point(497, 12);
            this.RemoveButton.Name = "RemoveButton";
            this.RemoveButton.Size = new System.Drawing.Size(75, 23);
            this.RemoveButton.TabIndex = 11;
            this.RemoveButton.Text = "Remove";
            this.RemoveButton.UseVisualStyleBackColor = true;
            this.RemoveButton.Click += new System.EventHandler(this.RemoveButton_Click);
            // 
            // VarAnimSelect
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(584, 461);
            this.Controls.Add(this.RemoveButton);
            this.Controls.Add(this.MyList);
            this.Controls.Add(this.AllList);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.pictureBox1);
            this.Controls.Add(this.SearchBox);
            this.Controls.Add(this.SelectAnim);
            this.Controls.Add(this.CancelBtn);
            this.Controls.Add(this.AddButton);
            this.Controls.Add(this.AnimDisplay);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.Name = "VarAnimSelect";
            this.Text = "Select Animation - a2o (#128)";
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox1)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private Common.AvatarAnimatorControl AnimDisplay;
        private System.Windows.Forms.Button AddButton;
        private System.Windows.Forms.Button CancelBtn;
        private System.Windows.Forms.Button SelectAnim;
        private System.Windows.Forms.PictureBox pictureBox1;
        private System.Windows.Forms.TextBox SearchBox;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.ListBox AllList;
        private System.Windows.Forms.ListBox MyList;
        private System.Windows.Forms.Button RemoveButton;
    }
}