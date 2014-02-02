using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using tso.content.framework;
using tso.vitaboy;
using tso.content.codecs;
using System.Text.RegularExpressions;

namespace tso.content
{
    public class AvatarAppearanceProvider : FAR3Provider<Appearance>
    {
        public AvatarAppearanceProvider(Content contentManager)
            : base(contentManager, new AppearanceCodec(), new Regex(".*\\\\appearances\\\\.*\\.dat"))
        {
        }
    }
}
