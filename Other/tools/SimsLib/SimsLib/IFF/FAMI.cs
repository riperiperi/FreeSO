using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace SimsLib.IFF
{
    public class FAMI: IffChunk
    {

        private int m_Version;
        public int offset, LotNumber, Data1, Data2, Data3, Data4, Data5, Data6, Data7, Data10, Data11, Data12, Money;
        public uint HouseValue, Data8, Data9;
        public string String;

        public int Version
        {
            get { return m_Version; }
        }

        public FAMI(IffChunk Chunk) : base(Chunk)
        {
            MemoryStream MemStream = new MemoryStream(Chunk.Data);
            BinaryReader Reader = new BinaryReader(MemStream);
            Reader.ReadInt32();
            m_Version = Reader.ReadInt32();
            
            
            String = Encoding.ASCII.GetString(Reader.ReadBytes(4));
            LotNumber = Reader.ReadInt16();
            Data1 = Reader.ReadInt16();
            Data2 = Reader.ReadInt16();
            Data3 = Reader.ReadInt16();
            Money = Reader.ReadInt16();
            Data4 = Reader.ReadInt16();
            HouseValue = Reader.ReadUInt16();                       
            Data5 = Reader.ReadInt16();
            Data6 = Reader.ReadInt16();
            Data7 = Reader.ReadInt16();
            Data8 = Reader.ReadUInt16();
            Data9 = Reader.ReadUInt16();
            Data10 = Reader.ReadInt16();
            Data11 = Reader.ReadInt16();
            

        }
    }
}
