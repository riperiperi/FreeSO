namespace FSO.Server.Protocol.Electron.Model
{
    public enum ChangeRoommateResponseStatus : byte
    {
        INVITE_SUCCESS = 0,

        //invite codes
        ROOMIE_ELSEWHERE = 1,
        TOO_MANY_ROOMMATES = 2,
        OTHER_INVITE_PENDING = 3,

        //shared
        YOU_ARE_NOT_OWNER = 4, //everything but move out

        //kick or move out
        YOU_ARE_NOT_ROOMMATE = 5,
        LOT_MUST_BE_CLOSED = 6, //move out of lot with 1 roommate

        //invite response
        LOT_DOESNT_EXIST = 7,
        NO_INVITE_PENDING = 8,

        ACCEPT_SUCCESS = 9,
        DECLINE_SUCCESS = 10,
        KICK_SUCCESS = 11,
        SELFKICK_SUCCESS = 12,

        ROOMMATE_LEFT = 13,
        GOT_KICKED = 14,

        UNKNOWN = 255
    }
}
