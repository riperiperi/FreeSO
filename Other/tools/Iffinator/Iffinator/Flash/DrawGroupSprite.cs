/*This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
If a copy of the MPL was not distributed with this file, You can obtain one at
http://mozilla.org/MPL/2.0/.

The Original Code is the TSO LoginServer.

The Initial Developer of the Original Code is
Propeng. All Rights Reserved.

Contributor(s): Mats 'Afr0' Vederhus
*/

using System;
using System.Collections.Generic;
using System.Text;
using System.Drawing;

namespace Iffinator.Flash
{
    public struct PixelOffset
    {
        public int X, Y;

        public PixelOffset(int x, int y)
        {
            X = x;
            Y = y;
        }
    }

    public struct WorldOffset
    {
        public float X, Y, Z;

        public WorldOffset(float x, float y, float z)
        {
            X = x;
            Y = y;
            Z = z;
        }
    }

    public class DrawGroupSprite
    {
        private ushort m_Type;
        private uint m_Flags;
        private PixelOffset m_SpriteOffset;
        private WorldOffset m_ObjectOffset;
        private Bitmap m_Bitmap;
        private SpriteFrame m_Sprite;

        public ushort Type { get { return m_Type; } }
        public uint Flags { get { return m_Flags; } }
        public PixelOffset SpriteOffset { get { return m_SpriteOffset; } }
        public WorldOffset ObjectOffset { get { return m_ObjectOffset; } }
        public Bitmap Bitmap { get { return m_Bitmap; } }
        public SpriteFrame Sprite { get { return m_Sprite; } }

        public DrawGroupSprite(ushort type, uint flags, PixelOffset spriteOffset, WorldOffset objectOffset, SpriteFrame frame)
        {
            m_Type = type;
            m_Flags = flags;
            m_SpriteOffset = spriteOffset;
            m_ObjectOffset = objectOffset;
            m_Sprite = frame;

            m_Bitmap = (Bitmap)frame.BitmapData.BitMap.Clone();
            if ((m_Flags & 0x1) == 0x1)
            {
                m_Bitmap.RotateFlip(RotateFlipType.RotateNoneFlipX);
            }
        }
    }
}
