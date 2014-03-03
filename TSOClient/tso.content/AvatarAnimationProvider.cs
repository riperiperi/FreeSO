using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TSO.Vitaboy;
using TSO.Content.framework;
using System.Text.RegularExpressions;
using TSO.Content.codecs;

namespace TSO.Content
{
    /// <summary>
    /// Provides access to animation (*.anim) data in FAR3 archives.
    /// </summary>
    public class AvatarAnimationProvider : FAR3Provider<Animation>
    {
        public AvatarAnimationProvider(Content contentManager)
            : base(contentManager, new AnimationCodec(), new Regex(".*\\\\animations\\\\.*\\.dat"))
        {
        }
    }
}
