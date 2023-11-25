namespace FSO.Server.Database.DA.Elections
{
    public class DbElectionCycle
    {
        public uint cycle_id { get; set; }
        public uint start_date { get; set; }
        public uint end_date { get; set; }
        public DbElectionCycleState current_state { get; set; }
        public DbElectionCycleType election_type { get; set; }

        //for free vote
        public string name { get; set; }
        public int nhood_id { get; set; }
    }

    public enum DbElectionCycleState : byte
    {
        shutdown,
        nomination,
        election,
        ended,
        failsafe
    }

    public enum DbElectionCycleType : byte
    {
        election,
        shutdown
    }
}
