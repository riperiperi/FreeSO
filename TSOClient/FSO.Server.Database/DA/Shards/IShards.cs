using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FSO.Server.Database.DA.Shards
{
    public interface IShards
    {
        List<Shard> All();

        void CreateTicket(ShardTicket ticket);
        void DeleteTicket(string ticket_id);
        ShardTicket GetTicket(string ticket_id);
        void PurgeTickets(uint time);
    }
}
