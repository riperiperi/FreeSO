using System.Collections.Generic;

namespace FSO.Server.Database.DA.Shards
{
    public interface IShards
    {
        List<Shard> All();

        void CreateTicket(ShardTicket ticket);
        void DeleteTicket(string ticket_id);
        ShardTicket GetTicket(string ticket_id);
        void PurgeTickets(uint time);
        void UpdateVersion(int shard_id, string name, string number, int? update_id);
    }
}
