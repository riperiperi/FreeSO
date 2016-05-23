using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace SimsLib.IFF
{
    public class NBRS: IffChunk
    {

        private int m_Version;
        public int NeighborsNumber, SimHeader, SimVersion;
        public string String;
        public int[] NeighborHeader, NeighborId, RelId;

        public int Version
        {
            get { return m_Version; }
        }

        public NBRS(IffChunk Chunk)
            : base(Chunk)
        {
            MemoryStream MemStream = new MemoryStream(Chunk.Data);
            BinaryReader Reader = new BinaryReader(MemStream);
            Reader.ReadInt32();
            m_Version = Reader.ReadInt32();
            
            
            String = Encoding.ASCII.GetString(Reader.ReadBytes(4));
            NeighborsNumber = Reader.ReadInt32();

            NeighborHeader = new int[NeighborsNumber];
            NeighborId = new int[NeighborsNumber];
            RelId = new int[NeighborsNumber];

            for (int i = 0; i <= NeighborsNumber - 1; i++)
                {

                SimHeader = Reader.ReadInt32();
                Reader.ReadInt32();
                Reader.ReadInt32();
                NeighborHeader[i] = SimHeader;
                NeighborId[i] = Reader.ReadInt32();
                
                Reader.ReadInt32();
                SimVersion = Reader.ReadInt32();

                if (SimVersion != 0)
                    Reader.ReadBytes(4);
                    RelId[i] = Reader.ReadInt16();

                }

        }
    }
}
