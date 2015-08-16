using FSO.Server.Database.DA;
using FSO.Server.Database.DA.Shards;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FSO.Server.DataService.Shards
{
    public class ShardsDataService : AbstractDataService
    {
        private List<Shard> Shards = new List<Shard>();
        private IDAFactory DAFactory;

        public ShardsDataService(IDAFactory factory)
        {
            this.DAFactory = factory;
            this.Poll();
        }

        public Shard GetById(int id)
        {
            return Shards.FirstOrDefault(x => x.shard_id == id);
        }

        public Shard GetByName(string name)
        {
            return Shards.FirstOrDefault(x => x.name == name);
        }

        public List<Shard> GetShards()
        {
            return Shards;
        }

        /// <summary>
        /// Poll the db for latest information
        /// </summary>
        private void Poll()
        {
            using (var da = DAFactory.Get())
            {
                var all = da.Shards.All();
                this.Shards = all;
            }
        }
    }
}
