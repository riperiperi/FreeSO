namespace FSO.Server.Protocol.Embedded
{
    public enum ArchiveDbUserStatus
    {
        Normal,
        Unverified,
        Banned,
        Mod,
        Admin

    }
    public struct ArchiveDbUser
    {
        public uint ID;
        public int AvatarCount;
        public ArchiveDbUserStatus Status;
        public string Name;
        public string IP;
    }

    public struct ArchiveDbAvatar
    {
        public uint ID;
        public string Name;
        public string LotName;
        public ulong LastLogin;
    }

    public struct ArchiveDbIpBan
    {
        public string IP;
    }
}
