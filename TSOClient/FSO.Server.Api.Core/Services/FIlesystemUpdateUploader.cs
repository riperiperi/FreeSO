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
            File.Copy(fileName, destFullPath);

            if (Config.BaseURL == null)
            {
                return Task.FromResult($"file:///{Path.GetFullPath(destFullPath)}");
            }
            else
            {
                return Task.FromResult(new Uri(new Uri(Config.BaseURL), new Uri(destPath)).ToString());
            }
        }
    }
}
