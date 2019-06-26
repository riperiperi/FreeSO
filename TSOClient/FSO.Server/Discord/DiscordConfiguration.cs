using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FSO.Server.Discord
{
    public class DiscordConfiguration
    {
        public string ApiKey;
        public ulong ServerID;

        public ulong EventModChannelID;
        public ulong EventPublicChannelID;
        public ulong StatusChannelID;
    }
}
