namespace FSO.Server.Database.DA.AuthTickets
{
    public interface IAuthTickets
    {
        void Create(AuthTicket ticket);
        AuthTicket Get(string id);
        void Delete(string id);
        void Purge(uint time);
    }
}
