/*The contents of this file are subject to the Mozilla Public License Version 1.1
(the "License"); you may not use this file except in compliance with the
License. You may obtain a copy of the License at http://www.mozilla.org/MPL/

Software distributed under the License is distributed on an "AS IS" basis,
WITHOUT WARRANTY OF ANY KIND, either express or implied. See the License for
the specific language governing rights and limitations under the License.

The Original Code is the TSO LoginServer.

The Initial Developer of the Original Code is
Mats 'Afr0' Vederhus. All Rights Reserved.
*/

using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Windows.Forms;
using System.Drawing;
using Iffinator.Iff.DGRP;

namespace Iffinator.Iff
{
    /// <summary>
    /// Represents an *.iff archive.
    /// </summary>
    class IffArchive
    {
        private BinaryReader m_Reader;
        
        private List<IffChunk> m_Chunks;
        private List<DrawGroup> m_DGRPs;

        public IffArchive(string Path)
        {
            try
            {
                m_Reader = new BinaryReader(File.Open(Path, FileMode.Open));
                
                m_Chunks = new List<IffChunk>();
                m_DGRPs = new List<DrawGroup>();
            }
            catch (Exception)
            {
                throw new Exception("Archive could not be opened!");
            }

            ReadChunks();
        }

        private bool ReadChunks()
        {
            string Identifier = new string(m_Reader.ReadChars(60)).Replace("\0", "");

            if (Identifier != "IFF FILE 2.5:TYPE FOLLOWED BY SIZE JAMIE DOORNBOS & MAXIS 1")
            {
                MessageBox.Show("Unknown Iff Archive!");
                return false;
            }

            m_Reader.ReadBytes(4); //RSMP offset?

            while (m_Reader.BaseStream.Position < m_Reader.BaseStream.Length)
            {
                IffChunk Chunk = new IffChunk();
                Chunk.Offset = m_Reader.BaseStream.Position;
                Chunk.Type = new string(m_Reader.ReadChars(4));
                Chunk.Size = HexToInt(m_Reader.ReadBytes(4));
                Chunk.TypeNum = (short)HexToInt(m_Reader.ReadBytes(2));
                Chunk.ID = (short)HexToInt(m_Reader.ReadBytes(2));
                Chunk.Label = m_Reader.ReadChars(64);
                Chunk.Data = m_Reader.ReadBytes((int)Chunk.Size - 76);

                m_Chunks.Add(Chunk);
            }

            return true;
        }

        public void ExtractChunks(string Path)
        {
            BinaryWriter Writer;

            foreach (IffChunk Chunk in m_Chunks)
            {
                Writer = new BinaryWriter(File.Create(Path + "\\" + Chunk.TypeNum + "." + Chunk.Type));

                Writer.Write(Chunk.Data);
                Writer.Close();
            }
        }

