using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Drawing;
using System.Windows.Forms;
using LogThis;

namespace Iffinator.Flash
{
    public class SPRParser : IffChunk
    {
        private uint m_Version;
        private uint m_FrameCount;
        private List<SpriteFrame> m_Frames = new List<SpriteFrame>();
        private uint m_PaletteID;
        private PaletteMap m_PMap;
        private List<uint> m_OffsetTable = new List<uint>();

        private bool m_IsBigEndian = false;

        private byte[] m_ChunkData;

        /// <summary>
        /// How many frames there are in this SPR# chunk.
        /// </summary>
        public uint FrameCount
        {
            get { return m_FrameCount; }
        }

        public SpriteFrame GetFrame(int Index)
        {
            if (m_Version != 1001)
            {
                foreach (SpriteFrame Frame in m_Frames)
                {
                    if (Frame.FrameIndex == Index)
                        return Frame;
                }

                return ReadFrame(Index);
            }
            else
                return m_Frames[Index];
        }

        public SPRParser(IffChunk Chunk, List<PaletteMap> PaletteMaps) : base(Chunk)
        {
            MemoryStream MemStream = new MemoryStream(Chunk.Data);
            BinaryReader Reader = new BinaryReader(MemStream);

            //The two first bytes aren't used by the version...
            ushort FirstBytes = Reader.ReadUInt16();

            if (FirstBytes == 0)
            {
                m_IsBigEndian = true;
                m_Version = (uint)Endian.SwapUInt16(Reader.ReadUInt16());
            }
            else
            {
                m_Version = (uint)FirstBytes;
                Reader.ReadUInt16();
            }

            //In version 1001, all frames are decompressed from the beginning, and there's no point in storing
            //the compressed data AS WELL as the decompressed frames...!
            if (m_Version != 1001)
                m_ChunkData = Chunk.Data;

            if (m_IsBigEndian)
            {
                if (m_Version != 1001)
                {
                    m_FrameCount = Endian.SwapUInt32(Reader.ReadUInt32());
                    m_PaletteID = Endian.SwapUInt32(Reader.ReadUInt32());

                    for (uint i = 0; i < m_FrameCount; i++)
                        m_OffsetTable.Add(Endian.SwapUInt32(Reader.ReadUInt32()));

                    //Find and set the correct palettemap...
                    if (PaletteMaps.Count == 1 && m_PaletteID == 1) { m_PMap = PaletteMaps[0]; }
                    else
                        m_PMap = PaletteMaps.Find(delegate(PaletteMap PMap) { if (PMap.ID == m_PaletteID) { return true; } return false; });
                }
                else
                {
                    m_FrameCount = Endian.SwapUInt32(Reader.ReadUInt32());
                    m_PaletteID = Endian.SwapUInt32(Reader.ReadUInt32());

                    //Find and set the correct palettemap...
                    if (PaletteMaps.Count == 1 && m_PaletteID == 1) { m_PMap = PaletteMaps[0]; }
                    else
                        m_PMap = PaletteMaps.Find(delegate(PaletteMap PMap) { if (PMap.ID == m_PaletteID) { return true; } return false; });
                }
            }
            else
            {
                if (m_Version != 1001)
                {
                    m_FrameCount = Reader.ReadUInt32();
                    m_PaletteID = Reader.ReadUInt32();

                    for (uint i = 0; i < m_FrameCount; i++)
                        m_OffsetTable.Add(Reader.ReadUInt32());

                    //Find and set the correct palettemap...
                    if (PaletteMaps.Count == 1 && m_PaletteID == 1) { m_PMap = PaletteMaps[0]; }
                    else
                        m_PMap = PaletteMaps.Find(delegate(PaletteMap PMap) { if (PMap.ID == m_PaletteID) { return true; } return false; });
                }
                else
                {
                    m_FrameCount = Reader.ReadUInt32();
                    m_PaletteID = Reader.ReadUInt32();

                    //Find and set the correct palettemap...
                    if (PaletteMaps.Count == 1 && m_PaletteID == 1) { m_PMap = PaletteMaps[0]; }
                    else
                        m_PMap = PaletteMaps.Find(delegate(PaletteMap PMap) { if (PMap.ID == m_PaletteID) { return true; } return false; });
                }
            }

            if (m_Version == 1001)
            {
                //Framecount may be set to -1 and should be ignored...
                while(true)
                {
                    SpriteFrame Frame = new SpriteFrame();

                    Frame.Version = Reader.ReadUInt32();
                    Frame.Size = Reader.ReadUInt32();

                    Reader.ReadBytes(4); //Reserved.

                    Frame.Height = Reader.ReadUInt16();
                    Frame.Width = Reader.ReadUInt16();
                    Frame.Init(true); //SPR#s don't have alpha channels, but alpha is used to plot transparent pixels.

                    DecompressFrame2(ref Frame, ref Reader);
                    Frame.BitmapData.Unlock(true); //The bitmapdata is locked when the frame is created.

                    m_Frames.Add(Frame);

                    if ((Reader.BaseStream.Position == Reader.BaseStream.Length) || 
                        (Reader.BaseStream.Position - Reader.BaseStream.Length < 14))
                        break;
                }
            }

            Reader.Close();
        }

