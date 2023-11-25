namespace FSO.Server.Database.DA.Neighborhoods
{
    public class DbNeighborhood
    {
        public int neighborhood_id { get; set; }
        public string name { get; set; }
        public string description { get; set; }
        public int shard_id { get; set; }

        public uint location { get; set; }
        public uint color { get; set; }
        public uint flag { get; set; }

        public int? town_hall_id { get; set; }
        public string icon_url { get; set; }
        public string guid { get; set; }

        public uint? mayor_id { get; set; }
        public uint mayor_elected_date { get; set; }
        public uint? election_cycle_id { get; set; }
    }
}
