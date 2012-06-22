/*The contents of this file are subject to the Mozilla Public License Version 1.1
(the "License"); you may not use this file except in compliance with the
License. You may obtain a copy of the License at http://www.mozilla.org/MPL/

Software distributed under the License is distributed on an "AS IS" basis,
WITHOUT WARRANTY OF ANY KIND, either express or implied. See the License for
the specific language governing rights and limitations under the License.

The Original Code is Form1.cs.

The Initial Developer of the Original Code is
Mats 'Afr0' Vederhus. All Rights Reserved.

Contributor(s): ______________________________________.
*/

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using System.IO;
using System.Security.Cryptography;
using WOSI.Utilities;

namespace Manifestation
{
    public partial class Form1 : Form
    {
        private string m_Parent, m_Child = "";
        private List<ManifestFile> m_ManifestFiles = new List<ManifestFile>();

        private FolderSelectForm m_FSelectForm;

        //Is this a new manifest? If an already created manifest is opened, this will be set to false.
        private bool m_IsNewManifest = true;

        public Form1()
        {
            InitializeComponent();
        }

        private string ReadASCII(ref BinaryReader Reader)
        {
            char[] Buffer = new char[1];
            string Str = "";

            while (true)
            {
                Reader.Read(Buffer, 0, 1);
                Str += Buffer[0];

                if (Buffer[0] == '\r')
                {
                    //'\n'
                    Reader.ReadByte();
                    break;
                }
            }

            return Str;
        }

        /// <summary>
        /// User clicked on the "Add files to manifest..." menuitem.
        /// </summary>
        private void addFilesToolStripMenuItem_Click(object sender, EventArgs e)
        {
            OpenFileDialog OFDialog = new OpenFileDialog();
            OFDialog.AddExtension = true;
            OFDialog.Filter = "All files (*.*)|*.*";
            OFDialog.Multiselect = true;

            if (OFDialog.ShowDialog() == DialogResult.OK)
            {
                foreach (string FileName in OFDialog.FileNames)
                {
                    LstFiles.Items.Add(Path.GetFileName(FileName));
                    m_ManifestFiles.Add(new ManifestFile(Path.GetFileName(FileName), FileName));
                }
            }
        }

        /// <summary>
        /// User clicked on the "Open manifest..." menuitem.
        /// </summary>
        private void openManifestToolStripMenuItem_Click(object sender, EventArgs e)
        {
            OpenFileDialog OFDialog = new OpenFileDialog();
            OFDialog.AddExtension = true;
            OFDialog.Filter = "Manifest (*.manifest)|*.manifest";

            if (OFDialog.ShowDialog() == DialogResult.OK)
            {
                m_ManifestFiles.Clear();
                LstFiles.Items.Clear();

                BinaryReader Reader = new BinaryReader(File.Open(OFDialog.FileName, FileMode.Open), Encoding.ASCII);
                m_Parent = ReadASCII(ref Reader).Replace("Parent=", "").Replace("\"", "");
                m_Child = ReadASCII(ref Reader).Replace("Child=", "").Replace("\"", "");

                TxtVersion.Text = Path.GetFileName(OFDialog.FileName).Replace(".manifest", "");
                LblParent.Text = "Manifest's parent: " + m_Parent;
                LblChild.Text = "Manifest's child: " + m_Child;

                int NumFiles = int.Parse(ReadASCII(ref Reader).Replace("NumFiles=", ""));

                for (int i = 0; i < NumFiles; i++)
                {
                    //Separate the checksum from the filename.
                    string[] SplitFilename = Reader.ReadString().Split(new string[] { " MD5: " }, StringSplitOptions.RemoveEmptyEntries);
                    string Filename = SplitFilename[0];
                    string Checksum = SplitFilename[1].TrimEnd(new char[] {'\r', '\n'});

                    m_ManifestFiles.Add(new ManifestFile(Filename, Filename, Checksum));
                    LstFiles.Items.Add(Filename);
                }

                Reader.Close();

                //An old manifest has been opened, so set to false.
                m_IsNewManifest = false;
            }
        }

