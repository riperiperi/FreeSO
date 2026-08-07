using Octokit;
using System.Text.Json;

namespace FSO.UpdateWorker
{
    internal class InstallerCache
    {
        private static Dictionary<string, string> PlatformNames = new()
        {
            { "windows", "Windows (x64)" },
            { "mac", "macOS (ARM)" },
            { "linux", "Linux (x64)" },
        };

        private readonly ManifestCache Manifests;
        private readonly string[] Platforms;
        private InstallerManifestResponse Response = new();

        public InstallerCache(ManifestCache manifests, string[] platforms)
        {
            Manifests = manifests;
            Platforms = platforms;
        }

        private static InstallerFile MakeFile(ReleaseAsset asset)
        {
            return new InstallerFile()
            {
                url = asset.BrowserDownloadUrl,
                size = asset.Size,
            };
        }

        private async Task<InstallerManifestChannel?> GetResponse(Release release)
        {
            if (release.Draft)
            {
                return null;
            }

            // Should have installer URLs for all of the chosen platforms.

            var result = new InstallerManifestChannel()
            {
                version = release.TagName,
                releaseUrl = release.HtmlUrl
            };

            foreach (var platform in Platforms)
            {
                var zipAsset = release.Assets.FirstOrDefault(x => x.Name.StartsWith($"client-{platform}-"));
                var installerAsset = release.Assets.FirstOrDefault(x => x.Name.StartsWith($"installer-{platform}-"));
                var serverAsset = release.Assets.FirstOrDefault(x => x.Name.StartsWith($"server-{platform}-"));

                if (zipAsset == null || (installerAsset == null && platform != "linux"))
                {
                    return null;
                }

                if (!PlatformNames.TryGetValue(platform, out string? name))
                {
                    name = "Unknown";
                }

                var platformAssets = new InstallerPlatform()
                {
                    name = name,
                    zip = MakeFile(zipAsset),
                    installer = installerAsset == null ? null : MakeFile(installerAsset),
                    server = serverAsset == null ? null : MakeFile(serverAsset)
                };

                switch (platform)
                {
                    case "windows":
                        result.windows = platformAssets;
                        break;
                    case "mac":
                        result.mac = platformAssets;
                        break;
                    case "linux":
                        result.linux = platformAssets;
                        break;
                }
            }

            // Finally, fetch the channel for this release from its manifest. If it doesn't match the channel for this installer manifest, ignore it.

            var manifest = await Manifests.GetMetadata(release);

            if (manifest == null)
            {
                return null;
            }

            result.channel = manifest.channel;

            return result;
        }

        public async Task<bool> ProcessLatest(List<Release> releases)
        {
            var processedChannels = new HashSet<string>();

            bool anyChanged = false;

            foreach (var release in releases)
            {
                // Select the first release for each channel that satisfies all of the criteria.

                var newResponse = await GetResponse(release);

                if (newResponse != null && !processedChannels.Contains(newResponse.channel))
                {
                    processedChannels.Add(newResponse.channel);

                    var existingIndex = Array.FindIndex(Response.channels, x => x.channel == newResponse.channel);

                    if (existingIndex == -1)
                    {
                        Response.channels = [.. Response.channels, newResponse];
                        anyChanged = true;
                    }
                    else
                    {
                        var existing = Response.channels[existingIndex];

                        if (!existing.Equals(newResponse))
                        {
                            Response.channels[existingIndex] = newResponse;

                            anyChanged = true;
                        }
                    }
                }
            }

            return anyChanged;
        }

        public void SaveResponse(string path)
        {
            Console.WriteLine($"Saving updated installer manifest to {path}");
            File.WriteAllText(path, JsonSerializer.Serialize(Response));
        }
    }
}
