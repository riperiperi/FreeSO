namespace FSO.Server.Database.DA.ArchiveRecents
{
    public interface IArchiveRecents
    {
        IEnumerable<int> AvatarsByUser(int user_id, int limit);
        void RecordAvatarUse(int user_id, int avatar_id);
    }
}
