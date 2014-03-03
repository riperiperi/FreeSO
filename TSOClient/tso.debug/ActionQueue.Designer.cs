namespace tso.debug
{
    partial class ActionQueue
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
            this.components = new System.ComponentModel.Container();
            this.objNameLabel = new System.Windows.Forms.Label();
            this.actionView = new System.Windows.Forms.ListView();
            this.interactionUpdateTimer = new System.Windows.Forms.Timer(this.components);
            this.SuspendLayout();
            // 
            // objNameLabel
            // 
            this.objNameLabel.AutoSize = true;
            this.objNameLabel.Location = new System.Drawing.Point(6, 10);
            this.objNameLabel.Name = "objNameLabel";
            this.objNameLabel.Size = new System.Drawing.Size(74, 13);
            this.objNameLabel.TabIndex = 0;
            this.objNameLabel.Text = "Active Object:";
            // 
            // actionView
            // 
            this.actionView.Location = new System.Drawing.Point(12, 36);
            this.actionView.Name = "actionView";
            this.actionView.Size = new System.Drawing.Size(339, 73);
            this.actionView.TabIndex = 1;
            this.actionView.UseCompatibleStateImageBehavior = false;
            // 
            // interactionUpdateTimer
            // 
            this.interactionUpdateTimer.Enabled = true;
            this.interactionUpdateTimer.Interval = 16;
            this.interactionUpdateTimer.Tick += new System.EventHandler(this.interactionUpdateTimer_Tick);
            // 
            // ActionQueue
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(363, 123);
            this.Controls.Add(this.actionView);
            this.Controls.Add(this.objNameLabel);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedToolWindow;
            this.Name = "ActionQueue";
            this.Text = "Action Queue";
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Label objNameLabel;
        private System.Windows.Forms.ListView actionView;
        private System.Windows.Forms.Timer interactionUpdateTimer;

    }
}