using FSO.Common.DataService;
using FSO.Common.Domain.Shards;
using FSO.Content.Model;
using FSO.Server.Database.DA;
using FSO.Server.Framework.Voltron;
using FSO.Server.Protocol.Electron.Packets;
using Ninject;
using NLog;
using System.Collections.Concurrent;
using System.Runtime.Caching;
using System.Text;

namespace FSO.Server.Servers.City.Handlers
{
    public class ShardLocationCache
    {
        public ConcurrentDictionary<uint, int> Dict = new ConcurrentDictionary<uint, int>();
        public DateTime CreateTime = DateTime.UtcNow;

        public ShardLocationCache(ConcurrentDictionary<uint, int> dict)
        {
            Dict = dict;
        }
    }

    public class CityResourceHandler
    {
        private static Logger LOG = LogManager.GetCurrentClassLogger();
        private IDAFactory DA;
        private IDataService DataService;
        private CityServerContext Context;
        private ServerConfiguration Config;
        private IShardsDomain Shards;
        private IKernel Kernel;
        private MemoryCache MemoryCacher = new("city_resource");

        public CityResourceHandler(CityServerContext context, IDAFactory da, IDataService dataService, IKernel kernel, ServerConfiguration config, IShardsDomain shards)
        {
            Context = context;
            DA = da;
            DataService = dataService;
            Kernel = kernel;
            Config = config;
            Shards = shards;
        }

        public static ConcurrentDictionary<int, ShardLocationCache> LotLocationCache = new ConcurrentDictionary<int, ShardLocationCache>();

        public int? IDForLocation(int shardid, uint loc)
        {
            var locToID = LotLocationCache.GetOrAdd(shardid, (ikey) =>
            {
                using (var da = DA.Get())
                {
                    return new ShardLocationCache(
                        new ConcurrentDictionary<uint, int>(da.Lots.All(ikey).Select(x => new KeyValuePair<uint, int>(x.location, x.lot_id)))
                        );
                }
            });
            if (DateTime.UtcNow - locToID.CreateTime > TimeSpan.FromMinutes(15))
            {
                ShardLocationCache removed;
                LotLocationCache.TryRemove(shardid, out removed);
            }

            try
            {
                return locToID.Dict.GetOrAdd(loc, (ikey) =>
                {
                    using (var da = DA.Get())
                    {
                        return da.Lots.GetByLocation(shardid, ikey).lot_id;
                    }
                });
            }
            catch (NullReferenceException e)
            {
                return null;
            }
        }

        public byte[] GetLotThumbnail(int shardid, uint id)
        {
            var dat = (byte[])MemoryCacher.Get("lt" + shardid + ":" + id);
            if (dat != null)
            {
                return dat;
            }

            var lot = IDForLocation(shardid, id);
            if (lot == null) return new byte[0];

            try
            {
                var path = Path.Combine(Config.SimNFS, "Lots/" + lot.Value.ToString("x8") + "/thumb.png");
                if (!File.Exists(path))
                {
                    return new byte[0];
                }

                var ndat = File.ReadAllBytes(path);
                MemoryCacher.Add("lt" + shardid + ":" + id, ndat, DateTime.Now.Add(new TimeSpan(1, 0, 0)));

                return ndat;
            }
            catch (Exception e)
            {
                return new byte[0];
            }
        }

        public byte[] GetLotFacade(int shardid, uint id)
        {
            var dat = (byte[])MemoryCacher.Get("lf" + shardid + ":" + id);
            if (dat != null)
            {
                return dat;
            }

            var lot = IDForLocation(shardid, id);
            if (lot == null) return new byte[0];

            try
            {
                string path = Path.Combine(Config.SimNFS, "Lots/" + lot.Value.ToString("x8") + "/thumb.fsof");
                if (!File.Exists(path))
                {
                    return new byte[0];
                }

                var ndat = File.ReadAllBytes(path);
                MemoryCacher.Add("lf" + shardid + ":" + id, ndat, DateTime.Now.Add(new TimeSpan(1, 0, 0)));

                return ndat;
            }
            catch (Exception e)
            {
                return new byte[0];
            }
        }

        private byte[] GetAvatarDescription(int shardId, uint avatarId)
        {
            string data = "";

            using (var da = DA.Get())
            {
                var ava = da.Avatars.Get(avatarId);

                if (ava != null)
                {
                    data = ava.description;
                }
            }

            return Encoding.UTF8.GetBytes(data);
        }

        public byte[] GetCityThumbnail(int shardid)
        {
            var dat = (byte[])MemoryCacher.Get("ct" + shardid);
            if (dat != null)
            {
                return dat;
            }

            try
            { 
                string path;
                // Try and send the default thumbnail for this shard's map

                var map = Shards.GetMapForId(shardid);
                if (map != null && map.Thumbnail is FileTextureRef file)
                {
                    path = file.FilePath;

                    if (!File.Exists(path))
                    {
                        return new byte[0];
                    }
                }
                else
                {
                    return new byte[0];
                }

                var ndat = File.ReadAllBytes(path);
                MemoryCacher.Add("ct" + shardid, ndat, DateTime.Now.Add(new TimeSpan(1, 0, 0)));

                return ndat;
            }
            catch (Exception e)
            {
                return new byte[0];
            }
        }

        public void Handle(IVoltronSession session, CityResourceRequest packet)
        {
            byte[] data = null;
            int shard = Context.ShardId;

            Task.Run(() =>
            {
                try
                {
                    switch (packet.Type)
                    {
                        case CityResourceRequestType.LOT_THUMBNAIL:
                            data = GetLotThumbnail(shard, packet.ResourceID);
                            break;
                        case CityResourceRequestType.LOT_FACADE:
                            data = GetLotFacade(shard, packet.ResourceID);
                            break;
                        case CityResourceRequestType.AVATAR_DESCRIPTION:
                            data = GetAvatarDescription(shard, packet.ResourceID);
                            break;
                        case CityResourceRequestType.CITY_THUMBNAIL:
                            data = GetCityThumbnail(shard);
                            break;
                    }

                    session.Write(new CityResourceResponse()
                    {
                        Type = packet.Type,
                        RequestID = packet.RequestID,
                        ResourceID = packet.ResourceID,
                        Data = data ?? new byte[0]
                    });
                }
                catch (Exception)
                {

                }
            });
        }
    }
}
