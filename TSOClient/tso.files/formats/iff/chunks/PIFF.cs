using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace FSO.Files.Formats.IFF.Chunks
{
    public class PIFF : IffChunk
    {
        public override void Read(IffFile iff, Stream stream)
        {
            throw new NotImplementedException();
        }
    }
    
    public class PIFFEntry
    {
        string Type;
        ushort ChunkID;
        bool Delete;

        string ChunkLabel;
        ushort ChunkFlags;
        uint NewDataSize;

        uint PatchCount;
    }

    public class PIFFPatch
    {
        uint Offset;
        uint Size;
        PIFFPatchMode Mode; 
    }

    public enum PIFFPatchMode
    {
        Remove = 0,
        Add = 1
    }
}
