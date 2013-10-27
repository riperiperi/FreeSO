/*The contents of this file are subject to the Mozilla Public License Version 1.1
(the "License"); you may not use this file except in compliance with the
License. You may obtain a copy of the License at http://www.mozilla.org/MPL/

Software distributed under the License is distributed on an "AS IS" basis,
WITHOUT WARRANTY OF ANY KIND, either express or implied. See the License for
the specific language governing rights and limitations under the License.

The Original Code is the SimsLib.

The Initial Developer of the Original Code is
Mats 'Afr0' Vederhus. All Rights Reserved.

Contributor(s):
*/

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Drawing;
using System.Windows.Forms;
using LogThis;

namespace SimsLib.IFF
{
    public enum SPR2Flags
    {
        HasColorChannel = 0x01,
        HasZBufferChannel = 0x03,
        HasAlphaChannel = 0x07
    }

    public class EncryptedRowHeader
    {
        public int Code;
        public int Count;
    }

    /// <summary>
    /// Represents a SPR2 sprite.
    /// A SPR2 holds a number of SpriteFrame
    /// objects.
    /// </summary>
    public class SPR2Parser : IffChunk
    {
        private const int INDEX_SPRID = 1;
        private const int INDEX_SPRVERSION = 68;
        private const int INDEX_SPROFFSETTABLE = 80;
        private const int INDEX_ROWPX = 104;

        private byte[] m_ChunkData;
        private List<int> m_FrameOffsets = new List<int>();
        private uint m_Version;
        private uint m_FrameCount;
        private uint m_PaletteID;
        private string m_Name;
        private PaletteMap m_PMap;
        private List<SpriteFrame> m_Frames = new List<SpriteFrame>();

        /// <summary>
        /// The name of this SPR2 chunk.
        /// </summary>
        public string Name
        {
            get { return m_Name; }
        }

        /// <summary>
        /// The number of frames in this SPR2.
        /// </summary>
        public uint FrameCount
        {
            get 
            {
                if (m_Version == 1000)
                    return (uint)m_FrameCount;
                else
                    return (uint)m_Frames.Count; 
            }
        }

