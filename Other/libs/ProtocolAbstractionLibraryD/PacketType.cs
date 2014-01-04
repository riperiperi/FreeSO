using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ProtocolAbstractionLibraryD
{
    public enum PacketType
    {
        LOGIN_REQUEST = 0x00,
        LOGIN_NOTIFY = 0x01,
        LOGIN_FAILURE = 0x02,
        CHARACTER_LIST = 0x05,
        CITY_LIST = 0x06,
        CHARACTER_CREATE = 0x07,
        CHARACTER_CREATION_STATUS = 0x08,

        CHARACTER_CREATE_CITY = 0x64,
        CHARACTER_CREATE_CITY_FAILED = 0x65,
        REQUEST_CITY_TOKEN = 0x67,
        CITY_TOKEN = 0x68
    }
}
