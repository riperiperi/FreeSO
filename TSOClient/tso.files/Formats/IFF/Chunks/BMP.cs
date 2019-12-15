/*
 * This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
 * If a copy of the MPL was not distributed with this file, You can obtain one at
 * http://mozilla.org/MPL/2.0/. 
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using FSO.Files.Utils;
using Microsoft.Xna.Framework.Graphics;

namespace FSO.Files.Formats.IFF.Chunks
{
    /// <summary>
    /// This chunk type holds an image in BMP format.
    /// </summary>
    public class BMP : IffChunk
    {
        public byte[] data;

        /// <summary>
        /// Reads a BMP chunk from a stream.
        /// </summary>
        /// <param name="iff">An Iff instance.</param>
        /// <param name="stream">A Stream object holding a BMP chunk.</param>
        public override void Read(IffFile iff, Stream stream)
        {
            data = new byte[stream.Length];
            stream.Read(data, 0, (int)stream.Length);
        }

        public Texture2D GetTexture(GraphicsDevice device)
        {
            var tex = ImageLoader.FromStream(device, new MemoryStream(data));
            return tex;
            //return Texture2D.FromStream(device, new MemoryStream(data));
        }

        public override bool Write(IffFile iff, Stream stream)
        {
            stream.Write(data, 0, data.Length);
            return true;
        }
    }


}
