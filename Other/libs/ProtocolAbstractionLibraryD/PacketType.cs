using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ProtocolAbstractionLibraryD
{
    /// <summary>
    /// All PacketType enumerations are used by both client and server unless otherwise specified.
    /// </summary>
    public enum PacketType
    {
        LOGIN_REQUEST = 0x00,
        LOGIN_NOTIFY = 0x01,
        LOGIN_FAILURE = 0x02,   //Only used by server.
        CHALLENGE_RESPONSE = 0x03, //Only used by client.
        LOGIN_SUCCESS = 0x04, //Only used by server.
        INVALID_VERSION = 0x05,
        CHARACTER_LIST = 0x06,
        CITY_LIST = 0x07,
        CHARACTER_CREATE = 0x08,
        CHARACTER_CREATION_STATUS = 0x09,

        RETIRE_CHARACTER = 0x010,
        RETIRE_CHARACTER_STATUS = 0x11,

        CHARACTER_CREATE_CITY = 0x64,
        CHARACTER_CREATE_CITY_FAILED = 0x65,
        REQUEST_CITY_TOKEN = 0x67,
        CITY_TOKEN = 0x68
    }
}
