using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Drawing;

namespace Iffinator.Flash
{
    class PaletteMap
    {
        private const int INDEX_PALTID = 1;
		private const int INDEX_PALTPX = 84;

        private uint m_ID;
        //private int[] m_Colors;
        private Color[] m_Colors;

        public uint ID
        {
            get { return m_ID; }
        }

        public PaletteMap(byte[] ChunkData)
        {
            MemoryStream MemStream = new MemoryStream(ChunkData);
            BinaryReader Reader = new BinaryReader(MemStream);

            Reader.BaseStream.Position = INDEX_PALTID;

            m_ID = Reader.ReadUInt32();
            //m_Colors = new int[256];
            m_Colors = new Color[256];

            for (int i = 0; i < 256; i++)
            {
                Reader.BaseStream.Position = INDEX_PALTPX + i * 3;

                if ((Reader.BaseStream.Length - Reader.BaseStream.Position) >= 3)
                    //m_Colors[i] = (Reader.ReadByte() << 16) | (Reader.ReadByte() << 8) | Reader.ReadByte();
                    m_Colors[i] = Color.FromArgb(Reader.ReadByte(), Reader.ReadByte(), Reader.ReadByte());
                /*else
                    m_Colors[i] = 0x808080;*/
            }

            Reader.Close();
        }

        /*public int GetColorAtIndex(int Index)
        {
            return m_Colors[Index];
        }*/

        public Color GetColorAtIndex(int Index)
        {
            return m_Colors[Index];
        }
    }
}
