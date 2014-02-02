using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Drawing;

namespace Iffinator.Flash
{
    class EncryptedRowHeader
    {
        public int Code;
        public int Count;
    }

    class SpriteParser
    {
        private const int INDEX_SPRID = 1;
		private const int INDEX_SPRVERSION = 68;
		private const int INDEX_SPROFFSETTABLE = 80;
		private const int INDEX_ROWPX = 104;

        //This is not part of the format, but is assigned when loading
        //a chunk in order to keep different chunks (and sprites) apart.
        private int m_ChunkID;
        private uint m_ID;
        private uint m_Version;
        private uint m_FrameCount;
        private uint m_PaletteID;
        private PaletteMap m_PMap;
        private List<SpriteFrame> m_Frames = new List<SpriteFrame>();

        public int ChunkID
        {
            get { return m_ChunkID; }
        }

        public uint ID
        {
            get { return m_ID; }
        }

        public uint FrameCount
        {
            get { return m_FrameCount; }
        }

        public SpriteFrame GetFrame(int Index)
        {
            return m_Frames[Index];
        }

        public SpriteParser(byte[] ChunkData, List<PaletteMap> PMaps, int ChunkID)
        {
            m_ChunkID = ChunkID;

            MemoryStream MemStream = new MemoryStream(ChunkData);
            BinaryReader Reader = new BinaryReader(MemStream);

            Reader.BaseStream.Position = INDEX_SPRID;
            m_ID = Reader.ReadByte();

            Reader.BaseStream.Position = INDEX_SPRVERSION;
            m_Version = Reader.ReadUInt32();

            if (m_Version != 1000 && m_Version != 1001)
                throw new Exception("Version was: " + m_Version);

            m_FrameCount = Reader.ReadUInt32();
            
            if (m_Version == 1000)
                m_PaletteID = Reader.ReadUInt32();

            bool PaletteFound = false;

            foreach (PaletteMap Map in PMaps)
            {
                if (Map.ID == m_PaletteID)
                {
                    m_PMap = Map;
                    PaletteFound = true;
                }
            }

            //This is guesswork, but when the PaletteID is 1, it seems to typically indicate
            //that there's only one palette to choose from...
            if (m_PaletteID == 1)
            {
                m_PMap = PMaps[0];
                PaletteFound = true;
            }

            if (!PaletteFound)
                throw new Exception("No palette found!");

            for (int i = 0; i < m_FrameCount; i++)
            {
                SpriteFrame Frame = new SpriteFrame();

                Reader.BaseStream.Position = INDEX_SPROFFSETTABLE + 4 * i;
                Reader.BaseStream.Position = Reader.ReadUInt32() + INDEX_SPRVERSION;

                Frame.Width = Reader.ReadUInt16();
                Frame.Height = Reader.ReadUInt16();
                Frame.Flag = Reader.ReadUInt16();

                //Zero value skipped.
                Reader.BaseStream.Position += 2;

                Frame.PaletteID = Reader.ReadUInt16();
                Frame.PalMap = PMaps[0];

                foreach (PaletteMap PMap in PMaps)
                {
                    if (PMap.ID == m_PaletteID)
                        Frame.PalMap = PMap;
                }

                Frame.TransparentPixel = m_PMap.GetColorAtIndex(Reader.ReadUInt16());
                Frame.XLocation = Reader.ReadUInt16();
                Frame.YLocation = Reader.ReadUInt16();
                Frame.Init();

                EncryptedRowHeader RowHeader = new EncryptedRowHeader();
                int RowID = 0;

                for (int j = 0; j < Frame.Height; j++)
                {
                    long InitPos = Reader.BaseStream.Position;

                    RowHeader = GetDecryptedValues(Reader.ReadUInt16());

                    if (RowHeader.Code == 0)
                    {
                        //byte[] RowSegmentData = Reader.ReadBytes(RowHeader.Count - 2);
                        ReadRowSegment(Reader, RowID, ref Frame);
                        Reader.BaseStream.Position = InitPos + RowHeader.Count;
                        RowID++;
                    }
                    else if (RowHeader.Code == 4)
                    {
                        RowID += RowHeader.Count;
                    }
                    else if (RowHeader.Code == 5)
                    {
                        break;
                    }
                }

                m_Frames.Add(Frame);
            }

            Reader.Close();
        }

