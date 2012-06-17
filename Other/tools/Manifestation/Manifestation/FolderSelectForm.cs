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
    public delegate void OnUpdatedFilelist(List<ManifestFile> Filelist);

    /// <summary>
    /// This form allows the user to create a virtual filesystem by typing the name of a folder
    /// and adding it to the name of a file, thus creating a path.
    /// </summary>
    public partial class FolderSelectForm : Form
    {
        private List<ManifestFile> m_ManifestFiles = new List<ManifestFile>();

        public event OnUpdatedFilelist UpdatedFileList;

        public FolderSelectForm(List<ManifestFile> ManifestFiles)
        {
            InitializeComponent();

            LstFiles.SelectionMode = SelectionMode.MultiExtended;

            m_ManifestFiles = ManifestFiles;

            foreach (ManifestFile MFile in m_ManifestFiles)
                LstFiles.Items.Add(MFile.VirtualPath);
        }


        /// <summary>
        /// User clicked the "Add selected files to folder" button.
        /// </summary>
        private void BtnAddFolder_Click(object sender, EventArgs e)
        {
            foreach (string SelectedFile in LstFiles.SelectedItems)
            {
                for (int i = 0; i < m_ManifestFiles.Count; i++)
                {
                    if (SelectedFile == m_ManifestFiles[i].VirtualPath)
                        m_ManifestFiles[i].VirtualPath = Path.Combine(TxtCurrentFolder.Text, m_ManifestFiles[i].VirtualPath);
                }
            }

            LstFiles.Items.Clear();

            foreach (ManifestFile MFile in m_ManifestFiles)
                LstFiles.Items.Add(MFile.VirtualPath);

            UpdatedFileList(m_ManifestFiles);
        }

        /// <summary>
        /// User clicked the "Replace" button.
        /// </summary>
        private void BtnReplace_Click(object sender, EventArgs e)
        {
            foreach (string SelectedFile in LstFiles.SelectedItems)
            {
                for (int i = 0; i < m_ManifestFiles.Count; i++)
                {
                    if (Path.GetFileName(SelectedFile) == Path.GetFileName(m_ManifestFiles[i].VirtualPath))
                    {
                        //Replace the current path of the selected file with the one in TxtCurrentFolder.Text!
                        m_ManifestFiles[i].VirtualPath = Path.Combine(TxtCurrentFolder.Text, 
                            Path.GetFileName(m_ManifestFiles[i].VirtualPath));
                    }
                }
            }

            LstFiles.Items.Clear();

            foreach (ManifestFile MFile in m_ManifestFiles)
                LstFiles.Items.Add(MFile.VirtualPath);

            UpdatedFileList(m_ManifestFiles);
        }
    }
}
