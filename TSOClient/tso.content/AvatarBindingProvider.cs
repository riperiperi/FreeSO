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
    public class AvatarBindingProvider : FAR3Provider<Binding>
    {
        public AvatarBindingProvider(Content contentManager)
            : base(contentManager, new BindingCodec(), new Regex(".*\\\\bindings\\\\.*\\.dat"))
        {
        }
    }
}
