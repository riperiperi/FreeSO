using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace SimsLib.IFF
{
    public class FAMh: IffChunk
    {

        private int m_Version;
        public int offset, LotNumber, HouseValue, Data5, Data6, Data7, Data10, Data11, Data12, Money;
        public uint Data8, Data9;
        public ushort Data1, Data2, Data3, Data4;
        public string String;

        public int Version
        {
            get { return m_Version; }
        }

        public FAMh(IffChunk Chunk) : base(Chunk)
        {
            MemoryStream MemStream = new MemoryStream(Chunk.Data);
            BinaryReader Reader = new BinaryReader(MemStream);
            Reader.ReadInt32();
            m_Version = Reader.ReadInt32();
            
            
            String = Encoding.ASCII.GetString(Reader.ReadBytes(4));
            Data1 = BitConverter.ToUInt16(Reader.ReadBytes(2), 0);
            
            Data2 = Reader.ReadUInt16();
            Data3 = Reader.ReadByte();
            Data4 = Reader.ReadByte();
            

        }
    }
}
