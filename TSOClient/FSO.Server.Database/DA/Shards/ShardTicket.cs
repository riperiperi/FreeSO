namespace FSO.Server.Database.DA.Shards
{
    public class ShardTicket
    {
        public string ticket_id { get; set; }
        public uint user_id { get; set; }
        public uint date { get; set; }
        public string ip { get; set; }
        public uint avatar_id { get; set; }
    }
}
