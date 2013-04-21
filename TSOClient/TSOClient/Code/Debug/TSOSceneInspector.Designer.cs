namespace TSOClient.Code.Debug
{
    partial class TSOSceneInspector
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
            this.tabControl1 = new System.Windows.Forms.TabControl();
            this.tabWorld = new System.Windows.Forms.TabPage();
            this.valueRotateZBar = new System.Windows.Forms.TrackBar();
            this.valueRotateZ = new System.Windows.Forms.NumericUpDown();
            this.valueRotateYBar = new System.Windows.Forms.TrackBar();
            this.valueRotateY = new System.Windows.Forms.NumericUpDown();
            this.valueRotateXBar = new System.Windows.Forms.TrackBar();
            this.valueRotateX = new System.Windows.Forms.NumericUpDown();
            this.label3 = new System.Windows.Forms.Label();
            this.valueScaleZ = new System.Windows.Forms.NumericUpDown();
            this.valueZ = new System.Windows.Forms.NumericUpDown();
            this.valueY = new System.Windows.Forms.NumericUpDown();
            this.valueX = new System.Windows.Forms.NumericUpDown();
            this.label1 = new System.Windows.Forms.Label();
            this.valueScaleLock = new System.Windows.Forms.CheckBox();
            this.valueScaleY = new System.Windows.Forms.NumericUpDown();
            this.valueScaleX = new System.Windows.Forms.NumericUpDown();
            this.label2 = new System.Windows.Forms.Label();
            this.tabCamera = new System.Windows.Forms.TabPage();
            this.cameraTargetZ = new System.Windows.Forms.NumericUpDown();
            this.cameraTargetY = new System.Windows.Forms.NumericUpDown();
            this.cameraTargetX = new System.Windows.Forms.NumericUpDown();
            this.label5 = new System.Windows.Forms.Label();
            this.cameraZ = new System.Windows.Forms.NumericUpDown();
            this.cameraY = new System.Windows.Forms.NumericUpDown();
            this.cameraX = new System.Windows.Forms.NumericUpDown();
            this.label4 = new System.Windows.Forms.Label();
            this.toolStrip1.SuspendLayout();
            this.tabControl1.SuspendLayout();
            this.tabWorld.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.valueRotateZBar)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.valueRotateZ)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.valueRotateYBar)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.valueRotateY)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.valueRotateXBar)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.valueRotateX)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.valueScaleZ)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.valueZ)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.valueY)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.valueX)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.valueScaleY)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.valueScaleX)).BeginInit();
            this.tabCamera.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.cameraTargetZ)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.cameraTargetY)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.cameraTargetX)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.cameraZ)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.cameraY)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.cameraX)).BeginInit();
            this.SuspendLayout();
            // 
            // toolStrip1
            // 
            this.toolStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.refreshBtn});
            this.toolStrip1.Location = new System.Drawing.Point(0, 0);
            this.toolStrip1.Name = "toolStrip1";
            this.toolStrip1.Size = new System.Drawing.Size(227, 25);
            this.toolStrip1.TabIndex = 1;
            this.toolStrip1.Text = "toolStrip1";
            // 
            // refreshBtn
            // 
            this.refreshBtn.Image = global::TSOClient.Resource.arrow_circle;
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
            this.uiTree.Size = new System.Drawing.Size(227, 165);
            this.uiTree.TabIndex = 2;
            this.uiTree.AfterSelect += new System.Windows.Forms.TreeViewEventHandler(this.uiTree_AfterSelect);
            // 
            // tabControl1
            // 
            this.tabControl1.Controls.Add(this.tabWorld);
            this.tabControl1.Controls.Add(this.tabCamera);
            this.tabControl1.Location = new System.Drawing.Point(5, 196);
            this.tabControl1.Name = "tabControl1";
            this.tabControl1.SelectedIndex = 0;
            this.tabControl1.Size = new System.Drawing.Size(219, 285);
            this.tabControl1.TabIndex = 5;
            // 
            // tabWorld
            // 
            this.tabWorld.Controls.Add(this.valueRotateZBar);
            this.tabWorld.Controls.Add(this.valueRotateZ);
            this.tabWorld.Controls.Add(this.valueRotateYBar);
            this.tabWorld.Controls.Add(this.valueRotateY);
            this.tabWorld.Controls.Add(this.valueRotateXBar);
            this.tabWorld.Controls.Add(this.valueRotateX);
            this.tabWorld.Controls.Add(this.label3);
            this.tabWorld.Controls.Add(this.valueScaleZ);
            this.tabWorld.Controls.Add(this.valueZ);
            this.tabWorld.Controls.Add(this.valueY);
            this.tabWorld.Controls.Add(this.valueX);
            this.tabWorld.Controls.Add(this.label1);
            this.tabWorld.Controls.Add(this.valueScaleLock);
            this.tabWorld.Controls.Add(this.valueScaleY);
            this.tabWorld.Controls.Add(this.valueScaleX);
            this.tabWorld.Controls.Add(this.label2);
            this.tabWorld.Location = new System.Drawing.Point(4, 22);
            this.tabWorld.Name = "tabWorld";
            this.tabWorld.Padding = new System.Windows.Forms.Padding(3);
            this.tabWorld.Size = new System.Drawing.Size(211, 259);
            this.tabWorld.TabIndex = 0;
            this.tabWorld.Text = "Element World";
            this.tabWorld.UseVisualStyleBackColor = true;
            // 
            // valueRotateZBar
            // 
            this.valueRotateZBar.Location = new System.Drawing.Point(4, 207);
            this.valueRotateZBar.Maximum = 360;
            this.valueRotateZBar.Name = "valueRotateZBar";
            this.valueRotateZBar.RightToLeft = System.Windows.Forms.RightToLeft.No;
            this.valueRotateZBar.Size = new System.Drawing.Size(129, 45);
            this.valueRotateZBar.TabIndex = 42;
            this.valueRotateZBar.TickFrequency = 45;
            this.valueRotateZBar.ValueChanged += new System.EventHandler(this.valueRotateZBar_Scroll);
            // 
            // valueRotateZ
            // 
            this.valueRotateZ.Location = new System.Drawing.Point(142, 207);
            this.valueRotateZ.Maximum = new decimal(new int[] {
            99999,
            0,
            0,
            0});
            this.valueRotateZ.Name = "valueRotateZ";
            this.valueRotateZ.Size = new System.Drawing.Size(60, 20);
            this.valueRotateZ.TabIndex = 41;
            this.valueRotateZ.ValueChanged += new System.EventHandler(this.valueRotateZ_ValueChanged);
            // 
            // valueRotateYBar
            // 
            this.valueRotateYBar.Location = new System.Drawing.Point(4, 175);
            this.valueRotateYBar.Maximum = 360;
            this.valueRotateYBar.Name = "valueRotateYBar";
            this.valueRotateYBar.RightToLeft = System.Windows.Forms.RightToLeft.No;
            this.valueRotateYBar.Size = new System.Drawing.Size(129, 45);
            this.valueRotateYBar.TabIndex = 40;
            this.valueRotateYBar.TickFrequency = 45;
            this.valueRotateYBar.ValueChanged += new System.EventHandler(this.valueRotateYBar_Scroll);
            // 
            // valueRotateY
            // 
            this.valueRotateY.Location = new System.Drawing.Point(142, 175);
            this.valueRotateY.Maximum = new decimal(new int[] {
            99999,
            0,
            0,
            0});
            this.valueRotateY.Name = "valueRotateY";
            this.valueRotateY.Size = new System.Drawing.Size(60, 20);
            this.valueRotateY.TabIndex = 39;
            this.valueRotateY.ValueChanged += new System.EventHandler(this.valueRotateY_ValueChanged);
            // 
            // valueRotateXBar
            // 
            this.valueRotateXBar.Location = new System.Drawing.Point(4, 141);
            this.valueRotateXBar.Maximum = 360;
            this.valueRotateXBar.Name = "valueRotateXBar";
            this.valueRotateXBar.RightToLeft = System.Windows.Forms.RightToLeft.No;
            this.valueRotateXBar.Size = new System.Drawing.Size(129, 45);
            this.valueRotateXBar.TabIndex = 38;
            this.valueRotateXBar.TickFrequency = 45;
            this.valueRotateXBar.ValueChanged += new System.EventHandler(this.valueRotateXBar_Scroll);
            // 
            // valueRotateX
            // 
            this.valueRotateX.Location = new System.Drawing.Point(142, 141);
            this.valueRotateX.Maximum = new decimal(new int[] {
            99999,
            0,
            0,
            0});
            this.valueRotateX.Name = "valueRotateX";
            this.valueRotateX.Size = new System.Drawing.Size(60, 20);
            this.valueRotateX.TabIndex = 37;
            this.valueRotateX.ValueChanged += new System.EventHandler(this.valueRotateX_ValueChanged);
            // 
            // label3
            // 
            this.label3.Location = new System.Drawing.Point(6, 120);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(166, 18);
            this.label3.TabIndex = 36;
            this.label3.Text = "Rotation (x,y,z):";
            this.label3.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // valueScaleZ
            // 
            this.valueScaleZ.DecimalPlaces = 5;
            this.valueScaleZ.Increment = new decimal(new int[] {
            1,
            0,
            0,
            65536});
            this.valueScaleZ.Location = new System.Drawing.Point(142, 74);
            this.valueScaleZ.Maximum = new decimal(new int[] {
            99999,
            0,
            0,
            0});
            this.valueScaleZ.Name = "valueScaleZ";
            this.valueScaleZ.Size = new System.Drawing.Size(60, 20);
            this.valueScaleZ.TabIndex = 35;
            this.valueScaleZ.ValueChanged += new System.EventHandler(this.valueScaleZ_ValueChanged);
            // 
            // valueZ
            // 
            this.valueZ.Location = new System.Drawing.Point(142, 25);
            this.valueZ.Maximum = new decimal(new int[] {
            99999,
            0,
            0,
            0});
            this.valueZ.Minimum = new decimal(new int[] {
            999999,
            0,
            0,
            -2147483648});
            this.valueZ.Name = "valueZ";
            this.valueZ.Size = new System.Drawing.Size(60, 20);
            this.valueZ.TabIndex = 34;
            this.valueZ.ValueChanged += new System.EventHandler(this.valueZ_ValueChanged);
            // 
            // valueY
            // 
            this.valueY.Location = new System.Drawing.Point(73, 25);
            this.valueY.Maximum = new decimal(new int[] {
            99999,
            0,
            0,
            0});
            this.valueY.Minimum = new decimal(new int[] {
            999999,
            0,
            0,
            -2147483648});
            this.valueY.Name = "valueY";
            this.valueY.Size = new System.Drawing.Size(60, 20);
            this.valueY.TabIndex = 33;
            this.valueY.ValueChanged += new System.EventHandler(this.valueZ_ValueChanged);
            // 
            // valueX
            // 
            this.valueX.Location = new System.Drawing.Point(7, 25);
            this.valueX.Maximum = new decimal(new int[] {
            99999,
            0,
            0,
            0});
            this.valueX.Minimum = new decimal(new int[] {
            999999,
            0,
            0,
            -2147483648});
            this.valueX.Name = "valueX";
            this.valueX.Size = new System.Drawing.Size(60, 20);
            this.valueX.TabIndex = 32;
            this.valueX.ValueChanged += new System.EventHandler(this.valueZ_ValueChanged);
            // 
            // label1
            // 
            this.label1.Location = new System.Drawing.Point(6, 3);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(150, 18);
            this.label1.TabIndex = 31;
            this.label1.Text = "Position (x,y,z):";
            this.label1.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // valueScaleLock
            // 
            this.valueScaleLock.AutoSize = true;
            this.valueScaleLock.Checked = true;
            this.valueScaleLock.CheckState = System.Windows.Forms.CheckState.Checked;
            this.valueScaleLock.Location = new System.Drawing.Point(9, 100);
            this.valueScaleLock.Name = "valueScaleLock";
            this.valueScaleLock.Size = new System.Drawing.Size(50, 17);
            this.valueScaleLock.TabIndex = 30;
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
            this.valueScaleY.Location = new System.Drawing.Point(73, 74);
            this.valueScaleY.Maximum = new decimal(new int[] {
            99999,
            0,
            0,
            0});
            this.valueScaleY.Name = "valueScaleY";
            this.valueScaleY.Size = new System.Drawing.Size(60, 20);
            this.valueScaleY.TabIndex = 29;
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
            this.valueScaleX.Location = new System.Drawing.Point(7, 74);
            this.valueScaleX.Maximum = new decimal(new int[] {
            99999,
            0,
            0,
            0});
            this.valueScaleX.Name = "valueScaleX";
            this.valueScaleX.Size = new System.Drawing.Size(60, 20);
            this.valueScaleX.TabIndex = 28;
            this.valueScaleX.ValueChanged += new System.EventHandler(this.valueScaleX_ValueChanged);
            // 
            // label2
            // 
            this.label2.Location = new System.Drawing.Point(6, 52);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(73, 18);
            this.label2.TabIndex = 27;
            this.label2.Text = "Scale (x,y,z):";
            this.label2.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // tabCamera
            // 
            this.tabCamera.Controls.Add(this.cameraTargetZ);
            this.tabCamera.Controls.Add(this.cameraTargetY);
            this.tabCamera.Controls.Add(this.cameraTargetX);
            this.tabCamera.Controls.Add(this.label5);
            this.tabCamera.Controls.Add(this.cameraZ);
            this.tabCamera.Controls.Add(this.cameraY);
            this.tabCamera.Controls.Add(this.cameraX);
            this.tabCamera.Controls.Add(this.label4);
            this.tabCamera.Location = new System.Drawing.Point(4, 22);
            this.tabCamera.Name = "tabCamera";
            this.tabCamera.Padding = new System.Windows.Forms.Padding(3);
            this.tabCamera.Size = new System.Drawing.Size(211, 259);
            this.tabCamera.TabIndex = 1;
            this.tabCamera.Text = "Scene Camera";
            this.tabCamera.UseVisualStyleBackColor = true;
            // 
            // cameraTargetZ
            // 
            this.cameraTargetZ.Location = new System.Drawing.Point(142, 70);
            this.cameraTargetZ.Maximum = new decimal(new int[] {
            99999,
            0,
            0,
            0});
            this.cameraTargetZ.Minimum = new decimal(new int[] {
            999,
            0,
            0,
            -2147483648});
            this.cameraTargetZ.Name = "cameraTargetZ";
            this.cameraTargetZ.Size = new System.Drawing.Size(60, 20);
            this.cameraTargetZ.TabIndex = 42;
            this.cameraTargetZ.ValueChanged += new System.EventHandler(this.cameraTargetX_ValueChanged);
            // 
            // cameraTargetY
            // 
            this.cameraTargetY.Location = new System.Drawing.Point(73, 70);
            this.cameraTargetY.Maximum = new decimal(new int[] {
            99999,
            0,
            0,
            0});
            this.cameraTargetY.Minimum = new decimal(new int[] {
            999,
            0,
            0,
            -2147483648});
            this.cameraTargetY.Name = "cameraTargetY";
            this.cameraTargetY.Size = new System.Drawing.Size(60, 20);
            this.cameraTargetY.TabIndex = 41;
            this.cameraTargetY.ValueChanged += new System.EventHandler(this.cameraTargetX_ValueChanged);
            // 
            // cameraTargetX
            // 
            this.cameraTargetX.Location = new System.Drawing.Point(7, 70);
            this.cameraTargetX.Maximum = new decimal(new int[] {
            99999,
            0,
            0,
            0});
            this.cameraTargetX.Minimum = new decimal(new int[] {
            999,
            0,
            0,
            -2147483648});
            this.cameraTargetX.Name = "cameraTargetX";
            this.cameraTargetX.Size = new System.Drawing.Size(60, 20);
            this.cameraTargetX.TabIndex = 40;
            this.cameraTargetX.ValueChanged += new System.EventHandler(this.cameraTargetX_ValueChanged);
            // 
            // label5
            // 
            this.label5.Location = new System.Drawing.Point(6, 48);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(150, 18);
            this.label5.TabIndex = 39;
            this.label5.Text = "Target (x,y,z):";
            this.label5.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // cameraZ
            // 
            this.cameraZ.Location = new System.Drawing.Point(142, 25);
            this.cameraZ.Maximum = new decimal(new int[] {
            99999,
            0,
            0,
            0});
            this.cameraZ.Minimum = new decimal(new int[] {
            999999,
            0,
            0,
            -2147483648});
            this.cameraZ.Name = "cameraZ";
            this.cameraZ.Size = new System.Drawing.Size(60, 20);
            this.cameraZ.TabIndex = 38;
            this.cameraZ.ValueChanged += new System.EventHandler(this.cameraX_ValueChanged);
            // 
            // cameraY
            // 
            this.cameraY.Location = new System.Drawing.Point(73, 25);
            this.cameraY.Maximum = new decimal(new int[] {
            99999,
            0,
            0,
            0});
            this.cameraY.Minimum = new decimal(new int[] {
            999999,
            0,
            0,
            -2147483648});
            this.cameraY.Name = "cameraY";
            this.cameraY.Size = new System.Drawing.Size(60, 20);
            this.cameraY.TabIndex = 37;
            this.cameraY.ValueChanged += new System.EventHandler(this.cameraX_ValueChanged);
            // 
            // cameraX
            // 
            this.cameraX.Location = new System.Drawing.Point(7, 25);
            this.cameraX.Maximum = new decimal(new int[] {
            99999,
            0,
            0,
            0});
            this.cameraX.Minimum = new decimal(new int[] {
            999999,
            0,
            0,
            -2147483648});
            this.cameraX.Name = "cameraX";
            this.cameraX.Size = new System.Drawing.Size(60, 20);
            this.cameraX.TabIndex = 36;
            this.cameraX.ValueChanged += new System.EventHandler(this.cameraX_ValueChanged);
            // 
            // label4
            // 
            this.label4.Location = new System.Drawing.Point(6, 3);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(150, 18);
            this.label4.TabIndex = 35;
            this.label4.Text = "Position (x,y,z):";
            this.label4.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // TSOSceneInspector
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.Color.AliceBlue;
            this.ClientSize = new System.Drawing.Size(227, 488);
            this.Controls.Add(this.tabControl1);
            this.Controls.Add(this.uiTree);
            this.Controls.Add(this.toolStrip1);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.SizableToolWindow;
            this.Name = "TSOSceneInspector";
            this.StartPosition = System.Windows.Forms.FormStartPosition.Manual;
            this.Text = "Scene Inspetor";
            this.toolStrip1.ResumeLayout(false);
            this.toolStrip1.PerformLayout();
            this.tabControl1.ResumeLayout(false);
            this.tabWorld.ResumeLayout(false);
            this.tabWorld.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.valueRotateZBar)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.valueRotateZ)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.valueRotateYBar)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.valueRotateY)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.valueRotateXBar)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.valueRotateX)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.valueScaleZ)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.valueZ)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.valueY)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.valueX)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.valueScaleY)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.valueScaleX)).EndInit();
            this.tabCamera.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.cameraTargetZ)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.cameraTargetY)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.cameraTargetX)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.cameraZ)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.cameraY)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.cameraX)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.ToolStrip toolStrip1;
        private System.Windows.Forms.TreeView uiTree;
        private System.Windows.Forms.ToolStripButton refreshBtn;
        private System.Windows.Forms.TabControl tabControl1;
        private System.Windows.Forms.TabPage tabWorld;
        private System.Windows.Forms.TabPage tabCamera;
        private System.Windows.Forms.TrackBar valueRotateZBar;
        private System.Windows.Forms.NumericUpDown valueRotateZ;
        private System.Windows.Forms.TrackBar valueRotateYBar;
        private System.Windows.Forms.NumericUpDown valueRotateY;
        private System.Windows.Forms.TrackBar valueRotateXBar;
        private System.Windows.Forms.NumericUpDown valueRotateX;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.NumericUpDown valueScaleZ;
        private System.Windows.Forms.NumericUpDown valueZ;
        private System.Windows.Forms.NumericUpDown valueY;
        private System.Windows.Forms.NumericUpDown valueX;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.CheckBox valueScaleLock;
        private System.Windows.Forms.NumericUpDown valueScaleY;
        private System.Windows.Forms.NumericUpDown valueScaleX;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.NumericUpDown cameraTargetZ;
        private System.Windows.Forms.NumericUpDown cameraTargetY;
        private System.Windows.Forms.NumericUpDown cameraTargetX;
        private System.Windows.Forms.Label label5;
        private System.Windows.Forms.NumericUpDown cameraZ;
        private System.Windows.Forms.NumericUpDown cameraY;
        private System.Windows.Forms.NumericUpDown cameraX;
        private System.Windows.Forms.Label label4;

    }
}