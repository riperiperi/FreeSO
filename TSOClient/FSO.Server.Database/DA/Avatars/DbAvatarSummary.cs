namespace FSO.Server.Database.DA.Avatars
{
    public class DbAvatarSummary
    {
        //fso_avatar data
        public uint avatar_id { get; set; }
        public int shard_id { get; set; }
        public uint user_id { get; set; }
        public string name { get; set; }
        public DbAvatarGender gender { get; set; }
        public uint date { get; set; }
        public byte skin_tone { get; set; }
        public ulong head { get; set; }
        public ulong body { get; set; }
        public string description { get; set; }
        
        //fso_lots
        public uint? lot_id { get; set; }
        public uint? lot_location { get; set; }
        public string lot_name { get; set; }
    }
}
