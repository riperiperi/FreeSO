namespace FSO.Server.Database.DA.AuthTickets
{
    public class AuthTicket
    {
        public string ticket_id { get; set; }
        public uint user_id { get; set; }
        public uint date { get; set; }
        public string ip { get; set; }
    }
}
