namespace FSO.Server.Database.DA.Elections
{
    public class DbElectionFreeVote
    {
        public uint avatar_id { get; set; }
        public int neighborhood_id { get; set; }
        public uint cycle_id { get; set; }
        public uint date { get; set; }
        public uint expire_date { get; set; }
    }
}