        public void ProcessSPR2(string Path)
        {
            List<IffChunk> SPR2s = new List<IffChunk>();

            foreach (IffChunk Chunk in m_Chunks)
            {
                if (Chunk.Type == "SPR2")
                    SPR2s.Add(Chunk);
            }

            foreach (IffChunk Chunk in SPR2s)
            {
                BinaryReader ChunkReader = new BinaryReader(new MemoryStream(Chunk.Data));

                SPR2 Spr = new SPR2();
                Spr.Version1 = ChunkReader.ReadInt16();

                if (Spr.Version1 == 0)
                    Spr.Version2 = ReadBigShort(ChunkReader);
                else
                    Spr.Version2 = ChunkReader.ReadInt16();

                Spr.FrameCount = ChunkReader.ReadInt32();
                MessageBox.Show("Framecount: " + Spr.FrameCount);
                Spr.PaletteID = ChunkReader.ReadInt32();
                Spr.OffsetTable = new int[Spr.FrameCount];

                for (int i = 0; i < Spr.FrameCount; i++ )
                    Spr.OffsetTable[i] = ChunkReader.ReadInt32();

                for (int i = 0; i < Spr.FrameCount; i++)
                {
                    SpriteFrame Frame = new SpriteFrame();
                    Frame.Width = ChunkReader.ReadInt16();
                    Frame.Height = ChunkReader.ReadInt16();
                    Frame.Flags = ChunkReader.ReadInt16();
                    Frame.Unknown = ChunkReader.ReadInt16();
                    Frame.PaletteID = ChunkReader.ReadInt16();
                    Frame.TransparentPixel = ChunkReader.ReadInt16();
                    Frame.XPos = ChunkReader.ReadInt16();
                    Frame.YPos = ChunkReader.ReadInt16();

                    BinaryWriter SpriteWriter = new BinaryWriter(Frame.SpriteData);

                    for (int l = 0; l <= Frame.Height; l++)
                    {
                        SpriteWriter.Write(ChunkReader.ReadBytes(2));
                        SpriteWriter.Write(ChunkReader.ReadBytes(Frame.Width));
                    }

                    SpriteWriter.Flush();
                    //SpriteWriter.Close();

                    Spr.AddFrame(Frame);

                    //Each SPR2 resource contains a reference to a PALT chunk/resource.
                    Spr.Pal = new Palette();
                    IffChunk TmpChunk = new IffChunk();

                    foreach (IffChunk C in m_Chunks)
                    {
                        if (C.TypeNum == Spr.PaletteID)
                        {
                            //Guess what? The typenumber of each chunk is
                            //NOT unique, so you have to check on type as
                            //well!
                            if (C.Type == "PALT")
                            {
                                TmpChunk = C;
                                break;
                            }
                        }
                    }

                    BinaryReader PaltReader = new BinaryReader(new MemoryStream(TmpChunk.Data));
                    Spr.Pal.AlwaysOne = PaltReader.ReadInt32();
                    Spr.Pal.Always256 = PaltReader.ReadInt32();
                    PaltReader.ReadBytes(8); //The PALT header has 8 additional bytes of 0.

                    Spr.Pal.RGBTable = new Palette.RGB[Spr.Pal.Always256];

                    for (int l = 0; l < Spr.Pal.Always256; l++)
                    {
                        Spr.Pal.RGBTable[l] = new Palette.RGB();
                        Spr.Pal.RGBTable[l].Red = PaltReader.ReadByte();
                        Spr.Pal.RGBTable[l].Green = PaltReader.ReadByte();
                        Spr.Pal.RGBTable[l].Blue = PaltReader.ReadByte();
                    }

                    PaltReader.Close();
                }

                for (int i = 0; i < Spr.FrameCount; i++)
                {
                    SpriteFrame Frame = Spr.GetFrame(i);

                    BinaryReader SpriteReader = new BinaryReader(new MemoryStream(Frame.SpriteData.ToArray()));
                    int X = 0, Y = 0;
                    bool Stop = false;

                    Bitmap BM = new Bitmap(Frame.Width, Frame.Height);
                    Color Transparent = Color.FromArgb(Spr.Pal.RGBTable[Frame.TransparentPixel].Red,
                        Spr.Pal.RGBTable[Frame.TransparentPixel].Green,
                        Spr.Pal.RGBTable[Frame.TransparentPixel].Blue);

                    Graphics Gfx = Graphics.FromImage(BM);

                    while (SpriteReader.BaseStream.Position < (SpriteReader.BaseStream.Length - 1) && !Stop)
                    {
                        byte Opcode = SpriteReader.ReadByte();
                        byte Data = SpriteReader.ReadByte();

                        switch (Opcode)
                        {
                            case 1: //Transparent pixels.
                                for (int Dat = 0; Dat < Data; Dat++)
                                {
                                    if(X < Frame.Width)
                                        BM.SetPixel(X, Y, Transparent);
                                    
                                    X++;
                                }

                                break;
                            case 2:
                                byte Col = SpriteReader.ReadByte();

                                for (int Dat = 0; Dat < Data; Dat++)
                                {
                                    if (X < Frame.Width)
                                        BM.SetPixel(X, Y, GetColorFromPalette(Col, Spr));
                                    
                                    X++;
                                }
                                break;
                            case 3: //Pixels.
                                byte Pixel = SpriteReader.ReadByte();

                                for (int Dat = 0; Dat < Data; Dat++)
                                {
                                    if (X < Frame.Width && Y < Frame.Height)
                                        BM.SetPixel(X, Y, GetColorFromPalette(Pixel, Spr));
                                    
                                    X++;
                                }

                                break;
                            case 4: //New line.
                                for (; X < BM.Width; X++)
                                    BM.SetPixel(X, Y, Transparent);

                                Y++; //Next line.
                                X = 0;
                                break;
                            case 5: //End of sprite.
                                Stop = true;
                                break;
                            case 9: //Transparent rows.
                                Gfx.FillRectangle(Brushes.Transparent, 
                                    new Rectangle(0, Y, BM.Width, (Y + (Data - 1))));
                                X = 0;
                                Y = Y + (Data - 1);
                                break;
                        }
                    }

                    Random Rnd = new Random(DateTime.Now.Millisecond);

                    BM.Save(Path + "\\" + i.ToString() + Rnd.Next() + ".png", System.Drawing.Imaging.ImageFormat.Png);
                }
            }
        }

