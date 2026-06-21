using FSO.Files.FSO;
using Octokit;
using System.Net.Http.Json;
using System.Text.Json;

namespace FSO.UpdateWorker
{
    internal class ReleaseCache
    {
        private readonly GitHubClient Client;
        private readonly HttpClient Http;

        private FSOUpdateResponse Response = new FSOUpdateResponse();
        private HashSet<string> SeenTags = [];

        public ReleaseCache(GitHubClient client, HttpClient http)
        {
            Client = client;
            Http = http;
        }

        public async Task<bool> AddReleases(List<Release> releases)
        {
            bool addedAny = false;

            foreach (var release in releases.OrderBy(x => x.CreatedAt)) // Oldest first
            {
                if (!SeenTags.Contains(release.TagName))
                {
                    if (!await AddRelease(release))
                    {
                        // It's not ready yet - try this one again later.
                        continue;
                    }

                    addedAny = true;
                    SeenTags.Add(release.TagName);
                }
            }

            return addedAny;
        }

        private async Task<bool> AddRelease(Release release)
        {
            if (release.Draft)
            {
                // Not ready yet.
                return false;
            }

            // Get the release's manifest and try to add it to the Response

            var manifestAsset = release.Assets.FirstOrDefault(x => x.Name == $"manifest-{release.TagName}.json");

            try
            {
                if (manifestAsset != null)
                {
                    // Assuming that we have permissions here.
                    var assetUrl = manifestAsset.Url;

                    var data = await Http.GetFromJsonAsync<FSOUpdateMetadataStandalone>(assetUrl);

                    if (data?.id != null)
                    {
                        AddReleaseManifest(data);
                    }
                    else
                    {
                        Console.WriteLine($"Couldn't parse update JSON for {release.TagName}, skipping.");
                    }
                }
                else
                {
                    Console.WriteLine($"Couldn't find manifest asset for {release.TagName}, skipping.");
                }
            }
            catch
            {
                // Nothing happens - this asset just gets skipped.
                Console.WriteLine($"Couldn't load update info for {release.TagName}, skipping.");
            }

            return true;
        }

        private FSOUpdateChannel GetOrAddChannel(FSOUpdateMetadataStandalone standalone)
        {
            var existing = Response.channels.FirstOrDefault(x => x.channel == standalone.channel);

            if (existing == null)
            {
                Console.WriteLine($"Adding new channel '{standalone.channel}'");
                existing = new FSOUpdateChannel()
                {
                    channel = standalone.channel,
                    publicKey = standalone.publicKey,
                };

                Response.channels = [.. Response.channels, existing];
            }

            existing.publicKey = standalone.publicKey;

            return existing;
        }

        private void AddReleaseManifest(FSOUpdateMetadataStandalone standalone)
        {
            var channel = GetOrAddChannel(standalone);

            List<FSOUpdateMetadata> updates = [standalone.Clone(), ..channel.updates];

            Console.WriteLine($"Adding new update '{standalone.id}'");
            channel.updates = [.. updates.OrderByDescending(x => x.date)];
        }

        public void SaveResponse(string path)
        {
            Console.WriteLine($"Saving updated releases to {path}");
            File.WriteAllText(path, JsonSerializer.Serialize(Response));
        }
    }
}
