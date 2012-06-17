using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Drawing;

namespace SimsLib.IFF
{
    class BMP_ : IffChunk
    {
        private Bitmap m_BitmapData;

        public BMP_(IffChunk Chunk) : base(Chunk)
        {
            MemoryStream MemStream = new MemoryStream(Chunk.Data);

            m_BitmapData = new Bitmap(MemStream);
        }
    }
}
