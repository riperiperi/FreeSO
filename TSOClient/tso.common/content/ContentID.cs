using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TSO.Common.content
{
    public class ContentID
    {
        public uint TypeID;
        public uint FileID;

        public ContentID(uint typeID, uint fileID)
        {
            this.TypeID = typeID;
            this.FileID = fileID;
        }

        public ulong Shift()
        {
            var fileIDLong = ((ulong)FileID) << 32;
            return fileIDLong | TypeID;
        }
    }
}
