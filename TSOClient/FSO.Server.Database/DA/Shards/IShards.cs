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
        void UpdateStatus(int shard_id, string internal_host, string public_host, string channel, string version, int? update_id);
        void UpdateInfo(int shard_id, string name, string map);
    }
}
