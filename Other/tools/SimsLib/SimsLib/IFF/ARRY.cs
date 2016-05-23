using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace SimsLib.IFF
{


   public class ARRY: IffChunk
    {
       private int m_Version, Count;
       public uint data1, data2, value1, value2;
       public ushort value3;

       public string Header;

       public int Version
       {
           get { return m_Version; }
       }

       public List<ArryEntry> Entries;


        public ARRY(IffChunk Chunk) : base(Chunk)
        {
            Count = 0;
            Entries = new List<ArryEntry>();
            MemoryStream MemStream = new MemoryStream(Chunk.Data);
            BinaryReader Reader = new BinaryReader(MemStream);

            Reader.ReadInt32();
            data1 = Reader.ReadUInt32();
            data2 = Reader.ReadUInt32();
            value1 = Reader.ReadUInt32();
            //Header = Encoding.ASCII.GetString();

            // Arry(3) - Objects
            if (ID == 3)
            while (Reader.BaseStream.Position != Reader.BaseStream.Length)
                {
                    ushort Siz, Pos, Lev;
                    
                    ushort Current = 0;
              
                    Siz = Reader.ReadByte();
                    ushort[] IDs = new ushort[Siz];

                    if (Reader.BaseStream.Position < (Reader.BaseStream.Length - Siz))
                    for (int i = 0; i < Siz - 1; i++)
                    {

                        IDs[i] = Reader.ReadUInt16();
                        
                    }

                
                    for (int i = 0; i <= Siz - 1; i++)
                    {
                        if ((i & 1) == 0)
                            Current = IDs[i];
                        else
                            IDs[i] = (IDs[i] < 8 ? Current : IDs[i]);

                        if ((Siz & 1) == 1)
                            IDs[i] = Current;
                    }
                   
                    

                    Pos = Reader.ReadUInt16();
                    Lev = Reader.ReadUInt16();  

                    Entries.Add(new ArryEntry
                    {
                        Size = Siz,
                        IDS = IDs,
                        Position = Pos,
                        Level = Lev
                    });

                    Count +=1;

                }

        }
    }


    public struct ArryEntry
    {

        public ushort Size, Position, Level, Pad;
        public ushort[] IDS;
    
    
    }


}
