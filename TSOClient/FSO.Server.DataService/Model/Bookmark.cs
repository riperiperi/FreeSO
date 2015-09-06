using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FSO.Common.DataService.Model
{
    public class Bookmark
    {
        public uint Bookmark_TargetID { get; set; }
        public BookmarkType Bookmark_Type { get; set; }
    }

    public enum BookmarkType : byte
    {
        BOOKMARK = 0x01
    }
}
