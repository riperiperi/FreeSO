using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace tso.common.content
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
    }
}
