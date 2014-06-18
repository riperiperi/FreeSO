using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework.Graphics;
using TSO.Content.framework;
using TSO.Content.codecs;
using Microsoft.Xna.Framework;

namespace TSO.Content
{
    /// <summary>
    /// Provides access to UI texture (*.bmp) data in FAR3 archives.
    /// </summary>
    public class UIGraphicsProvider : PackingslipProvider<Texture2D>
    {
        public static uint[] MASK_COLORS = new uint[]{
            new Color(0xFF, 0x00, 0xFF, 0xFF).PackedValue,
            new Color(0xFE, 0x02, 0xFE, 0xFF).PackedValue,
            new Color(0xFF, 0x01, 0xFF, 0xFF).PackedValue
        };

        public UIGraphicsProvider(Content contentManager, GraphicsDevice device)
            : base(contentManager, "packingslips\\uigraphics.xml", new TextureCodec(device, MASK_COLORS))
        {
        }
    }
}
