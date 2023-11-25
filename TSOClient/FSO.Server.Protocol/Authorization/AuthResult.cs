namespace FSO.Server.Protocol.Authorization
{
    public class AuthResult
    {
        public bool Valid;
        public string Ticket;
        public string ReasonCode;
        public string ReasonText;
        public string ReasonURL;
    }
}
