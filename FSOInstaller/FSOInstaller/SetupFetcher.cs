using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace FSOInstaller
{
    public partial class SetupFetcher : Form
    {
        public static string TSOLocation = "http://largedownloads.ea.com/pub/misc/tso/";

        private bool IsManifest;
        private delegate void TransferCompleteDelegate(byte[] data);
        private Queue<TSOManifestEntry> SetupFiles = new Queue<TSOManifestEntry>();
        private int TotalFiles;
        private TSOManifestEntry CurrentFile;

        public SetupFetcher()
        {
            InitializeComponent();
        }

        private void SetupFetcher_Load(object sender, EventArgs e)
        {
            // Start by downloading the manifest.

            IsManifest = true;
            if (!Directory.Exists("Packed/")) Directory.CreateDirectory("Packed/");

            TotalFiles = 1;
            DownloadFile("manifest.txt");
        }

        private void DownloadFile(string filename)
        {
            StatusLabel.Text = (filename == "manifest.txt") ? "Downloading Manifest..." : filename;

            var webClient = new WebClient();
            webClient.Credentials = new NetworkCredential("anonymous", "");
            webClient.DownloadDataCompleted += new DownloadDataCompletedEventHandler(FTPDownloadCompleted);
            webClient.DownloadProgressChanged += new DownloadProgressChangedEventHandler(FTPDownloadProgressChanged);
            webClient.DownloadDataAsync(new Uri(TSOLocation + filename));
        }

        private void HandleCurrentFile(byte[] data)
        {
            if (IsManifest)
            {
                var Manifest = new TSOManifest(Encoding.Default.GetString(data));
                SetupFiles.Clear();
                foreach (var entry in Manifest.Entries)
                {
                    if (!entry.Filename.EndsWith(".cab")) SetupFiles.Enqueue(entry);
                }
                IsManifest = false;
                TotalFiles = SetupFiles.Count;
            }
            else
            {
                var destPath = "Packed/" + CurrentFile.Filename;
                var dir = Path.GetDirectoryName(destPath);
                if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
                using (var targetFile = File.Create(destPath))
                {
                    targetFile.Write(data, 0, data.Length);
                }
            }
        }

        private void FTPDownloadProgressChanged(object sender, DownloadProgressChangedEventArgs e)
        {
            var progress = (TotalFiles + e.ProgressPercentage/100.0 - (SetupFiles.Count + 1.0)) / TotalFiles;
            ProgressBar.Value = (int)(progress*100);
        }

        private void FTPDownloadCompleted(object sender, DownloadDataCompletedEventArgs e)
        {
            HandleCurrentFile(e.Result);
            if (SetupFiles.Count > 0)
            {
                var file = SetupFiles.Dequeue();
                CurrentFile = file;
                DownloadFile(file.Filename);
            } else
            {
                MessageBox.Show("Done!");
            }
        }
    }
}
