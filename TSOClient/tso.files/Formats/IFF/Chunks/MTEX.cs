using FSO.Common.Rendering;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FSO.Files.Formats.IFF.Chunks
{
    /// <summary>
    /// Texture for a 3D Mesh. Can be jpg, png or bmp. 
    /// </summary>
    public class MTEX : IffChunk
    {
        private byte[] data;
        private Texture2D Cached;

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

        public override bool Write(IffFile iff, Stream stream)
        {
            stream.Write(data, 0, data.Length);
            return true;
        }

        public Texture2D GetTexture(GraphicsDevice device)
        {
            if (Cached == null) Cached = ImageLoader.FromStream(device, new MemoryStream(data));
            if (!IffFile.RETAIN_CHUNK_DATA) data = null;
            return Cached;
        }

        public void SetData(byte[] data)
        {
            this.data = data;
            Cached = null;
        }
    }
}
