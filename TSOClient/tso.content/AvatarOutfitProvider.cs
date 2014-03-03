using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TSO.Content.codecs;
using TSO.Content.framework;
using TSO.Vitaboy;
using System.Text.RegularExpressions;

namespace TSO.Content
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
