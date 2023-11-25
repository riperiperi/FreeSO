using FSO.Content.Framework;
using FSO.Files.Formats.IFF;
using FSO.Content.Codecs;
using System.Text.RegularExpressions;

namespace FSO.Content
{
    /// <summary>
    /// Provides access to object global (*.iff) data in FAR3 archives.
    /// </summary>
    public class WorldObjectGlobals : FileProvider<IffFile>
    {
        public WorldObjectGlobals(Content contentManager)
            : base(contentManager, new IffCodec(), new Regex(".*/globals/.*\\.iff"))
        {
        }
    }
}