        public void ProcessDGRPs()
        {
            List<IffChunk> DGRPs = new List<IffChunk>();

            foreach (IffChunk Chunk in m_Chunks)
            {
                if (Chunk.Type == "DGRP")
                    DGRPs.Add(Chunk);
            }

            foreach (IffChunk Chunk in DGRPs)
            {
                BinaryReader ChunkReader = new BinaryReader(new MemoryStream(Chunk.Data));
                DrawGroup DGRP = new DrawGroup();
                DGRP.Version = ChunkReader.ReadInt16();

                if (DGRP.Version == 20000 || DGRP.Version == 20001)
                {
                    DGRP.Count = Convert.ToInt32(ChunkReader.ReadBytes(2));

                    for (int i = 0; i < DGRP.Count; i++)
                    {
                        DGRPImg Img = new DGRPImg();
                        Img.SpriteInfoCount = Convert.ToInt32(ChunkReader.ReadBytes(2));
                        Img.DirFlags = ChunkReader.ReadByte();
                        Img.ZoomFactor = ChunkReader.ReadByte();

                        for (int j = 0; j < Img.SpriteInfoCount; j++)
                        {
                            SpriteInfo SInfo = new SpriteInfo();
                            SInfo.Tag = ChunkReader.ReadInt16();
                            SInfo.SprID = Convert.ToInt32(ChunkReader.ReadBytes(2));
                            SInfo.SprFrame = Convert.ToInt32(ChunkReader.ReadBytes(2));
                            SInfo.Flags = Convert.ToInt32(ChunkReader.ReadBytes(2));
                            SInfo.PixelX = Convert.ToInt32(ChunkReader.ReadBytes(2));
                            SInfo.PixelY = Convert.ToInt32(ChunkReader.ReadBytes(2));

                            if (DGRP.Version == 20001)
                                SInfo.ZOffset = ChunkReader.ReadInt32();

                            SInfo.Sprite = new SPR2();
                            IffChunk TmpChunk = new IffChunk();

                            foreach (IffChunk C in m_Chunks)
                            {
                                if (C.TypeNum == SInfo.SprID)
                                {
                                    if (C.Type == "SPR2")
                                    {
                                        TmpChunk = C;
                                        break;
                                    }
                                }
                            }

                            BinaryReader SpriteReader = new BinaryReader(new MemoryStream(TmpChunk.Data));
                            SInfo.Sprite.Version1 = SpriteReader.ReadInt16();

                            if (SInfo.Sprite.Version1 == 0)
                                SInfo.Sprite.Version2 = ReadBigShort(SpriteReader);
                            else
                                SInfo.Sprite.Version2 = SpriteReader.ReadInt16();

                            SInfo.Sprite.FrameCount = SpriteReader.ReadInt32();
                            SInfo.Sprite.PaletteID = SpriteReader.ReadInt32();
                            SInfo.Sprite.OffsetTable = new int[SInfo.Sprite.FrameCount];

                            for (int k = 0; k < SInfo.Sprite.FrameCount; k++)
                                SInfo.Sprite.OffsetTable[k] = SpriteReader.ReadInt32();

                            for (int k = 0; k < SInfo.Sprite.FrameCount; k++)
                            {
                                SpriteFrame Frame = new SpriteFrame();
                                Frame.Width = SpriteReader.ReadInt16();
                                Frame.Height = SpriteReader.ReadInt16();
                                Frame.Flags = SpriteReader.ReadInt16();
                                Frame.Unknown = SpriteReader.ReadInt16();
                                Frame.PaletteID = SpriteReader.ReadInt16();
                                Frame.TransparentPixel = SpriteReader.ReadInt16();
                                Frame.XPos = SpriteReader.ReadInt16();
                                Frame.YPos = SpriteReader.ReadInt16();

                                BinaryWriter SpriteWriter = new BinaryWriter(Frame.SpriteData);

                                for (int l = 0; l <= Frame.Height; l++)
                                {
                                    SpriteWriter.Write(SpriteReader.ReadBytes(2));
                                    SpriteWriter.Write(SpriteReader.ReadBytes(Frame.Width));
                                }

                                SInfo.Sprite.AddFrame(Frame);

                                SpriteWriter.Close();
                            }

                            SpriteReader.Close();

                            //Each SPR2 resource contains a reference to a PALT chunk/resource.
                            SInfo.Sprite.Pal = new Palette();

                            foreach (IffChunk C in m_Chunks)
                            {
                                if (C.TypeNum == SInfo.Sprite.PaletteID)
                                {
                                    //Guess what? The typenumber of each chunk is
                                    //NOT unique, so you have to check on type as
                                    //well!
                                    if (C.Type == "PALT")
                                    {
                                        TmpChunk = C;
                                        break;
                                    }
                                }
                            }

                            BinaryReader PaltReader = new BinaryReader(new MemoryStream(TmpChunk.Data));
                            SInfo.Sprite.Pal.AlwaysOne = PaltReader.ReadInt32();
                            SInfo.Sprite.Pal.Always256 = PaltReader.ReadInt32();
                            PaltReader.ReadBytes(8); //The PALT header has 8 additional bytes of 0.

                            SInfo.Sprite.Pal.RGBTable = new Palette.RGB[SInfo.Sprite.Pal.Always256];

                            for (int l = 0; l < SInfo.Sprite.Pal.Always256; l++)
                            {
                                SInfo.Sprite.Pal.RGBTable[l] = new Palette.RGB();
                                SInfo.Sprite.Pal.RGBTable[l].Red = PaltReader.ReadByte();
                                SInfo.Sprite.Pal.RGBTable[l].Green = PaltReader.ReadByte();
                                SInfo.Sprite.Pal.RGBTable[l].Blue = PaltReader.ReadByte();
                            }

                            PaltReader.Close();

                            Img.AddSpriteInfo(SInfo);
                        }

                        DGRP.AddImage(Img);
                    }
                }
                else if (DGRP.Version == 20003 || DGRP.Version == 20004)
                {
                    DGRP.Count = ChunkReader.ReadInt32();

                    for (int i = 0; i < DGRP.Count; i++)
                    {
                        DGRPImg Img = new DGRPImg();
                        Img.DirFlags = ChunkReader.ReadInt32();
                        Img.ZoomFactor = ChunkReader.ReadInt32();
                        Img.SpriteInfoCount = ChunkReader.ReadInt32();

                        //Each DrawGroup Image contains a number of SpriteInfo resources.

                        for (int j = 0; j < Img.SpriteInfoCount; j++)
                        {
                            SpriteInfo SInfo = new SpriteInfo();
                            SInfo.SprID = ChunkReader.ReadInt32();
                            SInfo.SprFrame = ChunkReader.ReadInt32();
                            SInfo.PixelX = ChunkReader.ReadInt32();
                            SInfo.PixelY = ChunkReader.ReadInt32();
                            SInfo.ZOffset = ChunkReader.ReadInt32();
                            SInfo.Flags = ChunkReader.ReadInt32();
                            SInfo.XOffset = ChunkReader.ReadInt32();
                            SInfo.YOffset = ChunkReader.ReadInt32();

                            //Each SpriteInfo resource contains a reference to a SPR2 chunk/resource.

                            SInfo.Sprite = new SPR2();
                            IffChunk TmpChunk = new IffChunk();

                            foreach (IffChunk C in m_Chunks)
                            {
                                if (C.TypeNum == SInfo.SprID)
                                {
                                    //Guess what? The typenumber of each chunk is
                                    //NOT unique, so you have to check on type as
                                    //well!
                                    if (C.Type == "SPR2")
                                    {
                                        TmpChunk = C;
                                        break;
                                    }
                                }
                            }

                            BinaryReader SpriteReader = new BinaryReader(new MemoryStream(TmpChunk.Data));
                            SInfo.Sprite.Version1 = SpriteReader.ReadInt16();

                            if (SInfo.Sprite.Version1 == 0)
                                SInfo.Sprite.Version2 = ReadBigShort(SpriteReader);
                            else
                                SInfo.Sprite.Version2 = SpriteReader.ReadInt16();

                            SInfo.Sprite.FrameCount = SpriteReader.ReadInt32();
                            SInfo.Sprite.PaletteID = SpriteReader.ReadInt32();
                            SInfo.Sprite.OffsetTable = new int[SInfo.Sprite.FrameCount];

                            for (int k = 0; k < SInfo.Sprite.FrameCount; k++)
                                SInfo.Sprite.OffsetTable[k] = SpriteReader.ReadInt32();

                            for (int k = 0; k < SInfo.Sprite.FrameCount; k++)
                            {
                                SpriteFrame Frame = new SpriteFrame();
                                Frame.Width = ReadBigShort(SpriteReader);
                                MessageBox.Show("Frame.Width: " + Frame.Width.ToString());
                                Frame.Height = ReadBigShort(SpriteReader);
                                MessageBox.Show("Frame.Height: " + Frame.Height.ToString());
                                Frame.Flags = SpriteReader.ReadInt16();
                                Frame.Unknown = ReadBigShort(SpriteReader);
                                MessageBox.Show("Unknown: " + Frame.Unknown.ToString());
                                Frame.PaletteID = SpriteReader.ReadInt16();
                                Frame.TransparentPixel = SpriteReader.ReadInt16();
                                Frame.XPos = SpriteReader.ReadInt16();
                                Frame.YPos = SpriteReader.ReadInt16();

                                BinaryWriter SpriteWriter = new BinaryWriter(Frame.SpriteData);

                                for (int l = 0; l <= Frame.Height; l++)
                                {
                                    SpriteWriter.Write(SpriteReader.ReadBytes(2));
                                    SpriteWriter.Write(SpriteReader.ReadBytes(Frame.Width));
                                }

                                SInfo.Sprite.AddFrame(Frame);

                                SpriteWriter.Close();
                            }

                            SpriteReader.Close();

                            //Each SPR2 resource contains a reference to a PALT chunk/resource.
                            SInfo.Sprite.Pal = new Palette();

                            foreach (IffChunk C in m_Chunks)
                            {
                                if (C.TypeNum == SInfo.Sprite.PaletteID)
                                {
                                    //Guess what? The typenumber of each chunk is
                                    //NOT unique, so you have to check on type as
                                    //well!
                                    if (C.Type == "PALT")
                                    {
                                        TmpChunk = C;
                                        break;
                                    }
                                }
                            }

                            BinaryReader PaltReader = new BinaryReader(new MemoryStream(TmpChunk.Data));
                            SInfo.Sprite.Pal.AlwaysOne = PaltReader.ReadInt32();
                            SInfo.Sprite.Pal.Always256 = PaltReader.ReadInt32();
                            PaltReader.ReadBytes(8); //The PALT header has 8 additional bytes of 0.

                            SInfo.Sprite.Pal.RGBTable = new Palette.RGB[SInfo.Sprite.Pal.Always256];

                            for (int l = 0; l < SInfo.Sprite.Pal.Always256; l++)
                            {
                                SInfo.Sprite.Pal.RGBTable[l] = new Palette.RGB();
                                SInfo.Sprite.Pal.RGBTable[l].Red = PaltReader.ReadByte();
                                SInfo.Sprite.Pal.RGBTable[l].Green = PaltReader.ReadByte();
                                SInfo.Sprite.Pal.RGBTable[l].Blue = PaltReader.ReadByte();
                            }

                            PaltReader.Close();

                            Img.AddSpriteInfo(SInfo);
                        }

                        DGRP.AddImage(Img);
                    }
                }

                m_DGRPs.Add(DGRP);
            }
        }

