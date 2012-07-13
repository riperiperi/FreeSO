using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using System.IO;
using System.Net;
using System.Threading;
using System.Security.Cryptography;
using ICSharpCode.SharpZipLib.Core;
using ICSharpCode.SharpZipLib.Zip;
using LogThis;
using WOSI.Utilities;

namespace PDPatcher
{
    public partial class Form1 : Form
    {
        private Point m_MouseOffset;
        private ImageList m_BtnExitImgList, m_BtnMinimizeImgList, m_BtnQuitImgList, m_BtnAboutImgList;
        //Address to webserver containing 'patch.php' and 'getmanifest.php', and the
        //gameclient's version (NOT the patchclient's version!)
        private string m_WebAddress = "", m_ClientVersion = "";
        
        //This event is set when all parents have been found, meaning it is safe to start
        //downloading files.
        private ManualResetEvent m_ParentResetEvent = new ManualResetEvent(false);

        //List of the full paths for downloaded manifests, so they can be processed in order.
        private List<string> m_DownloadedManifests = new List<string>();
        
        //List of files that weren't successfully downloaded.
        private List<List<DownloadFile>> m_UnfinishedDownloads = new List<List<DownloadFile>>();

        private double m_PercentDone = 0.0;
        private int m_NumFilesReceived, m_Total = 0;

        public Form1()
        {
            InitializeComponent();

            this.MouseDown += new MouseEventHandler(Form1_MouseDown);
            this.MouseMove += new MouseEventHandler(Form1_MouseMove);

            LblApplicationName.BackColor = Color.Transparent;
            TxtProgressDescription.BackColor = Color.FromArgb(53, 79, 109);
            LblProgressDescription.BackColor = Color.Transparent;
            TxtOverallProgress.BackColor = Color.FromArgb(53, 79, 109);

            Log.UseSensibleDefaults("PDPatcher-log.txt", "", eloglevel.info);

            ConfigureButtons();

            GetManifestAsync();
        }

        /// <summary>
        /// Checks if a specified manifest has a parent.
        /// </summary>
        /// <param name="ManifestPath">The path to the manifest to check.</param>
        /// <returns>The string with the parent manifest. Will be empty if no parent was found.</returns>
        private string CheckForParent(string ManifestPath)
        {
            StreamReader Reader = new StreamReader(File.Open(ManifestPath, FileMode.Open));
            string ReturnStr = Reader.ReadLine().Replace("Parent=", "").Replace("\"", "");
            Reader.Close();

            return ReturnStr;
        }

        /// <summary>
        /// Checks if a specified manifest has a child.
        /// </summary>
        /// <param name="ManifestPath">The path to the manifest to check.</param>
        /// <returns>The string with the child manifest. Will be empty if no child was found.</returns>
        private string CheckForChild(string ManifestPath)
        {
            StreamReader Reader = new StreamReader(File.Open(ManifestPath, FileMode.Open));
            Reader.ReadLine(); //Parent=""
            string ReturnStr = Reader.ReadLine().Replace("Child=", "").Replace("\"", "");
            Reader.Close();

            return ReturnStr;
        }

        /// <summary>
        /// Reads a line of ASCII characters from a file with a BinaryReader instance.
        /// </summary>
        /// <param name="Reader">A BinaryReader instance.</param>
        /// <returns>The line of ASCII characters that was read.</returns>
        private string ReadASCII(ref BinaryReader Reader)
        {
            char[] Buffer = new char[1];
            string Str = "";

            while (true)
            {
                Reader.Read(Buffer, 0, 1);
                Str += Buffer[0];

                if (Buffer[0] == '\n')
                {
                    break;
                }
            }

            return Str;
        }

        /// <summary>
        /// Reads all the files to download from a specific manifest.
        /// </summary>
        /// <param name="ManifestPath">The path to the manifest to read.</param>
        /// <returns>A list of the files to be downloaded.</returns>
        private List<DownloadFile> GetFilesToDownload(string ManifestPath)
        {
            List<DownloadFile> LstFiles = new List<DownloadFile>();
            
            BinaryReader Reader = new BinaryReader(File.Open(ManifestPath, FileMode.Open));
            string Line1 = ReadASCII(ref Reader);
            string Line2 = ReadASCII(ref Reader);
            string NumFilesStr = ReadASCII(ref Reader).Replace("NumFiles=", "").Replace("\r\n", "");
            int NumFiles = int.Parse(NumFilesStr);

            for (int i = 0; i < NumFiles; i++)
            {
                string[] SplitFilename = Reader.ReadString().Split(new string[] { " MD5: " }, 
                    StringSplitOptions.RemoveEmptyEntries);
                
                DownloadFile MFile = new DownloadFile(SplitFilename[0], SplitFilename[1], 
                    Path.GetFileName(ManifestPath).Replace(".manifest", ""));
                LstFiles.Add(MFile);
            }

            Reader.Close();

            return LstFiles;
        }

