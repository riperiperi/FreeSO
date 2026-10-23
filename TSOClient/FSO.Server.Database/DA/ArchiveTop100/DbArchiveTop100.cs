using FSO.Common.Enum;

namespace FSO.Server.Database.DA.ArchiveTop100
{
    public class DbArchiveTop100
    {
        public int shard_id { get; set; }
        public int lot_id { get; set; }
        public int minutes { get; set; }
        public int category { get; set; }
        public int rank { get; set; }
    }

    public class DbArchiveTop100WithLot : DbArchiveTop100
    {
        public int location { get; set; }
        public int neighborhood_id { get; set; }
        public string name { get; set; }
    }
}
