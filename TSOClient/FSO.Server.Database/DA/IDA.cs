using FSO.Server.Database.DA.AuthTickets;
using FSO.Server.Database.DA.Avatars;
using FSO.Server.Database.DA.Lots;
using FSO.Server.Database.DA.Shards;
using FSO.Server.Database.DA.Users;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FSO.Server.Database.DA
{
    public interface IDA : IDisposable
    {
        IUsers Users { get; }
        IAuthTickets AuthTickets { get; }
        IShards Shards { get; }
        IAvatars Avatars { get; }
        ILots Lots { get; }

        void Flush();
    }
}
