using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FSO.Server.Database.DA.Shards
{
    public class Shard
    {
        public int shard_id;
        public string name;
        public int rank;
        public string map;
        public ShardStatus status;
        public string internal_host;
        public string public_host;
        public string version_name;
        public string version_number;
    }

    public enum ShardStatus
    {
        Up,
        Down,
        Busy,
        Full,
        Closed,
        Frontier
    }
}