        /// <summary>
        /// Gets a frame at a specified index from this SPR2.
        /// </summary>
        /// <param name="Index">The index at which to retrieve the frame.</param>
        /// <returns>A SpriteFrame instance.</returns>
        public SpriteFrame GetFrame(int Index)
        {
            if (m_Version == 1000)
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

        /// <summary>
        /// Creates a new SPR2 instance.
        /// </summary>
        /// <param name="Chunk">The data for the chunk.</param>
        /// <param name="PMaps">The palettemaps for this SPR2.</param>
        public SPR2Parser(IffChunk Chunk, List<PaletteMap> PMaps) : base(Chunk)
        {
            m_Name = Name;

            int[] offsets;
            MemoryStream MemStream = new MemoryStream(Chunk.Data);
            BinaryReader Reader = new BinaryReader(MemStream);

            m_Version = Reader.ReadUInt32();

            //In version 1001, all frames are decompressed from the beginning, and there's no point in storing
            //the compressed data AS WELL as the decompressed frames...!
            if (m_Version == 1000)
                m_ChunkData = Chunk.Data;

            if (m_Version == 1001)
            {
                m_PaletteID = Reader.ReadUInt32();
                m_FrameCount = Reader.ReadUInt32();
            }
            else
            {
                m_FrameCount = Reader.ReadUInt32();
                m_PaletteID = Reader.ReadUInt32();
            }

            if (PMaps.Count == 1 && m_PaletteID == 1) { m_PMap = PMaps[0]; }
            else
                m_PMap = PMaps.Find(delegate(PaletteMap PMap) { if (PMap.ID == m_PaletteID) { return true; } return false; });

            //Some SPR2s blatantly specify the wrong ID because there's only one palettemap...
            if (m_PMap == null)
            {
                m_PMap = PMaps[0];
            }

            offsets = new int[m_FrameCount];

            if (m_Version == 1000)
            {
                for (int i = 0; i < m_FrameCount; i++)
                {
                    offsets[i] = Reader.ReadInt32();
                    m_FrameOffsets.Add(offsets[i]);
                }
            }

            if (m_Version == 1001)
            {
                for (int l = 0; l < m_FrameCount; l++)
                {
                    SpriteFrame Frame = new SpriteFrame();

                    Frame.Version = Reader.ReadUInt32();
                    Frame.Size = Reader.ReadUInt32();
                    Frame.Width = Reader.ReadUInt16();
                    Frame.Height = Reader.ReadUInt16();
                    Frame.Flag = Reader.ReadUInt32();
                    Frame.PaletteID = Reader.ReadUInt16();
                    Frame.TransparentPixel = m_PMap.GetColorAtIndex(Reader.ReadUInt16());
                    Frame.YLocation = Reader.ReadUInt16();
                    Frame.XLocation = Reader.ReadUInt16();

                    if ((SPR2Flags)Frame.Flag == SPR2Flags.HasAlphaChannel)
                        Frame.Init(true, true);
                    else
                    {
                        if ((SPR2Flags)Frame.Flag == SPR2Flags.HasZBufferChannel)
                            Frame.Init(false, true);
                        else
                            Frame.Init(false, false);
                    }

                    DecompressFrame2(ref Frame, ref Reader);
                    Frame.BitmapData.Unlock(true); //The bitmapdata is locked when the frame is created.

                    if (Frame.HasZBuffer)
                        Frame.ZBuffer.Unlock(true); //The bitmapdata is locked when the frame is created.

                    m_Frames.Add(Frame);
                }
            }

            Reader.Close();
        }

        /// <summary>
        /// Reads a frame from this SPR2.
        /// </summary>
        /// <param name="Index">The index of the frame.</param>
        /// <returns>A SpriteFrame instance.</returns>
        public SpriteFrame ReadFrame(int Index)
        {
            BinaryReader Reader = new BinaryReader(new MemoryStream(m_ChunkData));
            Reader.BaseStream.Seek(m_FrameOffsets[Index], SeekOrigin.Begin);

            SpriteFrame Frame = new SpriteFrame();

            Frame.FrameIndex = (uint)Index;
            Frame.Width = Reader.ReadUInt16();
            Frame.Height = Reader.ReadUInt16();
            Frame.Flag = Reader.ReadUInt16();
            var unknown = Reader.ReadUInt16();

            Frame.PaletteID = Reader.ReadUInt16();
            Frame.TransparentPixel = m_PMap.GetColorAtIndex(Reader.ReadUInt16());
            Frame.YLocation = Reader.ReadUInt16();
            Frame.XLocation = Reader.ReadUInt16();

            if (Frame.Flag == 0x07)
                Frame.Init(true, true);
            else
            {
                if ((SPR2Flags)Frame.Flag == SPR2Flags.HasZBufferChannel)
                    Frame.Init(false, true);
                else
                    Frame.Init(false, false);
            }

            DecompressFrame2(ref Frame, ref Reader);
            Frame.BitmapData.Unlock(true); //The bitmapdata is locked when the frame is created.

            if (Frame.HasZBuffer)
                Frame.ZBuffer.Unlock(true); //The bitmapdata is locked when the frame is created.

            Reader.Close();

            //Store the frame to avoid having to decompress in the future.
            m_Frames.Add(Frame);

            return Frame;
        }

        /// <summary>
        /// Decompresses a frame stored in this SPR2.
        /// </summary>
        /// <param name="Frame">A SpriteFrame instance that will contain the decompressed frame.</param>
        /// <param name="Reader">The binary reader to read the frame with.</param>
        private void DecompressFrame2(ref SpriteFrame Frame, ref BinaryReader Reader)
        {
            bool Quit = false;
            int CurrentRow = 0, CurrentColumn = 0;
            int Padding = 0;
            Color Clr, ZClr; //The current color and the current color for the z-buffer.

            while (Quit == false)
            {
                int[] RowHeader = GetDecryptedValues(Reader.ReadUInt16());
                switch (RowHeader[0])
                {
                    case 0: //Fill this row with pixel data that directly follows; the count byte of the row 
                            //command denotes the size in bytes of the row's command/count bytes together 
                            //with the supplied pixel data.
                        int RowCount = RowHeader[1];
                        RowCount -= 2; //Row command + count bytes.

                        while (RowCount > 0)
                        {
                            int[] PixelHeader = GetDecryptedValues(Reader.ReadUInt16());
                            RowCount -= 2;

                            int PixelCount = PixelHeader[1];

                            switch (PixelHeader[0])
                            {
                                case 1: //Set the next pixel count pixels in the z-buffer and color sprites to the 
                                        //values defined by the pixel data provided directly after this command.
                                    RowCount -= PixelCount * 2;

                                    while (PixelCount > 0)
                                    {
                                        Frame.ZBuffer.SetPixel(new Point(CurrentColumn, CurrentRow),
                                            Color.FromArgb(Reader.ReadByte(), 0, 0, 0));

                                        Clr = m_PMap.GetColorAtIndex(Reader.ReadByte());
                                        if (Clr != Frame.TransparentPixel)
                                            Frame.BitmapData.SetPixel(new Point(CurrentColumn, CurrentRow), Clr);
                                        else
                                            Frame.BitmapData.SetPixel(new Point(CurrentColumn, CurrentRow), 
                                                Color.FromArgb(0, 0, 0, 0));

                                        PixelCount--;
                                        CurrentColumn++;
                                    }

                                    break;
                                case 2: //Set the next pixel count pixels in the z-buffer, color, and alpha 
                                        //sprites to the values defined by the pixel data provided directly after 
                                        //this command.
                                    Padding = PixelCount % 2;
                                    RowCount -= (PixelCount * 3) + Padding;

                                    while (PixelCount > 0)
                                    {
                                        ZClr = Color.FromArgb(Reader.ReadByte());
                                        Clr = m_PMap.GetColorAtIndex(Reader.ReadByte());

                                        //Read the alpha.
                                        Clr = Color.FromArgb(Reader.ReadByte(), Clr);
                                        ZClr = Color.FromArgb(Clr.A, ZClr);

                                        Frame.BitmapData.SetPixel(new Point(CurrentColumn, CurrentRow), Clr);
                                        Frame.ZBuffer.SetPixel(new Point(CurrentColumn, CurrentRow), ZClr);

                                        PixelCount--;
                                        CurrentColumn++;
                                    }

                                    if (Padding != 0)
                                        Reader.ReadByte();

                                    break;
                                case 3: //Leave the next pixel count pixels in the color sprite filled with the 
                                        //transparent color, in the z-buffer sprite filled with 255, and in the 
                                        //alpha sprite filled with 0. This pixel command has no pixel data.
                                    while (PixelCount > 0)
                                    {
                                        //This is completely transparent regardless of whether the frame
                                        //supports alpha.
                                        Frame.BitmapData.SetPixel(new Point(CurrentColumn, CurrentRow), 
                                            Color.FromArgb(0, 0, 0, 0));

                                        if(Frame.HasZBuffer)
                                            Frame.ZBuffer.SetPixel(new Point(CurrentColumn, CurrentRow),
                                                Color.FromArgb(255, 255, 255, 255));

                                        PixelCount--;
                                        CurrentColumn++;
                                    }

                                    break;
                                case 6: //Set the next pixel count pixels in the color sprite to the palette color 
                                        //indices defined by the pixel data provided directly after this command.
                                    Padding = PixelCount % 2;
                                    RowCount -= PixelCount + Padding;

                                    while (PixelCount > 0)
                                    {
                                        Clr = m_PMap.GetColorAtIndex(Reader.ReadByte());
                                        if (Clr != Frame.TransparentPixel)
                                            Frame.BitmapData.SetPixel(new Point(CurrentColumn, CurrentRow), Clr);
                                        else
                                            Frame.BitmapData.SetPixel(new Point(CurrentColumn, CurrentRow), 
                                                Color.FromArgb(0, 0, 0, 0));

                                        if (Frame.HasZBuffer)
                                        {
                                            if (Clr != Frame.TransparentPixel)
                                                Frame.ZBuffer.SetPixel(new Point(CurrentColumn, CurrentRow), 
                                                    Color.FromArgb(255, 1, 1, 1));
                                            else
                                                Frame.BitmapData.SetPixel(new Point(CurrentColumn, CurrentRow), 
                                                    Color.FromArgb(255, 255, 255, 255));
                                        }

                                        PixelCount--;
                                        CurrentColumn++;
                                    }

                                    if (Padding != 0)
                                        Reader.ReadByte();

                                    break;
                            }

                            if (Reader.BaseStream.Position == Reader.BaseStream.Length)
                                break;
                        }

                        CurrentRow++;
                        CurrentColumn = 0;
 
                        break;
                    case 4: //Leave the next count rows in the color sprite filled with the transparent color, 
                            //in the z-buffer sprite filled with 255, and in the alpha sprite filled with 0.
                        for (int i = 0; i < RowHeader[1]; i++)
                        {
                            for (int j = 0; j < Frame.Width; j++)
                            {
                                Frame.BitmapData.SetPixel(new Point(CurrentColumn, CurrentRow), 
                                    Color.FromArgb(0, 0, 0, 0));

                                if (Frame.HasZBuffer)
                                {
                                    Frame.ZBuffer.SetPixel(new Point(CurrentColumn, CurrentRow),
                                        Color.FromArgb(255, 255, 255, 255));
                                }

                                CurrentColumn++;
                            }

                            CurrentColumn = 0;
                            CurrentRow++;
                        }

                        CurrentColumn = 0;

                        break;
                    case 5: //Sprite end marker; the count byte is always 0, but may be ignored.
                        Quit = true;
                        break;
                }

                if (Reader.BaseStream.Position == Reader.BaseStream.Length)
                    break;
            }
        }

        /// <summary>
        /// Saves the frames in this SPR2 to a specified directory.
        /// </summary>
        /// <param name="directory">The directory to save to.</param>
        public void ExportToBitmaps(string directory)
        {
            if (m_Version == 1001)
            {
                foreach (SpriteFrame Frame in m_Frames)
                {
                    string fileLocation = directory + '\\' + m_ID + '_' + Frame.Width + 'x' + Frame.Height + ".bmp";
                    if (File.Exists(fileLocation))
                        File.Delete(fileLocation);
                    Frame.BitmapData.BitMap.Save(fileLocation); //This calls FastPixel.Unlock()
                    Frame.BitmapData.Lock(); //The bitmap should always be locked!
                }
            }
        }

        /// <summary>
        /// Decrypts a spriteframe's rowheader.
        /// </summary>
        /// <param name="P">The rowheader that was read from the spriteframe's data.</param>
        /// <returns></returns>
        private int[] GetDecryptedValues(ushort P)
        {
            // 0xe000 = 1110 0000 0000 0000 : high order 3 bits
            // 0x1fff = 0001 1111 1111 1111 : low order 13 bits
            // 0xd = shift 13 bits to the right
            // as documented, 0xa000 is the stop value, with value 5 in the high order 3 bits ->
            //	0xa000 = 1010 0000 0000 0000 little endian
            //	high order 3 bits are 101, or value 5

            return new int[] {(P>>13),    // Code
                              ((P&0x1FFF))};            // Count
        }
    }
}