        /// <summary>
        /// Connects to a webserver and gives the client's version to "patch.php".
        /// This will result in receiving one or two manifests (for incremental
        /// updates).
        /// </summary>
        private void GetManifestAsync()
        {
            try
            {
                //This executable is expected to live inside "The Sims Online\TSOClient\",
                //which is where these files are also expected to live.
                if (!File.Exists("PatchConfig.ini"))
                {
                    if (!File.Exists("ClientVersion.ini"))
                        m_ClientVersion = "0000";
                    else
                    {
                        StreamReader Reader = new StreamReader(File.Open("ClientVersion.ini", FileMode.Open));
                        m_ClientVersion = Reader.ReadLine().Replace("Version: ", "");
                        Reader.Close();
                    }

                    WebRequest Request = WebRequest.Create("http://www.afr0games.com/patch.php?Version=0000");
                    m_WebAddress = "http://www.afr0games.com/";
                    Request.BeginGetResponse(new AsyncCallback(OnGotInitialResponse), Request);
                }
                else
                {
                    StreamReader Reader;

                    if (!File.Exists("ClientVersion.ini"))
                        m_ClientVersion = "0000";
                    else
                    {
                        Reader = new StreamReader(File.Open("ClientVersion.ini", FileMode.Open));
                        m_ClientVersion = Reader.ReadLine().Replace("Version: ", "");
                        Reader.Close();
                    }

                    Reader = new StreamReader(File.Open("PatchConfig.ini", FileMode.Open));
                    m_WebAddress = Reader.ReadLine().Replace("Address: ", "");
                    Reader.Close();

                    WebRequest Request = WebRequest.Create(m_WebAddress + "/patch.php?Version=0000");
                    Request.BeginGetResponse(new AsyncCallback(OnGotInitialResponse), Request);
                }
            }
            catch (Exception E)
            {
                Log.LogThis("Error in GetManifestAsync() \r\n" + E.ToString(), eloglevel.error); 
            }
        }

        /// <summary>
        /// Received an initial response from the webserver, containing either some manifests or
        /// a notice that no new manifests could be found.
        /// </summary>
        private void OnGotInitialResponse(IAsyncResult AR)
        {
            Directory.CreateDirectory("PatcherTmp");

            WebRequest Request = (WebRequest)AR.AsyncState;
            WebResponse Response = Request.EndGetResponse(AR);

            this.Invoke(new MethodInvoker(delegate { LblProgressDescription.Text = "Fetched manifests!"; }));
            Log.LogThis("Fetched initial manifests, checking for more...", eloglevel.info);

            if (Response.Headers["Content-Description"] == "No New Manifest")
            {
                //TODO: Show a messagebox informing the user that no new updates are available.
                MsgBox NoNewUpdatesBox = new MsgBox("No new updates available!");
                NoNewUpdatesBox.ShowDialog();

                return;
            }
            //There were at least two new manifests, so they were zipped together.
            else if (Response.Headers["Content-Description"] == "Zipped File Transfer")
            {
            	ZipInputStream zipInputStream = new ZipInputStream(Response.GetResponseStream());
                ZipEntry ZEntry = zipInputStream.GetNextEntry();

                while (ZEntry != null)
                {
                    string EntryName = Path.Combine("PatcherTmp\\", Path.GetFileName(ZEntry.Name));
                    m_DownloadedManifests.Add(Path.GetFileName(ZEntry.Name));

                    byte[] Buffer = new byte[4096];		// 4K is optimum

                    using (FileStream FStream = File.Create(EntryName))
                    {
                        StreamUtils.Copy(zipInputStream, FStream, Buffer);
                    }

                    ZEntry = zipInputStream.GetNextEntry();
                }

                //Ascending order...
                m_DownloadedManifests.Sort();
                //Reverse to descending!
                m_DownloadedManifests.Reverse();

                string Parent = CheckForParent("PatcherTmp\\" + m_DownloadedManifests[0]);

                if (Parent != "")
                {
                    WebRequest NewRequest = WebRequest.Create(m_WebAddress + "/getmanifest.php?Manifest=" + Parent);
                    Request.BeginGetResponse(new AsyncCallback(OnGotParentResponse), Request);
                }
                else
                    m_ParentResetEvent.Set();

                //Back to ascending order!
                m_DownloadedManifests.Reverse();

                string Child = CheckForChild("PatcherTmp\\" + m_DownloadedManifests[0]);

                if (Child != "")
                {
                    //Only fetch the child from the earliest manifest if the child was newer than the
                    //client's version.
                    if (int.Parse(m_ClientVersion) < int.Parse(Child.Replace(".manifest", "")))
                    {
                        WebRequest NewRequest = WebRequest.Create(m_WebAddress + "/getmanifest.php?Manifest=" + Child);
                        Request.BeginGetResponse(new AsyncCallback(OnGotChildResponse), Request);
                    }
                    else
                    {
                        List<List<DownloadFile>> AllFilesToDownload = new List<List<DownloadFile>>();

                        //Wait to ensure all parents have been found...
                        m_ParentResetEvent.WaitOne();

                        //m_DownloadedManifests has already been sorted in ascending order,
                        //so it should be safe to assume that the lists of files will be
                        //added in ascending order as well.
                        foreach(string Manifest in m_DownloadedManifests)
                            AllFilesToDownload.Add(GetFilesToDownload("PatcherTmp\\" + Manifest));

                        DownloadFiles(AllFilesToDownload);
                    }
                }
                else
                {
                    List<List<DownloadFile>> AllFilesToDownload = new List<List<DownloadFile>>();

                    //Wait to ensure all parents have been found...
                    m_ParentResetEvent.WaitOne();

                    foreach (string Manifest in m_DownloadedManifests)
                        AllFilesToDownload.Add(GetFilesToDownload("PatcherTmp\\" + Manifest));

                    DownloadFiles(AllFilesToDownload);
                }
            }
            //There was only one new manifest.
            else if (Response.Headers["Content-Description"] == "File Transfer")
            {
                BinaryWriter Writer = new BinaryWriter(File.Create("PatcherTmp\\" + 
                    Response.Headers["Content-Disposition"].Replace("attachment; filename=", "")));
                BinaryReader Reader = new BinaryReader(Response.GetResponseStream());

                int ResponseLength = (int)Response.ContentLength;

                for (int i = 0; i < ResponseLength; i++)
                    Writer.Write(Reader.ReadByte());

                Reader.Close();
                Writer.Close();

                string Parent = CheckForParent("PatcherTmp\\" + 
                    Response.Headers["Content-Disposition"].Replace("attachment; filename=", ""));

                if (Parent != "")
                {
                    WebRequest NewRequest = WebRequest.Create(m_WebAddress + "/getmanifest.php?Manifest=" + Parent);
                    Request.BeginGetResponse(new AsyncCallback(OnGotParentResponse), Request);
                }
            }
        }

