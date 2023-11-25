using FSO.Vitaboy;
using FSO.Content.Framework;
using System.Text.RegularExpressions;
using FSO.Content.Codecs;

namespace FSO.Content
{
    /// <summary>
    /// Provides access to animation (*.anim) data in FAR3 archives.
    /// </summary>
    public class AvatarAnimationProvider : TSOAvatarContentProvider<Animation>
    {
        public AvatarAnimationProvider(Content contentManager) : base(contentManager, new AnimationCodec(), 
            new Regex(".*/animations/.*\\.dat"), 
            new Regex("Avatar/Animations/.*\\.anim"))
        {
        }
    }
}
