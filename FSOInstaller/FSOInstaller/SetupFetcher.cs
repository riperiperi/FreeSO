using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
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

        public SetupFetcher()
        {
            InitializeComponent();
        }

        private void SetupFetcher_Load(object sender, EventArgs e)
        {
            // Start by downloading the manifest.

            var webClient = new WebClient();
            webClient.Credentials = new NetworkCredential("anonymous", "");
            webClient.DownloadDataCompleted += new DownloadDataCompletedEventHandler(FTPDownloadCompleted);
            webClient.DownloadProgressChanged += new DownloadProgressChangedEventHandler(FTPDownloadProgressChanged);
            webClient.DownloadDataAsync(new Uri(TSOLocation+"manifest.txt"));

        }

        private void HandleCurrentFile(byte[] data)
        {
            if (IsManifest)
            {
                var Manifes
            }
        }

        private void FTPDownloadProgressChanged(object sender, DownloadProgressChangedEventArgs e)
        {
            ProgressBar.Value = e.ProgressPercentage;
        }

        private void FTPDownloadCompleted(object sender, DownloadDataCompletedEventArgs e)
        {
            Invoke(HandleCurrentFile, new object[] { });
            
        }

        private void Invoke(Action<byte[]> handleCurrentFile, object[] v)
        {
            throw new NotImplementedException();
        }
    }
}
