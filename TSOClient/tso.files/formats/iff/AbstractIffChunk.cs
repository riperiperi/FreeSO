using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace TSO.Files.formats.iff
{
    /// <summary>
    /// An IFF is made up of chunks.
    /// </summary>
    public abstract class IffChunk 
    {
        public ushort ChunkID;
        public ushort ChunkFlags;
        public string ChunkLabel;
        public bool ChunkProcessed;
        public byte[] ChunkData;
        public Iff ChunkParent;

        /// <summary>
        /// Reads this chunk from an IFF.
        /// </summary>
        /// <param name="iff">The IFF to read from.</param>
        /// <param name="stream">The stream to read from.</param>
        public abstract void Read(Iff iff, Stream stream);

        /// <summary>
        /// The name of this chunk.
        /// </summary>
        /// <returns>The name of this chunk as a string.</returns>
        public override string ToString()
        {
            return "#" + ChunkID.ToString() + " " + ChunkLabel;
        }
    }
}
