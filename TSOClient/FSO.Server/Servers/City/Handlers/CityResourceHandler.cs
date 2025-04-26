using FSO.Common.DataService;
using FSO.Server.Database.DA;
using FSO.Server.Framework.Voltron;
using FSO.Server.Protocol.Electron.Packets;
using Ninject;
using NLog;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Runtime.Caching;

namespace FSO.Server.Servers.City.Handlers
{
    public static class MemoryCacher
    {
        public static MemoryCache Default = new MemoryCache("city_resource");
        public static object GetValue(string key)
        {
            MemoryCache memoryCache = Default;
            return memoryCache.Get(key);
        }

        public static bool Add(string key, object value, DateTimeOffset absExpiration)
        {
            MemoryCache memoryCache = Default;
            return memoryCache.Add(key, value, absExpiration);
        }

        public static void Delete(string key)
        {
            MemoryCache memoryCache = Default;
            memoryCache.Remove(key);
        }
    }

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
        private IKernel Kernel;

        public CityResourceHandler(CityServerContext context, IDAFactory da, IDataService dataService, IKernel kernel, ServerConfiguration config)
        {
            Context = context;
            DA = da;
            DataService = dataService;
            Kernel = kernel;
            Config = config;
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
            var dat = (byte[])MemoryCacher.GetValue("lt" + shardid + ":" + id);
            if (dat != null)
            {
                return dat;
            }

            var lot = IDForLocation(shardid, id);
            if (lot == null) return new byte[0];

            try
            {
                var ndat = File.ReadAllBytes(Path.Combine(Config.SimNFS, "Lots/" + lot.Value.ToString("x8") + "/thumb.png"));
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
            var dat = (byte[])MemoryCacher.GetValue("lf" + shardid + ":" + id);
            if (dat != null)
            {
                return dat;
            }

            var lot = IDForLocation(shardid, id);
            if (lot == null) return new byte[0];

            try
            {
                var ndat = File.ReadAllBytes(Path.Combine(Config.SimNFS, "Lots/" + lot.Value.ToString("x8") + "/thumb.fsof"));
                MemoryCacher.Add("lf" + shardid + ":" + id, ndat, DateTime.Now.Add(new TimeSpan(1, 0, 0)));

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
