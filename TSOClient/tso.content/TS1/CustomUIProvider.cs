using FSO.Content.Codecs;
using FSO.Content.Framework;
using FSO.Content.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace FSO.Content.TS1
{
    public class CustomUIProvider : FileProvider<ITextureRef>
    {
        public CustomUIProvider(Content contentManager)
            : base(contentManager, new TextureCodec(), new Regex("uigraphics/.*\\.png"))
        {
            UseContent = true;
        }
    }
}
