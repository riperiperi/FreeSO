namespace FSO.SimAntics.NetPlay.Model
{
    public enum VMNetMessageType : byte
    {
        //server -> client
        BroadcastTick = 0,
        Direct = 1,
        AvatarData = 2,
        CatchupTick = 3,

        //client -> server
        Command = 128
    }
}
