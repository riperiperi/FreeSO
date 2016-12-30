using FSO.Common.DatabaseService.Model;
using FSO.Common.Domain.Top100;
using FSO.Common.Enum;
using FSO.Server.Database.DA;
using FSO.Server.Servers.City;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Caching;
using System.Text;
using System.Threading.Tasks;

namespace FSO.Server.Domain
{
    public class ServerTop100Domain : Top100Domain
    {
        private MemoryCache Cache;
        private IDAFactory DAFactory;
        private CityServerContext Context;

        public ServerTop100Domain(IDAFactory DAFactory, CityServerContext context, MemoryCache cache)
        {
            Cache = cache;
            this.DAFactory = DAFactory;
            this.Context = context;
        }

        public List<Top100Entry> Query(Top100Category category)
        {
            lock (Cache)
            {
                var key = "top100/" + Context.ShardId + "/" + category.ToString();
                var value = Cache.GetCacheItem(key);
                
                if(value != null)
                {
                    return (List <Top100Entry>)value.Value;
                }

                using (var db = DAFactory.Get())
                {
                    List<Top100Entry> results = null;

                    if (category.IsLotCategory())
                    {
                        results = db.LotTop100.GetByCategory(Context.ShardId, category.ToLotCategory()).Select(x => new Top100Entry()
                        {
                            Rank = x.rank,
                            //Data service uses lot location as the ID
                            TargetId = (uint?)x.lot_location,
                            TargetName = x.lot_name
                        }).ToList();
                    }else{
                        //TODO:
                        results = new List<Top100Entry>();
                        for(var i=0; i < 100; i++)
                        {
                            results.Add(new Top100Entry {
                                Rank = (byte)i
                            });
                        }
                    }

                    Cache.Add(key, results, new CacheItemPolicy()
                    {
                        AbsoluteExpiration = DateTime.Now.Add(TimeSpan.FromMinutes(5))
                    });
                    return results;
                }
            }
        }

    }

    
}
