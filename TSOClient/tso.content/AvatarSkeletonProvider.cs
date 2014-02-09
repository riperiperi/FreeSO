using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using tso.vitaboy;
using tso.content.framework;
using System.Text.RegularExpressions;
using tso.content.codecs;

namespace tso.content
{
    public class AvatarSkeletonProvider : FAR3Provider<Skeleton>
    {
        public AvatarSkeletonProvider(Content contentManager)
            : base(contentManager, new SkeletonCodec(), new Regex(".*\\\\skeletons\\\\.*\\.dat"))
        {
        }
    }
}