        private void ReadRowSegment(BinaryReader SpriteFrameReader, int RowID, ref SpriteFrame Frame)
        {
            /*MemoryStream MemStream = new MemoryStream(RowSegmentData);
            BinaryReader SpriteFrameReader = new BinaryReader(MemStream);*/

            byte Count = SpriteFrameReader.ReadByte();
            byte FormatCode = (byte)(SpriteFrameReader.ReadByte() / 32);
            int XPos = 0, LastXPos = 0;
            int Z = 0;

            if (FormatCode == 1)
            {
                for (int i = 0; i < Count; i++)
                {
                    Z = SpriteFrameReader.ReadByte();
                    //TODO: Z-level...
                    //int Clr = Frame.PalMap.GetColorAtIndex(SpriteFrameReader.ReadByte());
                    Color Clr = Frame.PalMap.GetColorAtIndex(SpriteFrameReader.ReadByte());
                    int Alpha = SpriteFrameReader.ReadByte() /** 8*/;

                    //No idea if this is going to work...
                    /*Frame.BitmapData.SetPixel(XPos + i, RowID, Color.FromArgb((0xFF << 16 | 0xFF << 8 | 0xFF),
                        Color.FromArgb((0xFF << 24 | Frame.PalMap.GetColorAtIndex(SpriteFrameReader.ReadByte())))));*/
                    Frame.BitmapData.SetPixel(XPos + i, RowID, Color.FromArgb(Alpha,
                        Frame.PalMap.GetColorAtIndex(SpriteFrameReader.ReadByte()).R, 
                        Frame.PalMap.GetColorAtIndex(SpriteFrameReader.ReadByte()).G,
                        Frame.PalMap.GetColorAtIndex(SpriteFrameReader.ReadByte()).B));
                }

                LastXPos = XPos + Count;
            }
            else if (FormatCode == 2)
            {
                for (int i = 0; i < Count; i++)
                {
                    Z = SpriteFrameReader.ReadByte();
                    //TODO: Z-level...
                    //int Clr = Frame.PalMap.GetColorAtIndex(SpriteFrameReader.ReadByte());
                    Color Clr = Frame.PalMap.GetColorAtIndex(SpriteFrameReader.ReadByte());
                    int Alpha = SpriteFrameReader.ReadByte() /** 8*/;

                    //No idea if this is going to work...
                    /*Frame.BitmapData.SetPixel(XPos + i, RowID, Color.FromArgb((Alpha << 16 | Alpha << 8 | Alpha),
                        Color.FromArgb((Alpha << 24 | Clr))));*/
                    Frame.BitmapData.SetPixel(XPos + i, RowID, Color.FromArgb(Alpha,
                        Clr.R, Clr.G, Clr.B));
                }

                LastXPos = XPos + Count;
                SpriteFrameReader.BaseStream.Position = (Count % 2 == 1) ? 1 : 0;
            }
            else if (FormatCode == 3)
            {
                LastXPos = XPos + Count;
            }
            else if (FormatCode == 5)
            {
                SpriteFrameReader.Close();
                return;
            }
            else if (FormatCode == 6)
            {
                for (int i = 0; i < Count; i++)
                {
                    /*Frame.BitmapData.SetPixel(XPos + i, RowID,
                        Color.FromArgb((0xFF << 24 | Frame.PalMap.GetColorAtIndex(SpriteFrameReader.ReadByte()))));*/
                    Frame.BitmapData.SetPixel(XPos + i, RowID,
                        Frame.PalMap.GetColorAtIndex(SpriteFrameReader.ReadByte()));
                }

                SpriteFrameReader.BaseStream.Position = SpriteFrameReader.BaseStream.Position = (Count % 2 == 1) ? 1 : 0;
            }

            if ((SpriteFrameReader.BaseStream.Length - SpriteFrameReader.BaseStream.Position) > 0)
            {
                /*byte[] RestBytes = SpriteFrameReader.ReadBytes((int)(SpriteFrameReader.BaseStream.Length - 
                    SpriteFrameReader.BaseStream.Position));
                ReadRowSegment(RestBytes, RowID, ref Frame, LastXPos);*/
                ReadRowSegment(SpriteFrameReader, RowID, ref Frame, LastXPos);
            }

            //SpriteFrameReader.Close();
        }