        /// <summary>
        /// 'getmanifest.php' sent a requested parent manifest, so store it and check
        /// if it has a parent.
        /// </summary>
        private void OnGotParentResponse(IAsyncResult AR)
        {
            WebRequest Request = (WebRequest)AR.AsyncState;
            WebResponse Response = Request.EndGetResponse(AR);

            this.Invoke(new MethodInvoker(delegate { LblProgressDescription.Text = "Fetched a parent manifest!"; }));
            Log.LogThis("Found a parent manifest, checking for more...", eloglevel.info);

            BinaryWriter Writer = new BinaryWriter(File.Create("PatcherTmp\\" + 
                Response.Headers["Content-Disposition"].Replace("attachment; filename=", "")));
            BinaryReader Reader = new BinaryReader(Response.GetResponseStream());

            int ResponseLength = (int)Response.ContentLength;

            for (int i = 0; i < ResponseLength; i++)
                Writer.Write(Reader.ReadByte());

            Reader.Close();
            Writer.Close();

            m_DownloadedManifests.Add("PatcherTmp\\" +
                Response.Headers["Content-Disposition"].Replace("attachment; filename=", ""));

            string Parent = CheckForParent("PatcherTmp\\" +
                Response.Headers["Content-Disposition"].Replace("attachment; filename=", ""));

            if (Parent != "")
            {
                WebRequest NewRequest = WebRequest.Create(m_WebAddress + "/getmanifest.php?Manifest=" + Parent);
                Request.BeginGetResponse(new AsyncCallback(OnGotParentResponse), Request);
            }
            else
                m_ParentResetEvent.Set();
        }

