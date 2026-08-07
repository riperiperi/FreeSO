using Octokit;
using System.Text.Json;

namespace FSO.UpdateWorker
{
    internal class Program
    {
        private const int CheckFrequency = 1000 * 60 * 2; // 4 minutes

        static async Task Main(string[] args)
        {
            UpdateWorkerConfig config = new();

            try
            {
                if (File.Exists("config.json"))
                {
                    config = JsonSerializer.Deserialize<UpdateWorkerConfig>(File.ReadAllText("config.json")) ?? new();
                }
            }
            catch (Exception)
            {
                config = new();
            }

            string authorName = config.authorName;
            string repoName = config.repoName;
            var client = new GitHubClient(new ProductHeaderValue("freeso-updates"));

            var http = new HttpClient();
            http.DefaultRequestHeaders.Accept.Clear();
            http.DefaultRequestHeaders.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/octet-stream"));
            http.DefaultRequestHeaders.Add("User-Agent", "freeso-ci");
            if (config.githubToken != null)
            {
                client.Credentials = new Credentials(config.githubToken);
                http.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", config.githubToken);
            }

            var manifests = new ManifestCache(http);

            var cache = new ReleaseCache(client, http, manifests);
            var installer = new InstallerCache(manifests, config.installerPlatforms);

            if (!config.clearCache)
            {
                cache.LoadCache(config.targetPath);
            }

            while (true)
            {
                try
                {
                    List<Release> releases = [.. await client.Repository.Release.GetAll(authorName, repoName)];

                    bool hasChange = await cache.AddReleases(releases);

                    var remeshReleases = await client.Repository.Release.GetAll(config.remeshAuthorName, config.remeshRepoName);

                    hasChange |= await cache.AddRemeshes([.. remeshReleases], config);

                    if (hasChange)
                    {
                        cache.SaveResponse(config.targetPath);
                    }

                    if (config.installerTargetPath != null && await installer.ProcessLatest(releases))
                    {
                        installer.SaveResponse(config.installerTargetPath);
                    }

                    Thread.Sleep(CheckFrequency);

                    /*
                     * This only works when for a non-prerelease branch
                    while (true)
                    {
                        Thread.Sleep(CheckFrequency);

                        try
                        {
                            var latest = await client.Repository.Release.GetLatest(authorName, repoName);

                            if (latest.Id != releases.FirstOrDefault()?.Id)
                            {
                                // Get the full release list if something changed.
                                break;
                            }
                        }
                        catch
                        {
                            // Try again later.
                        }
                    }
                    */
                }
                catch
                {
                    Thread.Sleep(CheckFrequency);
                }
            }
        }
    }
}
