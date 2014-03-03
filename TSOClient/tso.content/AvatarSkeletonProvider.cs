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
    /// Provides access to skeleton (*.skel) data in FAR3 archives.
    /// </summary>
    public class AvatarSkeletonProvider : FAR3Provider<Skeleton>
    {
        public AvatarSkeletonProvider(Content contentManager)
            : base(contentManager, new SkeletonCodec(), new Regex(".*\\\\skeletons\\\\.*\\.dat"))
        {
        }
    }
}
