using FSO.Common;
using FSO.Common.Content;
using FSO.Content.Model;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;

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
                int id;
                if (TryParseCityId(map, out id)) DirCache[id] = map;
            }

            dir = Path.Combine(FSOEnvironment.ContentDir, "Cities/");
            if (Directory.Exists(dir))
            {
                foreach (var map in Directory.GetDirectories(dir))
                {
                    int id;
                    if (TryParseCityId(map, out id)) DirCache[id] = map; // user content overrides game content
                }
            }
        }

        // Parses the numeric suffix of a "city_NNNN" directory name. Non-
        // numeric names (e.g. "city_blank", a scaffold the FSO.CityEditor
        // tool ships) are silently skipped — they're loaded by absolute
        // path, not by this id-keyed cache.
        private static bool TryParseCityId(string fullPath, out int id)
        {
            var name = Path.GetFileName(fullPath);
            if (name == null || !name.StartsWith("city_"))
            {
                id = 0;
                return false;
            }
            return int.TryParse(name.Substring("city_".Length), out id);
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

        public CityMap Get(ContentID id)
        {
            throw new NotImplementedException();
        }
    }
}
