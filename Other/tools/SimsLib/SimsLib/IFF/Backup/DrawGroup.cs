using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Drawing;

namespace Iffinator.Flash
{
    class DrawGroup
    {
        private const int INDEX_VERSION = 68;
		private const int INDEX_IMAGE = 226;

        private int m_Version;
        private DrawGroupImg m_Img;

        public DrawGroup(byte[] ChunkData, List<SpriteParser> Sprites)
        {
            MemoryStream MemStream = new MemoryStream(ChunkData);
            BinaryReader Reader = new BinaryReader(MemStream);

            Reader.BaseStream.Position = INDEX_VERSION;
            m_Version = Reader.ReadUInt16() - 20000;

            uint Count = (m_Version < 3) ? Reader.ReadUInt16() : Reader.ReadUInt32();
            uint SpriteCount;
            DrawGroupImg Img;

            for (int i = 0; i < Count; i++)
            {
                if (m_Version < 3)
                {
                    SpriteCount = Reader.ReadUInt16();
                    ushort DirectionFlag = Reader.ReadUInt16();
                    ushort Zoom = Reader.ReadUInt16();

                    Img = new DrawGroupImg(DirectionFlag, Zoom);
                    Img.BitmapData = new Bitmap(136, 184);
                }
                else
                {
                    uint DirectionFlag = Reader.ReadUInt32();
                    uint Zoom = Reader.ReadUInt32();
                    SpriteCount = Reader.ReadUInt32();

                    m_Img = new DrawGroupImg(DirectionFlag, Zoom);
                    m_Img.BitmapData = new Bitmap(136, 384);
                }

                for (int j = 0; j < SpriteCount; j++)
                {
                    uint Tag, SprID, SprFrameID, Flags, PixelX, PixelY, ZOffset, XOffset, YOffset;

                    if (m_Version < 3)
                    {
                        SprID = Reader.ReadUInt16();
                        Tag = Reader.ReadUInt16();
                        SprFrameID = Reader.ReadUInt16();
                        Flags = Reader.ReadUInt16();
                        PixelX = Reader.ReadUInt16();
                        PixelY = Reader.ReadUInt16();

                        if (m_Version == 1)
                            ZOffset = Reader.ReadUInt16();
                    }
                    else
                    {
                        SprID = Reader.ReadUInt32();
                        Reader.BaseStream.Position =+ 3;
                        SprFrameID = Reader.ReadUInt32();
                        PixelX = Reader.ReadUInt32();
                        PixelY = Reader.ReadUInt32();
                        ZOffset = Reader.ReadUInt32();
                        Flags = Reader.ReadUInt32();
                        XOffset = Reader.ReadUInt32();
                        YOffset = Reader.ReadUInt32();
                    }

                    foreach (SpriteParser Sprite in Sprites)
                    {
                        if (Sprite.ID == SprID)
                        {
                            SpriteFrame Frame = Sprite.GetFrame((int)SprFrameID);

                            if (Frame != null)
                            {
                                RectangleF Rect = new RectangleF(Frame.XLocation, Frame.YLocation, Frame.Width, Frame.Height);
                                m_Img.BitmapData.Clone(Rect, Frame.BitmapData.PixelFormat);
                            }
                        }
                    }
                }
            }
        }
    }
}
