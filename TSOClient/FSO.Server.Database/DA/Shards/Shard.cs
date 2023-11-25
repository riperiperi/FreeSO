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
        public int? update_id; //new update system. set by whichever server is running the shard.
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
