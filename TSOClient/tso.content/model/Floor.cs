using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TSO.Files.formats.iff.chunks;

namespace TSO.Content.model
{
    /// <summary>
    /// A floor in the game world.
    /// </summary>
    public class Floor
    {
        public ushort ID;
        public SPR2 Near;
        public SPR2 Medium;
        public SPR2 Far;
    }
}