        private SpriteFrame ReadFrame(int Index)
        {
            MemoryStream MemStream = new MemoryStream(m_ChunkData);
            BinaryReader Reader = new BinaryReader(MemStream);

            Reader.BaseStream.Position = m_OffsetTable[Index];

            SpriteFrame Frame = new SpriteFrame();

            if (!m_IsBigEndian)
            {
                Reader.ReadBytes(4); //Reserved.

                Frame.Height = Reader.ReadUInt16();
                Frame.Width = Reader.ReadUInt16();
                Frame.PaletteID = (ushort)m_PaletteID;
            }
            else
            {
                Reader.ReadBytes(4); //Reserved.

                Frame.Height = Endian.SwapUInt16(Reader.ReadUInt16());
                Frame.Width = Endian.SwapUInt16(Reader.ReadUInt16());
                Frame.PaletteID = (ushort)m_PaletteID;
            }
            
            Frame.Init(true); //SPR#s don't have alpha channels, but alpha is used to plot transparent pixels.

            DecompressFrame2(ref Frame, ref Reader);
            Frame.BitmapData.Unlock(true); //The bitmapdata is locked when the frame is created.

            //Store the frame to avoid having to decompress in the future.
            m_Frames.Add(Frame);

            return Frame;
        }

        private void DecompressFrame(ref SpriteFrame Frame, ref BinaryReader Reader)
        {
            int row = 0;
            int column = 0;
            bool quit = false;
            int lastType = 0;
            int numCodesTillNewline = 0;

            while (quit == false)
            {
                ushort rowHeader = Reader.ReadUInt16();

                if (m_IsBigEndian)
                    rowHeader = Endian.SwapUInt16(rowHeader);

                ushort RowControlCode = (ushort)(rowHeader >> 13);
                ushort BytesInThisRow = (ushort)(rowHeader & 0x1FFF);

                switch (RowControlCode)
                {
                    case 0:     //Start marker; the count byte is ignored
                        break;
                    case 4:
                        column = 0;
                        numCodesTillNewline = BytesInThisRow;
                        for (int bytesRead = 0; bytesRead < numCodesTillNewline - 2; bytesRead += 2)
                        {
                            ushort rowHeader2 = Reader.ReadUInt16();

                            if (m_IsBigEndian)
                                rowHeader2 = Endian.SwapUInt16(rowHeader2);

                            ushort ColumnControlCode = (ushort)(rowHeader2 >> 13);
                            ushort BytesInThisColumn = (ushort)(rowHeader2 & 0x1FFF);

                            try
                            {
                                switch (ColumnControlCode)
                                {
                                    case 1: //Fill pixels with background.
                                        //column += BytesInThisColumn;

                                        for (int i = 0; i < BytesInThisColumn; i++)
                                        {
                                            int Z = Reader.ReadByte();
                                            byte b = Reader.ReadByte();
                                            Frame.BitmapData.SetPixel(new Point(column++, row), m_PMap.GetColorAtIndex(b));
                                            Color c = m_PMap.GetColorAtIndex(b);
                                            bytesRead += 2;
                                        }
                                        break;
                                    case 2: //TODO: Run-length encoding.
                                        for (int i = 0; i < BytesInThisColumn; i++)
                                        {
                                            byte b1 = Reader.ReadByte();
                                            byte b2 = Reader.ReadByte();
                                            Frame.BitmapData.SetPixel(new Point(column++, row), m_PMap.GetColorAtIndex(b1));
                                            Frame.BitmapData.SetPixel(new Point(column++, row), m_PMap.GetColorAtIndex(b2));
                                            bytesRead += 2;
                                        }
                                        break;
                                    case 3: //Copy image pixels.
                                        for (int i = 0; i < BytesInThisColumn; i++)
                                        {
                                            byte b = Reader.ReadByte();
                                            Frame.BitmapData.SetPixel(new Point(column++, row), m_PMap.GetColorAtIndex(b));
                                            bytesRead++;
                                        }
                                        if (Reader.BaseStream.Position % 2 == 1) { Reader.ReadByte(); bytesRead++; }
                                        break;
                                    default:
                                        break;
                                }
                            }
                            catch (Exception e)
                            {
                                Log.LogThis(String.Format("Error reading code {0} ({1}). Last code read was {2}.",
                                    ColumnControlCode, e.Message, lastType), eloglevel.error);
                            }
                            lastType = ColumnControlCode;
                        }
                        row++;
                        break;
                    case 5:
                        quit = true;
                        break;
                    case 9: //Fill lines with background.
                        for (int i = 0; i < BytesInThisRow; i++)
                        {
                            row++;
                            column = 0;
                        }
                        break;
                    case 10: //Start marker; the count byte is ignored
                        break;
                    default:
                        //MessageBox.Show("Error reading code " + lastType + '!');
                        Log.LogThis(String.Format("Error reading code: " + lastType + "!"), eloglevel.error);
                        break;
                }
                if (Reader.BaseStream.Position == Reader.BaseStream.Length)
                    break;
                lastType = RowControlCode;
            }
        }

