using System.Collections.Generic;

namespace FSO.Server.Database.DA.Bookmarks
{
    public interface IBookmarks
    {
        List<DbBookmark> GetByAvatarId(uint avatar_id);
        List<uint> GetAvatarIgnore(uint avatar_id);
        void Create(DbBookmark bookmark);
        bool Delete(DbBookmark bookmark);
    }
}
