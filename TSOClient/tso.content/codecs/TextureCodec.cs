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

namespace FSO.Content.Codecs
{
    /// <summary>
    /// Codec for textures (*.jpg).
    /// </summary>
    public class TextureCodec : IContentCodec<Texture2D>
    {
        private GraphicsDevice Device;
        private bool Mask = false;
        private uint[] MaskColors = null;

        /// <summary>
        /// Creates a new instance of TextureCodec.
        /// </summary>
        /// <param name="device">A GraphicsDevice instance.</param>
        public TextureCodec(GraphicsDevice device)
        {
            this.Device = device;
        }

        /// <summary>
        /// Creates a new instance of TextureCodec.
        /// </summary>
        /// <param name="device">A GraphicsDevice instance.</param>
        /// <param name="maskColors">A list of masking colors to use for this texture.</param>
        public TextureCodec(GraphicsDevice device, uint[] maskColors)
        {
            this.Device = device;
            this.Mask = true;
            this.MaskColors = maskColors;
        }

        #region IContentCodec<Texture2D> Members

        public Texture2D Decode(System.IO.Stream stream)
        {
            /**
             * This may not be the right way to get the texture to load as ARGB but it works :S
             */
            Texture2D texture = null;
            if(Mask)
            {
                stream.Seek(0, SeekOrigin.Begin);
                texture = Texture2D.FromStream(Device, stream);

                TextureUtils.ManualTextureMaskSingleThreaded(ref texture, MaskColors);
            }
            else
            {
                texture = Texture2D.FromStream(Device, stream);
            }

            return texture;
        }

        #endregion
    }
}
