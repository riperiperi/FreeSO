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
    /// Provides access to binding (*.bnd) data in FAR3 archives.
    /// </summary>
    public class AvatarBindingProvider : FAR3Provider<Binding>
    {
        public AvatarBindingProvider(Content contentManager)
            : base(contentManager, new BindingCodec(), new Regex(".*\\\\bindings\\\\.*\\.dat"))
        {
        }
    }
}
