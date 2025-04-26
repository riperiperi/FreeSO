namespace FSO.Server.Database.DA.ArchiveUsers
{
    public interface IArchiveUsers
    {
        ArchiveUser GetByClientHash(string clientHash);
        ArchiveUser GetByDisplayName(string displayName);
        void UpdateDisplayName(uint id, string displayName);
        uint Create(ArchiveUser user);
    }
}
