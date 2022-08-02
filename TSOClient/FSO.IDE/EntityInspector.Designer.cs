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
            this.EntityView = new System.Windows.Forms.ListView();
            this.IDColumn = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.NameHeader = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.MTLeadHeader = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.ContainerHeader = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.ContSlotHeader = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.RefreshButton = new System.Windows.Forms.Button();
            this.TracerButton = new System.Windows.Forms.Button();
            this.DeleteButton = new System.Windows.Forms.Button();
            this.OpenResource = new System.Windows.Forms.Button();
            this.SuspendLayout();
            // 
            // EntityView
            // 
            this.EntityView.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.EntityView.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
            this.IDColumn,
            this.NameHeader,
            this.MTLeadHeader,
            this.ContainerHeader,
            this.ContSlotHeader});
            this.EntityView.FullRowSelect = true;
            this.EntityView.HideSelection = false;
            this.EntityView.Location = new System.Drawing.Point(3, 3);
            this.EntityView.Name = "EntityView";
            this.EntityView.Size = new System.Drawing.Size(615, 452);
            this.EntityView.TabIndex = 0;
            this.EntityView.UseCompatibleStateImageBehavior = false;
            this.EntityView.View = System.Windows.Forms.View.Details;
            this.EntityView.DoubleClick += new System.EventHandler(this.EntityView_DoubleClick);
            // 
            // IDColumn
            // 
            this.IDColumn.Text = "ID";
            // 
            // NameHeader
            // 
            this.NameHeader.Text = "Name";
            this.NameHeader.Width = 270;
            // 
            // MTLeadHeader
            // 
            this.MTLeadHeader.Text = "Multitile Lead";
            this.MTLeadHeader.Width = 83;
            // 
            // ContainerHeader
            // 
            this.ContainerHeader.Text = "Container";
            // 
            // ContSlotHeader
            // 
            this.ContSlotHeader.Text = "Slot #";
            this.ContSlotHeader.Width = 45;
            // 
            // RefreshButton
            // 
            this.RefreshButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.RefreshButton.Location = new System.Drawing.Point(624, 3);
            this.RefreshButton.Name = "RefreshButton";
            this.RefreshButton.Size = new System.Drawing.Size(103, 23);
            this.RefreshButton.TabIndex = 1;
            this.RefreshButton.Text = "Refresh";
            this.RefreshButton.UseVisualStyleBackColor = true;
            this.RefreshButton.Click += new System.EventHandler(this.RefreshButton_Click);
            // 
            // TracerButton
            // 
            this.TracerButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.TracerButton.Location = new System.Drawing.Point(624, 432);
            this.TracerButton.Name = "TracerButton";
            this.TracerButton.Size = new System.Drawing.Size(103, 23);
            this.TracerButton.TabIndex = 2;
            this.TracerButton.Text = "Open Tracer";
            this.TracerButton.UseVisualStyleBackColor = true;
            this.TracerButton.Click += new System.EventHandler(this.EntityView_DoubleClick);
            // 
            // DeleteButton
            // 
            this.DeleteButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.DeleteButton.Location = new System.Drawing.Point(624, 374);
            this.DeleteButton.Name = "DeleteButton";
            this.DeleteButton.Size = new System.Drawing.Size(103, 23);
            this.DeleteButton.TabIndex = 3;
            this.DeleteButton.Text = "Delete";
            this.DeleteButton.UseVisualStyleBackColor = true;
            this.DeleteButton.Click += new System.EventHandler(this.DeleteButton_Click);
            // 
            // OpenResource
            // 
            this.OpenResource.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.OpenResource.Location = new System.Drawing.Point(624, 403);
            this.OpenResource.Name = "OpenResource";
            this.OpenResource.Size = new System.Drawing.Size(103, 23);
            this.OpenResource.TabIndex = 4;
            this.OpenResource.Text = "Open Resource";
            this.OpenResource.UseVisualStyleBackColor = true;
            this.OpenResource.Click += new System.EventHandler(this.OpenResource_Click);
            // 
            // EntityInspector
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.OpenResource);
            this.Controls.Add(this.DeleteButton);
            this.Controls.Add(this.TracerButton);
            this.Controls.Add(this.RefreshButton);
            this.Controls.Add(this.EntityView);
            this.Name = "EntityInspector";
            this.Size = new System.Drawing.Size(730, 458);
            this.ResumeLayout(false);

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
    }
}
