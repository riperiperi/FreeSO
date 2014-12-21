/*This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
If a copy of the MPL was not distributed with this file, You can obtain one at
http://mozilla.org/MPL/2.0/.

The Original Code is the SimsLib.

The Initial Developer of the Original Code is
Mats 'Afr0' Vederhus. All Rights Reserved.

Contributor(s): Nicholas Roth
*/

using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Drawing;

namespace SimsLib.IFF
{
    /// <summary>
    /// Represents a DrawGRouP chunk.
    /// The DGRP chunk is used to combine multiple sprites into one object.
    /// </summary>
    public class DrawGroup : IffChunk
    {
        private const int INDEX_VERSION = 68;
		private const int INDEX_IMAGE = 226;

        private int m_Version;
        private List<DrawGroupImg> m_Images = new List<DrawGroupImg>();
        //The SPR2-sprites in this IFF, passed through the constructor. 
        private List<SPR2Parser> m_Sprites = new List<SPR2Parser>();

        public int Version { get { return m_Version; } }

        /// <summary>
        /// Gets a DrawGroupImg instance.
        /// </summary>
        /// <param name="Index">The index of the image to get.</param>
        /// <returns>A DrawGroupImg instance.</returns>
        public DrawGroupImg GetImage(int Index)
        {
            return LoadImage(Index);
        }

        /// <summary>
        /// The number of DrawGroupImg instances that this DrawGroup has.
        /// </summary>
        public int ImageCount
        {
            get { return m_Images.Count; }
        }

        /// <summary>
        /// Creates a new drawgroup instance.
        /// </summary>
        /// <param name="ChunkData">The data for the chunk.</param>
        /// <param name="Sprites">The sprites that the DGRP consists of.</param>
        public DrawGroup(IffChunk Chunk, List<SPR2Parser> Sprites) : base(Chunk)
        {
            MemoryStream MemStream = new MemoryStream(Chunk.Data);
            BinaryReader Reader = new BinaryReader(MemStream);

            m_ID = ID;
            m_Sprites = Sprites;

            m_Version = Reader.ReadUInt16() - 20000;

            uint Count = (m_Version < 3) ? Reader.ReadUInt16() : Reader.ReadUInt32();
            uint SpriteCount, DirectionFlag, Zoom;

            for (int i = 0; i < Count; i++)
            {
                if (m_Version < 3)
                {
                    SpriteCount = Reader.ReadUInt16();
                    DirectionFlag = Reader.ReadByte();
                    Zoom = Reader.ReadByte();
                }
                else
                {
                    DirectionFlag = Reader.ReadUInt32();
                    Zoom = Reader.ReadUInt32();
                    SpriteCount = Reader.ReadUInt32();
                }
                DrawGroupImg Image = new DrawGroupImg(SpriteCount, DirectionFlag, Zoom);

                for (int j = 0; j < SpriteCount; j++)
                {
                    ushort Tag = 0;
                    int PixelX = 0, PixelY = 0;
                    uint SprID = 0, SprFrameID = 0, Flags = 0;
                    float ZOffset = 0, XOffset = 0, YOffset = 0;

                    if (m_Version < 3)
                    {
                        Tag = Reader.ReadUInt16();
                        SprID = Reader.ReadUInt16();
                        SprFrameID = Reader.ReadUInt16();
                        Flags = Reader.ReadUInt16();
                        PixelX = Reader.ReadInt16();
                        PixelY = Reader.ReadInt16();

                        if (m_Version == 1)
                            ZOffset = Reader.ReadUInt32();
                    }
                    else
                    {
                        SprID = Reader.ReadUInt32();
                        SprFrameID = Reader.ReadUInt32();
                        PixelX = Reader.ReadInt32();
                        PixelY = Reader.ReadInt32();
                        ZOffset = Reader.ReadUInt32();
                        Flags = Reader.ReadUInt32();
                        if (m_Version == 4)
                        {
                            XOffset = Reader.ReadUInt32();
                            YOffset = Reader.ReadUInt32();
                        }
                    }

                    SpriteFrame Frame = FindSpriteFrame(SprID, SprFrameID);
                    if (Frame != null)
                    {
                        DrawGroupSprite Sprite = new DrawGroupSprite(Tag, Flags, new PixelOffset(PixelX, PixelY),
                            new WorldOffset(XOffset, YOffset, ZOffset), Frame);
                        Image.Sprites.Insert(0, Sprite);
                    }
                }

                m_Images.Add(Image);
            }
        }

        private DrawGroupImg LoadImage(int Index)
        {
            DrawGroupImg Img = m_Images[Index];
            Img.CompileSprites();

            return Img;
        }

        /// <summary>
        /// Searches in a list of sprites for a specific chunk, and returns the specified frame index.
        /// </summary>
        private SpriteFrame FindSpriteFrame(uint chunkID, uint frameIndex)
        {
            foreach (SPR2Parser Sprite in m_Sprites)
            {
                if (Sprite.ID == chunkID && Sprite.FrameCount > frameIndex)
                {
                    return Sprite.GetFrame((int)frameIndex);
                }
            }

            return null;
        }
    }
}
