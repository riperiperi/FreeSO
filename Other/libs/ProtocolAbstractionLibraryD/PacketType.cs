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

        LOGIN_REQUEST_CITY = 0x63,
        LOGIN_SUCCESS_CITY = 0x64,
        LOGIN_NOTIFY_CITY = 0x65,
        LOGIN_FAILURE_CITY = 0x66,
        CHARACTER_CREATE_CITY = 0x67,
        CHARACTER_CREATE_CITY_FAILED = 0x68,
        REQUEST_CITY_TOKEN = 0x69,
        CITY_TOKEN = 0x70,

        PLAYER_JOINED_SESSION = 0x71,
        PLAYER_LEFT_SESSION = 0x72,
        PLAYER_SENT_LETTER = 0x73,
        PLAYER_RECV_LETTER = 0x74,
        PLAYER_BROADCAST_LETTER = 0x75,
		PLAYER_ALREADY_ONLINE = 0x76 //Sent by login server to client when transfer was requested.
    }
}