        /// <summary>
        /// User clicked on the "Save manifest..." menuitem.
        /// </summary>
        private void saveManifestToolStripMenuItem_Click(object sender, EventArgs e)
        {
            SaveFileDialog SFDialog = new SaveFileDialog();
            SFDialog.AddExtension = true;
            SFDialog.Filter = "Manifest (*.manifest)|*.manifest";
            
            if (SFDialog.ShowDialog() == DialogResult.OK)
            {
                BinaryWriter Writer = new BinaryWriter(File.Create(SFDialog.FileName));
                Writer.Write(Encoding.ASCII.GetBytes("Parent=\"" + m_Parent + "\"" + "\r\n"));
                Writer.Write(Encoding.ASCII.GetBytes("Child=\"" + m_Child + "\"" + "\r\n"));
                Writer.Write(Encoding.ASCII.GetBytes("NumFiles=" + LstFiles.Items.Count + "\r\n"));

                foreach (ManifestFile MFile in m_ManifestFiles)
                {
                    if (m_IsNewManifest)
                    {
                        BinaryReader Reader = new BinaryReader(File.Open(MFile.RealPath, FileMode.Open));
                        byte[] FData = Reader.ReadBytes((int)Reader.BaseStream.Length);
                        Reader.Close();

                        if (!MFile.RealPath.Contains(".log") || !MFile.RealPath.Contains(".txt") ||
                            !MFile.RealPath.Contains(".h") || !MFile.RealPath.Contains(".xml"))
                        {
                            MD5CryptoServiceProvider MD5Crypto = new MD5CryptoServiceProvider();
                            byte[] Checksum = MD5Crypto.ComputeHash(FData);

                            Writer.Write(MFile.VirtualPath + " MD5: " + Encoding.ASCII.GetString(Checksum) + "\r\n");
                        }
                        else
                        {
                            Writer.Write(MFile.VirtualPath + " MD5: " +
                                CryptoUtils.CreateASCIIMD5Hash(Encoding.ASCII.GetString(FData)) + "\r\n");
                        }
                    }
                    //Manifest was created previously, which means the checksums were loaded when the
                    //manifest was opened.
                    else
                    {
                        if (!MFile.VirtualPath.Contains(".log") || !MFile.VirtualPath.Contains(".txt") ||
                            !MFile.VirtualPath.Contains(".h") || !MFile.VirtualPath.Contains(".xml"))
                        {
                            byte[] ASCIIBytes = Encoding.ASCII.GetBytes(MFile.Checksum);
                            Writer.Write(MFile.VirtualPath + " MD5: " + Encoding.ASCII.GetString(ASCIIBytes) + "\r\n");
                        }
                        else
                            Writer.Write(MFile.VirtualPath + "MD5: " + CryptoUtils.CreateASCIIMD5Hash(MFile.Checksum) + "\r\n");
                    }
                }

                Writer.Close();
            }
        }

        /// <summary>
        /// User clicked on the "Add parent..." menuitem.
        /// </summary>
        private void addParentToolStripMenuItem_Click(object sender, EventArgs e)
        {
            OpenFileDialog OFDialog = new OpenFileDialog();
            OFDialog.AddExtension = true;
            OFDialog.Filter = "Manifest (*.manifest)|*.manifest";

            if (OFDialog.ShowDialog() == DialogResult.OK)
            {
                //Manifests are expected to live inside the same directory
                //on a live server, so the path to a manifest isn't actually stored.
                m_Parent = Path.GetFileName(OFDialog.FileName);
                LblParent.Text = "Manifest's parent: " + m_Parent;
            }
        }

        /// <summary>
        /// User clicked on the "Add child..." menuitem.
        /// </summary>
        private void addChildToolStripMenuItem_Click(object sender, EventArgs e)
        {
            OpenFileDialog OFDialog = new OpenFileDialog();
            OFDialog.AddExtension = true;
            OFDialog.Filter = "Manifest (*.manifest)|*.manifest";

            if (OFDialog.ShowDialog() == DialogResult.OK)
            {
                //Manifests are expected to live inside the same directory
                //on a live server, so the path to a manifest isn't actually stored.
                m_Child = Path.GetFileName(OFDialog.FileName);
                LblChild.Text = "Manifest's child: " + m_Child;
            }
        }

        /// <summary>
        /// User clicked on a file in the LstFiles listbox.
        /// </summary>
        private void LstFiles_SelectedIndexChanged(object sender, EventArgs e)
        {
            m_FSelectForm = new FolderSelectForm(m_ManifestFiles);
            m_FSelectForm.UpdatedFileList += new OnUpdatedFilelist(m_FSelectForm_UpdatedFileList);

            m_FSelectForm.ShowDialog();
        }

        /// <summary>
        /// User updated the path of some files in the FolderSelectForm.
        /// </summary>
        /// <param name="Filelist">The list of updated files.</param>
        private void m_FSelectForm_UpdatedFileList(List<ManifestFile> Filelist)
        {
            m_ManifestFiles = Filelist;

            LstFiles.Items.Clear();

            this.Invoke(new MethodInvoker(delegate{
                foreach (ManifestFile MFile in Filelist)
                {
                    LstFiles.Items.Add(MFile.VirtualPath);
                }
            }));
        }

        /// <summary>
        /// User clicked on the "Exit" menuitem.
        /// </summary>
        private void exitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Application.Exit();
        }

        /// <summary>
        /// User clicked on the "Explanation" menuitem.
        /// </summary>
        private void explanationToolStripMenuItem_Click(object sender, EventArgs e)
        {
            MessageBox.Show("A manifest must have at least one file contained in it, and a parent or a child.\r\n" +
                "To add these, use the File menu. A manifest's child is the manifest for the update previous to the\r\n" +
                "update that this manifest represents. I.E if you're making a manifest for update 0003, its child \r\n" +
                "would be the manifest for update 0002. Then when you're done with manifest 0003, you would open the\r\n" + 
                "manifest for update 0002 and add 0003 as its parent.");
        }
    }
}
