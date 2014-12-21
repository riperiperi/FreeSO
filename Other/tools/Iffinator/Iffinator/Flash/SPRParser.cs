/*This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
If a copy of the MPL was not distributed with this file, You can obtain one at
http://mozilla.org/MPL/2.0/.

The Original Code is the Iffinator.

The Initial Developer of the Original Code is
Mats 'Afr0' Vederhus. All Rights Reserved.

Contributor(s):
*/

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
                    Frame.Init(true, false); //SPR#s don't have alpha channels, but alpha is used to plot transparent pixels.

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
            
            Frame.Init(true, false); //SPR#s don't have alpha channels, but alpha is used to plot transparent pixels.

            DecompressFrame2(ref Frame, ref Reader);
            Frame.BitmapData.Unlock(true); //The bitmapdata is locked when the frame is created.

            //Store the frame to avoid having to decompress in the future.
            m_Frames.Add(Frame);

            return Frame;
        }

        private void DecompressFrame2(ref SpriteFrame Frame, ref BinaryReader Reader)
        {
            bool quit = false;
            byte Clr = 0;
            Color Transparent;
            int CurrentRow = 0, CurrentColumn = 0;

            byte PixCommand, PixCount = 0;

            if (m_PMap == null)
                m_PMap = new PaletteMap();

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
                        CurrentColumn = 0;

                        while (RowCount > 0)
                        {
                            PixCommand = Reader.ReadByte();
                            PixCount = Reader.ReadByte();
                            RowCount -= 2;

                            switch (PixCommand)
                            {
                                case 1: //Leave the next pixel count pixels as transparent.
                                    Transparent = Color.FromArgb(0, 0, 0, 0);
                                    for (int j = CurrentColumn; j < (CurrentColumn + PixCount); j++)
                                    {
                                        Frame.BitmapData.SetPixel(new Point(j, CurrentRow), Transparent);
                                    }

                                    CurrentColumn += PixCount;

                                    break;
                                case 2: //Fill the next pixel count pixels with a palette color.
                                    //The pixel data is two bytes: the first byte denotes the palette color index, and the 
                                    //second byte is padding (which is always equal to the first byte but is ignored).
                                    Clr = Reader.ReadByte();
                                    Reader.ReadByte(); //Padding
                                    RowCount -= 2;

                                    for (int j = CurrentColumn; j < (CurrentColumn + PixCount); j++)
                                    {
                                        Frame.BitmapData.SetPixel(new Point(j, CurrentRow),
                                            m_PMap.GetColorAtIndex(Clr));
                                    }

                                    CurrentColumn += PixCount;

                                    break;
                                case 3: //Set the next pixel count pixels to the palette color indices defined by the 
                                    //pixel data provided directly after this command.

                                    byte Padding = (byte)(PixCount % 2);

                                    if (Padding != 0)
                                        RowCount -= (byte)(PixCount + Padding);
                                    else
                                        RowCount -= PixCount;

                                    for (int j = CurrentColumn; j < (CurrentColumn + PixCount); j++)
                                    {
                                        Clr = Reader.ReadByte();
                                        Frame.BitmapData.SetPixel(new Point(j, CurrentRow),
                                            m_PMap.GetColorAtIndex(Clr));
                                    }

                                    CurrentColumn += PixCount;

                                    if (Padding != 0)
                                        Reader.ReadByte();

                                    break;
                            }
                        }

                        CurrentRow++;

                        break;
                    case 0x05: //End marker. The count byte is always 0, but may be ignored.

                        //Some sprites don't have these, so read them using ReadBytes(), which
                        //simply returns an empty array if the stream couldn't be read.
                        Reader.ReadBytes(2); //PixCommand and PixCount.

                        quit = true;
                        break;
                    case 0x09: //Leave the next count rows as transparent.
                        PixCommand = Reader.ReadByte();
                        PixCount = Reader.ReadByte();

                        Transparent = Color.FromArgb(0, 0, 0, 0);

                        for (int i = 0; i < RowCount; i++)
                        {
                            for (int j = CurrentColumn; j < Frame.Width; j++)
                                Frame.BitmapData.SetPixel(new Point(j, CurrentRow), Transparent);

                            CurrentRow++;
                        }

                        break;
                    case 0x10: //Start marker, equivalent to 0x00; the count byte is ignored.
                        break;
                }

                if (Reader.BaseStream.Position == Reader.BaseStream.Length)
                    break;
            }
        }
    }
}
