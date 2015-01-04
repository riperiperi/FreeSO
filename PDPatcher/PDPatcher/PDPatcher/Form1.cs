using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using System.IO;
using System.Diagnostics;
using System.Threading;
using LogThis;
using KISS;

namespace PDPatcher
{
    public partial class Form1 : Form
    {
        private const string EXENAME = "PDPatcher.exe";

        private Requester m_Requester;
        private ManifestFile m_UpdaterManifest, m_ClientManifest, m_PatchManifest;
        //Files that make up the difference between client's version and patch version.
        private List<PatchFile> m_PatchDiff = new List<PatchFile>();
        private static int m_NumFilesDownloaded = 0;

        //Are we updating this app or the client?
        private static bool m_PatchingUpdater = true;

        private string RelativePath = GlobalSettings.Default.ClientPath;

        public Form1()
        {
            if (File.Exists(RelativePath + "Patcher.manifest"))
                m_UpdaterManifest = new ManifestFile(File.Open(RelativePath + "Patcher.manifest", FileMode.Open));
            else
            {
                MessageBox.Show("Couldn't find patcher's manifest - unable to update!");
                Environment.Exit(0);
            }

            if (File.Exists(RelativePath + "Client.manifest"))
                m_ClientManifest = new ManifestFile(File.Open(RelativePath + "Client.manifest", FileMode.Open));
            else
            {
                MessageBox.Show("Couldn't find client's manifest - unable to update!");
                Environment.Exit(0);
            }

            if (MessageBox.Show("Backup data before updating?", "Backup", MessageBoxButtons.YesNo) == DialogResult.Yes)
                FileManager.Backup(m_ClientManifest, RelativePath);

            InitializeComponent();

            this.FormClosing += new FormClosingEventHandler(Form1_FormClosing);

            m_Requester = new Requester("https://dl.dropboxusercontent.com/u/257809956/UpdaterPatchManifest.manifest");

            m_Requester.OnFetchedManifest += new FetchedManifestDelegate(m_Requester_OnFetchedManifest);
            m_Requester.OnTick += new DownloadTickDelegate(m_Requester_OnTick);
            m_Requester.OnFetchedFile += new FetchedFileDelegate(m_Requester_OnFetchedFile);
            Logger.OnMessageLogged += new MessageLoggedDelegate(Logger_OnMessageLogged);

            m_Requester.Initialize();
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            //Closing the form before an update is done crashes the application.
            //This prevents that...
            e.Cancel = true;
        }

        /// <summary>
        /// Another file was fetched!
        /// </summary>
        /// <param name="FileStream">Stream of the file that was fetched.</param>
        private void m_Requester_OnFetchedFile(MemoryStream MemStream)
        {
            string TmpPath;

            if(!m_PatchingUpdater)
                TmpPath = RelativePath + "Tmp\\" + Path.GetFileName(m_PatchDiff[m_NumFilesDownloaded].Address);
            else
                TmpPath = RelativePath + "Tmp\\" + Path.GetFileNameWithoutExtension(m_PatchDiff[m_NumFilesDownloaded].Address) +
                    " NEW" + Path.GetExtension(m_PatchDiff[m_NumFilesDownloaded].Address);

            using (BinaryWriter Writer = new BinaryWriter(File.Create(TmpPath), Encoding.Default))
            {
                Writer.Write(MemStream.ToArray());
                Writer.Flush();
            }

            //Delete original file...
            if (File.Exists(RelativePath + m_PatchDiff[m_NumFilesDownloaded].Address))
                File.Delete(RelativePath + m_PatchDiff[m_NumFilesDownloaded].Address);

            //...and replace it with the downloaded one!
            FileManager.CreateDirectory(RelativePath + m_PatchDiff[m_NumFilesDownloaded].Address);
            File.Move(TmpPath, RelativePath + m_PatchDiff[m_NumFilesDownloaded].Address);

            if ((m_NumFilesDownloaded + 1) != m_PatchDiff.Count)
            {
                Interlocked.Increment(ref m_NumFilesDownloaded);
                m_Requester.FetchFile(m_PatchDiff[m_NumFilesDownloaded].URL);

                this.Invoke(new MethodInvoker(() =>
                {
                    PrgTotal.Step = (int)((float)(m_PatchDiff.Count * 100) / (float)PrgTotal.Width);
                    PrgTotal.PerformStep();
                    PrgFile.Value = 0;
                }));
            }
            else
            {
                if (!m_PatchingUpdater)
                {
                    MessageBox.Show("Your client is up to date!\n Exiting...");
                    if (File.Exists(RelativePath + "Project Dollhouse Client.exe"))
                        Process.Start(RelativePath + "Project Dollhouse Client.exe");

                    Environment.Exit(0);
                }
                else
                {
                    MessageBox.Show("Done patching updater, please restart from desktop!");

                    //Desktop shortcuts points to new patcher, wich will rename itself when starting (see Program.cs).
                    if (File.Exists(Environment.GetFolderPath(Environment.SpecialFolder.Desktop) + "Project Dollhouse.lnk"))
                    {
                        Program.ModifyShortcut(Environment.GetFolderPath(Environment.SpecialFolder.Desktop) + 
                            "Project Dollhouse.lnk", RelativePath + "PDPatcher NEW.exe");
                    }

                    //TODO: Update start menu shortcut?

                    Environment.Exit(0);
                }
            }
        }

