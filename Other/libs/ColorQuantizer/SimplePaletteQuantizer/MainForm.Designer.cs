namespace SimplePaletteQuantizer
{
    partial class MainForm
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(MainForm));
            this.dialogOpenFile = new System.Windows.Forms.OpenFileDialog();
            this.panelStatistics = new System.Windows.Forms.Panel();
            this.splitContainerPngSizes = new System.Windows.Forms.SplitContainer();
            this.editProjectedPngSize = new System.Windows.Forms.TextBox();
            this.labelProjectedPngSize = new System.Windows.Forms.Label();
            this.editNewPngSize = new System.Windows.Forms.TextBox();
            this.labelNewPngSize = new System.Windows.Forms.Label();
            this.splitContainerGifSizes = new System.Windows.Forms.SplitContainer();
            this.editProjectedGifSize = new System.Windows.Forms.TextBox();
            this.labelProjectedGifSize = new System.Windows.Forms.Label();
            this.editNewGifSize = new System.Windows.Forms.TextBox();
            this.labelNewGifSize = new System.Windows.Forms.Label();
            this.panelMain = new System.Windows.Forms.Panel();
            this.splitterMain = new System.Windows.Forms.Splitter();
            this.panelLeft = new System.Windows.Forms.Panel();
            this.pictureSource = new System.Windows.Forms.PictureBox();
            this.panelSource = new System.Windows.Forms.Panel();
            this.listSource = new System.Windows.Forms.ComboBox();
            this.labelSource = new System.Windows.Forms.Label();
            this.checkShowError = new System.Windows.Forms.CheckBox();
            this.panelDirectory = new System.Windows.Forms.Panel();
            this.editDirectory = new System.Windows.Forms.TextBox();
            this.labelDirectory = new System.Windows.Forms.Label();
            this.panelFilename = new System.Windows.Forms.Panel();
            this.editFilename = new System.Windows.Forms.TextBox();
            this.labelFilename = new System.Windows.Forms.Label();
            this.panelSourceInfo = new System.Windows.Forms.Panel();
            this.editSourceInfo = new System.Windows.Forms.TextBox();
            this.panelRight = new System.Windows.Forms.Panel();
            this.pictureTarget = new System.Windows.Forms.PictureBox();
            this.panelDithering = new System.Windows.Forms.Panel();
            this.listDitherer = new System.Windows.Forms.ComboBox();
            this.labelParallelTasks = new System.Windows.Forms.Label();
            this.listParallel = new System.Windows.Forms.ComboBox();
            this.labelDitherer = new System.Windows.Forms.Label();
            this.panelColorCache = new System.Windows.Forms.Panel();
            this.listColorCache = new System.Windows.Forms.ComboBox();
            this.labelColorModel = new System.Windows.Forms.Label();
            this.listColorModel = new System.Windows.Forms.ComboBox();
            this.labelColorCache = new System.Windows.Forms.Label();
            this.panelMethod = new System.Windows.Forms.Panel();
            this.listMethod = new System.Windows.Forms.ComboBox();
            this.label1 = new System.Windows.Forms.Label();
            this.listColors = new System.Windows.Forms.ComboBox();
            this.labelMethod = new System.Windows.Forms.Label();
            this.panelTargetInfo = new System.Windows.Forms.Panel();
            this.editTargetInfo = new System.Windows.Forms.TextBox();
            this.panelControls = new System.Windows.Forms.Panel();
            this.buttonUpdate = new System.Windows.Forms.Button();
            this.buttonBrowse = new System.Windows.Forms.Button();
            this.panelStatistics.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.splitContainerPngSizes)).BeginInit();
            this.splitContainerPngSizes.Panel1.SuspendLayout();
            this.splitContainerPngSizes.Panel2.SuspendLayout();
            this.splitContainerPngSizes.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.splitContainerGifSizes)).BeginInit();
            this.splitContainerGifSizes.Panel1.SuspendLayout();
            this.splitContainerGifSizes.Panel2.SuspendLayout();
            this.splitContainerGifSizes.SuspendLayout();
            this.panelMain.SuspendLayout();
            this.panelLeft.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.pictureSource)).BeginInit();
            this.panelSource.SuspendLayout();
            this.panelDirectory.SuspendLayout();
            this.panelFilename.SuspendLayout();
            this.panelSourceInfo.SuspendLayout();
            this.panelRight.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.pictureTarget)).BeginInit();
            this.panelDithering.SuspendLayout();
            this.panelColorCache.SuspendLayout();
            this.panelMethod.SuspendLayout();
            this.panelTargetInfo.SuspendLayout();
            this.panelControls.SuspendLayout();
            this.SuspendLayout();
            // 
            // dialogOpenFile
            // 
            this.dialogOpenFile.Filter = "Supported images|*.png;*.jpg;*.gif;*.jpeg;*.bmp;*.tiff";
            // 
            // panelStatistics
            // 
            this.panelStatistics.AutoSize = true;
            this.panelStatistics.BackColor = System.Drawing.Color.WhiteSmoke;
            this.panelStatistics.Controls.Add(this.splitContainerPngSizes);
            this.panelStatistics.Controls.Add(this.splitContainerGifSizes);
            this.panelStatistics.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.panelStatistics.Location = new System.Drawing.Point(5, 460);
            this.panelStatistics.Name = "panelStatistics";
            this.panelStatistics.Size = new System.Drawing.Size(674, 60);
            this.panelStatistics.TabIndex = 4;
            // 
            // splitContainerPngSizes
            // 
            this.splitContainerPngSizes.Dock = System.Windows.Forms.DockStyle.Top;
            this.splitContainerPngSizes.Location = new System.Drawing.Point(0, 30);
            this.splitContainerPngSizes.Name = "splitContainerPngSizes";
            // 
            // splitContainerPngSizes.Panel1
            // 
            this.splitContainerPngSizes.Panel1.Controls.Add(this.editProjectedPngSize);
            this.splitContainerPngSizes.Panel1.Controls.Add(this.labelProjectedPngSize);
            this.splitContainerPngSizes.Panel1.Padding = new System.Windows.Forms.Padding(5);
            // 
            // splitContainerPngSizes.Panel2
            // 
            this.splitContainerPngSizes.Panel2.Controls.Add(this.editNewPngSize);
            this.splitContainerPngSizes.Panel2.Controls.Add(this.labelNewPngSize);
            this.splitContainerPngSizes.Panel2.Padding = new System.Windows.Forms.Padding(5);
            this.splitContainerPngSizes.Size = new System.Drawing.Size(674, 30);
            this.splitContainerPngSizes.SplitterDistance = 332;
            this.splitContainerPngSizes.TabIndex = 2;
            this.splitContainerPngSizes.TabStop = false;
            // 
            // editProjectedPngSize
            // 
            this.editProjectedPngSize.Dock = System.Windows.Forms.DockStyle.Fill;
            this.editProjectedPngSize.Location = new System.Drawing.Point(155, 5);
            this.editProjectedPngSize.Name = "editProjectedPngSize";
            this.editProjectedPngSize.ReadOnly = true;
            this.editProjectedPngSize.Size = new System.Drawing.Size(172, 20);
            this.editProjectedPngSize.TabIndex = 2;
            this.editProjectedPngSize.TabStop = false;
            // 
            // labelProjectedPngSize
            // 
            this.labelProjectedPngSize.Dock = System.Windows.Forms.DockStyle.Left;
            this.labelProjectedPngSize.Location = new System.Drawing.Point(5, 5);
            this.labelProjectedPngSize.Name = "labelProjectedPngSize";
            this.labelProjectedPngSize.Size = new System.Drawing.Size(150, 20);
            this.labelProjectedPngSize.TabIndex = 1;
            this.labelProjectedPngSize.Text = "Source file size (in bytes):";
            this.labelProjectedPngSize.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // editNewPngSize
            // 
            this.editNewPngSize.Dock = System.Windows.Forms.DockStyle.Fill;
            this.editNewPngSize.Location = new System.Drawing.Point(185, 5);
            this.editNewPngSize.Name = "editNewPngSize";
            this.editNewPngSize.ReadOnly = true;
            this.editNewPngSize.Size = new System.Drawing.Size(148, 20);
            this.editNewPngSize.TabIndex = 2;
            this.editNewPngSize.TabStop = false;
            // 
            // labelNewPngSize
            // 
            this.labelNewPngSize.Dock = System.Windows.Forms.DockStyle.Left;
            this.labelNewPngSize.Location = new System.Drawing.Point(5, 5);
            this.labelNewPngSize.Name = "labelNewPngSize";
            this.labelNewPngSize.Size = new System.Drawing.Size(180, 20);
            this.labelNewPngSize.TabIndex = 1;
            this.labelNewPngSize.Text = "New projected PNG size (in bytes):";
            this.labelNewPngSize.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // splitContainerGifSizes
            // 
            this.splitContainerGifSizes.Dock = System.Windows.Forms.DockStyle.Top;
            this.splitContainerGifSizes.Location = new System.Drawing.Point(0, 0);
            this.splitContainerGifSizes.Name = "splitContainerGifSizes";
            // 
            // splitContainerGifSizes.Panel1
            // 
            this.splitContainerGifSizes.Panel1.Controls.Add(this.editProjectedGifSize);
            this.splitContainerGifSizes.Panel1.Controls.Add(this.labelProjectedGifSize);
            this.splitContainerGifSizes.Panel1.Padding = new System.Windows.Forms.Padding(5);
            // 
            // splitContainerGifSizes.Panel2
            // 
            this.splitContainerGifSizes.Panel2.Controls.Add(this.editNewGifSize);
            this.splitContainerGifSizes.Panel2.Controls.Add(this.labelNewGifSize);
            this.splitContainerGifSizes.Panel2.Padding = new System.Windows.Forms.Padding(5);
            this.splitContainerGifSizes.Size = new System.Drawing.Size(674, 30);
            this.splitContainerGifSizes.SplitterDistance = 332;
            this.splitContainerGifSizes.TabIndex = 1;
            this.splitContainerGifSizes.TabStop = false;
            // 
            // editProjectedGifSize
            // 
            this.editProjectedGifSize.Dock = System.Windows.Forms.DockStyle.Fill;
            this.editProjectedGifSize.Location = new System.Drawing.Point(155, 5);
            this.editProjectedGifSize.Name = "editProjectedGifSize";
            this.editProjectedGifSize.ReadOnly = true;
            this.editProjectedGifSize.Size = new System.Drawing.Size(172, 20);
            this.editProjectedGifSize.TabIndex = 2;
            this.editProjectedGifSize.TabStop = false;
            // 
            // labelProjectedGifSize
            // 
            this.labelProjectedGifSize.Dock = System.Windows.Forms.DockStyle.Left;
            this.labelProjectedGifSize.Location = new System.Drawing.Point(5, 5);
            this.labelProjectedGifSize.Name = "labelProjectedGifSize";
            this.labelProjectedGifSize.Size = new System.Drawing.Size(150, 20);
            this.labelProjectedGifSize.TabIndex = 1;
            this.labelProjectedGifSize.Text = "Projected GIF size (in bytes):";
            this.labelProjectedGifSize.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // editNewGifSize
            // 
            this.editNewGifSize.Dock = System.Windows.Forms.DockStyle.Fill;
            this.editNewGifSize.Location = new System.Drawing.Point(185, 5);
            this.editNewGifSize.Name = "editNewGifSize";
            this.editNewGifSize.ReadOnly = true;
            this.editNewGifSize.Size = new System.Drawing.Size(148, 20);
            this.editNewGifSize.TabIndex = 2;
            this.editNewGifSize.TabStop = false;
            // 
            // labelNewGifSize
            // 
            this.labelNewGifSize.Dock = System.Windows.Forms.DockStyle.Left;
            this.labelNewGifSize.Location = new System.Drawing.Point(5, 5);
            this.labelNewGifSize.Name = "labelNewGifSize";
            this.labelNewGifSize.Size = new System.Drawing.Size(180, 20);
            this.labelNewGifSize.TabIndex = 1;
            this.labelNewGifSize.Text = "New projected GIF size (in bytes):";
            this.labelNewGifSize.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // panelMain
            // 
            this.panelMain.Controls.Add(this.splitterMain);
            this.panelMain.Controls.Add(this.panelLeft);
            this.panelMain.Controls.Add(this.panelRight);
            this.panelMain.Dock = System.Windows.Forms.DockStyle.Fill;
            this.panelMain.Location = new System.Drawing.Point(5, 5);
            this.panelMain.Name = "panelMain";
            this.panelMain.Padding = new System.Windows.Forms.Padding(0, 0, 0, 5);
            this.panelMain.Size = new System.Drawing.Size(674, 455);
            this.panelMain.TabIndex = 5;
            // 
            // splitterMain
            // 
            this.splitterMain.BackColor = System.Drawing.SystemColors.ControlDarkDark;
            this.splitterMain.Dock = System.Windows.Forms.DockStyle.Right;
            this.splitterMain.Location = new System.Drawing.Point(332, 0);
            this.splitterMain.Name = "splitterMain";
            this.splitterMain.Size = new System.Drawing.Size(5, 450);
            this.splitterMain.TabIndex = 2;
            this.splitterMain.TabStop = false;
            // 
            // panelLeft
            // 
            this.panelLeft.Controls.Add(this.pictureSource);
            this.panelLeft.Controls.Add(this.panelSource);
            this.panelLeft.Controls.Add(this.panelDirectory);
            this.panelLeft.Controls.Add(this.panelFilename);
            this.panelLeft.Controls.Add(this.panelSourceInfo);
            this.panelLeft.Dock = System.Windows.Forms.DockStyle.Fill;
            this.panelLeft.Location = new System.Drawing.Point(0, 0);
            this.panelLeft.Name = "panelLeft";
            this.panelLeft.Size = new System.Drawing.Size(337, 450);
            this.panelLeft.TabIndex = 0;
            // 
            // pictureSource
            // 
            this.pictureSource.Dock = System.Windows.Forms.DockStyle.Fill;
            this.pictureSource.Location = new System.Drawing.Point(0, 100);
            this.pictureSource.Name = "pictureSource";
            this.pictureSource.Size = new System.Drawing.Size(337, 350);
            this.pictureSource.SizeMode = System.Windows.Forms.PictureBoxSizeMode.Zoom;
            this.pictureSource.TabIndex = 13;
            this.pictureSource.TabStop = false;
            // 
            // panelSource
            // 
            this.panelSource.Controls.Add(this.listSource);
            this.panelSource.Controls.Add(this.labelSource);
            this.panelSource.Controls.Add(this.checkShowError);
            this.panelSource.Dock = System.Windows.Forms.DockStyle.Top;
            this.panelSource.Location = new System.Drawing.Point(0, 75);
            this.panelSource.Name = "panelSource";
            this.panelSource.Padding = new System.Windows.Forms.Padding(0, 0, 8, 5);
            this.panelSource.Size = new System.Drawing.Size(337, 25);
            this.panelSource.TabIndex = 12;
            // 
            // listSource
            // 
            this.listSource.Dock = System.Windows.Forms.DockStyle.Fill;
            this.listSource.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.listSource.Enabled = false;
            this.listSource.FormattingEnabled = true;
            this.listSource.Items.AddRange(new object[] {
            "Original",
            "GIF (default)"});
            this.listSource.Location = new System.Drawing.Point(79, 0);
            this.listSource.Name = "listSource";
            this.listSource.Size = new System.Drawing.Size(146, 21);
            this.listSource.TabIndex = 0;
            this.listSource.SelectedIndexChanged += new System.EventHandler(this.ListSourceSelectedIndexChanged);
            // 
            // labelSource
            // 
            this.labelSource.AutoSize = true;
            this.labelSource.Dock = System.Windows.Forms.DockStyle.Left;
            this.labelSource.Location = new System.Drawing.Point(0, 0);
            this.labelSource.Name = "labelSource";
            this.labelSource.Padding = new System.Windows.Forms.Padding(0, 4, 0, 4);
            this.labelSource.Size = new System.Drawing.Size(79, 21);
            this.labelSource.TabIndex = 12;
            this.labelSource.Text = "Image preview:";
            this.labelSource.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // checkShowError
            // 
            this.checkShowError.AutoSize = true;
            this.checkShowError.Checked = true;
            this.checkShowError.CheckState = System.Windows.Forms.CheckState.Checked;
            this.checkShowError.Dock = System.Windows.Forms.DockStyle.Right;
            this.checkShowError.Location = new System.Drawing.Point(225, 0);
            this.checkShowError.Name = "checkShowError";
            this.checkShowError.Padding = new System.Windows.Forms.Padding(8, 4, 0, 0);
            this.checkShowError.Size = new System.Drawing.Size(104, 20);
            this.checkShowError.TabIndex = 1;
            this.checkShowError.Text = "Show NRMSD";
            this.checkShowError.UseVisualStyleBackColor = true;
            this.checkShowError.CheckedChanged += new System.EventHandler(this.CheckShowErrorCheckedChanged);
            // 
            // panelDirectory
            // 
            this.panelDirectory.Controls.Add(this.editDirectory);
            this.panelDirectory.Controls.Add(this.labelDirectory);
            this.panelDirectory.Dock = System.Windows.Forms.DockStyle.Top;
            this.panelDirectory.Location = new System.Drawing.Point(0, 50);
            this.panelDirectory.Name = "panelDirectory";
            this.panelDirectory.Padding = new System.Windows.Forms.Padding(0, 0, 10, 5);
            this.panelDirectory.Size = new System.Drawing.Size(337, 25);
            this.panelDirectory.TabIndex = 14;
            // 
            // editDirectory
            // 
            this.editDirectory.Dock = System.Windows.Forms.DockStyle.Fill;
            this.editDirectory.Location = new System.Drawing.Point(76, 0);
            this.editDirectory.Name = "editDirectory";
            this.editDirectory.ReadOnly = true;
            this.editDirectory.Size = new System.Drawing.Size(251, 20);
            this.editDirectory.TabIndex = 2;
            this.editDirectory.TabStop = false;
            // 
            // labelDirectory
            // 
            this.labelDirectory.Dock = System.Windows.Forms.DockStyle.Left;
            this.labelDirectory.Location = new System.Drawing.Point(0, 0);
            this.labelDirectory.Name = "labelDirectory";
            this.labelDirectory.Size = new System.Drawing.Size(76, 20);
            this.labelDirectory.TabIndex = 1;
            this.labelDirectory.Text = "Directory:";
            this.labelDirectory.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // panelFilename
            // 
            this.panelFilename.Controls.Add(this.editFilename);
            this.panelFilename.Controls.Add(this.labelFilename);
            this.panelFilename.Dock = System.Windows.Forms.DockStyle.Top;
            this.panelFilename.Location = new System.Drawing.Point(0, 25);
            this.panelFilename.Name = "panelFilename";
            this.panelFilename.Padding = new System.Windows.Forms.Padding(0, 0, 10, 5);
            this.panelFilename.Size = new System.Drawing.Size(337, 25);
            this.panelFilename.TabIndex = 10;
            // 
            // editFilename
            // 
            this.editFilename.Dock = System.Windows.Forms.DockStyle.Fill;
            this.editFilename.Location = new System.Drawing.Point(76, 0);
            this.editFilename.Name = "editFilename";
            this.editFilename.ReadOnly = true;
            this.editFilename.Size = new System.Drawing.Size(251, 20);
            this.editFilename.TabIndex = 2;
            this.editFilename.TabStop = false;
            // 
            // labelFilename
            // 
            this.labelFilename.Dock = System.Windows.Forms.DockStyle.Left;
            this.labelFilename.Location = new System.Drawing.Point(0, 0);
            this.labelFilename.Name = "labelFilename";
            this.labelFilename.Size = new System.Drawing.Size(76, 20);
            this.labelFilename.TabIndex = 1;
            this.labelFilename.Text = "Filename:";
            this.labelFilename.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // panelSourceInfo
            // 
            this.panelSourceInfo.Controls.Add(this.editSourceInfo);
            this.panelSourceInfo.Dock = System.Windows.Forms.DockStyle.Top;
            this.panelSourceInfo.Location = new System.Drawing.Point(0, 0);
            this.panelSourceInfo.Name = "panelSourceInfo";
            this.panelSourceInfo.Padding = new System.Windows.Forms.Padding(0, 0, 8, 5);
            this.panelSourceInfo.Size = new System.Drawing.Size(337, 25);
            this.panelSourceInfo.TabIndex = 7;
            // 
            // editSourceInfo
            // 
            this.editSourceInfo.Dock = System.Windows.Forms.DockStyle.Fill;
            this.editSourceInfo.Location = new System.Drawing.Point(0, 0);
            this.editSourceInfo.Name = "editSourceInfo";
            this.editSourceInfo.ReadOnly = true;
            this.editSourceInfo.Size = new System.Drawing.Size(329, 20);
            this.editSourceInfo.TabIndex = 8;
            this.editSourceInfo.TabStop = false;
            this.editSourceInfo.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
            // 
            // panelRight
            // 
            this.panelRight.Controls.Add(this.pictureTarget);
            this.panelRight.Controls.Add(this.panelDithering);
            this.panelRight.Controls.Add(this.panelColorCache);
            this.panelRight.Controls.Add(this.panelMethod);
            this.panelRight.Controls.Add(this.panelTargetInfo);
            this.panelRight.Dock = System.Windows.Forms.DockStyle.Right;
            this.panelRight.Location = new System.Drawing.Point(337, 0);
            this.panelRight.Name = "panelRight";
            this.panelRight.Size = new System.Drawing.Size(337, 450);
            this.panelRight.TabIndex = 1;
            // 
            // pictureTarget
            // 
            this.pictureTarget.Dock = System.Windows.Forms.DockStyle.Fill;
            this.pictureTarget.Location = new System.Drawing.Point(0, 100);
            this.pictureTarget.Name = "pictureTarget";
            this.pictureTarget.Size = new System.Drawing.Size(337, 350);
            this.pictureTarget.SizeMode = System.Windows.Forms.PictureBoxSizeMode.Zoom;
            this.pictureTarget.TabIndex = 12;
            this.pictureTarget.TabStop = false;
            // 
            // panelDithering
            // 
            this.panelDithering.Controls.Add(this.listDitherer);
            this.panelDithering.Controls.Add(this.labelParallelTasks);
            this.panelDithering.Controls.Add(this.listParallel);
            this.panelDithering.Controls.Add(this.labelDitherer);
            this.panelDithering.Dock = System.Windows.Forms.DockStyle.Top;
            this.panelDithering.Location = new System.Drawing.Point(0, 75);
            this.panelDithering.Name = "panelDithering";
            this.panelDithering.Padding = new System.Windows.Forms.Padding(5, 0, 5, 5);
            this.panelDithering.Size = new System.Drawing.Size(337, 25);
            this.panelDithering.TabIndex = 13;
            // 
            // listDitherer
            // 
            this.listDitherer.Dock = System.Windows.Forms.DockStyle.Fill;
            this.listDitherer.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.listDitherer.FormattingEnabled = true;
            this.listDitherer.Items.AddRange(new object[] {
            "No dithering",
            "--[ Ordered ]--",
            "Bayer dithering (4x4)",
            "Bayer dithering (8x8)",
            "Clustered dot (4x4)",
            "Dot halftoning (8x8)",
            "--[ Error diffusion ]--",
            "Fan dithering (7x3)",
            "Shiau dithering (5x3)",
            "Sierra dithering (5x3)",
            "Stucki dithering (5x5)",
            "Burkes dithering (5x3)",
            "Atkinson dithering (5x5)",
            "Two-row Sierra dithering (5x3)",
            "Floyd–Steinberg dithering (3x3)",
            "Jarvis-Judice-Ninke dithering (5x5)"});
            this.listDitherer.Location = new System.Drawing.Point(52, 0);
            this.listDitherer.Name = "listDitherer";
            this.listDitherer.Size = new System.Drawing.Size(188, 21);
            this.listDitherer.TabIndex = 6;
            this.listDitherer.SelectedIndexChanged += new System.EventHandler(this.ListDithererSelectedIndexChanged);
            // 
            // labelParallelTasks
            // 
            this.labelParallelTasks.Dock = System.Windows.Forms.DockStyle.Right;
            this.labelParallelTasks.Location = new System.Drawing.Point(240, 0);
            this.labelParallelTasks.Name = "labelParallelTasks";
            this.labelParallelTasks.Size = new System.Drawing.Size(47, 20);
            this.labelParallelTasks.TabIndex = 5;
            this.labelParallelTasks.Text = "Parallel:";
            this.labelParallelTasks.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // listParallel
            // 
            this.listParallel.Dock = System.Windows.Forms.DockStyle.Right;
            this.listParallel.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.listParallel.FormattingEnabled = true;
            this.listParallel.Items.AddRange(new object[] {
            "1",
            "2",
            "4",
            "8",
            "16",
            "32",
            "64"});
            this.listParallel.Location = new System.Drawing.Point(287, 0);
            this.listParallel.MinimumSize = new System.Drawing.Size(45, 0);
            this.listParallel.Name = "listParallel";
            this.listParallel.Size = new System.Drawing.Size(45, 21);
            this.listParallel.TabIndex = 7;
            this.listParallel.SelectedIndexChanged += new System.EventHandler(this.ListParallelSelectedIndexChanged);
            // 
            // labelDitherer
            // 
            this.labelDitherer.Dock = System.Windows.Forms.DockStyle.Left;
            this.labelDitherer.Location = new System.Drawing.Point(5, 0);
            this.labelDitherer.Name = "labelDitherer";
            this.labelDitherer.Size = new System.Drawing.Size(47, 20);
            this.labelDitherer.TabIndex = 0;
            this.labelDitherer.Text = "Ditherer:";
            this.labelDitherer.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // panelColorCache
            // 
            this.panelColorCache.Controls.Add(this.listColorCache);
            this.panelColorCache.Controls.Add(this.labelColorModel);
            this.panelColorCache.Controls.Add(this.listColorModel);
            this.panelColorCache.Controls.Add(this.labelColorCache);
            this.panelColorCache.Dock = System.Windows.Forms.DockStyle.Top;
            this.panelColorCache.Location = new System.Drawing.Point(0, 50);
            this.panelColorCache.Name = "panelColorCache";
            this.panelColorCache.Padding = new System.Windows.Forms.Padding(5, 0, 5, 5);
            this.panelColorCache.Size = new System.Drawing.Size(337, 25);
            this.panelColorCache.TabIndex = 11;
            // 
            // listColorCache
            // 
            this.listColorCache.Dock = System.Windows.Forms.DockStyle.Fill;
            this.listColorCache.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.listColorCache.FormattingEnabled = true;
            this.listColorCache.Items.AddRange(new object[] {
            "Euclidean distance",
            "Locality-sensitive hash",
            "Octree search"});
            this.listColorCache.Location = new System.Drawing.Point(72, 0);
            this.listColorCache.Name = "listColorCache";
            this.listColorCache.Size = new System.Drawing.Size(127, 21);
            this.listColorCache.TabIndex = 4;
            this.listColorCache.SelectedIndexChanged += new System.EventHandler(this.ListColorCacheSelectedIndexChanged);
            // 
            // labelColorModel
            // 
            this.labelColorModel.Dock = System.Windows.Forms.DockStyle.Right;
            this.labelColorModel.Location = new System.Drawing.Point(199, 0);
            this.labelColorModel.Name = "labelColorModel";
            this.labelColorModel.Size = new System.Drawing.Size(68, 20);
            this.labelColorModel.TabIndex = 5;
            this.labelColorModel.Text = "Color model:";
            this.labelColorModel.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // listColorModel
            // 
            this.listColorModel.Dock = System.Windows.Forms.DockStyle.Right;
            this.listColorModel.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.listColorModel.Enabled = false;
            this.listColorModel.FormattingEnabled = true;
            this.listColorModel.Items.AddRange(new object[] {
            "RGB"});
            this.listColorModel.Location = new System.Drawing.Point(267, 0);
            this.listColorModel.MinimumSize = new System.Drawing.Size(65, 0);
            this.listColorModel.Name = "listColorModel";
            this.listColorModel.Size = new System.Drawing.Size(65, 21);
            this.listColorModel.TabIndex = 5;
            this.listColorModel.SelectedIndexChanged += new System.EventHandler(this.ListColorModelSelectedIndexChanged);
            // 
            // labelColorCache
            // 
            this.labelColorCache.Dock = System.Windows.Forms.DockStyle.Left;
            this.labelColorCache.Location = new System.Drawing.Point(5, 0);
            this.labelColorCache.Name = "labelColorCache";
            this.labelColorCache.Size = new System.Drawing.Size(67, 20);
            this.labelColorCache.TabIndex = 0;
            this.labelColorCache.Text = "Color cache:";
            this.labelColorCache.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // panelMethod
            // 
            this.panelMethod.Controls.Add(this.listMethod);
            this.panelMethod.Controls.Add(this.label1);
            this.panelMethod.Controls.Add(this.listColors);
            this.panelMethod.Controls.Add(this.labelMethod);
            this.panelMethod.Dock = System.Windows.Forms.DockStyle.Top;
            this.panelMethod.Location = new System.Drawing.Point(0, 25);
            this.panelMethod.Name = "panelMethod";
            this.panelMethod.Padding = new System.Windows.Forms.Padding(5, 0, 5, 5);
            this.panelMethod.Size = new System.Drawing.Size(337, 25);
            this.panelMethod.TabIndex = 10;
            // 
            // listMethod
            // 
            this.listMethod.Dock = System.Windows.Forms.DockStyle.Fill;
            this.listMethod.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.listMethod.FormattingEnabled = true;
            this.listMethod.Items.AddRange(new object[] {
            "HSL distinct selection",
            "Uniform quantization",
            "Popularity algorithm",
            "Median cut algorithm",
            "Octree quantization",
            "Wu\'s color quantizer",
            "NeuQuant quantizer",
            "Optimal palette"});
            this.listMethod.Location = new System.Drawing.Point(93, 0);
            this.listMethod.Name = "listMethod";
            this.listMethod.Size = new System.Drawing.Size(152, 21);
            this.listMethod.TabIndex = 2;
            this.listMethod.SelectedIndexChanged += new System.EventHandler(this.ListMethodSelectedIndexChanged);
            // 
            // label1
            // 
            this.label1.Dock = System.Windows.Forms.DockStyle.Right;
            this.label1.Location = new System.Drawing.Point(245, 0);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(42, 20);
            this.label1.TabIndex = 12;
            this.label1.Text = " Colors:";
            this.label1.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // listColors
            // 
            this.listColors.Dock = System.Windows.Forms.DockStyle.Right;
            this.listColors.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.listColors.FormattingEnabled = true;
            this.listColors.Items.AddRange(new object[] {
            "2",
            "4",
            "8",
            "16",
            "32",
            "64",
            "128",
            "256"});
            this.listColors.Location = new System.Drawing.Point(287, 0);
            this.listColors.MinimumSize = new System.Drawing.Size(45, 0);
            this.listColors.Name = "listColors";
            this.listColors.Size = new System.Drawing.Size(45, 21);
            this.listColors.TabIndex = 3;
            this.listColors.SelectedIndexChanged += new System.EventHandler(this.ListColorsSelectedIndexChanged);
            // 
            // labelMethod
            // 
            this.labelMethod.Dock = System.Windows.Forms.DockStyle.Left;
            this.labelMethod.Location = new System.Drawing.Point(5, 0);
            this.labelMethod.Name = "labelMethod";
            this.labelMethod.Size = new System.Drawing.Size(88, 20);
            this.labelMethod.TabIndex = 8;
            this.labelMethod.Text = "Select a method:";
            this.labelMethod.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // panelTargetInfo
            // 
            this.panelTargetInfo.Controls.Add(this.editTargetInfo);
            this.panelTargetInfo.Dock = System.Windows.Forms.DockStyle.Top;
            this.panelTargetInfo.Location = new System.Drawing.Point(0, 0);
            this.panelTargetInfo.Name = "panelTargetInfo";
            this.panelTargetInfo.Padding = new System.Windows.Forms.Padding(4, 0, 0, 5);
            this.panelTargetInfo.Size = new System.Drawing.Size(337, 25);
            this.panelTargetInfo.TabIndex = 6;
            // 
            // editTargetInfo
            // 
            this.editTargetInfo.Dock = System.Windows.Forms.DockStyle.Top;
            this.editTargetInfo.Location = new System.Drawing.Point(4, 0);
            this.editTargetInfo.Name = "editTargetInfo";
            this.editTargetInfo.ReadOnly = true;
            this.editTargetInfo.Size = new System.Drawing.Size(333, 20);
            this.editTargetInfo.TabIndex = 4;
            this.editTargetInfo.TabStop = false;
            this.editTargetInfo.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
            // 
            // panelControls
            // 
            this.panelControls.Controls.Add(this.buttonUpdate);
            this.panelControls.Controls.Add(this.buttonBrowse);
            this.panelControls.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.panelControls.Location = new System.Drawing.Point(5, 520);
            this.panelControls.Name = "panelControls";
            this.panelControls.Size = new System.Drawing.Size(674, 39);
            this.panelControls.TabIndex = 16;
            // 
            // buttonUpdate
            // 
            this.buttonUpdate.Anchor = System.Windows.Forms.AnchorStyles.Right;
            this.buttonUpdate.Enabled = false;
            this.buttonUpdate.Location = new System.Drawing.Point(534, 3);
            this.buttonUpdate.Name = "buttonUpdate";
            this.buttonUpdate.Size = new System.Drawing.Size(140, 32);
            this.buttonUpdate.TabIndex = 9;
            this.buttonUpdate.Text = "Refresh";
            this.buttonUpdate.UseVisualStyleBackColor = true;
            this.buttonUpdate.Click += new System.EventHandler(this.ButtonUpdateClick);
            // 
            // buttonBrowse
            // 
            this.buttonBrowse.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.buttonBrowse.Location = new System.Drawing.Point(0, 3);
            this.buttonBrowse.Name = "buttonBrowse";
            this.buttonBrowse.Size = new System.Drawing.Size(531, 32);
            this.buttonBrowse.TabIndex = 8;
            this.buttonBrowse.Text = "Browse for a file image...";
            this.buttonBrowse.UseVisualStyleBackColor = true;
            this.buttonBrowse.Click += new System.EventHandler(this.ButtonBrowseClick);
            // 
            // MainForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(684, 564);
            this.Controls.Add(this.panelMain);
            this.Controls.Add(this.panelStatistics);
            this.Controls.Add(this.panelControls);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.Name = "MainForm";
            this.Padding = new System.Windows.Forms.Padding(5);
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "Simple palette quantizer";
            this.Load += new System.EventHandler(this.MainFormLoad);
            this.Resize += new System.EventHandler(this.MainFormResize);
            this.panelStatistics.ResumeLayout(false);
            this.splitContainerPngSizes.Panel1.ResumeLayout(false);
            this.splitContainerPngSizes.Panel1.PerformLayout();
            this.splitContainerPngSizes.Panel2.ResumeLayout(false);
            this.splitContainerPngSizes.Panel2.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.splitContainerPngSizes)).EndInit();
            this.splitContainerPngSizes.ResumeLayout(false);
            this.splitContainerGifSizes.Panel1.ResumeLayout(false);
            this.splitContainerGifSizes.Panel1.PerformLayout();
            this.splitContainerGifSizes.Panel2.ResumeLayout(false);
            this.splitContainerGifSizes.Panel2.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.splitContainerGifSizes)).EndInit();
            this.splitContainerGifSizes.ResumeLayout(false);
            this.panelMain.ResumeLayout(false);
            this.panelLeft.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.pictureSource)).EndInit();
            this.panelSource.ResumeLayout(false);
            this.panelSource.PerformLayout();
            this.panelDirectory.ResumeLayout(false);
            this.panelDirectory.PerformLayout();
            this.panelFilename.ResumeLayout(false);
            this.panelFilename.PerformLayout();
            this.panelSourceInfo.ResumeLayout(false);
            this.panelSourceInfo.PerformLayout();
            this.panelRight.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.pictureTarget)).EndInit();
            this.panelDithering.ResumeLayout(false);
            this.panelColorCache.ResumeLayout(false);
            this.panelMethod.ResumeLayout(false);
            this.panelTargetInfo.ResumeLayout(false);
            this.panelTargetInfo.PerformLayout();
            this.panelControls.ResumeLayout(false);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.OpenFileDialog dialogOpenFile;
        private System.Windows.Forms.Panel panelStatistics;
        private System.Windows.Forms.SplitContainer splitContainerPngSizes;
        private System.Windows.Forms.SplitContainer splitContainerGifSizes;
        private System.Windows.Forms.Panel panelMain;
        private System.Windows.Forms.Splitter splitterMain;
        private System.Windows.Forms.Panel panelLeft;
        private System.Windows.Forms.Panel panelRight;
        private System.Windows.Forms.Panel panelSourceInfo;
        private System.Windows.Forms.Panel panelTargetInfo;
        private System.Windows.Forms.TextBox editTargetInfo;
        private System.Windows.Forms.TextBox editSourceInfo;
        private System.Windows.Forms.TextBox editProjectedPngSize;
        private System.Windows.Forms.Label labelProjectedPngSize;
        private System.Windows.Forms.TextBox editProjectedGifSize;
        private System.Windows.Forms.Label labelProjectedGifSize;
        private System.Windows.Forms.Label labelNewPngSize;
        private System.Windows.Forms.Label labelNewGifSize;
        private System.Windows.Forms.TextBox editNewPngSize;
        private System.Windows.Forms.TextBox editNewGifSize;
        private System.Windows.Forms.Panel panelMethod;
        private System.Windows.Forms.Label labelMethod;
        private System.Windows.Forms.ComboBox listMethod;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.ComboBox listColors;
        private System.Windows.Forms.Panel panelFilename;
        private System.Windows.Forms.PictureBox pictureTarget;
        private System.Windows.Forms.Panel panelColorCache;
        private System.Windows.Forms.Label labelColorCache;
        private System.Windows.Forms.ComboBox listColorCache;
        private System.Windows.Forms.Label labelColorModel;
        private System.Windows.Forms.ComboBox listColorModel;
        private System.Windows.Forms.TextBox editFilename;
        private System.Windows.Forms.Label labelFilename;
        private System.Windows.Forms.Panel panelSource;
        private System.Windows.Forms.ComboBox listSource;
        private System.Windows.Forms.Label labelSource;
        private System.Windows.Forms.CheckBox checkShowError;
        private System.Windows.Forms.PictureBox pictureSource;
        private System.Windows.Forms.Panel panelDirectory;
        private System.Windows.Forms.TextBox editDirectory;
        private System.Windows.Forms.Label labelDirectory;
        private System.Windows.Forms.Panel panelDithering;
        private System.Windows.Forms.ComboBox listDitherer;
        private System.Windows.Forms.Label labelParallelTasks;
        private System.Windows.Forms.ComboBox listParallel;
        private System.Windows.Forms.Label labelDitherer;
        private System.Windows.Forms.Panel panelControls;
        private System.Windows.Forms.Button buttonUpdate;
        private System.Windows.Forms.Button buttonBrowse;
    }
}

