using FSO.Common;
using FSO.Common.Utils;
using FSO.Files;
using FSO.Server.Clients;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FSO.Client.Rendering.City
{
    /// <summary>
    /// Handles loading lot thumbnails from the API, and managing them in memory. 
    /// This will also handle FSOF resources in future.
    /// </summary>
    public class LotThumbContent
    {
        public Dictionary<ulong, LotThumbEntry> Entries = new Dictionary<ulong, LotThumbEntry>();
        public Texture2D DefaultThumb;
        public int Second;
        public int LoadLimit = 100; //about 6mb of 256x256 thumbnails
        public int ExpiryTime = 10; //thumbs expire after about 10 seconds.
        private ApiClient Client;

        public LotThumbContent()
        {
            GameThread.SetInterval(Update, 1000);
            Client = new ApiClient(ApiClient.CDNUrl ?? GlobalSettings.Default.GameEntryUrl);

            DefaultThumb = TextureUtils.TextureFromFile(GameFacade.GraphicsDevice, GameFacade.GameFilePath("userdata/houses/defaulthouse.bmp"));
            TextureUtils.ManualTextureMask(ref DefaultThumb, new uint[] { 0xFF000000 });
        }
        
        private LotThumbEntry GetLotEntryForFrame(uint shardID, uint location)
        {
            LotThumbEntry result = null;
            var key = (((ulong)shardID) << 32) | location;
            if (!Entries.TryGetValue(key, out result)) {
                result = new LotThumbEntry() { Location = location };
                result.LotTexture = DefaultThumb;
                Client.GetThumbnailAsync(shardID, location, (data) =>
                {
                    if (data != null && !result.Dead)
                    {
                        using (var mem = new MemoryStream(data))
                        {
                            result.Loaded = true;
                            result.LotTexture = ImageLoader.FromStream(GameFacade.GraphicsDevice, mem);
                        }
                    }
                });
                Entries[key] = result;
            }
            result.LastDrawSecond = Second;
            return result;
        }

        public Texture2D GetLotThumbForFrame(uint shardID, uint location)
        {
            return GetLotEntryForFrame(shardID, location).LotTexture;
        }

        public LotThumbEntry GetLotEntry(uint shardID, uint location)
        {
            var entry = GetLotEntryForFrame(shardID, location);
            entry.Held++;
            return entry;
        }

        public void ReleaseLotThumb(uint shardID, uint location)
        {
            LotThumbEntry result = null;
            var key = (((ulong)shardID) << 32) | location;
            if (Entries.TryGetValue(key, out result))
                result.Held--;
        }

        public void Update()
        {
            Second++;

            var ordered = Entries.OrderBy(x => x.Value.LastDrawSecond).ToList();
            var largestSecond = ordered.LastOrDefault();
            if (ordered.Count > 0) {
                if (Entries.Count > 100)
                {
                    //remove all entries not on our last second
                    while (Entries.Count > 100)
                    {
                        var entry = ordered.First();
                        if (entry.Value.LastDrawSecond == largestSecond.Value.LastDrawSecond) break;
                        else
                        {
                            ordered.RemoveAt(0);
                            if (entry.Value.Held == 0)
                            {
                                if (entry.Value.Loaded) entry.Value.LotTexture.Dispose();
                                entry.Value.Dead = true;
                                Entries.Remove(entry.Key);
                            }
                        }
                    }
                } 
                //remove entries that haven't been used for a while
                foreach (var entry in ordered)
                {
                    if (Second - entry.Value.LastDrawSecond > ExpiryTime && entry.Value.Held == 0)
                    {
                        if (entry.Value.Loaded) entry.Value.LotTexture.Dispose();
                        entry.Value.Dead = true;
                        Entries.Remove(entry.Key);
                    }
                }
            }
        }
    }

    public class LotThumbEntry
    {
        public uint Location;
        public int LastDrawSecond;
        public Texture2D LotTexture;
        public int Held;
        public bool Loaded;
        public bool Dead;
    }
}
