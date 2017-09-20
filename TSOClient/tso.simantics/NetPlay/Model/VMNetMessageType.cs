using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FSO.SimAntics.NetPlay.Model
{
    public enum VMNetMessageType : byte
    {
        //server -> client
        BroadcastTick = 0,
        Direct = 1,
        AvatarData = 2,
        
        //client -> server
        Command = 128
    }
}
