namespace PDPatcher
{
	partial class ConfigurationFrm
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
			this.ipAddrBox = new System.Windows.Forms.TextBox();
			this.LblIPAddress = new System.Windows.Forms.Label();
			this.resList = new System.Windows.Forms.ComboBox();
			this.LblResolution = new System.Windows.Forms.Label();
			this.langList = new System.Windows.Forms.ComboBox();
			this.LblLanguage = new System.Windows.Forms.Label();
			this.serverQueryLabel = new System.Windows.Forms.Label();
			this.LblMadeBy = new System.Windows.Forms.Label();
			this.BtnSave = new System.Windows.Forms.Button();
			this.SuspendLayout();
			// 
			// ipAddrBox
			// 
			this.ipAddrBox.Location = new System.Drawing.Point(509, 29);
			this.ipAddrBox.Name = "ipAddrBox";
			this.ipAddrBox.Size = new System.Drawing.Size(121, 20);
			this.ipAddrBox.TabIndex = 0;
			// 
			// LblIPAddress
			// 
			this.LblIPAddress.AutoSize = true;
			this.LblIPAddress.Location = new System.Drawing.Point(506, 13);
			this.LblIPAddress.Name = "LblIPAddress";
			this.LblIPAddress.Size = new System.Drawing.Size(61, 13);
			this.LblIPAddress.TabIndex = 1;
			this.LblIPAddress.Text = "IP Address:";
			// 
			// resList
			// 
			this.resList.FormattingEnabled = true;
			this.resList.Location = new System.Drawing.Point(509, 92);
			this.resList.Name = "resList";
			this.resList.Size = new System.Drawing.Size(121, 21);
			this.resList.TabIndex = 2;
			// 
			// LblResolution
			// 
			this.LblResolution.AutoSize = true;
			this.LblResolution.Location = new System.Drawing.Point(506, 76);
			this.LblResolution.Name = "LblResolution";
			this.LblResolution.Size = new System.Drawing.Size(60, 13);
			this.LblResolution.TabIndex = 1;
			this.LblResolution.Text = "Resolution:";
			// 
			// langList
			// 
			this.langList.FormattingEnabled = true;
			this.langList.Location = new System.Drawing.Point(509, 151);
			this.langList.Name = "langList";
			this.langList.Size = new System.Drawing.Size(121, 21);
			this.langList.TabIndex = 2;
			// 
			// LblLanguage
			// 
			this.LblLanguage.AutoSize = true;
			this.LblLanguage.Location = new System.Drawing.Point(507, 135);
			this.LblLanguage.Name = "LblLanguage";
			this.LblLanguage.Size = new System.Drawing.Size(58, 13);
			this.LblLanguage.TabIndex = 1;
			this.LblLanguage.Text = "Language:";
			// 
			// serverQueryLabel
			// 
			this.serverQueryLabel.AutoSize = true;
			this.serverQueryLabel.Location = new System.Drawing.Point(12, 13);
			this.serverQueryLabel.Name = "serverQueryLabel";
			this.serverQueryLabel.Size = new System.Drawing.Size(72, 13);
			this.serverQueryLabel.TabIndex = 1;
			this.serverQueryLabel.Text = "Server status:";
			// 
			// LblMadeBy
			// 
			this.LblMadeBy.AutoSize = true;
			this.LblMadeBy.Location = new System.Drawing.Point(12, 240);
			this.LblMadeBy.Name = "LblMadeBy";
			this.LblMadeBy.Size = new System.Drawing.Size(121, 13);
			this.LblMadeBy.TabIndex = 1;
			this.LblMadeBy.Text = "Code by: LetsRaceBwoi";
			// 
			// BtnSave
			// 
			this.BtnSave.Location = new System.Drawing.Point(509, 204);
			this.BtnSave.Name = "BtnSave";
			this.BtnSave.Size = new System.Drawing.Size(121, 23);
			this.BtnSave.TabIndex = 3;
			this.BtnSave.Text = "Save";
			this.BtnSave.UseVisualStyleBackColor = true;
			this.BtnSave.Click += new System.EventHandler(this.BtnSave_Click);
			// 
			// ConfigurationFrm
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.ClientSize = new System.Drawing.Size(642, 262);
			this.Controls.Add(this.BtnSave);
			this.Controls.Add(this.langList);
			this.Controls.Add(this.resList);
			this.Controls.Add(this.LblMadeBy);
			this.Controls.Add(this.serverQueryLabel);
			this.Controls.Add(this.LblLanguage);
			this.Controls.Add(this.LblResolution);
			this.Controls.Add(this.LblIPAddress);
			this.Controls.Add(this.ipAddrBox);
			this.Name = "ConfigurationFrm";
			this.Text = "ConfigurationFrm";
			this.Load += new System.EventHandler(this.ConfigurationFrm_Load);
			this.ResumeLayout(false);
			this.PerformLayout();

		}

		#endregion

		private System.Windows.Forms.TextBox ipAddrBox;
		private System.Windows.Forms.Label LblIPAddress;
		private System.Windows.Forms.ComboBox resList;
		private System.Windows.Forms.Label LblResolution;
		private System.Windows.Forms.ComboBox langList;
		private System.Windows.Forms.Label LblLanguage;
		private System.Windows.Forms.Label serverQueryLabel;
		private System.Windows.Forms.Label LblMadeBy;
		private System.Windows.Forms.Button BtnSave;
	}
}