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

        public async Task<bool> AddRemeshes(List<Release> releases, UpdateWorkerConfig config)
        {
            bool changed = false;

            foreach (var channelName in config.remeshChannels)
            {
                var latest = releases.FirstOrDefault(x => x.TagName.StartsWith($"{channelName}."));

                if (latest == null)
                {
                    continue;
                }

                string[] split = latest.TagName.Split('.');

                if (split.Length != 2 || !int.TryParse(split[1], out int version))
                {
                    continue;
                }

                var existing = Response.remeshes.FirstOrDefault(x => x.channel == channelName);

                if (existing != null)
                {
                    // Only update if the version has increased.

                    if (version <= existing.version)
                    {
                        continue;
                    }
                }

                // Get the remesh's manifest and try to add it to the Response

                var manifestAsset = latest.Assets.FirstOrDefault(x => x.Name == $"freeso-remeshes.json");

                try
                {
                    if (manifestAsset != null)
                    {
                        // Assuming that we have permissions here.
                        var assetUrl = manifestAsset.Url;

                        var data = await Http.GetFromJsonAsync<FSORemeshChannel>(assetUrl);

                        if (data?.channel != channelName)
                        {
                            Console.WriteLine($"Couldn't parse remesh JSON for {latest.TagName}, skipping.");
                        }

                        Response.remeshes = [..Response.remeshes.Where(x => x.channel != channelName), data];

                        if (config.autoRemeshChannel == channelName)
                        {
                            Response.autoRemeshChannel = channelName;
                        }

                        Console.WriteLine($"Updating remesh channel '{channelName}'");

                        changed = true;
                    }
                    else
                    {
                        Console.WriteLine($"Couldn't find manifest asset for {latest.TagName}, skipping.");
                    }
                }
                catch
                {
                    // Nothing happens - this asset just gets skipped.
                    Console.WriteLine($"Couldn't load remesh info for {latest.TagName}, skipping.");
                }
            }

            return changed;
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

        public void LoadCache(string path)
        {
            try
            {
                var text = File.ReadAllText(path);

                Response = JsonSerializer.Deserialize<FSOUpdateResponse>(text) ?? Response;

                foreach (var channel in Response.channels)
                {
                    foreach (var update in channel.updates)
                    {
                        SeenTags.Add(update.id);
                    }
                }

                Console.WriteLine($"Loaded cache from {path} with {SeenTags.Count} items.");
            }
            catch
            {
                Console.WriteLine($"Cache at {path} could not be loaded.");
            }
        }
    }
}
