using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using tso.content.codecs;
using tso.content.framework;
using tso.vitaboy;
using System.Text.RegularExpressions;

namespace tso.content
{
    /// <summary>
    /// Provides access to outfit (*.oft) data in FAR3 archives.
    /// </summary>
    public class AvatarOutfitProvider : FAR3Provider<Outfit>
    {
        public AvatarOutfitProvider(Content contentManager)
            : base(contentManager, new OutfitCodec(), new Regex(".*\\\\outfits\\\\.*\\.dat"))
        {
        }
    }
}
