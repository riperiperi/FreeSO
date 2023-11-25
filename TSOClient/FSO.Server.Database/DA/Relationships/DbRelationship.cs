namespace FSO.Server.Database.DA.Relationships
{
    public class DbRelationship
    {
        public uint from_id { get; set; }
        public uint to_id { get; set; }
        public int value { get; set; }
        public uint index { get; set; }
        public uint? comment_id { get; set; }
        public uint date { get; set; }
    }
}
