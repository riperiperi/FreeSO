namespace FSO.Server.Database.DA.Elections
{
    public class DbMayorRating
    {
        public uint rating_id { get; set; }
        public uint from_user_id { get; set; }
        public uint to_user_id { get; set; }

        public uint rating { get; set; }
        public string comment { get; set; }
        public uint date { get; set; }

        public uint from_avatar_id { get; set; }
        public uint to_avatar_id { get; set; }
        public byte anonymous { get; set; }
        public uint neighborhood { get; set; }
    }
}
