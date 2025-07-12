using FSO.Server.Framework.Aries;
using System;

namespace FSO.Server.Servers.City
{
    public class CityServerContext
    {
        public int ShardId;
        public CityServerConfiguration Config;
        public ISessions Sessions;
        public Action<bool> BroadcastUserList;
    }
}
