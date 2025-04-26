using FSO.Client.Controllers;
using FSO.Common.Utils;
using FSO.Files;
using FSO.Files.RC;
using FSO.Server.Clients;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace FSO.Client.Rendering.City
{
    /// <summary>
    /// Handles loading lot thumbnails from the API, and managing them in memory. 
    /// </summary>
    public class LotThumbContent : IDisposable
    {
        public Dictionary<ulong, LotThumbEntry> Entries = new Dictionary<ulong, LotThumbEntry>();
        public Dictionary<ulong, LotThumbEntry> FacadeEntries = new Dictionary<ulong, LotThumbEntry>();
        public Texture2D DefaultThumb;
        public FSOF DefaultFSOF;
        public int Second;
        public int LoadLimit = 100; //about 6mb of 256x256 thumbnails
        public int ExpiryTime = 10; //thumbs expire after about 10 seconds.
        private ApiClient Client;
        private CityResourceController Resource;

        private bool IsArchive = true;

        public LotThumbContent(CityResourceController resource)
        {
            Resource = resource;

            GameThread.SetInterval(Update, 1000);
            Client = new ApiClient(ApiClient.CDNUrl ?? GlobalSettings.Default.GameEntryUrl);

            DefaultThumb = TextureUtils.TextureFromFile(GameFacade.GraphicsDevice, GameFacade.GameFilePath("userdata/houses/defaulthouse.bmp"));
            using (var strm = File.Open("Content/3D/defaulthouse.fsof", FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            {
                DefaultFSOF = new FSOF();
                DefaultFSOF.Read(strm);
                DefaultFSOF.LoadGPU(GameFacade.GraphicsDevice);
            }
               
            TextureUtils.ManualTextureMask(ref DefaultThumb, new uint[] { 0xFF000000 });
        }

        private LotThumbEntry GetLotEntryForFrame(uint shardID, uint location, bool facade)
        {
            LotThumbEntry result = null;
            var key = (((ulong)shardID) << 32) | location;

            var entries = (facade) ? FacadeEntries : Entries;

            if (!entries.TryGetValue(key, out result)) {
                result = new LotThumbEntry() { Location = location };
                if (facade)
                {
                    result.LotFacade = DefaultFSOF;

                    Action<byte[]> callback = (data) =>
                    {
                        if (data != null && !result.Dead && !result.Loaded)
                        {
                            using (var mem = new MemoryStream(data))
                            {
                                result.Loaded = true;
                                try
                                {
                                    result.LotFacade = new FSOF();
                                    result.LotFacade.Read(mem);
                                    result.LotFacade.LoadGPU(GameFacade.GraphicsDevice);
                                }
                                catch
                                {
                                    result.LotFacade = null;
                                }
                            }
                        }
                    };

                    if (IsArchive)
                    {
                        Resource.GetFacadeAsync(shardID, location, callback);
                    }
                    else
                    {
                        Client.GetFacadeAsync(shardID, location, callback);
                    }
                }
                else
                {
                    result.LotTexture = DefaultThumb;

                    Action<byte[]> callback = (data) =>
                    {
                        if (data != null && !result.Dead && !result.Loaded)
                        {
                            using (var mem = new MemoryStream(data))
                            {
                                result.Loaded = true;
                                try
                                {
                                    result.LotTexture = ImageLoader.FromStream(GameFacade.GraphicsDevice, mem);
                                }
                                catch
                                {
                                    result.LotTexture = new Texture2D(GameFacade.GraphicsDevice, 1, 1);
                                }
                            }
                        }
                    };

                    if (IsArchive)
                    {
                        Resource.GetThumbnailAsync(shardID, location, callback);
                    }
                    else
                    {
                        Client.GetThumbnailAsync(shardID, location, callback);
                    }
                }
                entries[key] = result;
            }
            result.LastDrawSecond = Second;
            return result;
        }

        public Texture2D GetLotThumbForFrame(uint shardID, uint location)
        {
            return GetLotEntryForFrame(shardID, location, false).LotTexture;
        }

        public FSOF GetLotFacadeForFrame(uint shardID, uint location)
        {
            return GetLotEntryForFrame(shardID, location, true).LotFacade;
        }

        public LotThumbEntry GetLotEntry(uint shardID, uint location, bool facade)
        {
            var entry = GetLotEntryForFrame(shardID, location, facade);
            entry.Held++;
            return entry;
        }

        public void ReleaseLotThumb(uint shardID, uint location, bool facade)
        {
            LotThumbEntry result = null;
            var entries = (facade) ? FacadeEntries : Entries;
            var key = (((ulong)shardID) << 32) | location;
            if (Entries.TryGetValue(key, out result))
                result.Held--;
        }

        public void OverrideLotThumb(uint shardID, uint location, Texture2D tex)
        {
            var entry = GetLotEntry(shardID, location, false);
            entry.Held++; //keep this forever
            if (entry.Loaded)
            {
                entry.LotTexture?.Dispose();
            }
            entry.LotTexture = tex;
            entry.Loaded = true;
        }

        private void Process(Dictionary<ulong, LotThumbEntry> Entries)
        {
            var ordered = Entries.OrderBy(x => x.Value.LastDrawSecond).ToList();
            var largestSecond = ordered.LastOrDefault();
            if (ordered.Count > 0)
            {
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
                                if (entry.Value.Loaded)
                                {
                                    entry.Value.LotTexture?.Dispose();
                                    entry.Value.LotFacade?.Dispose();
                                }
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
                        if (entry.Value.Loaded)
                        {
                            entry.Value.LotTexture?.Dispose();
                            entry.Value.LotFacade?.Dispose();
                        }
                        entry.Value.Dead = true;
                        Entries.Remove(entry.Key);
                    }
                }
            }
        }

        public void Update()
        {
            Second++;
            Process(Entries);
            Process(FacadeEntries);
        }

        public void Dispose()
        {
            DefaultThumb.Dispose();
            DefaultFSOF.Dispose();
            foreach (var entry in Entries)
            {
                if (entry.Value.Loaded)
                {
                    entry.Value.LotTexture?.Dispose();
                    entry.Value.LotFacade?.Dispose();
                }
                entry.Value.Dead = true;
            }
        }
    }

    public class LotThumbEntry
    {
        public uint Location;
        public int LastDrawSecond;
        public Texture2D LotTexture;
        public FSOF LotFacade;
        public int Held;
        public bool Loaded;
        public bool Dead;
        public bool FacadeEntry;
    }
}