        private void ReadRowSegment(BinaryReader SpriteFrameReader, int RowID, ref SpriteFrame Frame, int XPos)
        {
            /*MemoryStream MemStream = new MemoryStream(RowSegmentData);
            BinaryReader SpriteFrameReader = new BinaryReader(MemStream);*/

            byte Count = SpriteFrameReader.ReadByte();
            byte FormatCode = (byte)(SpriteFrameReader.ReadByte() / 32);
            int LastXPos = 0;
            int Z = 0;

            if (FormatCode == 1)
            {
                for (int i = 0; i < Count; i++)
                {
                    Z = SpriteFrameReader.ReadByte();
                    //TODO: Z-level...
                    //int Clr = Frame.PalMap.GetColorAtIndex(SpriteFrameReader.ReadByte());
                    Color Clr = Frame.PalMap.GetColorAtIndex(SpriteFrameReader.ReadByte());
                    int Alpha = SpriteFrameReader.ReadByte() /** 8*/;

                    //No idea if this is going to work...
                    /*Frame.BitmapData.SetPixel(XPos + i, RowID, Color.FromArgb((0xFF << 16 | 0xFF << 8 | 0xFF),
                        Color.FromArgb((0xFF << 24 | Frame.PalMap.GetColorAtIndex(SpriteFrameReader.ReadByte())))));*/
                    if (XPos + i < Frame.Width)
                    {
                        Frame.BitmapData.SetPixel(XPos + i, RowID, Color.FromArgb(Alpha,
                            Frame.PalMap.GetColorAtIndex(SpriteFrameReader.ReadByte())));
                    }
                }

                LastXPos = XPos + Count;
            }
            else if (FormatCode == 2)
            {
                for (int i = 0; i < Count; i++)
                {
                    Z = SpriteFrameReader.ReadByte();
                    //TODO: Z-level...
                    //int Clr = Frame.PalMap.GetColorAtIndex(SpriteFrameReader.ReadByte());
                    Color Clr = Frame.PalMap.GetColorAtIndex(SpriteFrameReader.ReadByte());
                    int Alpha = SpriteFrameReader.ReadByte() /** 8*/;

                    if (i == Frame.Width)
                        break;

                    //No idea if this is going to work...
                    /*Frame.BitmapData.SetPixel(XPos + i, RowID, Color.FromArgb((Alpha << 16 | Alpha << 8 | Alpha),
                        Color.FromArgb((Alpha << 24 | Clr))));*/
                    if(XPos + i < Frame.Width)
                        Frame.BitmapData.SetPixel(XPos + i, RowID, Color.FromArgb(Alpha, Clr));
                }

                LastXPos = XPos + Count;
                SpriteFrameReader.BaseStream.Position = (Count % 2 == 1) ? 1 : 0;
            }
            else if (FormatCode == 3)
            {
                LastXPos = XPos + Count;
            }
            else if (FormatCode == 5)
            {
                return;
            }
            else if (FormatCode == 6)
            {
                for (int i = 0; i < Count; i++)
                {
                    /*Frame.BitmapData.SetPixel(XPos + i, RowID,
                        Color.FromArgb((0xFF << 24 | Frame.PalMap.GetColorAtIndex(SpriteFrameReader.ReadByte()))));*/
                    if (XPos + i < Frame.Width)
                    {
                        Frame.BitmapData.SetPixel(XPos + i, RowID,
                            Frame.PalMap.GetColorAtIndex(SpriteFrameReader.ReadByte()));
                    }
                }

                SpriteFrameReader.BaseStream.Position = SpriteFrameReader.BaseStream.Position = (Count % 2 == 1) ? 1 : 0;
            }

            //if ((SpriteFrameReader.BaseStream.Length - SpriteFrameReader.BaseStream.Position) > 0)
            //{
                /*byte[] RestBytes = SpriteFrameReader.ReadBytes((int)(SpriteFrameReader.BaseStream.Length -
                    SpriteFrameReader.BaseStream.Position));
                ReadRowSegment(RestBytes, RowID, ref Frame);*/
                //ReadRowSegment(SpriteFrameReader, RowID, ref Frame, LastXPos);
            //}

            //SpriteFrameReader.Close();
        }

        /// <summary>
        /// Decrypts a spriteframe's rowheader.
        /// </summary>
        /// <param name="P">The rowheader that was read from the spriteframe's data.</param>
        /// <returns></returns>
        private EncryptedRowHeader GetDecryptedValues(ushort P)
        {
            EncryptedRowHeader RowHeader = new EncryptedRowHeader();

            // 0xe000 = 1110 0000 0000 0000 : high order 3 bits
            // 0x1fff = 0001 1111 1111 1111 : low order 13 bits
            // 0xd = shift 13 bits to the right
            // as documented, 0xa000 is the stop value, with value 5 in the high order 3 bits ->
            //	0xa000 = 1010 0000 0000 0000 little endian
            //	high order 3 bits are 101, or value 5
            RowHeader.Code = ((P & 0xe000) << 0xd);
            RowHeader.Count = (P & 0x1fff);

            return RowHeader;
        }
    }
}
