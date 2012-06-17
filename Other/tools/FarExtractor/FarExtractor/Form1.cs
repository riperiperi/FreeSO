/*The contents of this file are subject to the Mozilla Public License Version 1.1
(the "License"); you may not use this file except in compliance with the
License. You may obtain a copy of the License at http://www.mozilla.org/MPL/

Software distributed under the License is distributed on an "AS IS" basis,
WITHOUT WARRANTY OF ANY KIND, either express or implied. See the License for
the specific language governing rights and limitations under the License.

The Original Code is the TSO LoginServer.

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
                                byte[] DecompressedBuffer = Decompress(Reader, Entry);

                                Writer = new BinaryWriter(File.Create(FBrowserDiag.SelectedPath + "\\" + Entry.Filename));
                                Writer.Write(DecompressedBuffer);
                                Writer.Close();
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
                                //int Filesize = (int)Entry.DecompressedFileSize;
                                byte[] DecompressedBuffer = Decompress(Reader, Entry);

                                Writer = new BinaryWriter(File.Create(FBrowserDiag.SelectedPath + "\\" + Entry.Filename));
                                Writer.Write(DecompressedBuffer);
                                Writer.Close();
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
                            else if (Entry.Filename.Contains(".jpg")) //TODO: Determine whether compressed or not
                            {
                                //int Filesize = Entry.CalculateFileSize();
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
            Reader.ReadBytes(9); //Unknown part of the header, not needed for decompression.

            //Read the compression header
            uint Filesize = Reader.ReadUInt32();
            ushort CompressionID = Reader.ReadUInt16();

            if (CompressionID == 0xFB10)
            {
                byte[] Dummy = Reader.ReadBytes(3);
                uint DecompressedSize = (uint)((Dummy[0] << 0x10) | (Dummy[1] << 0x08) | +Dummy[2]);

                /*return Uncompress(Reader.ReadBytes((int)Entry.CompressedFileSize), DecompressedSize, 0, 
                    (int)Entry.CompressedFileSize, GameType.SimCity4);*/

                Decompresser Dec = new Decompresser();
                Dec.CompressedSize = Filesize;
                Dec.DecompressedSize = DecompressedSize;

                return Dec.Decompress(Reader.ReadBytes((int)Filesize));
            }
            else //The entry wasn't compressed...
            {
                MessageBox.Show("CompressionID didn't match!");
                Reader.BaseStream.Seek((Reader.BaseStream.Position - 6), SeekOrigin.Begin);
                return Reader.ReadBytes((int)Entry.DecompressedFileSize/*Entry.CalculateFileSize()*/);
            }
        }

        /// <summary>
        /// Uncompresses the File Data passed
        /// </summary>
        /// <param name="data">Relevant File Data</param>
        /// <param name="targetSize">Size of the uncompressed Data</param>
        /// <param name="size">Maximum number of Bytes that should be read from the Resource</param>
        /// <param name="offset">File offset, where we should start to decompress from</param>
        /// <returns>The uncompressed FileData</returns>
        private static Byte[] Uncompress(Byte[] data, uint targetSize, int offset, int size, GameType Game)
        {
            Byte[] uncdata = null;
            int index = offset;

            try
            {
                uncdata = new Byte[targetSize];
            }
            catch (Exception)
            {
                uncdata = new Byte[0];
            }


            int uncindex = 0;
            int plaincount = 0;
            int copycount = 0;
            int copyoffset = 0;
            Byte cc = 0;
            Byte cc1 = 0;
            Byte cc2 = 0;
            Byte cc3 = 0;
            int source;

#if LOG
			System.IO.StreamWriter sw = new System.IO.StreamWriter(@"c:\decomp.txt", false);
			string kind = "";
			int lineoffset = 0;
#endif
            try
            {
                while ((index < data.Length) && (data[index] < 0xfc))
                {
#if LOG
					lineoffset = index;
#endif
                    cc = data[index++];

                    if ((cc & 0x80) == 0)
                    {
#if LOG
						kind = "0x00400";
#endif
                        cc1 = data[index++];
                        plaincount = (cc & 0x03);
                        copycount = ((cc & 0x1C) >> 2) + 3;
                        copyoffset = ((cc & 0x60) << 3) + cc1 + 1;
                    }
                    else if ((cc & 0x40) == 0)
                    {
#if LOG
						kind = "0x04000";
#endif
                        cc1 = data[index++];
                        cc2 = data[index++];
                        plaincount = ((cc1 & 0xC0) >> 6) & 0x03;
                        copycount = (cc & 0x3F) + 4;
                        copyoffset = ((cc1 & 0x3F) << 8) + cc2 + 1;
                    }
                    else if ((cc & 0x20) == 0)
                    {
#if LOG
						kind = "0x20000";
#endif
                        cc1 = data[index++];
                        cc2 = data[index++];
                        cc3 = data[index++];
                        if (Game == GameType.SimCity4)
                        {
                            plaincount = (cc & 0x03);
                            copycount = ((cc & 0x1C) << 6) + cc3 + 5;
                            copyoffset = (cc1 & 8) + cc2;
                        }
                        else if (Game == GameType.TheSims2)
                        {
                            plaincount = (cc & 0x03);
                            copycount = ((cc & 0x0C) << 6) + cc3 + 5;
                            copyoffset = ((cc & 0x10) << 12) + (cc1 << 8) + cc2 + 1;
                        }
                    }
                    else
                    {
#if LOG
						kind = "0x0";
#endif
                        plaincount = (cc - 0xDF) << 2;
                        copycount = 0;
                        copyoffset = 0;
                    }

                    for (int i = 0; i < plaincount; i++) uncdata[uncindex++] = data[index++];

                    source = uncindex - copyoffset;
                    for (int i = 0; i < copycount; i++) uncdata[uncindex++] = uncdata[source++];

                    if (size != -1)
                        if (uncindex >= size)
                        {
                            byte[] newdata = new byte[uncindex];
                            for (int i = 0; i < uncindex; i++) newdata[i] = uncdata[i];
                            return newdata;
                        }

#if LOG
					sw.WriteLine("offset="+Helper.HexString(lineoffset)+", plainc="+Helper.HexString(plaincount)+", copyc="+Helper.HexString(copycount)+", copyo="+Helper.HexString(copyoffset)+", type="+Helper.HexString(cc)+", kind="+kind);
#endif
                }//while
            } //try
            catch (Exception ex)
            {
                //Helper.ExceptionMessage("", ex);
                Console.WriteLine(ex.ToString());
            }
            finally
            {
#if LOG
					sw.Close();
					sw.Dispose();
					sw = null;
#endif
            }

            if (index < data.Length)
            {
                plaincount = (data[index++] & 0x03);
                for (int i = 0; i < plaincount; i++)
                {
                    if (uncindex >= uncdata.Length) break;
                    uncdata[uncindex++] = data[index++];
                }
            }
            return uncdata;
        }

        /// <summary>
        /// Returns the Stream that holds the given Resource
        /// </summary>
        /// <param name="pfd">The PackedFileDescriptor</param>
        /// <returns>The stream containing the resource. Be carfull, this is not at all Thread Save!!!</returns>
        public static System.IO.MemoryStream UncompressStream(System.IO.Stream s, int datalength, uint targetSize, int offset)
        {
            byte[] uncdata;

            int end = (int)(s.Position + datalength);
            s.Seek(offset, System.IO.SeekOrigin.Current);


            try
            {
                uncdata = new Byte[targetSize];
            }
            catch (Exception)
            {
                uncdata = new Byte[0];
            }

            int uncindex = 0;
            int plaincount = 0;
            int copycount = 0;
            int copyoffset = 0;
            Byte cc = 0;
            Byte cc1 = 0;
            Byte cc2 = 0;
            Byte cc3 = 0;


            try
            {
                while ((s.Position < end))
                {
                    cc = (byte)s.ReadByte();
                    if (cc >= 0xfc)
                    {
                        s.Seek(-1, System.IO.SeekOrigin.Current);
                        break;
                    }

                    if ((cc & 0x80) == 0)
                    {
                        cc1 = (byte)s.ReadByte();
                        plaincount = (cc & 0x03);
                        copycount = ((cc & 0x1C) >> 2) + 3;
                        copyoffset = ((cc & 0x60) << 3) + cc1 + 1;
                    }
                    else if ((cc & 0x40) == 0)
                    {
                        cc1 = (byte)s.ReadByte();
                        cc2 = (byte)s.ReadByte();
                        plaincount = ((cc1 & 0xC0) >> 6) & 0x03;
                        copycount = (cc & 0x3F) + 4;
                        copyoffset = ((cc1 & 0x3F) << 8) + cc2 + 1;
                    }
                    else if ((cc & 0x20) == 0)
                    {
                        cc1 = (byte)s.ReadByte();
                        cc2 = (byte)s.ReadByte();
                        cc3 = (byte)s.ReadByte();
                        plaincount = (cc & 0x03);
                        copycount = ((cc & 0x0C) << 6) + cc3 + 5;
                        copyoffset = ((cc & 0x10) << 12) + (cc1 << 8) + cc2 + 1;
                    }
                    else
                    {
                        plaincount = (cc - 0xDF) << 2;
                        copycount = 0;
                        copyoffset = 0;
                    }

                    for (int i = 0; i < plaincount; i++) uncdata[uncindex++] = (byte)s.ReadByte();

                    int source = uncindex - copyoffset;
                    for (int i = 0; i < copycount; i++) uncdata[uncindex++] = uncdata[source++];
                }//while
            } //try
            catch (Exception ex)
            {
                //Helper.ExceptionMessage("", ex);
                throw ex;
            }


            if (s.Position < end)
            {
                plaincount = (s.ReadByte() & 0x03);
                for (int i = 0; i < plaincount; i++)
                {
                    if (uncindex >= uncdata.Length) break;
                    uncdata[uncindex++] = (byte)s.ReadByte();
                }
            }
            return new System.IO.MemoryStream(uncdata);
        }

        #endregion
    }
}
