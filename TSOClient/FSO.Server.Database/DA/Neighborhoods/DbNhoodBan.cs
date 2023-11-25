namespace FSO.Server.Database.DA.Neighborhoods
{
    public class DbNhoodBan
    {
        public uint user_id { get; set; }
        public string ban_reason { get; set; }
        public uint end_date { get; set; }
    }
}
