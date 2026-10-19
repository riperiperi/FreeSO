namespace FSO.IDE
{
    partial class EntityInspector
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
            EntityView = new ListView();
            IDColumn = new ColumnHeader();
            NameHeader = new ColumnHeader();
            MTLeadHeader = new ColumnHeader();
            ContainerHeader = new ColumnHeader();
            ContSlotHeader = new ColumnHeader();
            RefreshButton = new Button();
            TracerButton = new Button();
            DeleteButton = new Button();
            OpenResource = new Button();
            panel1 = new Panel();
            panel1.SuspendLayout();
            SuspendLayout();
            // 
            // EntityView
            // 
            EntityView.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            EntityView.Columns.AddRange(new ColumnHeader[] { IDColumn, NameHeader, MTLeadHeader, ContainerHeader, ContSlotHeader });
            EntityView.FullRowSelect = true;
            EntityView.Location = new Point(3, 3);
            EntityView.Name = "EntityView";
            EntityView.Size = new Size(615, 452);
            EntityView.TabIndex = 0;
            EntityView.UseCompatibleStateImageBehavior = false;
            EntityView.View = View.Details;
            EntityView.DoubleClick += EntityView_DoubleClick;
            // 
            // IDColumn
            // 
            IDColumn.Text = "ID";
            // 
            // NameHeader
            // 
            NameHeader.Text = "Name";
            NameHeader.Width = 270;
            // 
            // MTLeadHeader
            // 
            MTLeadHeader.Text = "Multitile Lead";
            MTLeadHeader.Width = 83;
            // 
            // ContainerHeader
            // 
            ContainerHeader.Text = "Container";
            // 
            // ContSlotHeader
            // 
            ContSlotHeader.Text = "Slot #";
            ContSlotHeader.Width = 45;
            // 
            // RefreshButton
            // 
            RefreshButton.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            RefreshButton.Location = new Point(624, 3);
            RefreshButton.Name = "RefreshButton";
            RefreshButton.Size = new Size(103, 23);
            RefreshButton.TabIndex = 1;
            RefreshButton.Text = "Refresh";
            RefreshButton.UseVisualStyleBackColor = true;
            RefreshButton.Click += RefreshButton_Click;
            // 
            // TracerButton
            // 
            TracerButton.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
            TracerButton.Location = new Point(624, 432);
            TracerButton.Name = "TracerButton";
            TracerButton.Size = new Size(103, 23);
            TracerButton.TabIndex = 2;
            TracerButton.Text = "Open Tracer";
            TracerButton.UseVisualStyleBackColor = true;
            TracerButton.Click += EntityView_DoubleClick;
            // 
            // DeleteButton
            // 
            DeleteButton.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
            DeleteButton.Location = new Point(624, 374);
            DeleteButton.Name = "DeleteButton";
            DeleteButton.Size = new Size(103, 23);
            DeleteButton.TabIndex = 3;
            DeleteButton.Text = "Delete";
            DeleteButton.UseVisualStyleBackColor = true;
            DeleteButton.Click += DeleteButton_Click;
            // 
            // OpenResource
            // 
            OpenResource.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
            OpenResource.Location = new Point(624, 403);
            OpenResource.Name = "OpenResource";
            OpenResource.Size = new Size(103, 23);
            OpenResource.TabIndex = 4;
            OpenResource.Text = "Open Resource";
            OpenResource.UseVisualStyleBackColor = true;
            OpenResource.Click += OpenResource_Click;
            // 
            // panel1
            // 
            panel1.Controls.Add(EntityView);
            panel1.Controls.Add(OpenResource);
            panel1.Controls.Add(RefreshButton);
            panel1.Controls.Add(DeleteButton);
            panel1.Controls.Add(TracerButton);
            panel1.Dock = DockStyle.Fill;
            panel1.Location = new Point(0, 0);
            panel1.Margin = new Padding(0);
            panel1.Name = "panel1";
            panel1.Size = new Size(730, 458);
            panel1.TabIndex = 5;
            // 
            // EntityInspector
            // 
            AutoScaleDimensions = new SizeF(96F, 96F);
            AutoScaleMode = AutoScaleMode.Dpi;
            Controls.Add(panel1);
            Name = "EntityInspector";
            Size = new Size(730, 458);
            panel1.ResumeLayout(false);
            ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.ListView EntityView;
        private System.Windows.Forms.ColumnHeader IDColumn;
        private System.Windows.Forms.ColumnHeader NameHeader;
        private System.Windows.Forms.ColumnHeader MTLeadHeader;
        private System.Windows.Forms.ColumnHeader ContainerHeader;
        private System.Windows.Forms.ColumnHeader ContSlotHeader;
        private System.Windows.Forms.Button RefreshButton;
        private System.Windows.Forms.Button TracerButton;
        private System.Windows.Forms.Button DeleteButton;
        private System.Windows.Forms.Button OpenResource;
        private Panel panel1;
    }
}
