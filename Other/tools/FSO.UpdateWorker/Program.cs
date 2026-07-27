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

            while (true)
            {
                var releases = await client.Repository.Release.GetAll(authorName, repoName);

                if (await cache.AddReleases([ ..releases ]))
                {
                    cache.SaveResponse(config.targetPath);
                }

                Thread.Sleep(CheckFrequency);
            }
        }
    }
}
