using FSO.Common.Content;
using FSO.Content.Model;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FSO.Content
{
    public class CityMapsProvider : IContentProvider<CityMap>
    {
        private ConcurrentDictionary<int, CityMap> Cache;
        private Dictionary<int, string> DirCache;
        private Content Content;
        
        public CityMapsProvider(Content content)
        {
            this.Content = content;
        }

        public void Init()
        {
            DirCache = new Dictionary<int, string>();
            Cache = new ConcurrentDictionary<int, CityMap>();

            var dir = Content.GetPath("cities");
            foreach (var map in Directory.GetDirectories(dir))
            {
                var id = int.Parse(Path.GetFileName(map).Replace("city_", ""));
                DirCache.Add(id, map);
            }
        }

        public CityMap Get(string id)
        {
            return Get(ulong.Parse(id));
        }

        public CityMap Get(ulong id)
        {
            CityMap result;
            if (Cache.TryGetValue((int)id, out result))
            {
                return result;
            } else
            {
                return Cache.GetOrAdd((int)id, new CityMap(DirCache[(int)id]));
            }
        }

        public CityMap Get(uint type, uint fileID)
        {
            throw new NotImplementedException();
        }

        public List<IContentReference<CityMap>> List()
        {
            throw new NotImplementedException();
        }
    }
}
