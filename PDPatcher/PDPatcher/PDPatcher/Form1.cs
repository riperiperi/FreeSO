using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using System.IO;
using System.Diagnostics;
using LogThis;
using KISS;

namespace PDPatcher
{
    public partial class Form1 : Form
    {
        private Requester m_Requester;
        private ManifestFile m_ClientManifest, m_PatchManifest;
        //Files that make up the difference between client's version and patch version.
        private List<PatchFile> m_PatchDiff = new List<PatchFile>();
        private int m_NumFilesDownloaded = 0;

        public Form1()
        {
            InitializeComponent();

            m_ClientManifest = new ManifestFile(File.Open("Client.manifest", FileMode.Open));

            m_Requester = new Requester("https://dl.dropboxusercontent.com/u/257809956/PatchManifest.manifest");

            m_Requester.OnFetchedManifest += new FetchedManifestDelegate(m_Requester_OnFetchedManifest);
            m_Requester.OnTick += new DownloadTickDelegate(m_Requester_OnTick);
            m_Requester.OnFetchedFile += new FetchedFileDelegate(m_Requester_OnFetchedFile);
            Logger.OnMessageLogged += new MessageLoggedDelegate(Logger_OnMessageLogged);

            m_Requester.Initialize();
        }

        /// <summary>
        /// Another file was fetched!
        /// </summary>
        /// <param name="FileStream">Stream of the file that was fetched.</param>
        private void m_Requester_OnFetchedFile(Stream FileStream)
        {
            string AppDir = AppDomain.CurrentDomain.BaseDirectory;

            using(BinaryWriter Writer = new BinaryWriter(File.Create(AppDir + 
                "Tmp\\" + m_PatchManifest.PatchFiles[m_NumFilesDownloaded].Address)))
            {
                BinaryReader Reader = new BinaryReader(FileStream);
                Writer.Write(Reader.ReadBytes((int)FileStream.Length - 1));
            }

            //Delete original file...
            File.Delete(AppDir + m_PatchManifest.PatchFiles[m_NumFilesDownloaded].Address);
            //...and replace it with the downloaded one!
            File.Move("Tmp\\" + m_PatchManifest.PatchFiles[m_NumFilesDownloaded].Address,
                AppDir + m_PatchManifest.PatchFiles[m_NumFilesDownloaded].Address);

            if (m_NumFilesDownloaded < m_PatchDiff.Count)
            {
                m_NumFilesDownloaded++;
                m_Requester.FetchFile(m_PatchDiff[m_NumFilesDownloaded].URL);
            }
            else
            {
                MessageBox.Show("Your client is up to date!\n Exiting...");
                if (File.Exists("Project Dollhouse Client.exe"))
                    Process.Start("Project Dollhouse Client.exe");

                Application.Exit();
            }
        }

        /// <summary>
        /// The manifest was fetched.
        /// </summary>
        /// <param name="Manifest">The patch manifest that was fetched.</param>
        private void m_Requester_OnFetchedManifest(ManifestFile Manifest)
        {
            m_PatchManifest = Manifest;

            //Versions didn't match, do update.
            if (m_ClientManifest.Version != m_PatchManifest.Version)
            {
                foreach (PatchFile clPF in m_ClientManifest.PatchFiles)
                {
                    foreach (PatchFile pmPF in m_PatchManifest.PatchFiles)
                    {
                        if (NeedToDownloadFile(pmPF, clPF))
                            m_PatchDiff.Add(pmPF);
                    }
                }

                Directory.CreateDirectory("Tmp");
                m_Requester.FetchFile(m_PatchDiff[0].URL);
            }
            else
            {
                MessageBox.Show("Your client is up to date!\n Exiting...");
                if (File.Exists("Project Dollhouse Client.exe"))
                    Process.Start("Project Dollhouse Client.exe");
                
                Application.Exit();
            }
        }

        private bool NeedToDownloadFile(PatchFile Patch, PatchFile Client)
        {
            string PatchName = Path.GetFileName(Patch.Address);
            string ClientName = Path.GetFileName(Patch.Address);

            if ((Patch.FileHash != Client.FileHash) || (PatchName != ClientName))
                return true;

            return false;
        }

        /// <summary>
        /// The requester ticked, supplying information about a download in progress.
        /// </summary>
        /// <param name="State">The state of the download in progress.</param>
        private void m_Requester_OnTick(RequestState State)
        {
            if(LblSpeed.InvokeRequired)
                this.Invoke(new MethodInvoker(() => { LblSpeed.Text = State.KBPerSec.ToString() + " KB/Sec"; }));
            else
                LblSpeed.Text = State.KBPerSec.ToString() + "KB / Sec";

            if (PrgFile.InvokeRequired)
            {
                this.Invoke(new MethodInvoker(() =>
                    {
                        PrgFile.Step = (int)(PrgFile.Width * (State.PctComplete / State.ContentLength));
                        PrgFile.PerformStep();
                    }));
            }
            else
            {
                PrgFile.Step = (int)(PrgFile.Width * (State.PctComplete / State.ContentLength));
                PrgFile.PerformStep();
            }
        }

        /// <summary>
        /// KISS threw an exception.
        /// </summary>
        /// <param name="Msg">The message that was logged.</param>
        private void Logger_OnMessageLogged(LogMessage Msg)
        {
            switch (Msg.Level)
            {
                case LogLevel.error:
                    Log.LogThis(Msg.Message, eloglevel.error);
                    break;
                case LogLevel.info:
                    Log.LogThis(Msg.Message, eloglevel.info);
                    break;
                case LogLevel.warn:
                    Log.LogThis(Msg.Message, eloglevel.warn);
                    break;
            }
        }
    }
}
