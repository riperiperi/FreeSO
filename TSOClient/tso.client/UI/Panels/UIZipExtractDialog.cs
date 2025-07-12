using FSO.Client.Utils;
using FSO.Common.Utils;
using System.IO;

namespace FSO.Client.UI.Panels
{
    public class UIZipExtractDialog : UILoginProgress
    {
        private MultithreadedZipExtractor _zipExtractor;
        private string _zipPath;
        private string _zipName;
        private string _destPath;

        public event Callback<bool> OnComplete;

        public UIZipExtractDialog(string title, string zipPath, string destPath) : base()
        {
            _zipPath = zipPath;
            _destPath = destPath;
            _zipName = Path.GetFileName(_zipPath);

            if (title != null) Caption = title;
            else Caption = GameFacade.Strings.GetString("f128", "7");
        }

        public void Start()
        {
            _zipExtractor = new MultithreadedZipExtractor(_zipPath, _destPath, OnUpdate);
        }

        private void OnUpdate(ZipExtractionStatus status, int extractedCount, int totalCount)
        {
            GameThread.NextUpdate(x =>
            {
                if (status == ZipExtractionStatus.Completed)
                {
                    OnComplete?.Invoke(true);
                }
                else if (status == ZipExtractionStatus.Preparing)
                {
                    ProgressCaption = GameFacade.Strings.GetString("f128", "13", new string[] {
                        _zipName,
                        totalCount.ToString(),
                    });
                }
                else
                {
                    Progress = (100f * extractedCount) / totalCount;
                    ProgressCaption = GameFacade.Strings.GetString("f128", "12", new string[] {
                        _zipName,
                        extractedCount.ToString(),
                        totalCount.ToString(),
                    });
                }
            });
        }
    }
}
