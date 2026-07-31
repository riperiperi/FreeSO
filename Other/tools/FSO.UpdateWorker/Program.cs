using Octokit;
using System.Text.Json;

namespace FSO.UpdateWorker
{
    internal class Program
    {
        private const int CheckFrequency = 120000;

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

            var cache = new ReleaseCache(client, http);

            if (!config.clearCache)
            {
                cache.LoadCache(config.targetPath);
            }

            while (true)
            {
                try
                {
                    var releases = await client.Repository.Release.GetAll(authorName, repoName);

                    if (await cache.AddReleases([.. releases]))
                    {
                        cache.SaveResponse(config.targetPath);
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
