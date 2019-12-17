namespace FSO.Debug.Controls
{
    partial class VMRoutineDisplay
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
            this.grid = new System.Windows.Forms.DataGridView();
            this.Margin = new System.Windows.Forms.DataGridViewImageColumn();
            this.Index = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.Opcode = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.Operand = new System.Windows.Forms.DataGridViewTextBoxColumn();
            ((System.ComponentModel.ISupportInitialize)(this.grid)).BeginInit();
            this.SuspendLayout();
            // 
            // grid
            // 
            this.grid.AllowUserToAddRows = false;
            this.grid.AllowUserToDeleteRows = false;
            this.grid.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.grid.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
            this.Margin,
            this.Index,
            this.Opcode,
            this.Operand});
            this.grid.Dock = System.Windows.Forms.DockStyle.Fill;
            this.grid.Location = new System.Drawing.Point(0, 0);
            this.grid.Name = "grid";
            this.grid.ReadOnly = true;
            this.grid.ShowCellErrors = false;
            this.grid.ShowRowErrors = false;
            this.grid.Size = new System.Drawing.Size(569, 290);
            this.grid.TabIndex = 0;
            // 
            // Margin
            // 
            this.Margin.DataPropertyName = "Margin";
            this.Margin.HeaderText = "";
            this.Margin.Name = "Margin";
            this.Margin.ReadOnly = true;
            this.Margin.Width = 25;
            // 
            // Index
            // 
            this.Index.DataPropertyName = "Index";
            this.Index.HeaderText = "#";
            this.Index.Name = "Index";
            this.Index.ReadOnly = true;
            this.Index.Width = 25;
            // 
            // Opcode
            // 
            this.Opcode.DataPropertyName = "Opcode";
            this.Opcode.HeaderText = "Opcode";
            this.Opcode.Name = "Opcode";
            this.Opcode.ReadOnly = true;
            // 
            // Operand
            // 
            this.Operand.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.Fill;
            this.Operand.DataPropertyName = "Operand";
            this.Operand.HeaderText = "Operand";
            this.Operand.Name = "Operand";
            this.Operand.ReadOnly = true;
            // 
            // VMRoutineDisplay
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.grid);
            this.Name = "VMRoutineDisplay";
            this.Size = new System.Drawing.Size(569, 290);
            ((System.ComponentModel.ISupportInitialize)(this.grid)).EndInit();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.DataGridView grid;
        private new System.Windows.Forms.DataGridViewImageColumn Margin;
        private System.Windows.Forms.DataGridViewTextBoxColumn Index;
        private System.Windows.Forms.DataGridViewTextBoxColumn Opcode;
        private System.Windows.Forms.DataGridViewTextBoxColumn Operand;
    }
}
