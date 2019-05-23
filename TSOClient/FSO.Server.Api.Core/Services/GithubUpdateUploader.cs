using FSO.Server.Common.Config;
using Octokit;
using Octokit.Internal;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace FSO.Server.Api.Core.Services
{
    public class GithubUpdateUploader : IUpdateUploader
    {
        private GithubConfig Config;

        private static string Description = "This is an automated client release produced by the master FreeSO server. " +
            "These releases match up with a branch on GitHub, but with some addon content such as a custom catalog and splash. " +
            "It can be downloaded and installed directly, but it is better to do so through the game's launcher/updater. \n\n" +
            "The incremental update is applied by simply extracting the zip over the previous version of the game " +
            "(though the updater still affords extra validation here)";

        public GithubUpdateUploader(GithubConfig config)
        {
            Config = config;
        }

        public async Task<string> UploadFile(string destPath, string fileName, string groupName)
        {
            destPath = Path.GetFileName(destPath);
            var credentials = new InMemoryCredentialStore(new Credentials(Config.AccessToken));

            var client = new GitHubClient(new ProductHeaderValue(Config.AppName), credentials);

            Release release;
            try
            {
                release = await client.Repository.Release.Get(Config.User, Config.Repository, groupName);
            }
            catch (Exception)
            {
                release = null;
            }
            if (release == null) {
                var newRel = new NewRelease(groupName);
                newRel.Body = Description;
                release = await client.Repository.Release.Create(Config.User, Config.Repository, newRel);
            }

            using (var file = File.Open(fileName, System.IO.FileMode.Open, FileAccess.Read, FileShare.Read)) {
                var asset = await client.Repository.Release.UploadAsset(release, new ReleaseAssetUpload(destPath, "application/zip", file, new TimeSpan(1, 0, 0)));
                return asset.BrowserDownloadUrl;
            }
        }
    }
}
