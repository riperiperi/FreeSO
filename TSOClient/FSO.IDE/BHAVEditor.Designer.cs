
using System;

namespace FSO.IDE
{
    partial class BHAVEditor
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(BHAVEditor));
            System.Windows.Forms.ListViewItem listViewItem1 = new System.Windows.Forms.ListViewItem("CT - Notify Current Object Social Occurred");
            this.menuStrip1 = new System.Windows.Forms.MenuStrip();
            this.fileToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.saveToFilebhavToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.loadFromFileToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.openParentResourceToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.editToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.undoToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.redoToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripSeparator1 = new System.Windows.Forms.ToolStripSeparator();
            this.removeToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.setFirstToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.insertToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.trueToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.falseToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.MainTable = new System.Windows.Forms.TableLayoutPanel();
            this.splitContainer1 = new System.Windows.Forms.SplitContainer();
            this.PrimitivesGroup = new System.Windows.Forms.GroupBox();
            this.pictureBox1 = new System.Windows.Forms.PictureBox();
            this.SearchBox = new System.Windows.Forms.TextBox();
            this.PrimitiveList = new System.Windows.Forms.ListBox();
            this.tableLayoutPanel2 = new System.Windows.Forms.TableLayoutPanel();
            this.DebugBtn = new System.Windows.Forms.Button();
            this.SimBtn = new System.Windows.Forms.Button();
            this.ObjectBtn = new System.Windows.Forms.Button();
            this.PositionBtn = new System.Windows.Forms.Button();
            this.MathBtn = new System.Windows.Forms.Button();
            this.ControlBtn = new System.Windows.Forms.Button();
            this.LooksBtn = new System.Windows.Forms.Button();
            this.SubroutineBtn = new System.Windows.Forms.Button();
            this.TSOBtn = new System.Windows.Forms.Button();
            this.AllBtn = new System.Windows.Forms.Button();
            this.OperandGroup = new System.Windows.Forms.GroupBox();
            this.OperandScroller = new System.Windows.Forms.FlowLayoutPanel();
            this.OperandEditTable = new System.Windows.Forms.TableLayoutPanel();
            this.EditorControl = new FSO.IDE.EditorComponent.BHAVViewControl();
            this.DebugTable = new System.Windows.Forms.TableLayoutPanel();
            this.ObjectDataGrid = new System.Windows.Forms.PropertyGrid();
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this.StackView = new System.Windows.Forms.ListView();
            this.StackTreeNameCol = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.StackSourceCol = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.menuStrip1.SuspendLayout();
            this.MainTable.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer1)).BeginInit();
            this.splitContainer1.Panel1.SuspendLayout();
            this.splitContainer1.Panel2.SuspendLayout();
            this.splitContainer1.SuspendLayout();
            this.PrimitivesGroup.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox1)).BeginInit();
            this.tableLayoutPanel2.SuspendLayout();
            this.OperandGroup.SuspendLayout();
            this.OperandScroller.SuspendLayout();
            this.DebugTable.SuspendLayout();
            this.groupBox1.SuspendLayout();
            this.SuspendLayout();
            // 
            // menuStrip1
            // 
            this.menuStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.fileToolStripMenuItem,
            this.editToolStripMenuItem,
            this.insertToolStripMenuItem});
            this.menuStrip1.Location = new System.Drawing.Point(0, 0);
            this.menuStrip1.Name = "menuStrip1";
            this.menuStrip1.Size = new System.Drawing.Size(1014, 24);
            this.menuStrip1.TabIndex = 1;
            this.menuStrip1.Text = "menuStrip1";
            // 
            // fileToolStripMenuItem
            // 
            this.fileToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.saveToFilebhavToolStripMenuItem,
            this.loadFromFileToolStripMenuItem,
            this.openParentResourceToolStripMenuItem});
            this.fileToolStripMenuItem.Name = "fileToolStripMenuItem";
            this.fileToolStripMenuItem.Size = new System.Drawing.Size(37, 20);
            this.fileToolStripMenuItem.Text = "File";
            // 
            // saveToFilebhavToolStripMenuItem
            // 
            this.saveToFilebhavToolStripMenuItem.Name = "saveToFilebhavToolStripMenuItem";
            this.saveToFilebhavToolStripMenuItem.Size = new System.Drawing.Size(191, 22);
            this.saveToFilebhavToolStripMenuItem.Text = "Save to File (.bhav)";
            // 
            // loadFromFileToolStripMenuItem
            // 
            this.loadFromFileToolStripMenuItem.Name = "loadFromFileToolStripMenuItem";
            this.loadFromFileToolStripMenuItem.Size = new System.Drawing.Size(191, 22);
            this.loadFromFileToolStripMenuItem.Text = "Load from File";
            // 
            // openParentResourceToolStripMenuItem
            // 
            this.openParentResourceToolStripMenuItem.Name = "openParentResourceToolStripMenuItem";
            this.openParentResourceToolStripMenuItem.Size = new System.Drawing.Size(191, 22);
            this.openParentResourceToolStripMenuItem.Text = "Open Parent Resource";
            this.openParentResourceToolStripMenuItem.Click += new System.EventHandler(this.openParentResourceToolStripMenuItem_Click);
            // 
            // editToolStripMenuItem
            // 
            this.editToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.undoToolStripMenuItem,
            this.redoToolStripMenuItem,
            this.toolStripSeparator1,
            this.removeToolStripMenuItem,
            this.setFirstToolStripMenuItem});
            this.editToolStripMenuItem.Name = "editToolStripMenuItem";
            this.editToolStripMenuItem.Size = new System.Drawing.Size(39, 20);
            this.editToolStripMenuItem.Text = "Edit";
            // 
            // undoToolStripMenuItem
            // 
            this.undoToolStripMenuItem.Name = "undoToolStripMenuItem";
            this.undoToolStripMenuItem.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.Z)));
            this.undoToolStripMenuItem.Size = new System.Drawing.Size(169, 22);
            this.undoToolStripMenuItem.Text = "Undo";
            this.undoToolStripMenuItem.Click += new System.EventHandler(this.undoToolStripMenuItem_Click);
            // 
            // redoToolStripMenuItem
            // 
            this.redoToolStripMenuItem.Name = "redoToolStripMenuItem";
            this.redoToolStripMenuItem.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.Y)));
            this.redoToolStripMenuItem.Size = new System.Drawing.Size(169, 22);
            this.redoToolStripMenuItem.Text = "Redo";
            this.redoToolStripMenuItem.Click += new System.EventHandler(this.redoToolStripMenuItem_Click);
            // 
            // toolStripSeparator1
            // 
            this.toolStripSeparator1.Name = "toolStripSeparator1";
            this.toolStripSeparator1.Size = new System.Drawing.Size(166, 6);
            // 
            // removeToolStripMenuItem
            // 
            this.removeToolStripMenuItem.Name = "removeToolStripMenuItem";
            this.removeToolStripMenuItem.ShortcutKeys = System.Windows.Forms.Keys.Delete;
            this.removeToolStripMenuItem.Size = new System.Drawing.Size(169, 22);
            this.removeToolStripMenuItem.Text = "Remove";
            this.removeToolStripMenuItem.Click += new System.EventHandler(this.removeToolStripMenuItem_Click);
            // 
            // setFirstToolStripMenuItem
            // 
            this.setFirstToolStripMenuItem.Name = "setFirstToolStripMenuItem";
            this.setFirstToolStripMenuItem.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.D1)));
            this.setFirstToolStripMenuItem.Size = new System.Drawing.Size(169, 22);
            this.setFirstToolStripMenuItem.Text = "Set as First";
            this.setFirstToolStripMenuItem.Click += new System.EventHandler(this.setFirstToolStripMenuItem_Click);
            // 
            // insertToolStripMenuItem
            // 
            this.insertToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.trueToolStripMenuItem,
            this.falseToolStripMenuItem});
            this.insertToolStripMenuItem.Name = "insertToolStripMenuItem";
            this.insertToolStripMenuItem.Size = new System.Drawing.Size(48, 20);
            this.insertToolStripMenuItem.Text = "Insert";
            // 
            // trueToolStripMenuItem
            // 
            this.trueToolStripMenuItem.Name = "trueToolStripMenuItem";
            this.trueToolStripMenuItem.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.T)));
            this.trueToolStripMenuItem.Size = new System.Drawing.Size(140, 22);
            this.trueToolStripMenuItem.Text = "True";
            this.trueToolStripMenuItem.Click += new System.EventHandler(this.trueToolStripMenuItem_Click);
            // 
            // falseToolStripMenuItem
            // 
            this.falseToolStripMenuItem.Name = "falseToolStripMenuItem";
            this.falseToolStripMenuItem.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.F)));
            this.falseToolStripMenuItem.Size = new System.Drawing.Size(140, 22);
            this.falseToolStripMenuItem.Text = "False";
            this.falseToolStripMenuItem.Click += new System.EventHandler(this.falseToolStripMenuItem_Click);
            // 
            // MainTable
            // 
            this.MainTable.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.MainTable.ColumnCount = 3;
            this.MainTable.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 260F));
            this.MainTable.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.MainTable.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 260F));
            this.MainTable.Controls.Add(this.splitContainer1, 0, 0);
            this.MainTable.Controls.Add(this.EditorControl, 1, 0);
            this.MainTable.Controls.Add(this.DebugTable, 2, 0);
            this.MainTable.Location = new System.Drawing.Point(0, 27);
            this.MainTable.Name = "MainTable";
            this.MainTable.RowCount = 1;
            this.MainTable.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.MainTable.Size = new System.Drawing.Size(1014, 569);
            this.MainTable.TabIndex = 2;
            // 
            // splitContainer1
            // 
            this.splitContainer1.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.splitContainer1.Location = new System.Drawing.Point(3, 3);
            this.splitContainer1.Name = "splitContainer1";
            this.splitContainer1.Orientation = System.Windows.Forms.Orientation.Horizontal;
            // 
            // splitContainer1.Panel1
            // 
            this.splitContainer1.Panel1.Controls.Add(this.PrimitivesGroup);
            // 
            // splitContainer1.Panel2
            // 
            this.splitContainer1.Panel2.Controls.Add(this.OperandGroup);
            this.splitContainer1.Size = new System.Drawing.Size(254, 563);
            this.splitContainer1.SplitterDistance = 314;
            this.splitContainer1.TabIndex = 2;
            // 
            // PrimitivesGroup
            // 
            this.PrimitivesGroup.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.PrimitivesGroup.Controls.Add(this.pictureBox1);
            this.PrimitivesGroup.Controls.Add(this.SearchBox);
            this.PrimitivesGroup.Controls.Add(this.PrimitiveList);
            this.PrimitivesGroup.Controls.Add(this.tableLayoutPanel2);
            this.PrimitivesGroup.Location = new System.Drawing.Point(3, -1);
            this.PrimitivesGroup.Name = "PrimitivesGroup";
            this.PrimitivesGroup.Size = new System.Drawing.Size(248, 312);
            this.PrimitivesGroup.TabIndex = 5;
            this.PrimitivesGroup.TabStop = false;
            this.PrimitivesGroup.Text = "Primitives";
            // 
            // pictureBox1
            // 
            this.pictureBox1.Image = global::FSO.IDE.Properties.Resources.search;
            this.pictureBox1.Location = new System.Drawing.Point(11, 142);
            this.pictureBox1.Name = "pictureBox1";
            this.pictureBox1.Size = new System.Drawing.Size(18, 19);
            this.pictureBox1.TabIndex = 3;
            this.pictureBox1.TabStop = false;
            // 
            // SearchBox
            // 
            this.SearchBox.Location = new System.Drawing.Point(32, 141);
            this.SearchBox.Name = "SearchBox";
            this.SearchBox.Size = new System.Drawing.Size(208, 20);
            this.SearchBox.TabIndex = 4;
            this.SearchBox.TextChanged += new System.EventHandler(this.SearchBox_TextChanged);
            // 
            // PrimitiveList
            // 
            this.PrimitiveList.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.PrimitiveList.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.PrimitiveList.FormattingEnabled = true;
            this.PrimitiveList.Location = new System.Drawing.Point(10, 166);
            this.PrimitiveList.Name = "PrimitiveList";
            this.PrimitiveList.Size = new System.Drawing.Size(230, 132);
            this.PrimitiveList.TabIndex = 3;
            this.PrimitiveList.SelectedIndexChanged += new System.EventHandler(this.listBox1_SelectedIndexChanged);
            // 
            // tableLayoutPanel2
            // 
            this.tableLayoutPanel2.ColumnCount = 2;
            this.tableLayoutPanel2.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.tableLayoutPanel2.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.tableLayoutPanel2.Controls.Add(this.DebugBtn, 0, 3);
            this.tableLayoutPanel2.Controls.Add(this.SimBtn, 1, 2);
            this.tableLayoutPanel2.Controls.Add(this.ObjectBtn, 0, 2);
            this.tableLayoutPanel2.Controls.Add(this.PositionBtn, 1, 1);
            this.tableLayoutPanel2.Controls.Add(this.MathBtn, 1, 0);
            this.tableLayoutPanel2.Controls.Add(this.ControlBtn, 0, 0);
            this.tableLayoutPanel2.Controls.Add(this.LooksBtn, 0, 1);
            this.tableLayoutPanel2.Controls.Add(this.SubroutineBtn, 0, 4);
            this.tableLayoutPanel2.Controls.Add(this.TSOBtn, 1, 3);
            this.tableLayoutPanel2.Controls.Add(this.AllBtn, 1, 4);
            this.tableLayoutPanel2.Location = new System.Drawing.Point(7, 18);
            this.tableLayoutPanel2.Name = "tableLayoutPanel2";
            this.tableLayoutPanel2.RowCount = 5;
            this.tableLayoutPanel2.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 20F));
            this.tableLayoutPanel2.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 20F));
            this.tableLayoutPanel2.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 20F));
            this.tableLayoutPanel2.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 20F));
            this.tableLayoutPanel2.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 20F));
            this.tableLayoutPanel2.Size = new System.Drawing.Size(236, 118);
            this.tableLayoutPanel2.TabIndex = 2;
            // 
            // DebugBtn
            // 
            this.DebugBtn.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(255)))), ((int)(((byte)(115)))), ((int)(((byte)(115)))));
            this.DebugBtn.BackgroundImage = ((System.Drawing.Image)(resources.GetObject("DebugBtn.BackgroundImage")));
            this.DebugBtn.FlatAppearance.BorderColor = System.Drawing.Color.White;
            this.DebugBtn.FlatStyle = System.Windows.Forms.FlatStyle.Popup;
            this.DebugBtn.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.DebugBtn.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(102)))), ((int)(((byte)(0)))), ((int)(((byte)(0)))));
            this.DebugBtn.Location = new System.Drawing.Point(1, 70);
            this.DebugBtn.Margin = new System.Windows.Forms.Padding(1);
            this.DebugBtn.Name = "DebugBtn";
            this.DebugBtn.Size = new System.Drawing.Size(112, 20);
            this.DebugBtn.TabIndex = 7;
            this.DebugBtn.Text = "Debug";
            this.DebugBtn.UseVisualStyleBackColor = false;
            // 
            // SimBtn
            // 
            this.SimBtn.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(255)))), ((int)(((byte)(151)))), ((int)(((byte)(253)))));
            this.SimBtn.BackgroundImage = ((System.Drawing.Image)(resources.GetObject("SimBtn.BackgroundImage")));
            this.SimBtn.FlatAppearance.BorderColor = System.Drawing.Color.White;
            this.SimBtn.FlatStyle = System.Windows.Forms.FlatStyle.Popup;
            this.SimBtn.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.SimBtn.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(105)))), ((int)(((byte)(0)))), ((int)(((byte)(140)))));
            this.SimBtn.Location = new System.Drawing.Point(122, 47);
            this.SimBtn.Margin = new System.Windows.Forms.Padding(4, 1, 1, 1);
            this.SimBtn.Name = "SimBtn";
            this.SimBtn.Size = new System.Drawing.Size(112, 20);
            this.SimBtn.TabIndex = 6;
            this.SimBtn.Text = "Sim";
            this.SimBtn.UseVisualStyleBackColor = false;
            // 
            // ObjectBtn
            // 
            this.ObjectBtn.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(105)))), ((int)(((byte)(0)))), ((int)(((byte)(140)))));
            this.ObjectBtn.BackgroundImage = ((System.Drawing.Image)(resources.GetObject("ObjectBtn.BackgroundImage")));
            this.ObjectBtn.FlatAppearance.BorderColor = System.Drawing.Color.White;
            this.ObjectBtn.FlatStyle = System.Windows.Forms.FlatStyle.Popup;
            this.ObjectBtn.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.ObjectBtn.ForeColor = System.Drawing.Color.White;
            this.ObjectBtn.Location = new System.Drawing.Point(1, 47);
            this.ObjectBtn.Margin = new System.Windows.Forms.Padding(1);
            this.ObjectBtn.Name = "ObjectBtn";
            this.ObjectBtn.Size = new System.Drawing.Size(112, 20);
            this.ObjectBtn.TabIndex = 5;
            this.ObjectBtn.Text = "Object";
            this.ObjectBtn.UseVisualStyleBackColor = false;
            // 
            // PositionBtn
            // 
            this.PositionBtn.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(0)))), ((int)(((byte)(89)))), ((int)(((byte)(178)))));
            this.PositionBtn.BackgroundImage = global::FSO.IDE.Properties.Resources.diagbg10;
            this.PositionBtn.FlatAppearance.BorderColor = System.Drawing.Color.White;
            this.PositionBtn.FlatStyle = System.Windows.Forms.FlatStyle.Popup;
            this.PositionBtn.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.PositionBtn.ForeColor = System.Drawing.Color.White;
            this.PositionBtn.Location = new System.Drawing.Point(122, 24);
            this.PositionBtn.Margin = new System.Windows.Forms.Padding(4, 1, 1, 1);
            this.PositionBtn.Name = "PositionBtn";
            this.PositionBtn.Size = new System.Drawing.Size(112, 20);
            this.PositionBtn.TabIndex = 4;
            this.PositionBtn.Text = "Position";
            this.PositionBtn.UseVisualStyleBackColor = false;
            // 
            // MathBtn
            // 
            this.MathBtn.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(70)))), ((int)(((byte)(140)))), ((int)(((byte)(0)))));
            this.MathBtn.BackgroundImage = global::FSO.IDE.Properties.Resources.diagbg10;
            this.MathBtn.FlatStyle = System.Windows.Forms.FlatStyle.Popup;
            this.MathBtn.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.MathBtn.ForeColor = System.Drawing.Color.White;
            this.MathBtn.Location = new System.Drawing.Point(122, 1);
            this.MathBtn.Margin = new System.Windows.Forms.Padding(4, 1, 1, 1);
            this.MathBtn.Name = "MathBtn";
            this.MathBtn.Size = new System.Drawing.Size(112, 20);
            this.MathBtn.TabIndex = 2;
            this.MathBtn.Text = "Math";
            this.MathBtn.UseVisualStyleBackColor = false;
            // 
            // ControlBtn
            // 
            this.ControlBtn.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(255)))), ((int)(((byte)(191)))), ((int)(((byte)(0)))));
            this.ControlBtn.BackgroundImage = global::FSO.IDE.Properties.Resources.diagbg20;
            this.ControlBtn.FlatAppearance.BorderColor = System.Drawing.Color.White;
            this.ControlBtn.FlatStyle = System.Windows.Forms.FlatStyle.Popup;
            this.ControlBtn.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.ControlBtn.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(102)))), ((int)(((byte)(76)))), ((int)(((byte)(0)))));
            this.ControlBtn.Location = new System.Drawing.Point(1, 1);
            this.ControlBtn.Margin = new System.Windows.Forms.Padding(1);
            this.ControlBtn.Name = "ControlBtn";
            this.ControlBtn.Size = new System.Drawing.Size(112, 20);
            this.ControlBtn.TabIndex = 1;
            this.ControlBtn.Text = "Control";
            this.ControlBtn.UseVisualStyleBackColor = false;
            // 
            // LooksBtn
            // 
            this.LooksBtn.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(115)))), ((int)(((byte)(220)))), ((int)(((byte)(255)))));
            this.LooksBtn.BackgroundImage = global::FSO.IDE.Properties.Resources.diagbg20;
            this.LooksBtn.FlatAppearance.BorderColor = System.Drawing.Color.White;
            this.LooksBtn.FlatStyle = System.Windows.Forms.FlatStyle.Popup;
            this.LooksBtn.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.LooksBtn.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(0)))), ((int)(((byte)(105)))), ((int)(((byte)(140)))));
            this.LooksBtn.Location = new System.Drawing.Point(1, 24);
            this.LooksBtn.Margin = new System.Windows.Forms.Padding(1);
            this.LooksBtn.Name = "LooksBtn";
            this.LooksBtn.Size = new System.Drawing.Size(112, 20);
            this.LooksBtn.TabIndex = 3;
            this.LooksBtn.Text = "Looks";
            this.LooksBtn.UseVisualStyleBackColor = false;
            // 
            // SubroutineBtn
            // 
            this.SubroutineBtn.BackgroundImage = global::FSO.IDE.Properties.Resources.diagbg;
            this.SubroutineBtn.FlatAppearance.BorderColor = System.Drawing.Color.White;
            this.SubroutineBtn.FlatStyle = System.Windows.Forms.FlatStyle.Popup;
            this.SubroutineBtn.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.SubroutineBtn.Location = new System.Drawing.Point(1, 93);
            this.SubroutineBtn.Margin = new System.Windows.Forms.Padding(1);
            this.SubroutineBtn.Name = "SubroutineBtn";
            this.SubroutineBtn.Size = new System.Drawing.Size(112, 20);
            this.SubroutineBtn.TabIndex = 9;
            this.SubroutineBtn.Text = "Subroutine";
            this.SubroutineBtn.UseVisualStyleBackColor = true;
            // 
            // TSOBtn
            // 
            this.TSOBtn.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(140)))), ((int)(((byte)(0)))), ((int)(((byte)(0)))));
            this.TSOBtn.BackgroundImage = ((System.Drawing.Image)(resources.GetObject("TSOBtn.BackgroundImage")));
            this.TSOBtn.FlatAppearance.BorderColor = System.Drawing.Color.White;
            this.TSOBtn.FlatStyle = System.Windows.Forms.FlatStyle.Popup;
            this.TSOBtn.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.TSOBtn.ForeColor = System.Drawing.Color.White;
            this.TSOBtn.Location = new System.Drawing.Point(122, 70);
            this.TSOBtn.Margin = new System.Windows.Forms.Padding(4, 1, 1, 1);
            this.TSOBtn.Name = "TSOBtn";
            this.TSOBtn.Size = new System.Drawing.Size(112, 20);
            this.TSOBtn.TabIndex = 8;
            this.TSOBtn.Text = "TSO";
            this.TSOBtn.UseVisualStyleBackColor = false;
            // 
            // AllBtn
            // 
            this.AllBtn.BackColor = System.Drawing.Color.Black;
            this.AllBtn.BackgroundImage = global::FSO.IDE.Properties.Resources.diagbg20;
            this.AllBtn.FlatAppearance.BorderColor = System.Drawing.Color.White;
            this.AllBtn.FlatStyle = System.Windows.Forms.FlatStyle.Popup;
            this.AllBtn.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.AllBtn.ForeColor = System.Drawing.Color.White;
            this.AllBtn.Location = new System.Drawing.Point(122, 93);
            this.AllBtn.Margin = new System.Windows.Forms.Padding(4, 1, 1, 1);
            this.AllBtn.Name = "AllBtn";
            this.AllBtn.Size = new System.Drawing.Size(112, 20);
            this.AllBtn.TabIndex = 10;
            this.AllBtn.Text = "All";
            this.AllBtn.UseVisualStyleBackColor = false;
            // 
            // OperandGroup
            // 
            this.OperandGroup.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.OperandGroup.Controls.Add(this.OperandScroller);
            this.OperandGroup.Location = new System.Drawing.Point(3, 0);
            this.OperandGroup.Name = "OperandGroup";
            this.OperandGroup.Size = new System.Drawing.Size(248, 239);
            this.OperandGroup.TabIndex = 5;
            this.OperandGroup.TabStop = false;
            this.OperandGroup.Text = "Operand";
            // 
            // OperandScroller
            // 
            this.OperandScroller.AutoScroll = true;
            this.OperandScroller.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.OperandScroller.Controls.Add(this.OperandEditTable);
            this.OperandScroller.Dock = System.Windows.Forms.DockStyle.Fill;
            this.OperandScroller.FlowDirection = System.Windows.Forms.FlowDirection.TopDown;
            this.OperandScroller.Location = new System.Drawing.Point(3, 16);
            this.OperandScroller.Name = "OperandScroller";
            this.OperandScroller.Size = new System.Drawing.Size(242, 220);
            this.OperandScroller.TabIndex = 6;
            this.OperandScroller.WrapContents = false;
            this.OperandScroller.Resize += new System.EventHandler(this.OperandScroller_Resize);
            // 
            // OperandEditTable
            // 
            this.OperandEditTable.AutoSize = true;
            this.OperandEditTable.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.OperandEditTable.BackgroundImageLayout = System.Windows.Forms.ImageLayout.None;
            this.OperandEditTable.ColumnCount = 1;
            this.OperandEditTable.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.OperandEditTable.Dock = System.Windows.Forms.DockStyle.Fill;
            this.OperandEditTable.Location = new System.Drawing.Point(0, 0);
            this.OperandEditTable.Margin = new System.Windows.Forms.Padding(0);
            this.OperandEditTable.MaximumSize = new System.Drawing.Size(236, 0);
            this.OperandEditTable.Name = "OperandEditTable";
            this.OperandEditTable.RowCount = 1;
            this.OperandEditTable.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.OperandEditTable.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 1F));
            this.OperandEditTable.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 1F));
            this.OperandEditTable.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 1F));
            this.OperandEditTable.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 1F));
            this.OperandEditTable.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 1F));
            this.OperandEditTable.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 1F));
            this.OperandEditTable.Size = new System.Drawing.Size(0, 0);
            this.OperandEditTable.TabIndex = 8;
            // 
            // EditorControl
            // 
            this.EditorControl.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.EditorControl.Location = new System.Drawing.Point(260, 0);
            this.EditorControl.Margin = new System.Windows.Forms.Padding(0);
            this.EditorControl.Name = "EditorControl";
            this.EditorControl.Size = new System.Drawing.Size(494, 569);
            this.EditorControl.TabIndex = 0;
            // 
            // DebugTable
            // 
            this.DebugTable.ColumnCount = 1;
            this.DebugTable.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.DebugTable.Controls.Add(this.ObjectDataGrid, 0, 1);
            this.DebugTable.Controls.Add(this.groupBox1, 0, 0);
            this.DebugTable.Dock = System.Windows.Forms.DockStyle.Fill;
            this.DebugTable.Location = new System.Drawing.Point(757, 3);
            this.DebugTable.Name = "DebugTable";
            this.DebugTable.RowCount = 2;
            this.DebugTable.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 200F));
            this.DebugTable.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.DebugTable.Size = new System.Drawing.Size(254, 563);
            this.DebugTable.TabIndex = 3;
            // 
            // ObjectDataGrid
            // 
            this.ObjectDataGrid.CategoryForeColor = System.Drawing.SystemColors.InactiveCaptionText;
            this.ObjectDataGrid.Dock = System.Windows.Forms.DockStyle.Fill;
            this.ObjectDataGrid.Location = new System.Drawing.Point(3, 203);
            this.ObjectDataGrid.Name = "ObjectDataGrid";
            this.ObjectDataGrid.PropertySort = System.Windows.Forms.PropertySort.Categorized;
            this.ObjectDataGrid.Size = new System.Drawing.Size(248, 357);
            this.ObjectDataGrid.TabIndex = 0;
            this.ObjectDataGrid.ToolbarVisible = false;
            // 
            // groupBox1
            // 
            this.groupBox1.Controls.Add(this.StackView);
            this.groupBox1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.groupBox1.Location = new System.Drawing.Point(3, 3);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.Size = new System.Drawing.Size(248, 194);
            this.groupBox1.TabIndex = 1;
            this.groupBox1.TabStop = false;
            this.groupBox1.Text = "Stack";
            // 
            // StackView
            // 
            this.StackView.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
            this.StackTreeNameCol,
            this.StackSourceCol});
            this.StackView.Dock = System.Windows.Forms.DockStyle.Fill;
            this.StackView.HideSelection = false;
            this.StackView.Items.AddRange(new System.Windows.Forms.ListViewItem[] {
            listViewItem1});
            this.StackView.Location = new System.Drawing.Point(3, 16);
            this.StackView.Margin = new System.Windows.Forms.Padding(6);
            this.StackView.MultiSelect = false;
            this.StackView.Name = "StackView";
            this.StackView.Size = new System.Drawing.Size(242, 175);
            this.StackView.TabIndex = 0;
            this.StackView.UseCompatibleStateImageBehavior = false;
            this.StackView.View = System.Windows.Forms.View.Details;
            this.StackView.SelectedIndexChanged += new System.EventHandler(this.StackView_SelectedIndexChanged);
            // 
            // StackTreeNameCol
            // 
            this.StackTreeNameCol.Text = "Tree Name";
            this.StackTreeNameCol.Width = 150;
            // 
            // StackSourceCol
            // 
            this.StackSourceCol.Text = "Source";
            this.StackSourceCol.Width = 88;
            // 
            // BHAVEditor
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1014, 592);
            this.Controls.Add(this.MainTable);
            this.Controls.Add(this.menuStrip1);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.MainMenuStrip = this.menuStrip1;
            this.Name = "BHAVEditor";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "BHAV Editor";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.BHAVEditor_FormClosing);
            this.KeyDown += new System.Windows.Forms.KeyEventHandler(this.BHAVEditor_KeyDown);
            this.menuStrip1.ResumeLayout(false);
            this.menuStrip1.PerformLayout();
            this.MainTable.ResumeLayout(false);
            this.splitContainer1.Panel1.ResumeLayout(false);
            this.splitContainer1.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer1)).EndInit();
            this.splitContainer1.ResumeLayout(false);
            this.PrimitivesGroup.ResumeLayout(false);
            this.PrimitivesGroup.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox1)).EndInit();
            this.tableLayoutPanel2.ResumeLayout(false);
            this.OperandGroup.ResumeLayout(false);
            this.OperandScroller.ResumeLayout(false);
            this.OperandScroller.PerformLayout();
            this.DebugTable.ResumeLayout(false);
            this.groupBox1.ResumeLayout(false);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion
        private System.Windows.Forms.MenuStrip menuStrip1;
        private System.Windows.Forms.ToolStripMenuItem fileToolStripMenuItem;
        public EditorComponent.BHAVViewControl EditorControl;
        private System.Windows.Forms.TableLayoutPanel MainTable;
        private System.Windows.Forms.Button ControlBtn;
        private System.Windows.Forms.SplitContainer splitContainer1;
        private System.Windows.Forms.TableLayoutPanel tableLayoutPanel2;
        private System.Windows.Forms.Button SimBtn;
        private System.Windows.Forms.Button ObjectBtn;
        private System.Windows.Forms.Button PositionBtn;
        private System.Windows.Forms.Button LooksBtn;
        private System.Windows.Forms.Button MathBtn;
        private System.Windows.Forms.Button SubroutineBtn;
        private System.Windows.Forms.Button TSOBtn;
        private System.Windows.Forms.Button DebugBtn;
        private System.Windows.Forms.ListBox PrimitiveList;
        private System.Windows.Forms.TextBox SearchBox;
        private System.Windows.Forms.PictureBox pictureBox1;
        private System.Windows.Forms.GroupBox PrimitivesGroup;
        private System.Windows.Forms.GroupBox OperandGroup;
        private System.Windows.Forms.Button AllBtn;
        private System.Windows.Forms.FlowLayoutPanel OperandScroller;
        private System.Windows.Forms.TableLayoutPanel OperandEditTable;
        private System.Windows.Forms.ToolStripMenuItem editToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem undoToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem redoToolStripMenuItem;
        private System.Windows.Forms.TableLayoutPanel DebugTable;
        private System.Windows.Forms.PropertyGrid ObjectDataGrid;
        private System.Windows.Forms.GroupBox groupBox1;
        private System.Windows.Forms.ListView StackView;
        private System.Windows.Forms.ColumnHeader StackTreeNameCol;
        private System.Windows.Forms.ColumnHeader StackSourceCol;
        private System.Windows.Forms.ToolStripMenuItem saveToFilebhavToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem loadFromFileToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem openParentResourceToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem insertToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem trueToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem falseToolStripMenuItem;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparator1;
        private System.Windows.Forms.ToolStripMenuItem removeToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem setFirstToolStripMenuItem;
    }
}

