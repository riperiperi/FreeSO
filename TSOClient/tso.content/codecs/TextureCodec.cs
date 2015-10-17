/*
 * This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
 * If a copy of the MPL was not distributed with this file, You can obtain one at
 * http://mozilla.org/MPL/2.0/. 
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework.Graphics;
using FSO.Content.Framework;
using System.IO;
using FSO.Common.Utils;
using FSO.Content.Model;

namespace FSO.Content.Codecs
{
    /// <summary>
    /// Codec for textures (*.jpg).
    /// </summary>
    public class TextureCodec : IContentCodec<ITextureRef>
    {
        private bool Mask = false;
        private uint[] MaskColors = null;

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
            this.Mask = true;
            this.MaskColors = maskColors;
        }

        #region IContentCodec<Texture2D> Members

        public ITextureRef Decode(System.IO.Stream stream)
        {
            byte[] data = new byte[stream.Length];
            stream.Read(data, 0, data.Length);

            if (Mask == false)
            {
                return new InMemoryTextureRef(data);
            }
            else
            {
                return new InMemoryTextureRefWithMask(data, MaskColors);
            }
        }

        #endregion
    }
}
