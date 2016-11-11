using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FSO.Server.Database.DA.Bookmarks
{
    public class DbBookmark
    {
        public uint avatar_id { get; set; }
        public byte type { get; set; }
        public uint target_id { get; set; }
    }
}
