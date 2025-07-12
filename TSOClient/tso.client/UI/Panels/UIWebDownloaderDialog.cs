using FSO.Common.Utils;
using System;
using System.IO;
using System.Net;

namespace FSO.Client.UI.Panels
{
    public class UIWebDownloaderDialog : UILoginProgress
    {
        private WebClient DownloadClient;
        private DownloadItem[] Items;
        private int CurrentItem;
        private DownloadItem ItemMeta;

        public event Callback<bool> OnComplete;

        public UIWebDownloaderDialog(string title, DownloadItem[] items) : base()
        {
            if (title != null) Caption = title;
            else Caption = GameFacade.Strings.GetString("f101", "9");
            ProgressCaption = "";
            Items = items;

            DownloadClient = new WebClient();
            DownloadClient.DownloadProgressChanged += DownloadClient_DownloadProgressChanged;
            DownloadClient.DownloadFileCompleted += DownloadClient_DownloadFileCompleted;
            AdvanceDownloader();
        }

        private void DownloadClient_DownloadProgressChanged(object sender, DownloadProgressChangedEventArgs e)
        {
            GameThread.NextUpdate(x =>
            {
                Progress = (100 * (CurrentItem-1) + e.ProgressPercentage) / Items.Length;
                ProgressCaption = GameFacade.Strings.GetString("f101", "2", new string[] {
                    ItemMeta.Name,
                    (e.BytesReceived/1000000f).ToString("0.00"),
                    (e.TotalBytesToReceive/1000000f).ToString("0.00")+"MB",
                    CurrentItem.ToString(),
                    Items.Length.ToString()
                });
            });
        }

        private void DeleteFiles()
        {
            foreach (var item in Items)
            {
                if (File.Exists(item.DestPath))
                {
                    File.Delete(item.DestPath);
                }
            }
        }

        private void DownloadClient_DownloadFileCompleted(object sender, System.ComponentModel.AsyncCompletedEventArgs e)
        {
            if (e.Error != null || e.Cancelled)
            {
                DeleteFiles();

                GameThread.NextUpdate(x => OnComplete?.Invoke(false));
                return;
            }
            AdvanceDownloader();
        }

        public void AdvanceDownloader()
        {
            if (CurrentItem >= Items.Length)
            {
                GameThread.NextUpdate(x => OnComplete?.Invoke(true));
                return;
            }
            var item = Items[CurrentItem++];
            ItemMeta = item;
            Directory.CreateDirectory(Path.GetDirectoryName(item.DestPath));
            DownloadClient.DownloadFileAsync(new Uri(item.Url), item.DestPath);
        }
    }

    public class DownloadItem
    {
        public string Url;
        public string DestPath;
        public string Name;
    }
}
