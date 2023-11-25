namespace FSO.Server.Database.DA.Elections
{
    public class DbElectionCandidate
    {
        public uint election_cycle_id { get; set; }
        public uint candidate_avatar_id { get; set; }
        public string comment { get; set; }
        public DbCandidateState state { get; set; }
    }

    public enum DbCandidateState
    {
        informed,
        running,
        disqualified,
        lost,
        won
    }
}
