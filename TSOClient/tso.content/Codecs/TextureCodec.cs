using FSO.Content.Framework;
using FSO.Content.Model;
using FSO.Common;

namespace FSO.Content.Codecs
{
    /// <summary>
    /// Codec for textures (*.jpg).
    /// </summary>
    public class TextureCodec : IContentCodec<ITextureRef>
    {
        private bool Mask = false;
        private uint[] MaskColors = null;
        private bool Mipmap = false;

        /// <summary>
        /// Creates a new instance of TextureCodec.
        /// </summary>
        /// <param name="device">A GraphicsDevice instance.</param>
        public TextureCodec()
        {
        }

        /// <summary>
        /// Creates a new instance of TextureCodec.
        /// </summary>
        /// <param name="device">A GraphicsDevice instance.</param>
        /// <param name="maskColors">A list of masking colors to use for this texture.</param>
        public TextureCodec(uint[] maskColors)
        {
            this.Mask = maskColors.Length > 0;
            this.MaskColors = maskColors;
        }

        /// <summary>
        /// Creates a new instance of TextureCodec.
        /// </summary>
        /// <param name="device">A GraphicsDevice instance.</param>
        /// <param name="maskColors">A list of masking colors to use for this texture.</param>
        /// <param name="mips">If this texture codec should generate mipmaps.</param>
        public TextureCodec(uint[] maskColors, bool mips) : this(maskColors)
        {
            Mipmap = FSOEnvironment.EnableNPOTMip && mips;
        }


        #region IContentCodec<Texture2D> Members

        public override object GenDecode(System.IO.Stream stream)
        {
            byte[] data = new byte[stream.Length];
            stream.Read(data, 0, data.Length);

            if (Mask == false)
            {
                var r = new InMemoryTextureRef(data);
                r.Mipmap = Mipmap;
                return r;
            }
            else
            {
                return new InMemoryTextureRefWithMask(data, MaskColors);
            }
        }

        #endregion
    }
}