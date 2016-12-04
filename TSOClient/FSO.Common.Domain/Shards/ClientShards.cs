using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FSO.Server.Protocol.CitySelector;

namespace FSO.Common.Domain.Shards
{
    public class ClientShards : IShardsDomain
    {
        public int? CurrentShard { get; set; }

        public List<ShardStatusItem> All
        {
            get; set;
        } = new List<ShardStatusItem>();

        public ShardStatusItem GetById(int id)
        {
            return All.FirstOrDefault(x => x.Id  == id);
        }

        public ShardStatusItem GetByName(string name)
        {
            return All.FirstOrDefault(x => x.Name == name);
        }
    }
}
