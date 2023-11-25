namespace FSO.Server.Database.DA.Lots
{
    public class DbLotServerTicket
    {
        public string ticket_id { get; set; }
        public uint user_id { get; set; }
        public uint date { get; set; }
        public string ip { get; set; }
        public uint avatar_id { get; set; }
        public int lot_id { get; set; }
        public int avatar_claim_id { get; set; }
        public string avatar_claim_owner { get; set; }
        public string lot_owner { get; set; }
    }
}
