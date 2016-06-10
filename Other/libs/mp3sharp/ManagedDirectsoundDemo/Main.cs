using System;
using System.Drawing;
using System.Windows.Forms;
using System.IO;
using Microsoft.DirectX;
using Microsoft.DirectX.DirectSound;
using Buffer = Microsoft.DirectX.DirectSound.SecondaryBuffer;

using Mp3Sharp;

namespace Mp3Sharp
{

	/// <summary>
	/// This is a modified version of the "PlaySound" Managed DirectSound sample. 
	/// </summary>
	public class MainForm : Form
	{
		private System.ComponentModel.Container components = null;
		private Button btnSoundfile;
		private Label lblFilename;
		private Button btnPlay;
		private Button btnStop;
		private Button btnCancel;
        
		//private SecondaryBuffer ApplicationStreamedSound = null;
		private Device ApplicationDevice = null;
		private string PathSoundFile = string.Empty;
		private System.Windows.Forms.CheckBox cbLoopCheck;

		private StreamedSound ApplicationStreamedSound = null;

		public static int Main(string[] Args)
		{
			Application.Run(new MainForm());
			return 0;
		}
    
		protected override void Dispose( bool disposing )
		{
			if(disposing)
			{
				if (null != components)
				{
					components.Dispose();
				}
			}
			base.Dispose(disposing);	
		}
		public MainForm()
		{
			//
			// Required for Windows Form Designer support
			//
			InitializeComponent();
		}
		#region InitializeComponent code
		private void InitializeComponent()
		{
			this.btnSoundfile = new System.Windows.Forms.Button();
			this.lblFilename = new System.Windows.Forms.Label();
			this.btnPlay = new System.Windows.Forms.Button();
			this.btnStop = new System.Windows.Forms.Button();
			this.btnCancel = new System.Windows.Forms.Button();
			this.cbLoopCheck = new System.Windows.Forms.CheckBox();
			this.SuspendLayout();
			// 
			// btnSoundfile
			// 
			this.btnSoundfile.Location = new System.Drawing.Point(12, 13);
			this.btnSoundfile.Name = "btnSoundfile";
			this.btnSoundfile.Size = new System.Drawing.Size(83, 24);
			this.btnSoundfile.TabIndex = 0;
			this.btnSoundfile.Text = "Sound &file...";
			this.btnSoundfile.Click += new System.EventHandler(this.btnSoundfile_Click);
			// 
			// lblFilename
			// 
			this.lblFilename.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
			this.lblFilename.Location = new System.Drawing.Point(113, 13);
			this.lblFilename.Name = "lblFilename";
			this.lblFilename.Size = new System.Drawing.Size(414, 24);
			this.lblFilename.TabIndex = 1;
			this.lblFilename.Text = "No file loaded.";
			this.lblFilename.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
			// 
			// btnPlay
			// 
			this.btnPlay.Enabled = false;
			this.btnPlay.Location = new System.Drawing.Point(125, 55);
			this.btnPlay.Name = "btnPlay";
			this.btnPlay.Size = new System.Drawing.Size(90, 27);
			this.btnPlay.TabIndex = 3;
			this.btnPlay.Text = "&Play";
			this.btnPlay.Click += new System.EventHandler(this.btnPlay_Click);
			// 
			// btnStop
			// 
			this.btnStop.Enabled = false;
			this.btnStop.Location = new System.Drawing.Point(211, 55);
			this.btnStop.Name = "btnStop";
			this.btnStop.Size = new System.Drawing.Size(90, 27);
			this.btnStop.TabIndex = 4;
			this.btnStop.Text = "&Stop";
			this.btnStop.Click += new System.EventHandler(this.btnStop_Click);
			// 
			// btnCancel
			// 
			this.btnCancel.Location = new System.Drawing.Point(437, 55);
			this.btnCancel.Name = "btnCancel";
			this.btnCancel.Size = new System.Drawing.Size(90, 27);
			this.btnCancel.TabIndex = 5;
			this.btnCancel.Text = "E&xit";
			this.btnCancel.Click += new System.EventHandler(this.btnCancel_Click);
			// 
			// cbLoopCheck
			// 
			this.cbLoopCheck.Enabled = false;
			this.cbLoopCheck.Location = new System.Drawing.Point(11, 51);
			this.cbLoopCheck.Name = "cbLoopCheck";
			this.cbLoopCheck.Size = new System.Drawing.Size(104, 18);
			this.cbLoopCheck.TabIndex = 2;
			this.cbLoopCheck.Text = "&Loop sound";
			// 
			// MainForm
			// 
			this.AutoScaleBaseSize = new System.Drawing.Size(6, 15);
			this.ClientSize = new System.Drawing.Size(540, 88);
			this.Controls.Add(this.btnSoundfile);
			this.Controls.Add(this.lblFilename);
			this.Controls.Add(this.cbLoopCheck);
			this.Controls.Add(this.btnPlay);
			this.Controls.Add(this.btnStop);
			this.Controls.Add(this.btnCancel);
			this.Name = "MainForm";
			this.Text = "PlayMP3";
			this.Load += new System.EventHandler(this.MainForm_Load);
			this.ResumeLayout(false);

		}
		#endregion

		private void btnCancel_Click(object sender, System.EventArgs e)
		{
			Close();
		}

		private void btnStop_Click(object sender, System.EventArgs e)
		{
			if(null != ApplicationStreamedSound)
				ApplicationStreamedSound.Stop();
		}

		private void btnSoundfile_Click(object sender, System.EventArgs e)
		{
			OpenFileDialog ofd = new OpenFileDialog();

			if(string.Empty == PathSoundFile)
				PathSoundFile = DXUtil.SdkMediaPath;

			ofd.InitialDirectory = PathSoundFile;
			ofd.Filter=  "Mp3 files(*.mp3)|*.mp3";

			if( DialogResult.Cancel == ofd.ShowDialog() )
				return;
     
			if(LoadSoundFile(ofd.FileName))
			{
				PathSoundFile = Path.GetDirectoryName(ofd.FileName);
				lblFilename.Text = Path.GetFileName(ofd.FileName);
				EnablePlayUI(true);
			}
			else
			{
				lblFilename.Text = "No file loaded.";
				EnablePlayUI(false);
			}
		}

		private bool LoadSoundFile(string name)
		{
			try
			{
				ApplicationStreamedSound = new StreamedMp3Sound(ApplicationDevice, new Mp3Stream(name));
			}
			catch(SoundException)
			{
				return false;
			}
			return true;
		}

		private void EnablePlayUI(bool enable)
		{
			if (enable)
			{
				cbLoopCheck.Enabled = true;
				btnCancel.Enabled = true;
				btnPlay.Enabled = true;
				btnStop.Enabled = true;
			}
			else
			{
				cbLoopCheck.Enabled = false;
				btnCancel.Enabled = false;
				btnPlay.Enabled = false;
				btnStop.Enabled = false;
			}
		}

		private void MainForm_Load(object sender, System.EventArgs e)
		{
			ApplicationDevice = new Device();
			ApplicationDevice.SetCooperativeLevel(this, CooperativeLevel.Priority);
		}

		private void btnPlay_Click(object sender, System.EventArgs e)
		{
			if(null != ApplicationStreamedSound)
				if (cbLoopCheck.Checked) ApplicationStreamedSound.Loop(); else ApplicationStreamedSound.Play();
		}
	}
}