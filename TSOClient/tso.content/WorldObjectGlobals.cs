using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using tso.content.framework;
using tso.files.formats.iff;
using tso.content.codecs;
using System.Text.RegularExpressions;

namespace tso.content
{
    public class WorldObjectGlobals : FileProvider<Iff>
    {
        public WorldObjectGlobals(Content contentManager)
            : base(contentManager, new IffCodec(), new Regex(".*\\\\globals\\\\.*\\.iff"))
        {
        }
    }
}