        /// <summary>
        /// 'getmanifest.php' sent a requested child manifest, so store it and check if 
        /// it has a child that is newer than the gameclient's version.
        /// </summary>
        private void OnGotChildResponse(IAsyncResult AR)
        {
            WebRequest Request = (WebRequest)AR.AsyncState;
            WebResponse Response = Request.EndGetResponse(AR);

            this.Invoke(new MethodInvoker(delegate { LblProgressDescription.Text = "Fetched a child manifest!"; }));
            Log.LogThis("Found a child manifest, checking for more...", eloglevel.info);

            BinaryWriter Writer = new BinaryWriter(File.Create("PatcherTmp\\" +
                Response.Headers["Content-Disposition"].Replace("attachment; filename=", "")));
            BinaryReader Reader = new BinaryReader(Response.GetResponseStream());

            int ResponseLength = (int)Response.ContentLength;

            for (int i = 0; i < ResponseLength; i++)
                Writer.Write(Reader.ReadByte());

            Reader.Close();
            Writer.Close();

            //Not sure if "attachment; filename=" is part of the header, but replace it
            //just in case...
            m_DownloadedManifests.Add("PatcherTmp\\" +
                Response.Headers["Content-Disposition"].Replace("attachment; filename=", ""));

            string Child = CheckForChild("PatcherTmp\\" +
                Response.Headers["Content-Disposition"].Replace("attachment; filename=", ""));

            if (Child != "")
            {
                //Only fetch the child from the earliest manifest if the child was newer than the
                //client's version.
                if (int.Parse(m_ClientVersion) < int.Parse(Child.Replace(".manifest", "")))
                {
                    WebRequest NewRequest = WebRequest.Create(m_WebAddress + "/getmanifest.php?Manifest=" + Child);
                    Request.BeginGetResponse(new AsyncCallback(OnGotChildResponse), Request);
                }
                else
                {
                    List<List<DownloadFile>> AllFilesToDownload = new List<List<DownloadFile>>();

                    //Wait to ensure all parents have been found...
                    m_ParentResetEvent.WaitOne();

                    foreach (string Manifest in m_DownloadedManifests)
                        AllFilesToDownload.Add(GetFilesToDownload("PatcherTmp\\" + Manifest));

                    DownloadFiles(AllFilesToDownload);
                }
            }
            else
            {
                List<List<DownloadFile>> AllFilesToDownload = new List<List<DownloadFile>>();

                //Wait to ensure all parents have been found...
                m_ParentResetEvent.WaitOne();

                foreach (string Manifest in m_DownloadedManifests)
                    AllFilesToDownload.Add(GetFilesToDownload("PatcherTmp\\" + Manifest));

                DownloadFiles(AllFilesToDownload);
            }
        }

        /// <summary>
        /// Downloads all files from all manifests and applies them in order.
        /// </summary>
        /// <param name="AllFilesToDownload">A ragged list of files to download.</param>
        private void DownloadFiles(List<List<DownloadFile>> AllFilesToDownload)
        {
            this.Invoke(new MethodInvoker(delegate { LblProgressDescription.Text = "Finished fetching manifests!"; }));
            Log.LogThis("Finished fetching manifests, downloading files...", eloglevel.info);

            //Update the total percentage, so it is possible to calculate the
            //total progress.
            foreach(List<DownloadFile> FileList in AllFilesToDownload)
            {
                foreach(DownloadFile DFile in FileList)
                    m_Total++;
            }

            HttpWebRequest Request = (HttpWebRequest)WebRequest.Create(m_WebAddress + "/patches/" +
                AllFilesToDownload[0][0].ManifestVersion + "/" + Path.GetFileName(AllFilesToDownload[0][0].VirtualPath));
            Request.BeginGetResponse(OnDownloadResponse, new DownloadAsyncObject(AllFilesToDownload,
                Request));
        }

