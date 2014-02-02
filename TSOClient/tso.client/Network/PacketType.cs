using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TSOClient.Network
{
    public enum PacketType
    {
        LOGIN_NOTIFY = 0x01,
        LOGIN_FAILURE = 0x2,
        CHARACTER_LIST = 0x5,
        CITY_LIST = 0x6
    }
}
