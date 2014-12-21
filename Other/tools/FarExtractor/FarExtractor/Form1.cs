/*This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
If a copy of the MPL was not distributed with this file, You can obtain one at
http://mozilla.org/MPL/2.0/.

The Original Code is the FarExtractor.

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

namespace FarExtractor
{
    public enum GameType
    {
        TheSims2,
        SimCity4
    }

    public enum ArchiveType
    {
        DBPF,
        FAR3,
        FAR
    }

    public partial class Form1 : Form
    {
        private List<Far3Entry> m_Far3Entries = new List<Far3Entry>();
        private List<FarEntry> m_FarEntries = new List<FarEntry>();
        private List<DBPFEntry> m_DBPFEntries = new List<DBPFEntry>();
        
        private string m_CurrentFile;
        private uint m_ManifestOffset;

        private ArchiveType m_CurrentArchiveType;

        public Form1()
        {
            InitializeComponent();
        }

        /// <summary>
        /// The user clicked on the 'Open archive' menu item in the 'File' menu.
        /// </summary>
        private void openFARArchiveToolStripMenuItem_Click(object sender, EventArgs e)
        {
            OpenFileDialog OpenFDiag = new OpenFileDialog();
            OpenFDiag.Title = "Open FAR archive";
            OpenFDiag.Filter = "FAR archive|*.dat|FAR archive|*.far|DBPF Archive|*.dat";

            if (OpenFDiag.ShowDialog() == DialogResult.OK)
            {
                m_Far3Entries.Clear();
                m_DBPFEntries.Clear();
                m_FarEntries.Clear();

                if (OpenFDiag.FileName.Contains(".dat"))
                {
                    if (DetermineArchiveType(OpenFDiag.FileName) == ArchiveType.FAR3)
                    {
                        OpenDatArchive(OpenFDiag.FileName);
                        m_CurrentFile = OpenFDiag.FileName;
                        m_CurrentArchiveType = ArchiveType.FAR3;
                    }
                    else if(DetermineArchiveType(OpenFDiag.FileName) == ArchiveType.DBPF)
                    {
                        OpenDBPF(OpenFDiag.FileName);
                        m_CurrentFile = OpenFDiag.FileName;
                        m_CurrentArchiveType = ArchiveType.DBPF;
                    }
                }
                else if (OpenFDiag.FileName.Contains(".far"))
                {
                    OpenFARArchive(OpenFDiag.FileName);
                    m_CurrentFile = OpenFDiag.FileName;
                    m_CurrentArchiveType = ArchiveType.FAR;
                }
            }
        }

        private ArchiveType DetermineArchiveType(string Path)
        {
            BinaryReader Reader = new BinaryReader(File.Open(Path, FileMode.Open));

            string Header = Encoding.ASCII.GetString(Reader.ReadBytes(8));

            if (Header == "FAR!byAZ")
            {
                uint Version = Reader.ReadUInt32();
                MessageBox.Show("Version: " + Version.ToString());

                if (Version == 3)
                {
                    Reader.Close();
                    return ArchiveType.FAR3;
                }

                Reader.Close();
                return ArchiveType.FAR;
            }

            Reader.Close();
            return ArchiveType.DBPF;
        }

        private void OpenDBPF(string Path)
        {
            BinaryReader Reader = new BinaryReader(File.Open(Path, FileMode.Open));

            string Header = Encoding.ASCII.GetString(Reader.ReadBytes(4));
            
            uint MajVersion = Reader.ReadUInt32();
            uint MinVersion = Reader.ReadUInt32();

            Reader.ReadBytes(12); //Reserved

            uint DateCreated = Reader.ReadUInt32();
            uint DateModified = Reader.ReadUInt32();

            uint IndexMaj = Reader.ReadUInt32();
            uint IndexEntryCount = Reader.ReadUInt32();
            uint FirstEntryOffset = Reader.ReadUInt32();
            uint IndexSize = Reader.ReadUInt32();

            uint HoleEntryCount = Reader.ReadUInt32();
            uint HoleOffset = Reader.ReadUInt32();
            uint HoleSize = Reader.ReadUInt32(); //Size of all Holes in archive?

            uint IndexMin = Reader.ReadUInt32();

            LstFiles.Items.Clear();
            LstFiles.Items.Add("Number of files: " + IndexEntryCount.ToString());
            LstFiles.Items.Add("");

            Reader.BaseStream.Seek(FirstEntryOffset, SeekOrigin.Begin);

            for (int i = 0; i < IndexEntryCount; i++)
            {
                DBPFEntry Entry = new DBPFEntry();
                Entry.TypeID = Reader.ReadUInt32();
                Entry.GroupID = Reader.ReadUInt32();
                Entry.InstanceID = Reader.ReadUInt32();

                if(IndexMaj == 7 && IndexMin == 1)
                    Entry.InstanceID2 = Reader.ReadUInt32();

                Entry.DataOffset = Reader.ReadUInt32();
                Entry.DataSize = Reader.ReadUInt32();

                m_DBPFEntries.Add(Entry);
            }

            Reader.Close();
        }

        private void OpenDatArchive(string Path)
        {
            BinaryReader Reader = new BinaryReader(File.Open(Path, FileMode.Open));

            BinaryWriter Logger = new BinaryWriter(File.Create("Entries log.txt"));

            string Header = Encoding.ASCII.GetString(Reader.ReadBytes(8));
            uint Version = Reader.ReadUInt32();

            if ((Header != "FAR!byAZ") || (Version != 3))
            {
                MessageBox.Show("Archive wasn't a valid FAR V.3 archive!");
                return;
            }

            uint ManifestOffset = Reader.ReadUInt32();
            m_ManifestOffset = ManifestOffset;

            Reader.BaseStream.Seek(ManifestOffset, SeekOrigin.Begin);

            LstFiles.Items.Clear();

            uint NumFiles = Reader.ReadUInt32();
            LstFiles.Items.Add("Number of files: " + NumFiles.ToString());
            LstFiles.Items.Add("");

            for (int i = 0; i < NumFiles; i++)
            {
                Far3Entry Entry = new Far3Entry();
                Entry.DecompressedFileSize = Reader.ReadUInt32();
                Logger.Write("DecompressedFilesize: " + Entry.DecompressedFileSize + "\r\n");
                byte[] Dummy = Reader.ReadBytes(3);
                Entry.CompressedFileSize = (uint)((Dummy[0] << 0) | (Dummy[1] << 8) | (Dummy[2]) << 16);
                Logger.Write("CompressedFilesize: " + Entry.CompressedFileSize + "\r\n");
                //Entry.CompressedFileSize = Convert.ToUInt32(Reader.ReadBytes(3));
                Entry.DataType = Reader.ReadByte();
                Logger.Write("DataType: " + Entry.DataType + "\r\n");
                //Entry.CompressedSpecifics = Reader.ReadUInt16();
                //Entry.PowerValue = Reader.ReadByte();
                //Entry.Unknown = Reader.ReadByte();
                Entry.DataOffset = Reader.ReadUInt32();
                Logger.Write("DataOffset: " + Entry.DataOffset + "\r\n");
                //Entry.Unknown2 = Reader.ReadUInt16();
                Entry.Compressed = Reader.ReadByte();
                Logger.Write("Compressed: " + Entry.Compressed + "\r\n");
                Entry.AccessNumber = Reader.ReadByte();
                Logger.Write("AccessNumber: " + Entry.AccessNumber + "\r\n");
                Entry.FilenameLength = Reader.ReadUInt16();
                Logger.Write("FilenameLength: " + Entry.FilenameLength + "\r\n");
                Entry.TypeID = Reader.ReadUInt32();
                Logger.Write("TypeID: " + Entry.TypeID + "\r\n");
                Entry.FileID = Reader.ReadUInt32();
                Logger.Write("FileID: " + Entry.FileID + "\r\n");
                Entry.Filename = Encoding.ASCII.GetString(Reader.ReadBytes(Entry.FilenameLength));
                Logger.Write("Filename: " + Entry.Filename + "\r\n");
                Logger.Write("\r\n");

                m_Far3Entries.Add(Entry);
                LstFiles.Items.Add(Entry.Filename + ", " + string.Format("{0:X}", Entry.FileID));
            }

            Logger.Flush();

            Reader.Close();
            Logger.Close();
        }

        private void OpenFARArchive(string Path)
        {
            BinaryReader Reader = new BinaryReader(File.Open(Path, FileMode.Open));

            string Header = Encoding.ASCII.GetString(Reader.ReadBytes(8));
            uint Version = Reader.ReadUInt32();

            if ((Header != "FAR!byAZ") || (Version != 1))
            {
                MessageBox.Show("Archive wasn't a valid FAR V.1 archive!");
                return;
            }

            uint ManifestOffset = Reader.ReadUInt32();
            m_ManifestOffset = ManifestOffset;

            Reader.BaseStream.Seek(ManifestOffset, SeekOrigin.Begin);

            LstFiles.Items.Clear();

            uint NumFiles = Reader.ReadUInt32();
            LstFiles.Items.Add("Number of files: " + NumFiles.ToString());
            LstFiles.Items.Add("");

            for (int i = 0; i < NumFiles; i++)
            {
                FarEntry Entry = new FarEntry();
                Entry.DataLength = Reader.ReadInt32();
                Entry.DataLength2 = Reader.ReadInt32();
                Entry.DataOffset = Reader.ReadInt32();
                Entry.FilenameLength = Reader.ReadInt16();
                Entry.Filename = Encoding.ASCII.GetString(Reader.ReadBytes(Entry.FilenameLength));

                m_FarEntries.Add(Entry);
                LstFiles.Items.Add(Entry.Filename);
            }

            Reader.Close();
        }

        /// <summary>
        /// User clicked on the 'Extract archive...' menu option.
        /// </summary>
        private void extractFARArchiveToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (m_CurrentFile == "")
            {
                MessageBox.Show("You must open an archive first!");
                return;
            }

            BinaryReader Reader = new BinaryReader(File.Open(m_CurrentFile, FileMode.Open));
            BinaryWriter Writer;

            FolderBrowserDialog FBrowserDiag = new FolderBrowserDialog();
            FBrowserDiag.Description = "Select where to extract archive...";

            if (FBrowserDiag.ShowDialog() == DialogResult.OK)
            {
                if (m_CurrentFile.Contains(".dat"))
                {
                    if (m_CurrentArchiveType == ArchiveType.FAR3)
                    {
                        foreach (Far3Entry Entry in m_Far3Entries)
                        {
                            Reader.BaseStream.Seek(Entry.DataOffset, SeekOrigin.Begin);

                            if (Entry.Filename.Contains(".bmp"))
                            {
                                if (Entry.TypeID == 1)
                                {
                                    byte[] DecompressedBuffer = Decompress(Reader, Entry);

                                    Writer = new BinaryWriter(File.Create(FBrowserDiag.SelectedPath + "\\" + Entry.Filename));
                                    Writer.Write(DecompressedBuffer);
                                    Writer.Close();
                                }
                                else //TypeID should be 0x856DDBAC, meaning the entry isn't compressed.
                                {
                                    uint Filesize = Entry.DecompressedFileSize;

                                    Writer = new BinaryWriter(File.Create(FBrowserDiag.SelectedPath + "\\" + Entry.Filename));
                                    Writer.Write(Reader.ReadBytes((int)Filesize));
                                    Writer.Close();
                                }
                            }
                            else if (Entry.Filename.Contains(".tga"))
                            {
                                byte[] DecompressedBuffer = Decompress(Reader, Entry);

                                Writer = new BinaryWriter(File.Create(FBrowserDiag.SelectedPath + "\\" + Entry.Filename));
                                Writer.Write(DecompressedBuffer);
                                Writer.Close();
                            }
                            else if (Entry.Filename.Contains(".skel"))
                            {
                                byte[] DecompressedBuffer = Decompress(Reader, Entry);

                                Writer = new BinaryWriter(File.Create(FBrowserDiag.SelectedPath + "\\" + Entry.Filename));
                                Writer.Write(DecompressedBuffer);
                                Writer.Close();
                            }
                            else if (Entry.Filename.Contains(".anim"))
                            {
                                byte[] DecompressedBuffer = Decompress(Reader, Entry);

                                Writer = new BinaryWriter(File.Create(FBrowserDiag.SelectedPath + "\\" + Entry.Filename));
                                Writer.Write(DecompressedBuffer);
                                Writer.Close();
                            }
                            else if (Entry.Filename.Contains(".mesh"))
                            {
                                byte[] DecompressedBuffer = Decompress(Reader, Entry);

                                Writer = new BinaryWriter(File.Create(FBrowserDiag.SelectedPath + "\\" + Entry.Filename));
                                Writer.Write(DecompressedBuffer);
                                Writer.Close();
                            }
                            else if (Entry.Filename.Contains(".bnd"))
                            {
                                //int Filesize = Entry.CalculateFileSize();
                                uint Filesize = Entry.DecompressedFileSize;

                                Writer = new BinaryWriter(File.Create(FBrowserDiag.SelectedPath + "\\" + Entry.Filename));
                                Writer.Write(Reader.ReadBytes((int)Filesize));
                                Writer.Close();
                            }
                            else if (Entry.Filename.Contains(".apr")) //APpeaRance
                            {
                                //int Filesize = Entry.CalculateFileSize();
                                uint Filesize = Entry.DecompressedFileSize;

                                Writer = new BinaryWriter(File.Create(FBrowserDiag.SelectedPath + "\\" + Entry.Filename));
                                Writer.Write(Reader.ReadBytes((int)Filesize));
                                Writer.Close();
                            }
                            else if (Entry.Filename.Contains(".oft")) //OutFiT
                            {
                                //int Filesize = Entry.CalculateFileSize();
                                uint Filesize = Entry.DecompressedFileSize;

                                Writer = new BinaryWriter(File.Create(FBrowserDiag.SelectedPath + "\\" + Entry.Filename));
                                Writer.Write(Reader.ReadBytes((int)Filesize));
                                Writer.Close();
                            }
                            else if (Entry.Filename.Contains(".png"))
                            {
                                if (Entry.TypeID == 24)
                                {
                                    //int Filesize = (int)Entry.DecompressedFileSize;
                                    byte[] DecompressedBuffer = Decompress(Reader, Entry);

                                    Writer = new BinaryWriter(File.Create(FBrowserDiag.SelectedPath + "\\" + Entry.Filename));
                                    Writer.Write(DecompressedBuffer);
                                    Writer.Close();
                                }
                                else //TypeID should be 14, which means entry isn't compressed.
                                {
                                    uint Filesize = Entry.DecompressedFileSize;

                                    Writer = new BinaryWriter(File.Create(FBrowserDiag.SelectedPath + "\\" + Entry.Filename));
                                    Writer.Write(Reader.ReadBytes((int)Filesize));
                                    Writer.Close();
                                }
                            }
                            else if (Entry.Filename.Contains(".po")) //Purchasable Object
                            {
                                //int Filesize = Entry.CalculateFileSize();
                                uint Filesize = Entry.DecompressedFileSize;

                                Writer = new BinaryWriter(File.Create(FBrowserDiag.SelectedPath + "\\" + Entry.Filename));
                                Writer.Write(Reader.ReadBytes((int)Filesize));
                                Writer.Close();
                            }
                            else if (Entry.Filename.Contains(".col")) //COLlection
                            {
                                byte[] DecompressedBuffer = Decompress(Reader, Entry);

                                Writer = new BinaryWriter(File.Create(FBrowserDiag.SelectedPath + "\\" + Entry.Filename));
                                Writer.Write(DecompressedBuffer);
                                Writer.Close();
                            }
                            else if (Entry.Filename.Contains(".hag")) //Group?
                            {
                                byte[] DecompressedBuffer = Decompress(Reader, Entry);

                                Writer = new BinaryWriter(File.Create(FBrowserDiag.SelectedPath + "\\" + Entry.Filename));
                                Writer.Write(DecompressedBuffer);
                                Writer.Close();
                            }
                            else if (Entry.Filename.Contains(".jpg"))
                            {
                                uint Filesize = Entry.DecompressedFileSize;

                                Writer = new BinaryWriter(File.Create(FBrowserDiag.SelectedPath + "\\" + Entry.Filename));
                                Writer.Write(Reader.ReadBytes((int)Filesize));
                                Writer.Close();
                            }
                        }
                    }
                    else if (m_CurrentArchiveType == ArchiveType.DBPF)
                    {
                        foreach (DBPFEntry Entry in m_DBPFEntries)
                        {
                            Reader.BaseStream.Seek(Entry.DataOffset, SeekOrigin.Begin);
                            byte[] Data = Reader.ReadBytes((int)Entry.DataSize);

                            //NOTE: TypeID and GroupID are the same for all entries, so to differentiate the type,
                            //      the header of each file has to be read.
                            byte[] HeaderBuf = new byte[2];
                            Array.Copy(Data, HeaderBuf, 2);
                            string Header = Encoding.ASCII.GetString(HeaderBuf);

                            switch (Header)
                            {
                                case "XA":
                                    Writer = new BinaryWriter(File.Create(FBrowserDiag.SelectedPath + "\\" + Entry.InstanceID + ".xa"));
                                    Writer.Write(Data);
                                    Writer.Close();

                                    break;
                                case "UT": //UTalk
                                    Writer = new BinaryWriter(File.Create(FBrowserDiag.SelectedPath + "\\" + Entry.InstanceID + ".utk"));
                                    Writer.Write(Data);
                                    Writer.Close();

                                    break;
                                case "WA": //Wav
                                    Writer = new BinaryWriter(File.Create(FBrowserDiag.SelectedPath + "\\" + Entry.InstanceID + ".wav"));
                                    Writer.Write(Data);
                                    Writer.Close();

                                    break;
                                case "TK": //Tracks
                                    Writer = new BinaryWriter(File.Create(FBrowserDiag.SelectedPath + "\\" + Entry.InstanceID + ".tkdt"));
                                    Writer.Write(Data);
                                    Writer.Close();

                                    break;
                                case "BM": //BMP (TSOAudio.dat)
                                    Writer = new BinaryWriter(File.Create(FBrowserDiag.SelectedPath + "\\" + Entry.InstanceID + ".bmp"));
                                    Writer.Write(Data);
                                    Writer.Close();
                                    
                                    break;
                                case "HI": //Hitlist
                                    Writer = new BinaryWriter(File.Create(FBrowserDiag.SelectedPath + "\\" + Entry.InstanceID + ".bmp"));
                                    Writer.Write(Data);
                                    Writer.Close();

                                    break;
                                case "# ": //UIScript (TSOAudio.dat)
                                    Writer = new BinaryWriter(File.Create(FBrowserDiag.SelectedPath + "\\" + Entry.InstanceID + ".uis"));
                                    Writer.Write(Data);
                                    Writer.Close();

                                    break;
                                default: //MP3 has no fileheader!
                                    if (Entry.TypeID != 0x7B1ACFCD)
                                    {
                                        Writer = new BinaryWriter(File.Create(FBrowserDiag.SelectedPath + "\\" + Entry.InstanceID + ".mp3"));
                                        Writer.Write(Data);
                                        Writer.Close();
                                    }

                                    break;
                            }

                            if (Entry.TypeID == 0x7B1ACFCD) //Unknown
                            {
                                Writer = new BinaryWriter(File.Create(FBrowserDiag.SelectedPath + "\\" + Entry.InstanceID + ".unknown"));
                                Writer.Write(Data);
                                Writer.Close();
                            }
                        }
                    }
                }
                else if (m_CurrentFile.Contains(".far"))
                {
                    foreach(FarEntry Entry in m_FarEntries)
                    {
                        Reader.BaseStream.Seek((long)Entry.DataOffset, SeekOrigin.Begin);
                        byte[] Data = Reader.ReadBytes(Entry.DataLength);

                        Writer = new BinaryWriter(File.Create(FBrowserDiag.SelectedPath + "\\" + Entry.Filename));
                        Writer.Write(Data);
                        Writer.Close();
                    }
                }
            }

            Reader.Close();

            MessageBox.Show("Done!");
        }

        /// <summary>
        /// User clicked the 'About' menu option in the 'Help' menu.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void aboutToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            MessageBox.Show("Written by Mats 'Afr0' Vederhus\r\n" +
                "Copyright 2010\r\n" +
                "Uses decompression code ported from DBPF4J", "TSO Extractor");
        }

        #region Decompression

        private byte[] Decompress(BinaryReader Reader, Far3Entry Entry)
        {
            //Unknown part of the header, not needed for decompression.
            //NOTE: This header is part of the actual filedata if file isn't compressed.
            Reader.ReadBytes(9);

            //Read the compression header
            uint Filesize = Reader.ReadUInt32();
            ushort CompressionID = Reader.ReadUInt16();

            if (CompressionID == 0xFB10)
            {
                byte[] Dummy = Reader.ReadBytes(3);
                uint DecompressedSize = (uint)((Dummy[0] << 0x10) | (Dummy[1] << 0x08) | +Dummy[2]);

                Decompresser Dec = new Decompresser();
                Dec.CompressedSize = Filesize;
                Dec.DecompressedSize = DecompressedSize;

                return Dec.Decompress(Reader.ReadBytes((int)Filesize));
            }
            else //The entry wasn't compressed...
            {
                Reader.BaseStream.Seek((Reader.BaseStream.Position - 15), SeekOrigin.Begin);
                return Reader.ReadBytes((int)Entry.DecompressedFileSize);
            }
        }

        #endregion
    }
}
