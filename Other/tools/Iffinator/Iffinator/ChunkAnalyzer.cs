using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using System.IO;
using System.Threading;
using Microsoft.Win32;
using SimsLib.FAR1;
using Iffinator.Flash;

namespace Iffinator
{
    /// <summary>
    /// Holds information about an Iff archive in an FAR archive.
    /// </summary>
    public struct IffInfo
    {
        public string IffName;
        public string ArchivePath;
        public int NumChunks;      //Number of chunks of the type that was searched for.
    }

    public delegate void UpdateStatusDelegate(string CurrentFile);
    public delegate void FoundChunkDelegate(string IFFFile);

    public partial class ChunkAnalyzer : Form
    {
        private string m_TSOPath;
        private Dictionary<string, List<IffInfo>> m_IffInfoList = new Dictionary<string, List<IffInfo>>();
        
        private Thread m_WorkerThread;
        private UpdateStatusDelegate m_OnUpdateStatus;
        private FoundChunkDelegate m_OnFoundChunk;

        private const bool m_Debug = true;

        public ChunkAnalyzer()
        {
            InitializeComponent();

            RegistryKey softwareKey = Registry.LocalMachine.OpenSubKey("SOFTWARE");
            if (Array.Exists(softwareKey.GetSubKeyNames(), delegate(string s) { return s.CompareTo("Maxis") == 0; }))
            {
                RegistryKey maxisKey = softwareKey.OpenSubKey("Maxis");
                if (Array.Exists(maxisKey.GetSubKeyNames(), delegate(string s) { return s.CompareTo("The Sims Online") == 0; }))
                {
                    RegistryKey tsoKey = maxisKey.OpenSubKey("The Sims Online");
                    string installDir = (string)tsoKey.GetValue("InstallDir");
                    installDir += "\\TSOClient\\";
                    m_TSOPath = installDir;
                    LblTSOPath.Text = "TSO path: " + m_TSOPath;
                }
                else
                {
                    LblTSOPath.Text = "TSO was not found, please specify path: ";
                    TxtTSOPath.Visible = true;
                }
            }
            else
                LblTSOPath.Text = "TSO path: " + m_TSOPath;

            m_OnUpdateStatus = new UpdateStatusDelegate(UpdateStatus);
            m_OnFoundChunk = new FoundChunkDelegate(FoundChunk);
        }

        private void UpdateStatus(string CurrentFile)
        {
            LblScanning.Text = "Scanning: " + CurrentFile + "...";
        }

        private void FoundChunk(string IFFFile)
        {
            LstChunkTypes.Items.Add(IFFFile);
        }

        /// <summary>
        /// User clicked the "Analyze" button.
        /// </summary>
        private void button1_Click(object sender, EventArgs e)
        {
            LstChunkTypes.Items.Clear();
            m_IffInfoList.Clear();
            LblScanning.Visible = true;
            BtnAbort.Visible = true;
            m_WorkerThread = new Thread(new ThreadStart(ScanIFFs));
            m_WorkerThread.Start();
        }

        /// <summary>
        /// User clicked the "Abort" button.
        /// </summary>
        private void BtnAbort_Click(object sender, EventArgs e)
        {
            m_WorkerThread.Abort();
            LblScanning.Visible = false;
        }

