using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TSO.Content.framework;
using TSO.Files.formats.iff;
using TSO.Content.codecs;
using System.Text.RegularExpressions;

namespace TSO.Content
{
    /// <summary>
    /// Provides access to object global (*.iff) data in FAR3 archives.
    /// </summary>
    public class WorldObjectGlobals : FileProvider<Iff>
    {
        public WorldObjectGlobals(Content contentManager)
            : base(contentManager, new IffCodec(), new Regex(".*\\\\globals\\\\.*\\.iff"))
        {
        }
    }
}
