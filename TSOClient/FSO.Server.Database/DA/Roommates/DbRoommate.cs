namespace FSO.Server.Database.DA.Roommates
{
    public class DbRoommate
    {
        public uint avatar_id { get; set; }
        public int lot_id { get; set; }
        public byte permissions_level { get; set; }
        public byte is_pending { get; set; }
    }
}