        /// <summary>
        /// Invoked when a response was received from the webserver for a request to download a file.
        /// </summary>
        private void OnDownloadResponse(IAsyncResult AR)
        {
            DownloadAsyncObject AsyncObject = (DownloadAsyncObject)AR.AsyncState;
            HttpWebResponse Response = (HttpWebResponse)AsyncObject.Request.EndGetResponse(AR);

            this.Invoke(new MethodInvoker(delegate { LblProgressDescription.Text = "Downloaded: " + 
                Path.GetFileName(AsyncObject.FilesToDownload[AsyncObject.CurrentManifest]
                [AsyncObject.CurrentFile].VirtualPath); }));

            m_NumFilesReceived++;
            m_PercentDone = ((double)m_NumFilesReceived / m_Total) * 100.0;

            this.Invoke(new MethodInvoker(delegate
            {
                TxtOverallProgress.Text = "  Overall progress: " + m_PercentDone.ToString() + "%"; 
            }));

            //File didn't exist, so go right ahead and create it!
            if (!File.Exists(AsyncObject.FilesToDownload[AsyncObject.CurrentManifest][AsyncObject.CurrentFile].VirtualPath))
            {
                string Filename = Path.GetFileName(AsyncObject.FilesToDownload[AsyncObject.CurrentManifest]
                    [AsyncObject.CurrentFile].VirtualPath);
                string Extension = Path.GetExtension(Filename);

                //Make sure the file has a parent directory before attempting to create it...
                if (Path.GetDirectoryName(AsyncObject.FilesToDownload[AsyncObject.CurrentManifest]
                    [AsyncObject.CurrentFile].VirtualPath) != "")
                {
                    Directory.CreateDirectory(AsyncObject.FilesToDownload[AsyncObject.CurrentManifest]
                        [AsyncObject.CurrentFile].VirtualPath.Replace(Filename, ""));
                }

                BinaryWriter Writer = new BinaryWriter(File.Create(AsyncObject.FilesToDownload[AsyncObject.CurrentManifest]
                    [AsyncObject.CurrentFile].VirtualPath));
                BinaryReader Reader = new BinaryReader(Response.GetResponseStream());

                int ResponseLength = (int)Response.ContentLength;

                for (int i = 0; i < ResponseLength; i++)
                    Writer.Write(Reader.ReadByte());

                Writer.Flush();
                Reader.Close();
                Writer.Close();

                AsyncObject.CurrentFile++;
            }
            //The file downloaded already existed, so make a backup of the old one
            //before writing the new file...
            else
            {
                string Extension = Path.GetExtension(AsyncObject.FilesToDownload[AsyncObject.CurrentManifest]
                    [AsyncObject.CurrentFile].VirtualPath);

                if (!File.Exists(AsyncObject.FilesToDownload[AsyncObject.CurrentManifest][AsyncObject.CurrentFile].
                    VirtualPath.Replace(Extension, ".backup")))
                {
                    File.Move(AsyncObject.FilesToDownload[AsyncObject.CurrentManifest][AsyncObject.CurrentFile].VirtualPath,
                        AsyncObject.FilesToDownload[AsyncObject.CurrentManifest][AsyncObject.CurrentFile].VirtualPath.Replace(
                        Extension, ".backup"));
                }
                //A backup already existed from an update applied earlier, so move this backup into another file.
                else
                {
                    File.Move(AsyncObject.FilesToDownload[AsyncObject.CurrentManifest][AsyncObject.CurrentFile].VirtualPath,
                        AsyncObject.FilesToDownload[AsyncObject.CurrentManifest][AsyncObject.CurrentFile].VirtualPath.
                        Replace(Extension, ".backup2"));
                }

                BinaryWriter Writer = new BinaryWriter(File.Create(AsyncObject.FilesToDownload[AsyncObject.CurrentManifest]
                    [AsyncObject.CurrentFile].VirtualPath));
                BinaryReader Reader = new BinaryReader(Response.GetResponseStream());

                int ResponseLength = (int)Response.ContentLength;

                for (int i = 0; i < ResponseLength; i++)
                    Writer.Write(Reader.ReadByte());

                Writer.Flush();
                Reader.Close();
                Writer.Close();

                AsyncObject.CurrentFile++;
            }

            //Are there any more files left to download in this manifest?
            if (AsyncObject.CurrentFile < AsyncObject.FilesToDownload[AsyncObject.CurrentManifest].Count)
            {
                Log.LogThis("Downloading: " + AsyncObject.FilesToDownload[AsyncObject.CurrentManifest]
                    [AsyncObject.CurrentFile].VirtualPath, eloglevel.info);
                string ManifestVersion = AsyncObject.FilesToDownload[AsyncObject.CurrentManifest]
                    [AsyncObject.CurrentFile].ManifestVersion;

                HttpWebRequest Request = (HttpWebRequest)WebRequest.Create(m_WebAddress + "/patches/" +
                     ManifestVersion + "/"  + Path.GetFileName(AsyncObject.FilesToDownload[AsyncObject.CurrentManifest]
                    [AsyncObject.CurrentFile].VirtualPath));
                AsyncObject.Request = Request;
                Request.BeginGetResponse(OnDownloadResponse, AsyncObject);
            }
            else
            {
                //Increase this here, so the if-check below doesn't fail.
                AsyncObject.CurrentManifest++;

                if (AsyncObject.CurrentManifest < AsyncObject.FilesToDownload.Count)
                {
                    AsyncObject.CurrentFile = 0;

                    Log.LogThis("Finished downloading all files in a manifest, checking integrity...", eloglevel.info);

                    m_UnfinishedDownloads.Add(new List<DownloadFile>());

                    foreach (DownloadFile DFile in AsyncObject.FilesToDownload[AsyncObject.CurrentManifest - 1])
                    {
                        MD5CryptoServiceProvider MD5Crypto = new MD5CryptoServiceProvider();
                        byte[] DataBuf;

                        this.Invoke(new MethodInvoker(delegate
                        {
                            LblProgressDescription.Text = "Checking integrity for: " +
                                Path.GetFileName(DFile.VirtualPath);
                        }));

                        BinaryReader Reader = new BinaryReader(File.Open(DFile.VirtualPath, FileMode.Open));
                        DataBuf = Reader.ReadBytes((int)Reader.BaseStream.Length);
                        Reader.Close();

                        if (!DFile.VirtualPath.Contains(".log") || !DFile.VirtualPath.Contains(".txt")
                            || !DFile.VirtualPath.Contains(".h") || !DFile.VirtualPath.Contains(".xml"))
                        {
                            byte[] DownloadChecksum = MD5Crypto.ComputeHash(DataBuf);

                            //Found a file that was incorrectly downloaded!
                            if (DFile.Checksum.TrimEnd(new char[] { '\r', '\n' }) !=
                                Encoding.ASCII.GetString(DownloadChecksum))
                            {
                                Log.LogThis("Found incorrectly downloaded file: " + DFile.VirtualPath, eloglevel.warn);
                                m_UnfinishedDownloads[AsyncObject.CurrentManifest - 1].Add(DFile);
                            }
                        }
                        else
                        {
                            //Found a file that was incorrectly downloaded!
                            if (DFile.Checksum.TrimEnd(new char[] { '\r', '\n' }) !=
                                CryptoUtils.CreateASCIIMD5Hash(Encoding.ASCII.GetString(DataBuf)))
                            {
                                Log.LogThis("Found incorrectly downloaded file: " + DFile.VirtualPath, eloglevel.warn);
                                m_UnfinishedDownloads[AsyncObject.CurrentManifest - 1].Add(DFile);
                            }
                        }
                    }

                    if (AsyncObject.CurrentManifest < AsyncObject.FilesToDownload.Count)
                    {
                        Log.LogThis("Downloading: " + AsyncObject.FilesToDownload[AsyncObject.CurrentManifest - 1]
                            [AsyncObject.CurrentFile].VirtualPath, eloglevel.info);
                        string ManifestVersion = AsyncObject.FilesToDownload[AsyncObject.CurrentManifest - 1]
                            [AsyncObject.CurrentFile].ManifestVersion;

                        HttpWebRequest Request = (HttpWebRequest)WebRequest.Create(m_WebAddress + "/patches/" +
                             ManifestVersion + "/" + Path.GetFileName(
                             AsyncObject.FilesToDownload[AsyncObject.CurrentManifest - 1]
                             [AsyncObject.CurrentFile].VirtualPath));
                        AsyncObject.Request = Request;
                        Request.BeginGetResponse(OnDownloadResponse, AsyncObject);
                    }
                    else //All files in all manifests have been downloaded!
                    {
                        bool AskUser = false;

                        //Were there any unfinished downloads?
                        foreach (List<DownloadFile> DFileList in m_UnfinishedDownloads)
                        {
                            if (DFileList.Count > 0)
                            {
                                AskUser = true;
                                break;
                            }
                        }

                        if (AskUser)
                        {
                            MsgBox Message = new MsgBox();

                            if (Message.ShowDialog() == DialogResult.Yes)
                            {
                                //Delete incorrectly downloaded files before redownloading.
                                foreach (List<DownloadFile> FileList in m_UnfinishedDownloads)
                                {
                                    foreach (DownloadFile DFile in FileList)
                                        File.Delete(DFile.VirtualPath);
                                }

                                DownloadFiles(m_UnfinishedDownloads);
                            }
                            else
                            {
                                this.Invoke(new MethodInvoker(delegate
                                {
                                    LblProgressDescription.Text = "Rolling back update...";
                                }));

                                RollBackUpdate(AsyncObject.FilesToDownload);
                            }
                        }
                        else //Done!
                        {
                            if (File.Exists("ClientVersion.ini"))
                                File.Delete("ClientVersion.ini");

                            StreamWriter Writer = new StreamWriter(File.Create("ClientVersion.ini"));
                            Writer.WriteLine("Version: " + AsyncObject.FilesToDownload[AsyncObject.CurrentManifest - 1]
                                [AsyncObject.CurrentFile].ManifestVersion);
                            Writer.Close();

                            this.Invoke(new MethodInvoker(delegate
                            {
                                LblProgressDescription.Text = "Finished downloading and applying all updates!";
                            }));

                            Log.LogThis("Done!", eloglevel.info);
                        }
                    }
                }
                else //All files in all manifests have been downloaded!
                {
                    bool AskUser = false;

                    //Were there any unfinished downloads?
                    foreach(List<DownloadFile> DFileList in m_UnfinishedDownloads)
                    {
                        if (DFileList.Count > 0)
                        {
                            AskUser = true;
                            break;
                        }
                    }

                    if (AskUser)
                    {
                        MsgBox Message = new MsgBox();

                        if (Message.ShowDialog() == DialogResult.Yes)
                        {
                            //Delete incorrectly downloaded files before redownloading.
                            foreach (List<DownloadFile> FileList in m_UnfinishedDownloads)
                            {
                                foreach (DownloadFile DFile in FileList)
                                    File.Delete(DFile.VirtualPath);
                            }

                            DownloadFiles(m_UnfinishedDownloads);
                        }
                        else
                        {
                            this.Invoke(new MethodInvoker(delegate
                            {
                                LblProgressDescription.Text = "Rolling back update...";
                            }));

                            RollBackUpdate(AsyncObject.FilesToDownload);
                        }
                    }
                    else //Done!
                    {
                        if (File.Exists("ClientVersion.ini"))
                            File.Delete("ClientVersion.ini");

                        StreamWriter Writer = new StreamWriter(File.Create("ClientVersion.ini"));
                        Writer.WriteLine("Version: " + AsyncObject.FilesToDownload[AsyncObject.CurrentManifest - 1]
                            [AsyncObject.CurrentFile].ManifestVersion);
                        Writer.Close();

                        this.Invoke(new MethodInvoker(delegate
                        {
                            LblProgressDescription.Text = "Finished downloading and applying all updates!";
                        }));

                        Log.LogThis("Done!", eloglevel.info);
                    }
                }
            }
        }

