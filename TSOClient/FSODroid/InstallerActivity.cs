using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using Android.Content.PM;
using System.Net;
using System.IO;
using System.ComponentModel;
using Java.Util.Zip;
using System.Threading;

namespace FSODroid.Resources.Layout
{
    [Activity(Label = "FreeSO Installer"
        , Icon = "@drawable/icon"
        , AlwaysRetainTaskState = true
        , LaunchMode = Android.Content.PM.LaunchMode.SingleInstance
        , ScreenOrientation = ScreenOrientation.SensorLandscape
        , ConfigurationChanges = ConfigChanges.Orientation | ConfigChanges.Keyboard | ConfigChanges.KeyboardHidden | ConfigChanges.ScreenSize)]
    public class InstallerActivity : Activity
    {
        private EditText IPText;
        private Button IPButton;
        private TextView StatusText;
        private ProgressBar StatusBar;

        private WebClient DownloadClient;
        public event Action OnInstalled;
        private bool ReDownload = false;

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            SetContentView(Resource.Layout.Installer);

            // Create your application here
            IPText = FindViewById<EditText>(Resource.Id.IPText);
            IPButton = FindViewById<Button>(Resource.Id.IPButton);
            StatusText = FindViewById<TextView>(Resource.Id.StatusText);
            StatusBar = FindViewById<ProgressBar>(Resource.Id.StatusBar);

            IPButton.Click += IPButton_Click;
        }

        private void IPButton_Click(object sender, EventArgs e)
        {
            IPButton.Enabled = false;
            IPText.Enabled = false;

            var url = "http://" + IPText.Text + "/The%20Sims%20Online.zip";
            var dest = Path.Combine(System.Environment.GetFolderPath(System.Environment.SpecialFolder.MyDocuments), "The Sims Online.zip");

            if (ReDownload || !File.Exists(dest))
            {
                DownloadClient = new WebClient();
                DownloadClient.DownloadProgressChanged += Client_DownloadProgressChanged;
                DownloadClient.DownloadFileCompleted += Client_DownloadFileCompleted;
                DownloadClient.DownloadFileAsync(new Uri(url), dest);
            }
            else
            {
                Client_DownloadFileCompleted(DownloadClient, new AsyncCompletedEventArgs(null, false, null));
            }
        }

        private void Client_DownloadFileCompleted(object sender, System.ComponentModel.AsyncCompletedEventArgs e)
        {
            if (e.Error != null || e.Cancelled)
            {
                ShowAlert("Error downloading files.", "Something went wrong while downloading the game files! "+e.Error?.ToString()??"(cancelled)");
                ResetDownloader();
            }
            else
            {
                DownloadClient.Dispose();
                RunOnUiThread(() =>
                {
                    StatusText.Text = "Extracting TSO Files...";
                });
                //try zip extract
                string zipPath = Path.Combine(System.Environment.GetFolderPath(System.Environment.SpecialFolder.MyDocuments), "The Sims Online.zip");
                Directory.CreateDirectory(Path.Combine(System.Environment.GetFolderPath(System.Environment.SpecialFolder.MyDocuments), "The Sims Online/"));
                string extractPath = Path.Combine(System.Environment.GetFolderPath(System.Environment.SpecialFolder.MyDocuments), "The Sims Online/");

                var thread = new Thread(() =>
                {
                    try
                    {
                        var decomp = new ZipManager.Decompress(zipPath, extractPath);
                        decomp.OnContinue += Decomp_OnContinue;
                        decomp.UnZip();
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(ex.ToString());
                        ShowAlert("Error decompressing files.", "The zip downloaded to the device was invalid. Please try again.");
                        ResetDownloader();
                        return;
                    }
                    RunOnUiThread(() =>
                    {
                        var activity2 = new Intent(this, typeof(FSOActivity));
                        StartActivity(activity2);
                    });
                });
                thread.Start();
            }
        }

        private void Decomp_OnContinue(string obj)
        {
            RunOnUiThread(() =>
            {
                StatusText.Text = "Extracting TSO Files... ("+obj+")";
            });
        }

        private void Client_DownloadProgressChanged(object sender, DownloadProgressChangedEventArgs e)
        {
            RunOnUiThread(() =>
            {
                StatusText.Text = "Downloading TSO Files... (" + e.ProgressPercentage + "%)";
                StatusBar.Progress = e.ProgressPercentage;
            });
        }

        private void ResetDownloader()
        {
            RunOnUiThread(() =>
            {
                StatusText.Text = "Enter a location to download TSO files from.";
                StatusBar.Progress = 0;
                IPButton.Enabled = true;
                IPText.Enabled = true;
            });
            try
            {
                File.Delete(Path.Combine(System.Environment.GetFolderPath(System.Environment.SpecialFolder.MyDocuments), "The Sims Online.zip"));
            }
            catch (Exception e) { }
            ReDownload = true;
            if (DownloadClient != null) DownloadClient.Dispose();
        }
        
        public void ShowAlert(string title, string message)
        {
            AlertDialog.Builder alert = new AlertDialog.Builder(this);

            alert.SetTitle(title);
            alert.SetMessage(message);

            RunOnUiThread(() => {
                alert.Show();
            });
        }
    }
}