using System;

namespace FSO.Server.Database.DA.Hosts
{
    public class DbHost
    {
        public string call_sign { get; set; }
        public DbHostRole role { get; set; }
        public DbHostStatus status { get; set; }
        public string internal_host { get; set; }
        public string public_host { get; set; }
        public DateTime time_boot { get; set; }
        public int? shard_id { get; set; }
    }

    public enum DbHostRole
    {
        city,
        lot,
        task
    }

    public enum DbHostStatus
    {
        up,
        down
    }
}
