using FSO.Vitaboy;
using FSO.Content.Framework;
using System.Text.RegularExpressions;
using FSO.Content.Codecs;

namespace FSO.Content
{
    /// <summary>
    /// Provides access to binding (*.bnd) data in FAR3 archives.
    /// </summary>
    public class AvatarBindingProvider : TSOAvatarContentProvider<Binding>
    {
        public AvatarBindingProvider(Content contentManager) : base(contentManager, new BindingCodec(),
            new Regex(".*/bindings/.*\\.dat"),
            new Regex("Avatar/Bindings/.*\\.bnd"))
        {
        }
    }
}
