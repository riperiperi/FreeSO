using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using tso.content.framework;
using Microsoft.Xna.Framework.Graphics;
using tso.content.codecs;
using System.Text.RegularExpressions;

namespace tso.content
{
    /// <summary>
    /// Provides access to texture (*.jpg) data in FAR3 archives.
    /// </summary>
    public class AvatarTextureProvider : PackingslipProvider<Texture2D> {
        public AvatarTextureProvider(Content contentManager, GraphicsDevice device)
            : base(contentManager, "packingslips\\textures.xml", new TextureCodec(device))
        {
        }
    }
}
