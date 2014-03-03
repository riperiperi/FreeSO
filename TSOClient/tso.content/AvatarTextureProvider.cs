using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TSO.Content.framework;
using Microsoft.Xna.Framework.Graphics;
using TSO.Content.codecs;
using System.Text.RegularExpressions;

namespace TSO.Content
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
