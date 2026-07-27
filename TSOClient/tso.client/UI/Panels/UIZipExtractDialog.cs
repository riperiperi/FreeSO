using FSO.Client.Utils;
using FSO.Common.Utils;
using System.IO;

namespace FSO.Client.UI.Panels
{
    public class UIZipExtractDialog : UILoginProgress
    {
        private AbstractExtractor _zipExtractor;
        private string _zipPath;
        private string _destPath;

        public event Callback<bool, Exception> OnComplete;

        public UIZipExtractDialog(string title, string zipPath, string destPath) : base()
        {
            _zipPath = zipPath;
            _destPath = destPath;

            if (title != null) Caption = title;
            else Caption = GameFacade.Strings.GetString("f128", "7");
        }

        public void Start<T>() where T : AbstractExtractor, new()
        {
            _zipExtractor = new T();
            _zipExtractor.Start(_zipPath, _destPath, OnUpdate);
        }

        private void OnUpdate(ZipExtractionStatus status, int extractedCount, int totalCount)
        {
            GameThread.NextUpdate(x =>
            {
                string name = _zipExtractor.Filename;

                if (status == ZipExtractionStatus.Completed)
                {
                    OnComplete?.Invoke(true, null);
                }
                else if (status == ZipExtractionStatus.Preparing)
                {
                    ProgressCaption = GameFacade.Strings.GetString("f128", "13", new string[] {
                        name,
                        totalCount.ToString(),
                    });
                }
                else if (status == ZipExtractionStatus.Extracting)
                {
                    Progress = (100f * extractedCount) / totalCount;
                    ProgressCaption = GameFacade.Strings.GetString("f128", "12", new string[] {
                        name,
                        extractedCount.ToString(),
                        totalCount.ToString(),
                    });
                }
                else
                {
                    OnComplete?.Invoke(false, _zipExtractor.Error);
                }
            });
        }
    }
}