        /// <summary>
        /// Rolls back downloaded update(s).
        /// </summary>
        /// <param name="DownloadedFiles">A list of files that were downloaded.</param>
        private void RollBackUpdate(List<List<DownloadFile>> DownloadedFiles)
        {
            try
            {
                foreach (List<DownloadFile> Manifest in DownloadedFiles)
                {
                    //Delete all downloaded files and replace 'em with their backup
                    //counterpart if one exists.
                    foreach (DownloadFile DFile in Manifest)
                    {
                        File.Delete(DFile.VirtualPath);

                        string Extension = Path.GetExtension(DFile.VirtualPath);

                        if (File.Exists(DFile.VirtualPath.Replace(Extension, ".backup")))
                        {
                            File.Move(DFile.VirtualPath.Replace(Extension, ".backup"),
                                DFile.VirtualPath);
                        }

                        if (File.Exists(DFile.VirtualPath.Replace(Extension, ".backup2")))
                            File.Exists(DFile.VirtualPath.Replace(Extension, ".backup2"));
                    }
                }
            }
            catch (Exception E)
            {
                Log.LogThis("Error in RollbackUpdate(): \r\n" + E.ToString(), eloglevel.error);
            }
        }

        private void m_Client_NetworkError(string ErrorDescription)
        {
            this.Invoke(new MethodInvoker(delegate { LblProgressDescription.Text = ErrorDescription; }));
        }

