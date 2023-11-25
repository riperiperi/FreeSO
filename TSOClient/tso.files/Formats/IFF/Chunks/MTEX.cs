using FSO.Common;
using FSO.Common.Utils;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.IO;

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
            if (Cached == null)
            {
                Cached = ImageLoader.FromStream(device, new MemoryStream(data));
                if (FSOEnvironment.EnableNPOTMip)
                {
                    var data = new Color[Cached.Width * Cached.Height];
                    Cached.GetData(data);
                    var n = new Texture2D(device, Cached.Width, Cached.Height, true, SurfaceFormat.Color);
                    TextureUtils.UploadWithMips(n, device, data);
                    Cached.Dispose();
                    Cached = n;
                }
            }
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
