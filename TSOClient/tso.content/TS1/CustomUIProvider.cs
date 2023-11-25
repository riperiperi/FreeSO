using FSO.Content.Codecs;
using FSO.Content.Framework;
using FSO.Content.Model;
using System.Text.RegularExpressions;

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
