using FSO.Files.FSO;
using Octokit;
using System.Collections.Concurrent;
using System.Net.Http.Json;

namespace FSO.UpdateWorker
{
    internal class ManifestCache
    {
        private ConcurrentDictionary<string, FSOUpdateMetadataStandalone?> Manifests = [];
        private readonly HttpClient Http;

        public ManifestCache(HttpClient http)
        {
            Http = http;
        }

        public async Task<FSOUpdateMetadataStandalone?> GetMetadata(string url, string name)
        {
            if (Manifests.TryGetValue(url, out var data))
            {
                return data;
            }

            try
            {
                // Assuming that we have permissions here.
                data = await Http.GetFromJsonAsync<FSOUpdateMetadataStandalone>(url);

                if (data?.id != null)
                {
                    Manifests[url] = data;
                    return data;
                }
                else
                {
                    Console.WriteLine($"Couldn't parse update JSON for {name}, skipping.");
                }
            }
            catch
            {
                // Nothing happens - this asset just gets skipped.
                Console.WriteLine($"Couldn't load update info for {name}, skipping.");
            }

            return null;
        }

        public async Task<FSOUpdateMetadataStandalone?> GetMetadata(ReleaseAsset? manifestAsset, string name)
        {
            if (manifestAsset != null)
            {
                return await GetMetadata(manifestAsset.Url, name);
            }
            else
            {
                Console.WriteLine($"Couldn't find manifest asset for {name}, skipping.");
            }

            return null;
        }

        public async Task<FSOUpdateMetadataStandalone?> GetMetadata(Release release)
        {
            var manifestAsset = release.Assets.FirstOrDefault(x => x.Name == $"manifest-{release.TagName}.json");

            return await GetMetadata(manifestAsset, release.TagName);
        }
    }
}
