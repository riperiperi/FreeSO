using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace tso.files.formats.iff
{
    public abstract class AbstractIffChunk {
        public ushort ChunkID;
        public ushort ChunkFlags;
        public string ChunkLabel;
        public bool ChunkProcessed;
        public byte[] ChunkData;
        public Iff ChunkParent;

        public abstract void Read(Iff iff, Stream stream);

        public override string ToString()
        {
            return "#" + ChunkID + " " + ChunkLabel;
        }
    }
}
