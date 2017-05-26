using FSO.Content.Framework;
using FSO.Content.Model;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FSO.Content.TS1
{
    public class TS1AvatarTextureProvider : TS1SubProvider<ITextureRef>
    {
        public TS1AvatarTextureProvider(TS1Provider baseProvider) : base(baseProvider, ".bmp")
        {
        }

        public override ITextureRef Get(string name)
        {
            return base.Get(name.Replace(".jpg", "").ToLowerInvariant() + ".bmp");
        }
    }
}
