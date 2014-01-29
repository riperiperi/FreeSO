using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using System.IO;

namespace Manifestation
{
    public partial class Form1 : Form
    {
        private bool m_IsManifestSaved = false;
        private List<PatchFile> m_PatchFiles = new List<PatchFile>();

        public Form1()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Creates a new manifest.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void newManifestToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (!m_IsManifestSaved && LstFiles.Items.Count > 0)
            {
                DialogResult Result = MessageBox.Show("Do you want to save the current manifest?", "Save", 
                    MessageBoxButtons.YesNo);
                
                switch (Result)
                {
                    case DialogResult.Yes:
                        SaveManifest();
                        this.Text = this.Text.Replace("*", "");
                        break;
                    case DialogResult.No:
                        break;
                }
            }

            m_PatchFiles.Clear();
            LstFiles.Items.Clear();
        }

        /// <summary>
        /// Saves a manifest to disk.
        /// </summary>
        private void SaveManifest()
        {
            SaveFileDialog SFDialog = new SaveFileDialog();
            SFDialog.AddExtension = true;
            SFDialog.AutoUpgradeEnabled = true;
            SFDialog.Filter = "*.manifest|*.manifest";
            SFDialog.Title = "Save Manifest...";

            if (SFDialog.ShowDialog() == DialogResult.OK)
            {
                ManifestFile ManFile = new ManifestFile(SFDialog.FileName,
                    NumMajor.Value.ToString() + "." + NumMinor.Value.ToString() + "." + NumPatch.Value.ToString(),
                    m_PatchFiles);
            }

            m_IsManifestSaved = true;
        }

        /// <summary>
        /// User added/updated the base URL of the files in the manifest.
        /// </summary>
        private void BtnUpdateURL_Click(object sender, EventArgs e)
        {
            foreach (PatchFile PFile in m_PatchFiles)
                PFile.URL = TxtBaseURL.Text + PFile.Address.Replace("\\", "/");
        }

        /// <summary>
        /// User wanted to add a folder to the current manifest.
        /// </summary>
        private void addFileToolStripMenuItem_Click(object sender, EventArgs e)
        {
            FolderBrowserDialog FBDiag = new FolderBrowserDialog();
            FBDiag.Description = "Add a folder to the manifest.";

            if (FBDiag.ShowDialog() == DialogResult.OK)
            {
                this.Text += "*";

                DirectoryInfo RootFolder = new DirectoryInfo(FBDiag.SelectedPath);

                //Find all files in the selected folder, including child folders.
                foreach (string FilePath in Directory.GetFiles(FBDiag.SelectedPath, "*.*", SearchOption.AllDirectories))
                {
                    string FilenameWithFolder = "";

                    if (Path.GetDirectoryName(FilePath) == FBDiag.SelectedPath)
                        FilenameWithFolder = Path.GetFileName(FilePath);
                    else
                    {
                        FilenameWithFolder = GetFilePath(RootFolder.Name, FilePath);
                    }

                    LstFiles.Items.Add(FilenameWithFolder);
                    m_PatchFiles.Add(new PatchFile()
                    {
                        Address = FilenameWithFolder,
                        FileHash = PatchFile.CalculateHash(FilePath)
                    });
                }
            }
        }

        /// <summary>
        /// Gets a file's path including all folders including all folders after and
        /// including the selected root folder.
        /// </summary>
        /// <param name="RootFolderName">The name of the root folder.</param>
        /// <param name="FilePath">The full path of the file.</param>
        private string GetFilePath(string RootFolderName, string FilePath)
        {
            int RootPos = (FilePath.IndexOf(RootFolderName) + RootFolderName.Length) + 1;
            return FilePath.Substring(RootPos, (FilePath.Length - RootPos));
        }

        private void saveManifestToolStripMenuItem_Click(object sender, EventArgs e)
        {
            SaveManifest();
            m_IsManifestSaved = true;
            this.Text = this.Text.Replace("*", "");
        }

        private void loadManifestToolStripMenuItem_Click(object sender, EventArgs e)
        {
            OpenFileDialog OFDiag = new OpenFileDialog();
            OFDiag.AutoUpgradeEnabled = true;
            OFDiag.CheckPathExists = true;
            OFDiag.Title = "Open Manifest";

            if (OFDiag.ShowDialog() == DialogResult.OK)
            {
                ManifestFile Manifest = new ManifestFile(File.Open(OFDiag.FileName, FileMode.Open));
                m_PatchFiles = Manifest.PatchFiles;

                foreach (PatchFile PFile in m_PatchFiles)
                    LstFiles.Items.Add(PFile.Address);
            }
        }

        private void exitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Application.Exit();
        }

        private void usageToolStripMenuItem_Click(object sender, EventArgs e)
        {
            MessageBox.Show("Add a folder with files to create a manifest.\n" +
                "If the manifest is to reside on a server, add a base URL and click Update URLs.\n");
        }

        private void aboutToolStripMenuItem_Click(object sender, EventArgs e)
        {
            MessageBox.Show("Manifestation v. 1.0\n" +
                "by Mats \"Afr0\" Vederhus");
        }
    }
}
