namespace FSO.Server.Protocol.Electron.Model
{
    public enum ChangeRoommateType : byte
    {
        INVITE = 0,
        KICK = 1,
        ACCEPT = 2,
        DECLINE = 3,
        POLL = 4
    }
}
