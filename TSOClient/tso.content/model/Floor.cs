using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using tso.files.formats.iff.chunks;

namespace tso.content.model
{
    public class Floor
    {
        public ushort ID;
        public SPR2 Near;
        public SPR2 Medium;
        public SPR2 Far;
    }
}
