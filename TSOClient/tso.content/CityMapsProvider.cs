using FSO.Common.Content;
using FSO.Content.Model;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FSO.Content
{
    public class CityMapsProvider : IContentProvider<CityMap>
    {
        private Dictionary<int, CityMap> Cache;
        private Content Content;
        
        public CityMapsProvider(Content content)
        {
            this.Content = content;
        }

        public void Init()
        {
            Cache = new Dictionary<int, CityMap>();

            var dir = Content.GetPath("cities");
            foreach (var map in Directory.GetDirectories(dir))
            {
                var id = int.Parse(Path.GetFileName(map).Replace("city_", ""));
                Cache.Add(id, new CityMap(map));
            }
        }

        public CityMap Get(string id)
        {
            return Get(ulong.Parse(id));
        }

        public CityMap Get(ulong id)
        {
            return Cache[(int)id];
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
