using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace SimsLib.IFF
{
    public class HOUS: IffChunk
    {

        private int m_Version;
        public int Data1, Data2, Data5, Data6, Data7, Data10, Data11, Data12, Money;
        public uint Data8, Data9;
        public ushort Data3, Data4;
        public string String, String2;

        public int Version
        {
            get { return m_Version; }
        }

        public HOUS(IffChunk Chunk)
            : base(Chunk)
        {
            MemoryStream MemStream = new MemoryStream(Chunk.Data);
            BinaryReader Reader = new BinaryReader(MemStream);

            if (ID == 0)

                {
            Reader.ReadInt32();
            m_Version = Reader.ReadInt32();
            
            
            String = Encoding.ASCII.GetString(Reader.ReadBytes(4));
            Data1 = Reader.ReadInt16();
            Reader.ReadInt32();
            Reader.ReadInt32();
            
            
            Data2 = Reader.ReadInt16();
            Data3 = Reader.ReadUInt16();
            Data4 = Reader.ReadUInt16();
            Data5 = Reader.ReadUInt16();
            Data6 = Reader.ReadUInt16();
                }
            else if (ID == 1)
                {

                    Reader.ReadInt32();
                    m_Version = Reader.ReadInt32();
                    String = Encoding.ASCII.GetString(Reader.ReadBytes(8));
                    Data1 = Reader.ReadInt16();
                    
                    String2 = Encoding.ASCII.GetString(Reader.ReadBytes(5));
                    Data3 = Reader.ReadUInt16();
                    Data4 = Reader.ReadUInt16();
                    Data5 = Reader.ReadUInt16();
                    Data6 = Reader.ReadUInt16();
                }
        }
    }
}
