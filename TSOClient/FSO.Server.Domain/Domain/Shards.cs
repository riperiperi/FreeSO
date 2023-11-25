using FSO.Common.Domain.Shards;
using FSO.Server.Database.DA;
using FSO.Server.Protocol.CitySelector;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace FSO.Server.Domain
{
    public class Shards : IShardsDomain
    {
        private List<ShardStatusItem> _Shards = new List<ShardStatusItem>();
        private IDAFactory _DbFactory;
        private DateTime _LastPoll;

        public Shards(IDAFactory factory)
        {
            _DbFactory = factory;
            Poll();
        }

        public List<ShardStatusItem> All
        {
            get{
                return _Shards;
            }
        }

        public int? CurrentShard
        {
            get
            {
                throw new Exception("CurrentShard not avaliable in server domain");
            }
        }

        public void AutoUpdate()
        {
            Task.Delay(60000).ContinueWith(x =>
            {
                try{
                    Poll();
                }catch(Exception ex){
                }
                AutoUpdate();
            });
        }

        public void Update()
        {

        }

        private void Poll()
        {
            _LastPoll = DateTime.UtcNow;
            
            using (var db = _DbFactory.Get())
            {
                _Shards = db.Shards.All().Select(x => new ShardStatusItem()
                {
                    Id = x.shard_id,
                    Name = x.name,
                    Map = x.map,
                    Rank = x.rank,
                    Status = (Server.Protocol.CitySelector.ShardStatus)(byte)x.status,
                    PublicHost = x.public_host,
                    InternalHost = x.internal_host,
                    VersionName = x.version_name,
                    VersionNumber = x.version_number,
                    UpdateID = x.update_id
                }).ToList();
            }
        }

        public ShardStatusItem GetById(int id)
        {
            return _Shards.FirstOrDefault(x => x.Id == id);
        }

        public ShardStatusItem GetByName(string name)
        {
            return _Shards.FirstOrDefault(x => x.Name == name);
        }
    }
}
