using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
