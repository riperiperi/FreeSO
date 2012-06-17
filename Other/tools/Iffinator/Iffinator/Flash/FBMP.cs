using System;
using System.Collections.Generic;
using System.Text;
using System.Drawing;
using System.IO;

namespace Iffinator.Flash
{
    /// <summary>
    /// A chunk that holds a regular Windows bitmap.
    /// Acts as a palettemap (PALT) for SPR and SPR2.
    /// </summary>
    public class FBMP : IffChunk
    {
        private Bitmap m_BitmapData;

        public FBMP(IffChunk Chunk) : base(Chunk)
        {
            MemoryStream MemStream = new MemoryStream(Chunk.Data);

            m_BitmapData = new Bitmap(MemStream);
        }
    }
}
