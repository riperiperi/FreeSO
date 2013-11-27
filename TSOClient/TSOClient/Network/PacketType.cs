using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TSOClient.Network
{
    public enum PacketType
    {
        LOGIN_REQUEST = 0x00,
        LOGIN_NOTIFY = 0x01,
        LOGIN_FAILURE = 0x02,
        CHARACTER_LIST = 0x05,
        CITY_LIST = 0x06,
        CHARACTER_CREATE = 0x07
    }
}