        private void DecompressFrame2(ref SpriteFrame Frame, ref BinaryReader Reader)
        {
            bool quit = false;
            byte Clr = 0;
            Color Transparent;
            int CurrentRow = 0, CurrentColumn = 0;

            byte PixCommand, PixCount = 0;

            while (quit == false)
            {
                byte RowCommand = Reader.ReadByte();
                byte RowCount = Reader.ReadByte();

                switch (RowCommand)
                {
                    case 0x00: //Start marker; the count byte is ignored.
                        break;
                    //Fill this row with pixel data that directly follows; the count byte of the row command denotes the 
                    //size in bytes of the row and pixel data.
                    case 0x04:
                        RowCount -= 2;

                        while (RowCount > 0)
                        {
                            PixCommand = Reader.ReadByte();
                            PixCount = Reader.ReadByte();
                            RowCount -= 2;

                            switch (PixCommand)
                            {
                                case 0x01: //Leave the next pixel count pixels as transparent.
                                    Transparent = Color.FromArgb(0, 0, 0, 0);
                                    for (int j = CurrentColumn; j < (CurrentColumn + PixCount); j++)
                                        Frame.BitmapData.SetPixel(new Point(j, CurrentRow), Transparent);

                                    CurrentColumn += PixCount;

                                    break;
                                case 0x02: //Fill the next pixel count pixels with a palette color.
                                    //The pixel data is two bytes: the first byte denotes the palette color index, and the 
                                    //second byte is padding (which is always equal to the first byte but is ignored).
                                    Clr = Reader.ReadByte();
                                    Reader.ReadByte(); //Padding
                                    RowCount -= 2;

                                    for (int j = CurrentColumn; j < (CurrentColumn + PixCount); j++)
                                        Frame.BitmapData.SetPixel(new Point(j, CurrentRow), m_PMap.GetColorAtIndex(Clr));

                                    CurrentColumn += PixCount;

                                    break;
                                case 0x03: //Set the next pixel count pixels to the palette color indices defined by the 
                                    //pixel data provided directly after this command.
                                    for (int j = CurrentColumn; j < (CurrentColumn + PixCount); j++)
                                    {
                                        Clr = Reader.ReadByte();
                                        Frame.BitmapData.SetPixel(new Point(j, CurrentRow), m_PMap.GetColorAtIndex(Clr));
                                    }

                                    CurrentColumn += PixCount;
                                    byte Padding = (byte)(PixCount % 2);

                                    if (Padding != 0)
                                    {
                                        //Reader.ReadByte();
                                        RowCount -= (byte)(PixCount + Padding);
                                    }
                                    else
                                        RowCount -= PixCount;

                                    break;
                            }
                        }

                        break;
                    case 0x05: //End marker. The count byte is always 0, but may be ignored.
                        PixCommand = Reader.ReadByte();
                        PixCount = Reader.ReadByte();

                        quit = true;
                        break;
                    case 0x09: //Leave the next count rows as transparent.
                        PixCommand = Reader.ReadByte();
                        PixCount = Reader.ReadByte();

                        Transparent = Color.FromArgb(0, 0, 0, 0);

                        for (int i = 0; i < PixCount; i++)
                        {
                            for (int j = CurrentColumn; j < Frame.Width; j++)
                                Frame.BitmapData.SetPixel(new Point(j, CurrentRow), Transparent);

                            CurrentRow++;
                        }

                        break;
                    case 0x16: //Start marker, equivalent to 0x00; the count byte is ignored.
                        break;
                }

                CurrentRow++;
                CurrentColumn = 0;

                if (Reader.BaseStream.Position == Reader.BaseStream.Length)
                    break;
            }
        }
    }
}
