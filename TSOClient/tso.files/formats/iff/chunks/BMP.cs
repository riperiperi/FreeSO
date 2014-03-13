using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using TSO.Files.utils;
using Microsoft.Xna.Framework.Graphics;

namespace TSO.Files.formats.iff.chunks
{
    /// <summary>
    /// This chunk type holds an image in BMP format.
    /// </summary>
    public class BMP : IffChunk
    {
        private byte[] data;

        public override void Read(Iff iff, Stream stream)
        {
            data = new byte[stream.Length];
            stream.Read(data, 0, (int)stream.Length);
        }

        public Texture2D GetTexture(GraphicsDevice device)
        {
            return Texture2D.FromFile(device, new MemoryStream(data));
        }
    }


}
