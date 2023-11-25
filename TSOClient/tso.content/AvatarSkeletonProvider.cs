using FSO.Vitaboy;
using FSO.Content.Framework;
using System.Text.RegularExpressions;
using FSO.Content.Codecs;

namespace FSO.Content
{
    /// <summary>
    /// Provides access to skeleton (*.skel) data in FAR3 archives.
    /// </summary>
    public class AvatarSkeletonProvider : TSOAvatarContentProvider<Skeleton>
    {
        public AvatarSkeletonProvider(Content contentManager) : base(contentManager, new SkeletonCodec(),
            new Regex(".*/skeletons/.*\\.dat"),
            new Regex("Avatar/Skeletons/.*\\.skel"))
        {
        }
    }
}
