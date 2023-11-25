using FSO.Content.Framework;
using FSO.Content.Model;

namespace FSO.Content.TS1
{
    public class TS1AvatarTextureProvider : TS1SubProvider<ITextureRef>
    {
        public TS1AvatarTextureProvider(TS1Provider baseProvider) : base(baseProvider, new string[] { ".bmp", ".tga" })
        {
        }

        public override ITextureRef Get(string name)
        {
            var try1 = base.Get(name.ToLowerInvariant() + ".bmp");
            if (try1 != null) return try1;
            return base.Get(name.ToLowerInvariant()+".tga");
        }
    }
}