        /// <summary>
        /// This function searches through all the IFFs in the game to
        /// find chunks of the type specified by the user.
        /// </summary>
        private void ScanIFFs()
        {
            string ObjDataPath = "", HouseDataPath = "";
            bool LookingForSprites = false;

            if (m_TSOPath != "" || TxtTSOPath.Text != "")
            {
                if (TxtChunkType.Text != "")
                {
                    if(TxtChunkType.Text.Contains("SPR#") || TxtChunkType.Text.Contains("SPR2") || 
                        TxtChunkType.Text.Contains("DGRP"))
                        LookingForSprites = true;

                    string[] Dirs = Directory.GetDirectories(m_TSOPath);

                    foreach (string Dir in Dirs)
                    {
                        if (Dir.Contains("objectdata"))
                            ObjDataPath = Dir;

                        if (Dir.Contains("housedata"))
                            HouseDataPath = Dir;
                    }

                    string[] ObjDataDirs = Directory.GetDirectories(ObjDataPath);
                    string[] HouseDataDirs = Directory.GetDirectories(HouseDataPath);

                    foreach (string Dir in ObjDataDirs)
                    {
                        string[] Files = Directory.GetFiles(Dir);

                        foreach (string ArchivePath in Files)
                        {
                            if (ArchivePath.Contains(".far"))
                            {
                                FARArchive Archive = new FARArchive(ArchivePath);
                                List<KeyValuePair<string, byte[]>> ArchiveFiles = Archive.GetAllEntries();

                                foreach (KeyValuePair<string, byte[]> ArchiveFile in ArchiveFiles)
                                {
                                    if (!LookingForSprites)
                                    {
                                        //Skip the OTFs in 'objotf.far'...
                                        if (ArchiveFile.Key.Contains(".iff"))
                                        {
                                            Iff IffFile;
                                            List<IffChunk> Chunks = new List<IffChunk>();
                                            int NumChunks = 0;

                                            LblScanning.Invoke(m_OnUpdateStatus, new object[] { ArchiveFile.Key });

                                            if (!m_Debug)
                                                IffFile = new Iff(ArchiveFile.Value, ArchiveFile.Key);
                                            else
                                                IffFile = new Iff(ArchiveFile.Value);

                                            //Figure out how many chunks of the type being searched for
                                            //is in the current IFF archive. Is there a faster way to do this?
                                            foreach (IffChunk Chunk in IffFile.Chunks)
                                            {
                                                if (Chunk.Resource == TxtChunkType.Text)
                                                    NumChunks++;
                                            }

                                            List<IffInfo> InfoList = new List<IffInfo>();

                                            foreach (IffChunk Chunk in IffFile.Chunks)
                                            {
                                                if (Chunk.Resource == TxtChunkType.Text)
                                                {
                                                    LstChunkTypes.Invoke(m_OnFoundChunk, new object[] { ArchiveFile.Key });

                                                    IffInfo Info = new IffInfo();
                                                    Info.ArchivePath = ArchivePath;
                                                    Info.IffName = ArchiveFile.Key;
                                                    Info.NumChunks = NumChunks;

                                                    InfoList.Add(Info);
                                                }
                                            }

                                            m_IffInfoList.Add(Path.GetFileName(ArchivePath) + "\\" + ArchiveFile.Key, InfoList);
                                        }
                                    }
                                    else
                                    {
                                        Iff IffFile;
                                        int NumChunks = 0;

                                        if (ArchiveFile.Key.Contains(".spf") || ArchiveFile.Key.Contains(".wll") || ArchiveFile.Key.Contains(".flr"))
                                        {
                                            LblScanning.Invoke(m_OnUpdateStatus, new object[] { ArchiveFile.Key });

                                            if (!m_Debug)
                                                IffFile = new Iff(ArchiveFile.Value, ArchiveFile.Key);
                                            else
                                                IffFile = new Iff(ArchiveFile.Value);

                                            //Figure out how many chunks of the type is being searched for
                                            //is in the current IFF archive. Is there a faster way to do this?
                                            foreach (IffChunk Chunk in IffFile.Chunks)
                                            {
                                                if (Chunk.Resource == TxtChunkType.Text)
                                                    NumChunks++;
                                            }

                                            List<IffInfo> InfoList = new List<IffInfo>();

                                            foreach (IffChunk Chunk in IffFile.Chunks)
                                            {
                                                if (Chunk.Resource == TxtChunkType.Text)
                                                {
                                                    LstChunkTypes.Invoke(m_OnFoundChunk, new object[] { ArchiveFile.Key });

                                                    IffInfo Info = new IffInfo();
                                                    Info.ArchivePath = ArchivePath;
                                                    Info.IffName = ArchiveFile.Key;
                                                    Info.NumChunks = NumChunks;

                                                    InfoList.Add(Info);
                                                }
                                            }

                                            m_IffInfoList.Add(Path.GetFileName(ArchivePath) + "\\" + ArchiveFile.Key, InfoList);
                                        }
                                    }
                                }
                            }
                            else //The files in "objectdata\globals\" are not in a FAR archive...
                            {
                                //Some of the files in "objectdata\globals\" are *.otf files...
                                if (ArchivePath.Contains(".iff"))
                                {
                                    if (!LookingForSprites)
                                    {
                                        Iff IffFile = new Iff(ArchivePath);
                                        int NumChunks = 0;

                                        LblScanning.Invoke(m_OnUpdateStatus, new object[] { ArchivePath });

                                        //Figure out how many chunks of the type is being searched for
                                        //is in the current IFF archive. Is there a faster way to do this?
                                        foreach (IffChunk Chunk in IffFile.Chunks)
                                        {
                                            if (Chunk.Resource == TxtChunkType.Text)
                                                NumChunks++;
                                        }

                                        List<IffInfo> InfoList = new List<IffInfo>();

                                        foreach (IffChunk Chunk in IffFile.Chunks)
                                        {
                                            if (Chunk.Resource == TxtChunkType.Text)
                                            {
                                                LstChunkTypes.Invoke(m_OnFoundChunk, new object[] { Path.GetFileName(ArchivePath) });

                                                IffInfo Info = new IffInfo();
                                                Info.ArchivePath = "";
                                                Info.IffName = IffFile.Path;
                                                Info.NumChunks = NumChunks;

                                                InfoList.Add(Info);
                                            }
                                        }

                                        m_IffInfoList.Add(Path.GetFileName(ArchivePath), InfoList);
                                    }
                                }
                            }
                        }
                    }

                    foreach (string Dir in HouseDataDirs)
                    {
                        string[] Files = Directory.GetFiles(Dir);

                        if (Dir.Contains("walls") || Dir.Contains("floors"))
                        {
                            foreach (string ArchivePath in Files)
                            {
                                FARArchive Archive = new FARArchive(ArchivePath);
                                List<KeyValuePair<string, byte[]>> ArchiveFiles = Archive.GetAllEntries();

                                foreach (KeyValuePair<string, byte[]> ArchiveFile in ArchiveFiles)
                                {
                                    if (!LookingForSprites)
                                    {
                                        //Don't waste time scanning *.spf files if not looking for sprites...
                                        if(!ArchiveFile.Key.Contains(".spf"))
                                        {
                                            Iff IffFile;
                                            int NumChunks = 0;

                                            LblScanning.Invoke(m_OnUpdateStatus, new object[] { ArchiveFile.Key });

                                            if (!m_Debug)
                                                IffFile = new Iff(ArchiveFile.Value, ArchiveFile.Key);
                                            else
                                                IffFile = new Iff(ArchiveFile.Value);

                                            //Figure out how many chunks of the type is being searched for
                                            //is in the current IFF archive. Is there a faster way to do this?
                                            foreach (IffChunk Chunk in IffFile.Chunks)
                                            {
                                                if (Chunk.Resource == TxtChunkType.Text)
                                                    NumChunks++;
                                            }

                                            List<IffInfo> InfoList = new List<IffInfo>();

                                            foreach (IffChunk Chunk in IffFile.Chunks)
                                            {
                                                if (Chunk.Resource == TxtChunkType.Text)
                                                {
                                                    LstChunkTypes.Invoke(m_OnFoundChunk, new object[] { ArchiveFile.Key });

                                                    IffInfo Info = new IffInfo();
                                                    Info.ArchivePath = ArchivePath;
                                                    Info.IffName = ArchiveFile.Key;
                                                    Info.NumChunks = NumChunks;

                                                    InfoList.Add(Info);
                                                }
                                            }

                                            m_IffInfoList.Add(Path.GetFileName(ArchivePath) + "\\" + ArchiveFile.Key, InfoList);
                                        }
                                    }
                                    else
                                    {
                                        if (ArchiveFile.Key.Contains(".spf") || ArchiveFile.Key.Contains(".wll") || ArchiveFile.Key.Contains(".flr"))
                                        {
                                            Iff IffFile;
                                            int NumChunks = 0;

                                            LblScanning.Invoke(m_OnUpdateStatus, new object[] { ArchiveFile.Key });

                                            if (!m_Debug)
                                                IffFile = new Iff(ArchiveFile.Value, ArchiveFile.Key);
                                            else
                                                IffFile = new Iff(ArchiveFile.Value);

                                            //Figure out how many chunks of the type is being searched for
                                            //is in the current IFF archive. Is there a faster way to do this?
                                            foreach (IffChunk Chunk in IffFile.Chunks)
                                            {
                                                if (Chunk.Resource == TxtChunkType.Text)
                                                    NumChunks++;
                                            }

                                            List<IffInfo> InfoList = new List<IffInfo>();

                                            foreach (IffChunk Chunk in IffFile.Chunks)
                                            {
                                                if (Chunk.Resource == TxtChunkType.Text)
                                                {
                                                    LstChunkTypes.Invoke(m_OnFoundChunk, new object[] { ArchiveFile.Key });

                                                    IffInfo Info = new IffInfo();
                                                    Info.ArchivePath = ArchivePath;
                                                    Info.IffName = ArchiveFile.Key;
                                                    Info.NumChunks = NumChunks;

                                                    InfoList.Add(Info);
                                                }
                                            }

                                            m_IffInfoList.Add(Path.GetFileName(ArchivePath) + "\\" + ArchiveFile.Key, InfoList);
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
                else
                    MessageBox.Show("Please specify a chunktype to search for!");
            }

            LblScanning.Invoke((MethodInvoker)delegate() 
            { 
                LblScanning.Text = "Done, found: " + TotalNumberOfChunksFound() + " chunks."; 
            });
            BtnAbort.Invoke((MethodInvoker)delegate() { BtnAbort.Visible = false; }); 
            m_WorkerThread.Abort();
        }

        /// <summary>
        /// Calculates the total number of chunks found after a scan.
        /// </summary>
        /// <returns>The total number of chunks found.</returns>
        private int TotalNumberOfChunksFound()
        {
            int Total = 0;

            foreach (KeyValuePair<string, List<IffInfo>> KVP in m_IffInfoList)
            {
                if(KVP.Value.Count >= 1)
                    Total += KVP.Value[0].NumChunks;
            }

            return Total;
        }

        /// <summary>
        /// User clicked on one of the entries in the LstChunkTypes list.
        /// </summary>
        private void LstChunkTypes_SelectedIndexChanged(object sender, EventArgs e)
        {
            foreach (KeyValuePair<string, List<IffInfo>> KVP in m_IffInfoList)
            {
                foreach (IffInfo Info in KVP.Value)
                {
                    if ((string)LstChunkTypes.Items[LstChunkTypes.SelectedIndex] == Path.GetFileName(Info.IffName))
                    {
                        LstChunkInfo.Items.Clear();

                        LstChunkInfo.Items.Add("IFF file: " + Info.IffName);

                        if (Info.ArchivePath.Contains("objectdata"))
                        {
                            if (Info.ArchivePath.Contains("globals"))
                                LstChunkInfo.Items.Add("Archive: objectdata\\globals\\" + Path.GetFileName(Info.ArchivePath));
                            else
                                LstChunkInfo.Items.Add("Archive: objectdata\\objects\\" + Path.GetFileName(Info.ArchivePath));
                        }
                        else if (Info.ArchivePath.Contains("housedata"))
                        {
                            if (Info.ArchivePath.Contains("floors"))
                                LstChunkInfo.Items.Add("Archive: housedata\\floors\\" + Path.GetFileName(Info.ArchivePath));
                            else if (Info.ArchivePath.Contains("floors1"))
                                LstChunkInfo.Items.Add("Archive: housedata\\floors1\\" + Path.GetFileName(Info.ArchivePath));
                            else if (Info.ArchivePath.Contains("floors2"))
                                LstChunkInfo.Items.Add("Archive: housedata\\floors2\\" + Path.GetFileName(Info.ArchivePath));
                            else if (Info.ArchivePath.Contains("floors3"))
                                LstChunkInfo.Items.Add("Archive: housedata\\floors3\\" + Path.GetFileName(Info.ArchivePath));
                            else if (Info.ArchivePath.Contains("floors4"))
                                LstChunkInfo.Items.Add("Archive: housedata\\floors4\\" + Path.GetFileName(Info.ArchivePath));
                            if (Info.ArchivePath.Contains("walls"))
                                LstChunkInfo.Items.Add("Archive: housedata\\walls\\" + Path.GetFileName(Info.ArchivePath));
                            else if (Info.ArchivePath.Contains("walls1"))
                                LstChunkInfo.Items.Add("Archive: housedata\\walls1\\" + Path.GetFileName(Info.ArchivePath));
                            else if (Info.ArchivePath.Contains("walls2"))
                                LstChunkInfo.Items.Add("Archive: housedata\\walls2\\" + Path.GetFileName(Info.ArchivePath));
                            else if (Info.ArchivePath.Contains("walls3"))
                                LstChunkInfo.Items.Add("Archive: housedata\\walls3\\" + Path.GetFileName(Info.ArchivePath));
                            else if (Info.ArchivePath.Contains("walls4"))
                                LstChunkInfo.Items.Add("Archive: housedata\\walls4\\" + Path.GetFileName(Info.ArchivePath));
                        }

                        LstChunkInfo.Items.Add("Number of chunks in this IFF of this type: " + Info.NumChunks);
                    }
                }
            }
        }

        /// <summary>
        /// User clicked the "What is this?" menuoption in the "Help" menu.
        /// </summary>
        private void whatIsThisToolStripMenuItem_Click(object sender, EventArgs e)
        {
            MessageBox.Show("Here you can search for specific chunks in TSO's files.\r\n" +
                "Type in the name of a chunk, and then click 'Analyze'!");
        }
    }
}