        /// <summary>
        /// The manifest was fetched.
        /// </summary>
        /// <param name="Manifest">The patch manifest that was fetched.</param>
        private void m_Requester_OnFetchedManifest(ManifestFile Manifest)
        {
            m_PatchManifest = Manifest;

            if (!m_PatchingUpdater)
            {
                //Versions didn't match, do update.
                if (m_ClientManifest.Version != m_PatchManifest.Version)
                {
                    foreach (PatchFile clPF in m_ClientManifest.PatchFiles)
                    {
                        foreach (PatchFile pmPF in m_PatchManifest.PatchFiles)
                        {
                            if (NeedToDownloadFile(pmPF, clPF))
                            {
                                if (!m_PatchDiff.Contains(pmPF))
                                    m_PatchDiff.Add(pmPF);
                            }
                        }
                    }

                    this.Invoke(new MethodInvoker(() =>
                    {
                        PrgFile.Value = 0;
                    }));

                    Directory.CreateDirectory(RelativePath + "Tmp");
                    m_Requester.FetchFile(m_PatchDiff[0].URL);
                }
                else
                {
                    MessageBox.Show("Your client is up to date!\n Exiting...");
                    if (File.Exists(RelativePath + "Project Dollhouse Client.exe"))
                        Process.Start(RelativePath + "Project Dollhouse Client.exe");

                    Environment.Exit(0);
                }
            }
            else
            {
                //Versions didn't match, do update.
                if (m_UpdaterManifest.Version != m_PatchManifest.Version)
                {
                    foreach (PatchFile clPF in m_UpdaterManifest.PatchFiles)
                    {
                        foreach (PatchFile pmPF in m_UpdaterManifest.PatchFiles)
                        {
                            if (NeedToDownloadFile(pmPF, clPF))
                            {
                                if (!m_PatchDiff.Contains(pmPF))
                                    m_PatchDiff.Add(pmPF);
                            }
                        }
                    }

                    this.Invoke(new MethodInvoker(() =>
                    {
                        PrgFile.Value = 0;
                    }));

                    Directory.CreateDirectory(RelativePath + "Tmp");
                    m_Requester.FetchFile(m_PatchDiff[0].URL);
                }
                else //Versions matched, no need to update this application!
                {
                    m_PatchingUpdater = false;
                    MessageBox.Show("Patcher was up to date!\n Checking client...");

                    //Start updating client!
                    m_Requester = new Requester("https://dl.dropboxusercontent.com/u/257809956/PatchManifest.manifest");
                    m_Requester.OnFetchedManifest += new FetchedManifestDelegate(m_Requester_OnFetchedManifest);
                    m_Requester.OnTick += new DownloadTickDelegate(m_Requester_OnTick);
                    m_Requester.OnFetchedFile += new FetchedFileDelegate(m_Requester_OnFetchedFile);

                    m_Requester.Initialize();
                }
            }
        }

        /// <summary>
        /// Compare two PatchFile instances to see if a file needs to be downloaded.
        /// </summary>
        /// <param name="Patch">The PatchFile in the server manifest.</param>
        /// <param name="Client">The PatchFile in the client manifest.</param>
        /// <returns>True if the file needed to be downloaded, false otherwise.</returns>
        private bool NeedToDownloadFile(PatchFile Patch, PatchFile Client)
        {
            string PatchName = Path.GetFileName(RelativePath + Patch.Address);
            string ClientName = Path.GetFileName(RelativePath + Patch.Address);

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
            if (LblDownloading.InvokeRequired)
                this.Invoke(new MethodInvoker(() => { LblDownloading.Text = "Downloading: " + State.Response.ResponseUri; }));
            else
                LblDownloading.Text = "Downloading: " + State.Response.ResponseUri;

            if(LblSpeed.InvokeRequired)
                this.Invoke(new MethodInvoker(() => { LblSpeed.Text = State.KBPerSec.ToString() + " KB/Sec"; }));
            else
                LblSpeed.Text = State.KBPerSec.ToString() + "KB / Sec";

            if (PrgFile.InvokeRequired)
            {
                this.Invoke(new MethodInvoker(() =>
                    {
                        PrgFile.Step = (int)(PrgFile.Width * (State.ContentLength / State.PctComplete));
                        PrgFile.PerformStep();
                    }));
            }
            else
            {
                PrgFile.Step = (int)(PrgFile.Width * (State.ContentLength / State.PctComplete));
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