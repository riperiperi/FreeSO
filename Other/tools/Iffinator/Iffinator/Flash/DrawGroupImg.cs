/*The contents of this file are subject to the Mozilla Public License Version 1.1
(the "License"); you may not use this file except in compliance with the
License. You may obtain a copy of the License at http://www.mozilla.org/MPL/

Software distributed under the License is distributed on an "AS IS" basis,
WITHOUT WARRANTY OF ANY KIND, either express or implied. See the License for
the specific language governing rights and limitations under the License.

The Original Code is the TSO LoginServer.

The Initial Developer of the Original Code is
Propeng. All Rights Reserved.

Contributor(s): Mats 'Afr0' Vederhus
*/

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text;

namespace Iffinator.Flash
{
    public class DrawGroupImg
    {
        private uint m_SpriteCount;
        private uint m_DirectionFlag;
        private uint m_Zoom;
        private List<DrawGroupSprite> m_Sprites = new List<DrawGroupSprite>();
        private Bitmap m_CompiledBitmap;

        public uint SpriteCount { get { return m_SpriteCount; } }
        public uint DirectionFlag { get { return m_DirectionFlag; } }
        public uint Zoom { get { return m_Zoom; } }
        public List<DrawGroupSprite> Sprites { get { return m_Sprites; } }
        public Bitmap CompiledBitmap { get { return m_CompiledBitmap; } }

        public DrawGroupImg(uint SpriteCount, uint DirectionFlag, uint Zoom)
        {
            m_SpriteCount = SpriteCount;
            m_DirectionFlag = DirectionFlag;
            m_Zoom = Zoom;
        }

        /// <summary>
        /// Compiles the list of sprites into a tile bitmap
        /// </summary>
        public void CompileSprites()
        {
            // TODO: Render transparency and z-buffer channels
            // TODO: Mirrored sprites are not aligned correctly

            m_CompiledBitmap = new Bitmap(136, 384);
            Graphics gfx = Graphics.FromImage(m_CompiledBitmap);

            foreach (DrawGroupSprite Sprite in Sprites)
            {
                int xOffset = m_CompiledBitmap.Width / 2 + Sprite.SpriteOffset.X;
                int yOffset = m_CompiledBitmap.Height / 2 + Sprite.SpriteOffset.Y;

                gfx.DrawImageUnscaled(Sprite.Bitmap, Sprite.Sprite.XLocation, Sprite.Sprite.YLocation);
            }
            gfx.Dispose();
        }
    }
}