        private short ReadBigShort(BinaryReader Reader)
        {
            byte A = Reader.ReadByte();
            byte B = Reader.ReadByte();

            return (short)((A << 8) + B);
        }

        public void ExtractFrames(string Path)
        {
            foreach (DrawGroup DGRP in m_DGRPs)
            {
                for (int i = 0; i < DGRP.NumberOfImages; i++)
                {
                    DGRPImg Img = DGRP.GetImage(i);

                    for (int j = 0; j < Img.SpriteInfoCount; j++)
                    {
                        SpriteInfo SprInfo = Img.GetSpriteInfo(j);

                        for (int k = 0; k < SprInfo.Sprite.FrameCount; k++)
                        {
                            SpriteFrame Frame = SprInfo.Sprite.GetFrame(k);
                            Bitmap BM = new Bitmap(Frame.Width, Frame.Height);
                            Color Transparent = Color.FromArgb(SprInfo.Sprite.Pal.RGBTable[255].Red,
                                SprInfo.Sprite.Pal.RGBTable[255].Green, SprInfo.Sprite.Pal.RGBTable[255].Blue);

                            BinaryReader SpriteReader = new BinaryReader(Frame.SpriteData);

                            for (int Y = 0; Y < Frame.Height; Y++)
                            {
                                byte Opcode = SpriteReader.ReadByte();
                                byte Data = SpriteReader.ReadByte();

                                switch (Opcode)
                                {
                                    case 1: //Transparent pixels
                                        for(int X = 0; X < Frame.Width; X++)
                                            BM.SetPixel(X, Y, Transparent);
                                        break;
                                    case 2:
                                        byte Col = SpriteReader.ReadByte();

                                        for(int X = 0; X < Frame.Width; X++)
                                            BM.SetPixel(X, Y, GetColorFromPalette(Col, SprInfo));

                                        break;
                                    case 3: //Pixels
                                        byte Pixel = SpriteReader.ReadByte();

                                        for (int X = 0; X < Frame.Width; X++)
                                            BM.SetPixel(X, Y, GetColorFromPalette(Pixel, SprInfo));

                                        break;
                                }
                            }

                            BM.Save(Path + "\\" + k.ToString() + ".bmp");
                            MessageBox.Show("Saved: " + Path + "\\" + k.ToString() + ".bmp!");
                        }
                    }
                }
            }
        }