        #region Buttons

        /// <summary>
        /// Configures the properties needed to display the buttons properly (which are actually ImageBoxes).
        /// </summary>
        private void ConfigureButtons()
        {
            m_BtnExitImgList = new ImageList();
            m_BtnExitImgList.ImageSize = new Size(16, 16);

            Bitmap ExitBitmap = Properties.Resources._2ab5bffa_Patcher_XCloseBtn;
            ExitBitmap.MakeTransparent(Color.FromArgb(255, 0, 255));

            m_BtnExitImgList.Images.AddStrip(ExitBitmap);

            BtnExit.BackColor = Color.Transparent;
            BtnExit.Image = m_BtnExitImgList.Images[1];

            BtnExit.MouseEnter += new EventHandler(BtnExit_MouseEnter);
            BtnExit.MouseLeave += new EventHandler(BtnExit_MouseLeave);
            BtnExit.Click += new EventHandler(BtnExit_Click);

            m_BtnMinimizeImgList = new ImageList();
            m_BtnMinimizeImgList.ImageSize = new Size(16, 16);

            Bitmap MinimizeBitmap = Properties.Resources._2ab5bffb_Patcher_MinimizeBtn;
            MinimizeBitmap.MakeTransparent(Color.FromArgb(255, 0, 255));

            m_BtnMinimizeImgList.Images.AddStrip(MinimizeBitmap);

            BtnMinimize.BackColor = Color.Transparent;
            BtnMinimize.Image = m_BtnMinimizeImgList.Images[1];

            BtnMinimize.MouseEnter += new EventHandler(BtnMinimize_MouseEnter);
            BtnMinimize.MouseLeave += new EventHandler(BtnMinimize_MouseLeave);
            BtnMinimize.Click += new EventHandler(BtnMinimize_Click);

            m_BtnQuitImgList = new ImageList();
            m_BtnQuitImgList.ImageSize = new Size(64, 34);

            Bitmap BtnQuitBitmap = Properties.Resources.e2b66db8GZBtn;
            BtnQuitBitmap.MakeTransparent(Color.FromArgb(255, 0, 255));
            m_BtnQuitImgList.Images.AddStrip(BtnQuitBitmap);

            BtnQuit.BackColor = Color.Transparent;
            BtnQuit.Image = m_BtnQuitImgList.Images[1];
            BtnQuit.FlatStyle = FlatStyle.Flat;
            BtnQuit.FlatAppearance.BorderSize = 0;
            BtnQuit.FlatAppearance.MouseOverBackColor = Color.Transparent;
            BtnQuit.FlatAppearance.MouseDownBackColor = Color.Transparent;

            BtnQuit.MouseEnter += new EventHandler(BtnQuit_MouseEnter);
            BtnQuit.MouseLeave += new EventHandler(BtnQuit_MouseLeave);
            BtnQuit.Click += new EventHandler(BtnQuit_Click);

            m_BtnAboutImgList = new ImageList();
            m_BtnAboutImgList.ImageSize = new Size(64, 34);

            Bitmap BtnAboutBitmap = Properties.Resources.e2b66db8GZBtn;
            BtnAboutBitmap.MakeTransparent(Color.FromArgb(255, 0, 255));
            m_BtnAboutImgList.Images.AddStrip(BtnAboutBitmap);

            BtnAbout.BackColor = Color.Transparent;
            BtnAbout.Image = m_BtnAboutImgList.Images[1];
            BtnAbout.FlatStyle = FlatStyle.Flat;
            BtnAbout.FlatAppearance.BorderSize = 0;
            BtnAbout.FlatAppearance.MouseOverBackColor = Color.Transparent;
            BtnAbout.FlatAppearance.MouseDownBackColor = Color.Transparent;

            BtnAbout.MouseEnter += new EventHandler(BtnAbout_MouseEnter);
            BtnAbout.MouseLeave += new EventHandler(BtnAbout_MouseLeave);
            BtnAbout.Click += new EventHandler(BtnAbout_Click);
        }

