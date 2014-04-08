using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TSO.Files.formats.iff.chunks;

namespace TSO.Content.model
{
    /// <summary>
    /// A wall resource.
    /// </summary>
    public class Wall
    {
        public ushort ID;
        public SPR Near;
        public SPR Medium;
        public SPR Far;
    }
}
