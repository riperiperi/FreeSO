using Foundation;
using System;
using System.ComponentModel;
using System.IO;
using System.IO.Compression;
using System.Net;
using System.Threading;
using UIKit;

namespace FSOiOS
{
    public partial class FSOInstallViewController : UIViewController
    {
        private WebClient DownloadClient;
        public event Action OnInstalled;
        private bool ReDownload = false;

        public FSOInstallViewController (IntPtr handle) : base (handle)
        {
        }

        public override void ViewDidLoad()
        {
            IPEntry.ShouldReturn += (textField) =>
            {
                textField.ResignFirstResponder();
                return true;
            };

            var g = new UITapGestureRecognizer(() => View.EndEditing(true));
            View.AddGestureRecognizer(g);
            
            UIAlertView _error = new UIAlertView("Welcome!", "To run FreeSO on iOS, you must transfer the TSO game files into this app. For instructions, see the forums.", null, "Ok", null);
            _error.Show();
        }

        private void ResetDownloader()
        {
            StatusText.Text = "Enter a location to download TSO files from.";
            StatusProgress.Progress = 0f;
            IpConfirm.Enabled = true;
            IPEntry.Enabled = true;
            try
            {
                File.Delete(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "The Sims Online.zip"));
            }
            catch (Exception e) { }
            ReDownload = true;
            if (DownloadClient != null) DownloadClient.Dispose();
        }

        partial void IpConfirm_TouchUpInside(UIButton sender)
        {
            IpConfirm.Enabled = false;
            IPEntry.Enabled = false;

            var url = "http://"+IPEntry.Text+"/The%20Sims%20Online.zip";
            var dest = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "The Sims Online.zip");

            if (ReDownload || !File.Exists(dest))
            {
                DownloadClient = new WebClient();
                DownloadClient.DownloadProgressChanged += Client_DownloadProgressChanged;
                DownloadClient.DownloadFileCompleted += Client_DownloadFileCompleted;
                DownloadClient.DownloadFileAsync(new Uri(url), dest);
            } else
            {
                Client_DownloadFileCompleted(DownloadClient, new AsyncCompletedEventArgs(null, false, null));
            }
        }

        private void Client_DownloadFileCompleted(object sender, System.ComponentModel.AsyncCompletedEventArgs e)
        {
            if (e.Error != null || e.Cancelled)
            {
                InvokeOnMainThread(() =>
                {
                    UIAlertView _error = new UIAlertView("An error occurred", "An error occurred during your download. Please try again,"
                    + " and make sure the connection to your PC is stable!", null, "Ok", null);

                    _error.Show();
                    ResetDownloader();
                });
            } else
            {
                InvokeOnMainThread(() =>
                {
                    StatusText.Text = "Extracting TSO Files...";
                });
                (new Thread(() =>
                {
                    //try zip extract
                    string zipPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "The Sims Online.zip");
                    Directory.CreateDirectory(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "The Sims Online/"));
                    string extractPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "The Sims Online/");

                    try
                    {
                        ZipFile.ExtractToDirectory(zipPath, extractPath);
                    }
                    catch (Exception ex)
                    {
                        InvokeOnMainThread(() =>
                        {
                            UIAlertView _error = new UIAlertView("An error occurred", "Fatal error occurred during zip extraction. " + ex.ToString(), null, "Ok", null);
                            _error.Show();
                            ResetDownloader();
                            return;
                        });
                    }
                    InvokeOnMainThread(() =>
                    {
                        OnInstalled?.Invoke();
                    });
                })).Start();
            }
        }

        private void Client_DownloadProgressChanged(object sender, DownloadProgressChangedEventArgs e)
        {
            InvokeOnMainThread(() =>
            {
                StatusText.Text = "Downloading TSO Files... (" + e.ProgressPercentage + "%)";
                StatusProgress.Progress = e.ProgressPercentage / 100f;
            });
        }
    }
}