        private void BtnAbout_Click(object sender, EventArgs e)
        {
            MsgBox AboutBox = new MsgBox(false);

            AboutBox.Show(this);
            AboutBox.Location = new Point(300, 200);
        }

        private void BtnAbout_MouseEnter(object sender, EventArgs e)
        {
            BtnAbout.Image = m_BtnAboutImgList.Images[2];
        }

        private void BtnAbout_MouseLeave(object sender, EventArgs e)
        {
            BtnAbout.Image = m_BtnAboutImgList.Images[1];
        }

        private void BtnQuit_Click(object sender, EventArgs e)
        {
            MsgBox QuitBox = new MsgBox(true);
            QuitBox.Show(this);
            QuitBox.Location = new Point(300, 200);
        }

        private void BtnQuit_MouseEnter(object sender, EventArgs e)
        {
            BtnQuit.Image = m_BtnQuitImgList.Images[2];
        }

        private void BtnQuit_MouseLeave(object sender, EventArgs e)
        {
            BtnQuit.Image = m_BtnQuitImgList.Images[1];
        }

        private void BtnMinimize_MouseEnter(object sender, EventArgs e)
        {
            BtnMinimize.Image = m_BtnMinimizeImgList.Images[2];
        }

        private void BtnMinimize_MouseLeave(object sender, EventArgs e)
        {
            BtnMinimize.Image = m_BtnMinimizeImgList.Images[1];
        }

        private void BtnMinimize_Click(object sender, EventArgs e)
        {
            BtnMinimize.Image = m_BtnExitImgList.Images[3];
            this.WindowState = FormWindowState.Minimized;
        }

        private void BtnExit_Click(object sender, EventArgs e)
        {
            Application.Exit();
        }

        private void BtnExit_MouseEnter(object sender, EventArgs e)
        {
            BtnExit.Image = m_BtnExitImgList.Images[2];
        }

        private void BtnExit_MouseLeave(object sender, EventArgs e)
        {
            BtnExit.Image = m_BtnExitImgList.Images[1];
        }

        /// <summary>
        /// The form was informed that the user moved the mouse.
        /// </summary>
        private void Form1_MouseMove(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                Point MousePosition = Control.MousePosition;
                MousePosition.Offset(m_MouseOffset);
                this.Location = MousePosition;
            }
        }

        /// <summary>
        /// A mousebutton was pressed down while the mouse was within
        /// the region of this form.
        /// </summary>
        private void Form1_MouseDown(object sender, MouseEventArgs e)
        {
            m_MouseOffset = new Point(-e.X, -e.Y);
        }

        #endregion
    }
}
