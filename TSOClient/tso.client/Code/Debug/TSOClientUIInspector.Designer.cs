namespace TSOClient.Code.Debug
{
    partial class TSOClientUIInspector
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
            this.propertyBox = new System.Windows.Forms.GroupBox();
            this.valueAlpha = new System.Windows.Forms.NumericUpDown();
            this.label3 = new System.Windows.Forms.Label();
            this.valueScaleLock = new System.Windows.Forms.CheckBox();
            this.valueScaleY = new System.Windows.Forms.NumericUpDown();
            this.valueScaleX = new System.Windows.Forms.NumericUpDown();
            this.label2 = new System.Windows.Forms.Label();
            this.valueY = new System.Windows.Forms.NumericUpDown();
            this.valueX = new System.Windows.Forms.NumericUpDown();
            this.label1 = new System.Windows.Forms.Label();
            this.toolStrip1.SuspendLayout();
            this.propertyBox.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.valueAlpha)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.valueScaleY)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.valueScaleX)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.valueY)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.valueX)).BeginInit();
            this.SuspendLayout();
            // 
            // toolStrip1
            // 
            this.toolStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.refreshBtn});
            this.toolStrip1.Location = new System.Drawing.Point(0, 0);
            this.toolStrip1.Name = "toolStrip1";
            this.toolStrip1.Size = new System.Drawing.Size(222, 25);
            this.toolStrip1.TabIndex = 1;
            this.toolStrip1.Text = "toolStrip1";
            // 
            // refreshBtn
            // 
            this.refreshBtn.Image = global::TSOClient.Properties.Resources.arrow_circle;
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
            this.uiTree.Size = new System.Drawing.Size(222, 165);
            this.uiTree.TabIndex = 2;
            this.uiTree.AfterSelect += new System.Windows.Forms.TreeViewEventHandler(this.uiTree_AfterSelect);
            // 
            // propertyBox
            // 
            this.propertyBox.BackColor = System.Drawing.Color.White;
            this.propertyBox.Controls.Add(this.valueAlpha);
            this.propertyBox.Controls.Add(this.label3);
            this.propertyBox.Controls.Add(this.valueScaleLock);
            this.propertyBox.Controls.Add(this.valueScaleY);
            this.propertyBox.Controls.Add(this.valueScaleX);
            this.propertyBox.Controls.Add(this.label2);
            this.propertyBox.Controls.Add(this.valueY);
            this.propertyBox.Controls.Add(this.valueX);
            this.propertyBox.Controls.Add(this.label1);
            this.propertyBox.Location = new System.Drawing.Point(5, 196);
            this.propertyBox.Name = "propertyBox";
            this.propertyBox.Size = new System.Drawing.Size(212, 187);
            this.propertyBox.TabIndex = 3;
            this.propertyBox.TabStop = false;
            this.propertyBox.Text = "Properties";
            // 
            // valueAlpha
            // 
            this.valueAlpha.DecimalPlaces = 5;
            this.valueAlpha.Increment = new decimal(new int[] {
            1,
            0,
            0,
            65536});
            this.valueAlpha.Location = new System.Drawing.Point(15, 139);
            this.valueAlpha.Maximum = new decimal(new int[] {
            1,
            0,
            0,
            0});
            this.valueAlpha.Name = "valueAlpha";
            this.valueAlpha.Size = new System.Drawing.Size(136, 20);
            this.valueAlpha.TabIndex = 10;
            this.valueAlpha.ValueChanged += new System.EventHandler(this.valueAlpha_ValueChanged);
            // 
            // label3
            // 
            this.label3.Location = new System.Drawing.Point(12, 118);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(73, 18);
            this.label3.TabIndex = 8;
            this.label3.Text = "Alpha:";
            this.label3.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // valueScaleLock
            // 
            this.valueScaleLock.AutoSize = true;
            this.valueScaleLock.Checked = true;
            this.valueScaleLock.CheckState = System.Windows.Forms.CheckState.Checked;
            this.valueScaleLock.Location = new System.Drawing.Point(157, 93);
            this.valueScaleLock.Name = "valueScaleLock";
            this.valueScaleLock.Size = new System.Drawing.Size(50, 17);
            this.valueScaleLock.TabIndex = 7;
            this.valueScaleLock.Text = "Lock";
            this.valueScaleLock.UseVisualStyleBackColor = true;
            // 
            // valueScaleY
            // 
            this.valueScaleY.DecimalPlaces = 5;
            this.valueScaleY.Increment = new decimal(new int[] {
            1,
            0,
            0,
            65536});
            this.valueScaleY.Location = new System.Drawing.Point(86, 90);
            this.valueScaleY.Maximum = new decimal(new int[] {
            99999,
            0,
            0,
            0});
            this.valueScaleY.Name = "valueScaleY";
            this.valueScaleY.Size = new System.Drawing.Size(65, 20);
            this.valueScaleY.TabIndex = 6;
            this.valueScaleY.ValueChanged += new System.EventHandler(this.valueScaleY_ValueChanged);
            // 
            // valueScaleX
            // 
            this.valueScaleX.DecimalPlaces = 5;
            this.valueScaleX.Increment = new decimal(new int[] {
            1,
            0,
            0,
            65536});
            this.valueScaleX.Location = new System.Drawing.Point(15, 90);
            this.valueScaleX.Maximum = new decimal(new int[] {
            99999,
            0,
            0,
            0});
            this.valueScaleX.Name = "valueScaleX";
            this.valueScaleX.Size = new System.Drawing.Size(65, 20);
            this.valueScaleX.TabIndex = 5;
            this.valueScaleX.ValueChanged += new System.EventHandler(this.valueScaleX_ValueChanged);
            // 
            // label2
            // 
            this.label2.Location = new System.Drawing.Point(12, 68);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(73, 18);
            this.label2.TabIndex = 4;
            this.label2.Text = "Scale:";
            this.label2.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // valueY
            // 
            this.valueY.Location = new System.Drawing.Point(115, 45);
            this.valueY.Maximum = new decimal(new int[] {
            99999,
            0,
            0,
            0});
            this.valueY.Name = "valueY";
            this.valueY.Size = new System.Drawing.Size(85, 20);
            this.valueY.TabIndex = 3;
            this.valueY.ValueChanged += new System.EventHandler(this.valueY_ValueChanged);
            // 
            // valueX
            // 
            this.valueX.Location = new System.Drawing.Point(15, 45);
            this.valueX.Maximum = new decimal(new int[] {
            99999,
            0,
            0,
            0});
            this.valueX.Name = "valueX";
            this.valueX.Size = new System.Drawing.Size(85, 20);
            this.valueX.TabIndex = 1;
            this.valueX.ValueChanged += new System.EventHandler(this.valueX_ValueChanged);
            // 
            // label1
            // 
            this.label1.Location = new System.Drawing.Point(12, 23);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(73, 18);
            this.label1.TabIndex = 0;
            this.label1.Text = "Position:";
            this.label1.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // TSOClientUIInspector
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.Color.AliceBlue;
            this.ClientSize = new System.Drawing.Size(222, 388);
            this.Controls.Add(this.propertyBox);
            this.Controls.Add(this.uiTree);
            this.Controls.Add(this.toolStrip1);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.SizableToolWindow;
            this.Name = "TSOClientUIInspector";
            this.StartPosition = System.Windows.Forms.FormStartPosition.Manual;
            this.Text = "UI Inspetor";
            this.toolStrip1.ResumeLayout(false);
            this.toolStrip1.PerformLayout();
            this.propertyBox.ResumeLayout(false);
            this.propertyBox.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.valueAlpha)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.valueScaleY)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.valueScaleX)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.valueY)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.valueX)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.ToolStrip toolStrip1;
        private System.Windows.Forms.TreeView uiTree;
        private System.Windows.Forms.ToolStripButton refreshBtn;
        private System.Windows.Forms.GroupBox propertyBox;
        private System.Windows.Forms.CheckBox valueScaleLock;
        private System.Windows.Forms.NumericUpDown valueScaleY;
        private System.Windows.Forms.NumericUpDown valueScaleX;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.NumericUpDown valueY;
        private System.Windows.Forms.NumericUpDown valueX;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.NumericUpDown valueAlpha;
        private System.Windows.Forms.Label label3;

    }
}