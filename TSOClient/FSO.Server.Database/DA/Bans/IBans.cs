namespace FSO.Server.Database.DA.Bans
{
    public interface IBans
    {
        DbBan GetByIP(string ip);
        void Add(string ip, uint userid, string reason, int enddate, string client_id);

        DbBan GetByClientId(string client_id);
        void Remove(uint user_id);
    }
}