        private Color GetColorFromPalette(int PaletteIndex, SpriteInfo SInfo)
        {
            return Color.FromArgb(SInfo.Sprite.Pal.RGBTable[PaletteIndex].Red,
                SInfo.Sprite.Pal.RGBTable[PaletteIndex].Green, SInfo.Sprite.Pal.RGBTable[PaletteIndex].Blue);
        }

        private Color GetColorFromPalette(int PaletteIndex, SPR2 Sprite)
        {
            return Color.FromArgb(Sprite.Pal.RGBTable[PaletteIndex].Red,
                Sprite.Pal.RGBTable[PaletteIndex].Green, Sprite.Pal.RGBTable[PaletteIndex].Blue);
        }

        private int ConvertFromCharArray(char[] CharArray)
        {
            int result = 0;

            for (int i = 0; i < CharArray.Length; ++i)
                result += (CharArray[(CharArray.Length - 1) - i] << (i * 8));

            return result;
        }

        private long HexToInt(byte[] Bytes)
        {
            long Result = 0;
            byte B = 0;

            for (int i = 0; i < Bytes.Length; i++)
            {
                B = Bytes[Bytes.Length - 1 - i];
                Result = Result + (long)(B * Math.Pow(256, i));
            }

            return Result;
        }

        public List<IffChunk> IffChunks
        {
            get { return m_Chunks; }
        }
    }
}
