using FSO.Server.Database.DA;
using FSO.Server.Database.DA.Shards;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FSO.Common.Domain.Shards
{
    public class Shards : IShardsDomain
    {
        private List<Shard> _Shards = new List<Shard>();
        private IDAFactory _DbFactory;

        public Shards(IDAFactory factory)
        {
            _DbFactory = factory;
            Poll();
        }

        public List<Shard> All
        {
            get{
                return _Shards;
            }
        }

        private void Poll()
        {
            using (var db = _DbFactory.Get())
            {
                _Shards = db.Shards.All();
            }
        }

        public Shard GetById(int id)
        {
            return _Shards.FirstOrDefault(x => x.shard_id == id);
        }

        public Shard GetByName(string name)
        {
            return _Shards.FirstOrDefault(x => x.name == name);
        }
    }
}
