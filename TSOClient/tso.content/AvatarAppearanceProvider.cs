using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TSO.Content.framework;
using TSO.Vitaboy;
using TSO.Content.codecs;
using System.Text.RegularExpressions;

namespace TSO.Content
{
    /// <summary>
    /// Provides access to appearance (*.apr) data in FAR3 archives.
    /// </summary>
    public class AvatarAppearanceProvider : FAR3Provider<Appearance>
    {
        public AvatarAppearanceProvider(Content contentManager)
            : base(contentManager, new AppearanceCodec(), new Regex(".*\\\\appearances\\\\.*\\.dat"))
        {
        }
    }
}
