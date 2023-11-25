namespace FSO.Server.Database.DA.AvatarClaims
{
    public class DbAvatarClaim
    {
        public int avatar_claim_id { get; set; }
        public uint avatar_id { get; set; }
        public string owner { get; set; }
        public uint location { get; set; }
    }
    public class DbAvatarActive
    {
        public uint avatar_id { get; set; }
        public string name { get; set; }
        public byte privacy_mode { get; set; }
        public uint location { get; set; }
    }
}
