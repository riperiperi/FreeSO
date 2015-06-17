using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Text;
using System.Windows.Forms;
using System.Net;
using System.Diagnostics;

namespace PDPatcher
{
	public partial class ConfigurationFrm : Form
	{
		private Timer timer1 = new Timer();

		public ConfigurationFrm()
		{
			InitializeComponent();

			timer1.Start();
			string[] lines = File.ReadAllLines(GlobalSettings.Default.ClientPath + @"Project Dollhouse Client.exe.config");
			ipAddrBox.Text = lines[73].Replace("                <value>", "").Replace("</value>", "");
			resList.Text = lines[22].Replace("                <value>", "").Replace("</value>", "") + "x" + lines[25].Replace("                <value>", "").Replace("</value>", "");
			langList.Text = lines[13].Replace("                <value>", "").Replace("</value>", "");
			getStream();
		}

		/// <summary>
		/// Get the server status
		/// </summary>
		private void getStream()
		{
			try
			{
				WebRequest wrGETURL;
				wrGETURL = WebRequest.Create("http://" + ipAddrBox.Text + ":8888/city");
				Stream objStream;
				objStream = wrGETURL.GetResponse().GetResponseStream();
				StreamReader objReader = new StreamReader(objStream);
				serverQueryLabel.Text = objReader.ReadToEnd().Replace("<b>", "").Replace("</b>", "").Replace("</br>", Environment.NewLine).Replace(@"<a href=""http://nancyfx.org"">", "").Replace("</a>", "");
			}
			catch
			{
				serverQueryLabel.Text = "Error getting server status." + Environment.NewLine + "Does the server exist?";

			}
			/*if (serverQueryLabel.Text.Contains("City: East Jerome"))
			{
				System.Resources.ResourceManager resources = new System.Resources.ResourceManager(typeof(launcherForm));
				this.BackgroundImage = new Bitmap(pdlauncher_LRB.Properties.Resources.ej);

			}
			else
			{
				System.Resources.ResourceManager resources = new System.Resources.ResourceManager(typeof(launcherForm));
				this.BackgroundImage = new Bitmap(pdlauncher_LRB.Properties.Resources.screenshottest);
			}*/
		}

		/// <summary>
		/// Set the program configuration
		/// </summary>
		private void setStream()
		{
			try
			{
				string[] lines = File.ReadAllLines(GlobalSettings.Default.ClientPath + @"Project Dollhouse Client.exe.config");
				lines[73] = "                <value>" + ipAddrBox.Text + "</value>";
				lines[22] = "                <value>" + resList.Text.Remove(resList.Text.IndexOf("x"), resList.Text.Length - resList.Text.IndexOf("x")) + "</value>";
				lines[25] = "                <value>" + resList.Text.Remove(0, resList.Text.Length - resList.Text.IndexOf("x")).Replace("x", "") + "</value>";
				lines[13] = "                <value>" + langList.Text + "</value>";
				File.WriteAllLines(GlobalSettings.Default.ClientPath + @"Project Dollhouse Client.exe.config", lines);
				try
				{
					WebRequest wrGETURL;
					wrGETURL = WebRequest.Create("http://" + ipAddrBox.Text + ":8888/city");
					Stream objStream;
					objStream = wrGETURL.GetResponse().GetResponseStream();
					StreamReader objReader = new StreamReader(objStream);
					serverQueryLabel.Text = objReader.ReadToEnd().Replace("<b>", "").Replace("</b>", "").Replace("</br>", Environment.NewLine).Replace(@"<a href=""http://nancyfx.org"">", "").Replace("</a>", "");
				}
				catch
				{
					serverQueryLabel.Text = "Error getting server status." + Environment.NewLine + "Does the server exist?";
				}
			}
			catch
			{
				MessageBox.Show("There was a problem doing that! Do you have the right path?");
			}
		}

		private void BtnSave_Click(object sender, EventArgs e)
		{
			setStream();

			if (File.Exists(GlobalSettings.Default.ClientPath + "Project Dollhouse Client.exe"))
				Process.Start(GlobalSettings.Default.ClientPath + "Project Dollhouse Client.exe");
		}

		private void ConfigurationFrm_Load(object sender, EventArgs e)
		{
		}
	}
}
