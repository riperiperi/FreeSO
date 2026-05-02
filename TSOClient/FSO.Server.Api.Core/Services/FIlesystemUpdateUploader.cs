using FSO.Server.Common.Config;
using System;
using System.IO;
using System.Threading.Tasks;

namespace FSO.Server.Api.Core.Services
{
    public class FilesystemUpdateUploader : IUpdateUploader
    {
        private FilesystemConfig Config;

        public FilesystemUpdateUploader(FilesystemConfig config)
        {
            Config = config;
        }

        public Task<string> UploadFile(string destPath, string fileName, string groupName)
        {
            var destFullPath = Path.Combine(Config.BasePath, destPath);
            // File.Copy does not create intermediate directories. Updates
            // land under subpaths like "updates/client-edenso-1.zip" — make
            // sure the parent dir exists before the copy so a fresh install
            // (or a wiped public/) doesn't blow up the publish.
            var parentDir = Path.GetDirectoryName(destFullPath);
            if (!string.IsNullOrEmpty(parentDir))
                Directory.CreateDirectory(parentDir);
            // Overwrite so re-running a publish that was interrupted after
            // a successful copy but before the DB row was committed doesn't
            // fail with "already exists".
            File.Copy(fileName, destFullPath, overwrite: true);

            if (Config.BaseURL == null)
            {
                return Task.FromResult($"file:///{Path.GetFullPath(destFullPath)}");
            }
            else
            {
                // Use the (Uri, string) overload — destPath is intentionally
                // a relative path like "updates/client-edenso-1.zip" and the
                // (Uri, Uri) overload throws UriFormatException because the
                // single-arg `new Uri(string)` defaults to UriKind.Absolute
                // and rejects relative inputs.
                return Task.FromResult(new Uri(new Uri(Config.BaseURL), destPath).ToString());
            }
        }
    }
}
