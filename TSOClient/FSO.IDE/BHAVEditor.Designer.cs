
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
            ListViewItem listViewItem1 = new ListViewItem("CT - Notify Current Object Social Occurred");
            menuStrip1 = new MenuStrip();
            fileToolStripMenuItem = new ToolStripMenuItem();
            saveToFilebhavToolStripMenuItem = new ToolStripMenuItem();
            loadFromFileToolStripMenuItem = new ToolStripMenuItem();
            openParentResourceToolStripMenuItem = new ToolStripMenuItem();
            editToolStripMenuItem = new ToolStripMenuItem();
            undoToolStripMenuItem = new ToolStripMenuItem();
            redoToolStripMenuItem = new ToolStripMenuItem();
            toolStripSeparator1 = new ToolStripSeparator();
            copyToolStripMenuItem = new ToolStripMenuItem();
            pasteStripMenuItem = new ToolStripMenuItem();
            toolStripSeparator2 = new ToolStripSeparator();
            removeToolStripMenuItem = new ToolStripMenuItem();
            setFirstToolStripMenuItem = new ToolStripMenuItem();
            viewToolStripMenuItem = new ToolStripMenuItem();
            snapPrimitivesToGridToolStripMenuItem = new ToolStripMenuItem();
            insertToolStripMenuItem = new ToolStripMenuItem();
            trueToolStripMenuItem = new ToolStripMenuItem();
            falseToolStripMenuItem = new ToolStripMenuItem();
            labelToolStripMenuItem = new ToolStripMenuItem();
            commentToolStripMenuItem = new ToolStripMenuItem();
            MainTable = new TableLayoutPanel();
            splitContainer1 = new SplitContainer();
            PrimitivesGroup = new GroupBox();
            pictureBox1 = new PictureBox();
            SearchBox = new TextBox();
            PrimitiveList = new ListBox();
            tableLayoutPanel2 = new TableLayoutPanel();
            DebugBtn = new Button();
            SimBtn = new Button();
            ObjectBtn = new Button();
            PositionBtn = new Button();
            MathBtn = new Button();
            ControlBtn = new Button();
            LooksBtn = new Button();
            SubroutineBtn = new Button();
            TSOBtn = new Button();
            AllBtn = new Button();
            OperandGroup = new GroupBox();
            OperandScroller = new FlowLayoutPanel();
            OperandEditTable = new TableLayoutPanel();
            EditorControl = new FSO.IDE.EditorComponent.BHAVViewControl();
            DebugTable = new TableLayoutPanel();
            ObjectDataGrid = new PropertyGrid();
            groupBox1 = new GroupBox();
            StackView = new ListView();
            StackTreeNameCol = new ColumnHeader();
            StackSourceCol = new ColumnHeader();
            menuStrip1.SuspendLayout();
            MainTable.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)splitContainer1).BeginInit();
            splitContainer1.Panel1.SuspendLayout();
            splitContainer1.Panel2.SuspendLayout();
            splitContainer1.SuspendLayout();
            PrimitivesGroup.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)pictureBox1).BeginInit();
            tableLayoutPanel2.SuspendLayout();
            OperandGroup.SuspendLayout();
            OperandScroller.SuspendLayout();
            DebugTable.SuspendLayout();
            groupBox1.SuspendLayout();
            SuspendLayout();
            // 
            // menuStrip1
            // 
            menuStrip1.Items.AddRange(new ToolStripItem[] { fileToolStripMenuItem, editToolStripMenuItem, viewToolStripMenuItem, insertToolStripMenuItem });
            menuStrip1.Location = new Point(0, 0);
            menuStrip1.Name = "menuStrip1";
            menuStrip1.Size = new Size(1014, 24);
            menuStrip1.TabIndex = 1;
            menuStrip1.Text = "menuStrip1";
            // 
            // fileToolStripMenuItem
            // 
            fileToolStripMenuItem.DropDownItems.AddRange(new ToolStripItem[] { saveToFilebhavToolStripMenuItem, loadFromFileToolStripMenuItem, openParentResourceToolStripMenuItem });
            fileToolStripMenuItem.Name = "fileToolStripMenuItem";
            fileToolStripMenuItem.Size = new Size(37, 20);
            fileToolStripMenuItem.Text = "File";
            // 
            // saveToFilebhavToolStripMenuItem
            // 
            saveToFilebhavToolStripMenuItem.Enabled = false;
            saveToFilebhavToolStripMenuItem.Name = "saveToFilebhavToolStripMenuItem";
            saveToFilebhavToolStripMenuItem.Size = new Size(191, 22);
            saveToFilebhavToolStripMenuItem.Text = "Save to File (.bhav)";
            // 
            // loadFromFileToolStripMenuItem
            // 
            loadFromFileToolStripMenuItem.Enabled = false;
            loadFromFileToolStripMenuItem.Name = "loadFromFileToolStripMenuItem";
            loadFromFileToolStripMenuItem.Size = new Size(191, 22);
            loadFromFileToolStripMenuItem.Text = "Load from File";
            // 
            // openParentResourceToolStripMenuItem
            // 
            openParentResourceToolStripMenuItem.Name = "openParentResourceToolStripMenuItem";
            openParentResourceToolStripMenuItem.Size = new Size(191, 22);
            openParentResourceToolStripMenuItem.Text = "Open Parent Resource";
            openParentResourceToolStripMenuItem.Click += openParentResourceToolStripMenuItem_Click;
            // 
            // editToolStripMenuItem
            // 
            editToolStripMenuItem.DropDownItems.AddRange(new ToolStripItem[] { undoToolStripMenuItem, redoToolStripMenuItem, toolStripSeparator1, copyToolStripMenuItem, pasteStripMenuItem, toolStripSeparator2, removeToolStripMenuItem, setFirstToolStripMenuItem });
            editToolStripMenuItem.Name = "editToolStripMenuItem";
            editToolStripMenuItem.Size = new Size(39, 20);
            editToolStripMenuItem.Text = "Edit";
            // 
            // undoToolStripMenuItem
            // 
            undoToolStripMenuItem.Name = "undoToolStripMenuItem";
            undoToolStripMenuItem.ShortcutKeys = Keys.Control | Keys.Z;
            undoToolStripMenuItem.Size = new Size(169, 22);
            undoToolStripMenuItem.Text = "Undo";
            undoToolStripMenuItem.Click += undoToolStripMenuItem_Click;
            // 
            // redoToolStripMenuItem
            // 
            redoToolStripMenuItem.Name = "redoToolStripMenuItem";
            redoToolStripMenuItem.ShortcutKeys = Keys.Control | Keys.Y;
            redoToolStripMenuItem.Size = new Size(169, 22);
            redoToolStripMenuItem.Text = "Redo";
            redoToolStripMenuItem.Click += redoToolStripMenuItem_Click;
            // 
            // toolStripSeparator1
            // 
            toolStripSeparator1.Name = "toolStripSeparator1";
            toolStripSeparator1.Size = new Size(166, 6);
            // 
            // copyToolStripMenuItem
            // 
            copyToolStripMenuItem.Name = "copyToolStripMenuItem";
            copyToolStripMenuItem.ShortcutKeys = Keys.Control | Keys.C;
            copyToolStripMenuItem.Size = new Size(169, 22);
            copyToolStripMenuItem.Text = "Copy";
            copyToolStripMenuItem.Click += copyToolStripMenuItem_Click;
            // 
            // pasteStripMenuItem
            // 
            pasteStripMenuItem.Name = "pasteStripMenuItem";
            pasteStripMenuItem.ShortcutKeys = Keys.Control | Keys.V;
            pasteStripMenuItem.Size = new Size(169, 22);
            pasteStripMenuItem.Text = "Paste";
            pasteStripMenuItem.Click += pasteToolStripMenuItem_Click;
            // 
            // toolStripSeparator2
            // 
            toolStripSeparator2.Name = "toolStripSeparator2";
            toolStripSeparator2.Size = new Size(166, 6);
            // 
            // removeToolStripMenuItem
            // 
            removeToolStripMenuItem.Name = "removeToolStripMenuItem";
            removeToolStripMenuItem.ShortcutKeys = Keys.Delete;
            removeToolStripMenuItem.Size = new Size(169, 22);
            removeToolStripMenuItem.Text = "Remove";
            removeToolStripMenuItem.Click += removeToolStripMenuItem_Click;
            // 
            // setFirstToolStripMenuItem
            // 
            setFirstToolStripMenuItem.Name = "setFirstToolStripMenuItem";
            setFirstToolStripMenuItem.ShortcutKeys = Keys.Control | Keys.D1;
            setFirstToolStripMenuItem.Size = new Size(169, 22);
            setFirstToolStripMenuItem.Text = "Set as First";
            setFirstToolStripMenuItem.Click += setFirstToolStripMenuItem_Click;
            // 
            // viewToolStripMenuItem
            // 
            viewToolStripMenuItem.DropDownItems.AddRange(new ToolStripItem[] { snapPrimitivesToGridToolStripMenuItem });
            viewToolStripMenuItem.Name = "viewToolStripMenuItem";
            viewToolStripMenuItem.Size = new Size(44, 20);
            viewToolStripMenuItem.Text = "View";
            // 
            // snapPrimitivesToGridToolStripMenuItem
            // 
            snapPrimitivesToGridToolStripMenuItem.Name = "snapPrimitivesToGridToolStripMenuItem";
            snapPrimitivesToGridToolStripMenuItem.ShortcutKeys = Keys.Control | Keys.S;
            snapPrimitivesToGridToolStripMenuItem.Size = new Size(236, 22);
            snapPrimitivesToGridToolStripMenuItem.Text = "Snap Primitives To Grid";
            snapPrimitivesToGridToolStripMenuItem.Click += SnapPrimitivesToGridToolStripMenuItem_Click;
            // 
            // insertToolStripMenuItem
            // 
            insertToolStripMenuItem.DropDownItems.AddRange(new ToolStripItem[] { trueToolStripMenuItem, falseToolStripMenuItem, labelToolStripMenuItem, commentToolStripMenuItem });
            insertToolStripMenuItem.Name = "insertToolStripMenuItem";
            insertToolStripMenuItem.Size = new Size(48, 20);
            insertToolStripMenuItem.Text = "Insert";
            // 
            // trueToolStripMenuItem
            // 
            trueToolStripMenuItem.Name = "trueToolStripMenuItem";
            trueToolStripMenuItem.ShortcutKeys = Keys.Control | Keys.T;
            trueToolStripMenuItem.Size = new Size(180, 22);
            trueToolStripMenuItem.Text = "True";
            trueToolStripMenuItem.Click += trueToolStripMenuItem_Click;
            // 
            // falseToolStripMenuItem
            // 
            falseToolStripMenuItem.Name = "falseToolStripMenuItem";
            falseToolStripMenuItem.ShortcutKeys = Keys.Control | Keys.F;
            falseToolStripMenuItem.Size = new Size(180, 22);
            falseToolStripMenuItem.Text = "False";
            falseToolStripMenuItem.Click += falseToolStripMenuItem_Click;
            // 
            // labelToolStripMenuItem
            // 
            labelToolStripMenuItem.Name = "labelToolStripMenuItem";
            labelToolStripMenuItem.ShortcutKeys = Keys.Control | Keys.L;
            labelToolStripMenuItem.Size = new Size(180, 22);
            labelToolStripMenuItem.Text = "Label";
            labelToolStripMenuItem.Click += labelToolStripMenuItem_Click;
            // 
            // commentToolStripMenuItem
            // 
            commentToolStripMenuItem.Name = "commentToolStripMenuItem";
            commentToolStripMenuItem.ShortcutKeys = Keys.Control | Keys.Oem2;
            commentToolStripMenuItem.ShowShortcutKeys = false;
            commentToolStripMenuItem.Size = new Size(180, 22);
            commentToolStripMenuItem.Text = "Comment         Ctrl+/";
            commentToolStripMenuItem.Click += commentToolStripMenuItem_Click;
            // 
            // MainTable
            // 
            MainTable.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            MainTable.ColumnCount = 3;
            MainTable.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 260F));
            MainTable.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            MainTable.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 260F));
            MainTable.Controls.Add(splitContainer1, 0, 0);
            MainTable.Controls.Add(EditorControl, 1, 0);
            MainTable.Controls.Add(DebugTable, 2, 0);
            MainTable.Location = new Point(0, 27);
            MainTable.Name = "MainTable";
            MainTable.RowCount = 1;
            MainTable.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            MainTable.Size = new Size(1014, 569);
            MainTable.TabIndex = 2;
            // 
            // splitContainer1
            // 
            splitContainer1.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            splitContainer1.Location = new Point(3, 3);
            splitContainer1.Name = "splitContainer1";
            splitContainer1.Orientation = Orientation.Horizontal;
            // 
            // splitContainer1.Panel1
            // 
            splitContainer1.Panel1.Controls.Add(PrimitivesGroup);
            // 
            // splitContainer1.Panel2
            // 
            splitContainer1.Panel2.Controls.Add(OperandGroup);
            splitContainer1.Size = new Size(254, 563);
            splitContainer1.SplitterDistance = 314;
            splitContainer1.TabIndex = 2;
            // 
            // PrimitivesGroup
            // 
            PrimitivesGroup.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            PrimitivesGroup.Controls.Add(pictureBox1);
            PrimitivesGroup.Controls.Add(SearchBox);
            PrimitivesGroup.Controls.Add(PrimitiveList);
            PrimitivesGroup.Controls.Add(tableLayoutPanel2);
            PrimitivesGroup.Location = new Point(3, -1);
            PrimitivesGroup.Name = "PrimitivesGroup";
            PrimitivesGroup.Size = new Size(248, 312);
            PrimitivesGroup.TabIndex = 5;
            PrimitivesGroup.TabStop = false;
            PrimitivesGroup.Text = "Primitives";
            // 
            // pictureBox1
            // 
            pictureBox1.Image = Properties.Resources.search;
            pictureBox1.Location = new Point(11, 142);
            pictureBox1.Name = "pictureBox1";
            pictureBox1.Size = new Size(18, 19);
            pictureBox1.TabIndex = 3;
            pictureBox1.TabStop = false;
            // 
            // SearchBox
            // 
            SearchBox.Location = new Point(32, 141);
            SearchBox.Name = "SearchBox";
            SearchBox.Size = new Size(208, 22);
            SearchBox.TabIndex = 4;
            SearchBox.TextChanged += SearchBox_TextChanged;
            // 
            // PrimitiveList
            // 
            PrimitiveList.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            PrimitiveList.BorderStyle = BorderStyle.FixedSingle;
            PrimitiveList.FormattingEnabled = true;
            PrimitiveList.Location = new Point(10, 166);
            PrimitiveList.Name = "PrimitiveList";
            PrimitiveList.Size = new Size(230, 132);
            PrimitiveList.TabIndex = 3;
            PrimitiveList.SelectedIndexChanged += listBox1_SelectedIndexChanged;
            // 
            // tableLayoutPanel2
            // 
            tableLayoutPanel2.ColumnCount = 2;
            tableLayoutPanel2.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            tableLayoutPanel2.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            tableLayoutPanel2.Controls.Add(DebugBtn, 0, 3);
            tableLayoutPanel2.Controls.Add(SimBtn, 1, 2);
            tableLayoutPanel2.Controls.Add(ObjectBtn, 0, 2);
            tableLayoutPanel2.Controls.Add(PositionBtn, 1, 1);
            tableLayoutPanel2.Controls.Add(MathBtn, 1, 0);
            tableLayoutPanel2.Controls.Add(ControlBtn, 0, 0);
            tableLayoutPanel2.Controls.Add(LooksBtn, 0, 1);
            tableLayoutPanel2.Controls.Add(SubroutineBtn, 0, 4);
            tableLayoutPanel2.Controls.Add(TSOBtn, 1, 3);
            tableLayoutPanel2.Controls.Add(AllBtn, 1, 4);
            tableLayoutPanel2.Location = new Point(7, 18);
            tableLayoutPanel2.Name = "tableLayoutPanel2";
            tableLayoutPanel2.RowCount = 5;
            tableLayoutPanel2.RowStyles.Add(new RowStyle(SizeType.Percent, 20F));
            tableLayoutPanel2.RowStyles.Add(new RowStyle(SizeType.Percent, 20F));
            tableLayoutPanel2.RowStyles.Add(new RowStyle(SizeType.Percent, 20F));
            tableLayoutPanel2.RowStyles.Add(new RowStyle(SizeType.Percent, 20F));
            tableLayoutPanel2.RowStyles.Add(new RowStyle(SizeType.Percent, 20F));
            tableLayoutPanel2.Size = new Size(236, 118);
            tableLayoutPanel2.TabIndex = 2;
            // 
            // DebugBtn
            // 
            DebugBtn.BackColor = Color.FromArgb(255, 115, 115);
            DebugBtn.BackgroundImage = (Image)resources.GetObject("DebugBtn.BackgroundImage");
            DebugBtn.FlatAppearance.BorderColor = Color.White;
            DebugBtn.FlatStyle = FlatStyle.Popup;
            DebugBtn.Font = new Font("Segoe UI", 8.25F);
            DebugBtn.ForeColor = Color.FromArgb(102, 0, 0);
            DebugBtn.Location = new Point(1, 70);
            DebugBtn.Margin = new Padding(1);
            DebugBtn.Name = "DebugBtn";
            DebugBtn.Size = new Size(112, 20);
            DebugBtn.TabIndex = 7;
            DebugBtn.Text = "Debug";
            DebugBtn.UseVisualStyleBackColor = false;
            // 
            // SimBtn
            // 
            SimBtn.BackColor = Color.FromArgb(255, 151, 253);
            SimBtn.BackgroundImage = (Image)resources.GetObject("SimBtn.BackgroundImage");
            SimBtn.FlatAppearance.BorderColor = Color.White;
            SimBtn.FlatStyle = FlatStyle.Popup;
            SimBtn.Font = new Font("Segoe UI", 8.25F);
            SimBtn.ForeColor = Color.FromArgb(105, 0, 140);
            SimBtn.Location = new Point(122, 47);
            SimBtn.Margin = new Padding(4, 1, 1, 1);
            SimBtn.Name = "SimBtn";
            SimBtn.Size = new Size(112, 20);
            SimBtn.TabIndex = 6;
            SimBtn.Text = "Sim";
            SimBtn.UseVisualStyleBackColor = false;
            // 
            // ObjectBtn
            // 
            ObjectBtn.BackColor = Color.FromArgb(105, 0, 140);
            ObjectBtn.BackgroundImage = (Image)resources.GetObject("ObjectBtn.BackgroundImage");
            ObjectBtn.FlatAppearance.BorderColor = Color.White;
            ObjectBtn.FlatStyle = FlatStyle.Popup;
            ObjectBtn.Font = new Font("Segoe UI", 8.25F);
            ObjectBtn.ForeColor = Color.White;
            ObjectBtn.Location = new Point(1, 47);
            ObjectBtn.Margin = new Padding(1);
            ObjectBtn.Name = "ObjectBtn";
            ObjectBtn.Size = new Size(112, 20);
            ObjectBtn.TabIndex = 5;
            ObjectBtn.Text = "Object";
            ObjectBtn.UseVisualStyleBackColor = false;
            // 
            // PositionBtn
            // 
            PositionBtn.BackColor = Color.FromArgb(0, 89, 178);
            PositionBtn.BackgroundImage = Properties.Resources.diagbg10;
            PositionBtn.FlatAppearance.BorderColor = Color.White;
            PositionBtn.FlatStyle = FlatStyle.Popup;
            PositionBtn.Font = new Font("Segoe UI", 8.25F);
            PositionBtn.ForeColor = Color.White;
            PositionBtn.Location = new Point(122, 24);
            PositionBtn.Margin = new Padding(4, 1, 1, 1);
            PositionBtn.Name = "PositionBtn";
            PositionBtn.Size = new Size(112, 20);
            PositionBtn.TabIndex = 4;
            PositionBtn.Text = "Position";
            PositionBtn.UseVisualStyleBackColor = false;
            // 
            // MathBtn
            // 
            MathBtn.BackColor = Color.FromArgb(70, 140, 0);
            MathBtn.BackgroundImage = Properties.Resources.diagbg10;
            MathBtn.FlatStyle = FlatStyle.Popup;
            MathBtn.Font = new Font("Segoe UI", 8.25F);
            MathBtn.ForeColor = Color.White;
            MathBtn.Location = new Point(122, 1);
            MathBtn.Margin = new Padding(4, 1, 1, 1);
            MathBtn.Name = "MathBtn";
            MathBtn.Size = new Size(112, 20);
            MathBtn.TabIndex = 2;
            MathBtn.Text = "Math";
            MathBtn.UseVisualStyleBackColor = false;
            // 
            // ControlBtn
            // 
            ControlBtn.BackColor = Color.FromArgb(255, 191, 0);
            ControlBtn.BackgroundImage = Properties.Resources.diagbg20;
            ControlBtn.FlatAppearance.BorderColor = Color.White;
            ControlBtn.FlatStyle = FlatStyle.Popup;
            ControlBtn.Font = new Font("Segoe UI", 8.25F);
            ControlBtn.ForeColor = Color.FromArgb(102, 76, 0);
            ControlBtn.Location = new Point(1, 1);
            ControlBtn.Margin = new Padding(1);
            ControlBtn.Name = "ControlBtn";
            ControlBtn.Size = new Size(112, 20);
            ControlBtn.TabIndex = 1;
            ControlBtn.Text = "Control";
            ControlBtn.UseVisualStyleBackColor = false;
            // 
            // LooksBtn
            // 
            LooksBtn.BackColor = Color.FromArgb(115, 220, 255);
            LooksBtn.BackgroundImage = Properties.Resources.diagbg20;
            LooksBtn.FlatAppearance.BorderColor = Color.White;
            LooksBtn.FlatStyle = FlatStyle.Popup;
            LooksBtn.Font = new Font("Segoe UI", 8.25F);
            LooksBtn.ForeColor = Color.FromArgb(0, 105, 140);
            LooksBtn.Location = new Point(1, 24);
            LooksBtn.Margin = new Padding(1);
            LooksBtn.Name = "LooksBtn";
            LooksBtn.Size = new Size(112, 20);
            LooksBtn.TabIndex = 3;
            LooksBtn.Text = "Looks";
            LooksBtn.UseVisualStyleBackColor = false;
            // 
            // SubroutineBtn
            // 
            SubroutineBtn.BackgroundImage = Properties.Resources.diagbg;
            SubroutineBtn.FlatAppearance.BorderColor = Color.White;
            SubroutineBtn.FlatStyle = FlatStyle.Popup;
            SubroutineBtn.Font = new Font("Segoe UI", 8.25F);
            SubroutineBtn.Location = new Point(1, 93);
            SubroutineBtn.Margin = new Padding(1);
            SubroutineBtn.Name = "SubroutineBtn";
            SubroutineBtn.Size = new Size(112, 20);
            SubroutineBtn.TabIndex = 9;
            SubroutineBtn.Text = "Subroutine";
            SubroutineBtn.UseVisualStyleBackColor = true;
            // 
            // TSOBtn
            // 
            TSOBtn.BackColor = Color.FromArgb(140, 0, 0);
            TSOBtn.BackgroundImage = (Image)resources.GetObject("TSOBtn.BackgroundImage");
            TSOBtn.FlatAppearance.BorderColor = Color.White;
            TSOBtn.FlatStyle = FlatStyle.Popup;
            TSOBtn.Font = new Font("Segoe UI", 8.25F);
            TSOBtn.ForeColor = Color.White;
            TSOBtn.Location = new Point(122, 70);
            TSOBtn.Margin = new Padding(4, 1, 1, 1);
            TSOBtn.Name = "TSOBtn";
            TSOBtn.Size = new Size(112, 20);
            TSOBtn.TabIndex = 8;
            TSOBtn.Text = "TSO";
            TSOBtn.UseVisualStyleBackColor = false;
            // 
            // AllBtn
            // 
            AllBtn.BackColor = Color.Black;
            AllBtn.BackgroundImage = Properties.Resources.diagbg20;
            AllBtn.FlatAppearance.BorderColor = Color.White;
            AllBtn.FlatStyle = FlatStyle.Popup;
            AllBtn.Font = new Font("Segoe UI", 8.25F);
            AllBtn.ForeColor = Color.White;
            AllBtn.Location = new Point(122, 93);
            AllBtn.Margin = new Padding(4, 1, 1, 1);
            AllBtn.Name = "AllBtn";
            AllBtn.Size = new Size(112, 20);
            AllBtn.TabIndex = 10;
            AllBtn.Text = "All";
            AllBtn.UseVisualStyleBackColor = false;
            // 
            // OperandGroup
            // 
            OperandGroup.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            OperandGroup.Controls.Add(OperandScroller);
            OperandGroup.Location = new Point(3, 0);
            OperandGroup.Name = "OperandGroup";
            OperandGroup.Size = new Size(248, 239);
            OperandGroup.TabIndex = 5;
            OperandGroup.TabStop = false;
            OperandGroup.Text = "Operand";
            // 
            // OperandScroller
            // 
            OperandScroller.AutoScroll = true;
            OperandScroller.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            OperandScroller.Controls.Add(OperandEditTable);
            OperandScroller.Dock = DockStyle.Fill;
            OperandScroller.FlowDirection = FlowDirection.TopDown;
            OperandScroller.Location = new Point(3, 18);
            OperandScroller.Name = "OperandScroller";
            OperandScroller.Size = new Size(242, 218);
            OperandScroller.TabIndex = 6;
            OperandScroller.WrapContents = false;
            OperandScroller.Resize += OperandScroller_Resize;
            // 
            // OperandEditTable
            // 
            OperandEditTable.AutoSize = true;
            OperandEditTable.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            OperandEditTable.BackgroundImageLayout = ImageLayout.None;
            OperandEditTable.ColumnCount = 1;
            OperandEditTable.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            OperandEditTable.Dock = DockStyle.Fill;
            OperandEditTable.Location = new Point(0, 0);
            OperandEditTable.Margin = new Padding(0);
            OperandEditTable.MaximumSize = new Size(236, 0);
            OperandEditTable.Name = "OperandEditTable";
            OperandEditTable.RowCount = 1;
            OperandEditTable.RowStyles.Add(new RowStyle());
            OperandEditTable.RowStyles.Add(new RowStyle(SizeType.Absolute, 1F));
            OperandEditTable.RowStyles.Add(new RowStyle(SizeType.Absolute, 1F));
            OperandEditTable.RowStyles.Add(new RowStyle(SizeType.Absolute, 1F));
            OperandEditTable.RowStyles.Add(new RowStyle(SizeType.Absolute, 1F));
            OperandEditTable.RowStyles.Add(new RowStyle(SizeType.Absolute, 1F));
            OperandEditTable.RowStyles.Add(new RowStyle(SizeType.Absolute, 1F));
            OperandEditTable.Size = new Size(0, 0);
            OperandEditTable.TabIndex = 8;
            // 
            // EditorControl
            // 
            EditorControl.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            EditorControl.Location = new Point(260, 0);
            EditorControl.Margin = new Padding(0);
            EditorControl.Name = "EditorControl";
            EditorControl.Size = new Size(494, 569);
            EditorControl.TabIndex = 0;
            // 
            // DebugTable
            // 
            DebugTable.ColumnCount = 1;
            DebugTable.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            DebugTable.Controls.Add(ObjectDataGrid, 0, 1);
            DebugTable.Controls.Add(groupBox1, 0, 0);
            DebugTable.Dock = DockStyle.Fill;
            DebugTable.Location = new Point(757, 3);
            DebugTable.Name = "DebugTable";
            DebugTable.RowCount = 2;
            DebugTable.RowStyles.Add(new RowStyle(SizeType.Absolute, 200F));
            DebugTable.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            DebugTable.Size = new Size(254, 563);
            DebugTable.TabIndex = 3;
            // 
            // ObjectDataGrid
            // 
            ObjectDataGrid.BackColor = SystemColors.Control;
            ObjectDataGrid.CategoryForeColor = SystemColors.InactiveCaptionText;
            ObjectDataGrid.Dock = DockStyle.Fill;
            ObjectDataGrid.Location = new Point(3, 203);
            ObjectDataGrid.Name = "ObjectDataGrid";
            ObjectDataGrid.PropertySort = PropertySort.Categorized;
            ObjectDataGrid.Size = new Size(248, 357);
            ObjectDataGrid.TabIndex = 0;
            ObjectDataGrid.ToolbarVisible = false;
            // 
            // groupBox1
            // 
            groupBox1.Controls.Add(StackView);
            groupBox1.Dock = DockStyle.Fill;
            groupBox1.Location = new Point(3, 3);
            groupBox1.Name = "groupBox1";
            groupBox1.Size = new Size(248, 194);
            groupBox1.TabIndex = 1;
            groupBox1.TabStop = false;
            groupBox1.Text = "Stack";
            // 
            // StackView
            // 
            StackView.Columns.AddRange(new ColumnHeader[] { StackTreeNameCol, StackSourceCol });
            StackView.Dock = DockStyle.Fill;
            StackView.Items.AddRange(new ListViewItem[] { listViewItem1 });
            StackView.Location = new Point(3, 18);
            StackView.Margin = new Padding(6);
            StackView.MultiSelect = false;
            StackView.Name = "StackView";
            StackView.Size = new Size(242, 173);
            StackView.TabIndex = 0;
            StackView.UseCompatibleStateImageBehavior = false;
            StackView.View = View.Details;
            StackView.SelectedIndexChanged += StackView_SelectedIndexChanged;
            // 
            // StackTreeNameCol
            // 
            StackTreeNameCol.Text = "Tree Name";
            StackTreeNameCol.Width = 150;
            // 
            // StackSourceCol
            // 
            StackSourceCol.Text = "Source";
            StackSourceCol.Width = 88;
            // 
            // BHAVEditor
            // 
            AutoScaleDimensions = new SizeF(96F, 96F);
            AutoScaleMode = AutoScaleMode.Dpi;
            ClientSize = new Size(1014, 592);
            Controls.Add(MainTable);
            Controls.Add(menuStrip1);
            Icon = (Icon)resources.GetObject("$this.Icon");
            MainMenuStrip = menuStrip1;
            Name = "BHAVEditor";
            StartPosition = FormStartPosition.CenterScreen;
            Text = "BHAV Editor";
            Activated += BHAVEditor_Activated;
            Deactivate += BHAVEditor_Deactivate;
            FormClosing += BHAVEditor_FormClosing;
            KeyDown += BHAVEditor_KeyDown;
            KeyPress += BHAVEditor_KeyPress;
            menuStrip1.ResumeLayout(false);
            menuStrip1.PerformLayout();
            MainTable.ResumeLayout(false);
            splitContainer1.Panel1.ResumeLayout(false);
            splitContainer1.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)splitContainer1).EndInit();
            splitContainer1.ResumeLayout(false);
            PrimitivesGroup.ResumeLayout(false);
            PrimitivesGroup.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)pictureBox1).EndInit();
            tableLayoutPanel2.ResumeLayout(false);
            OperandGroup.ResumeLayout(false);
            OperandScroller.ResumeLayout(false);
            OperandScroller.PerformLayout();
            DebugTable.ResumeLayout(false);
            groupBox1.ResumeLayout(false);
            ResumeLayout(false);
            PerformLayout();

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
        private System.Windows.Forms.ToolStripMenuItem commentToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem labelToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem copyToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem pasteStripMenuItem;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparator2;
        private System.Windows.Forms.ToolStripMenuItem viewToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem snapPrimitivesToGridToolStripMenuItem;
    }
}

