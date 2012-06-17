/*The contents of this file are subject to the Mozilla Public License Version 1.1
(the "License"); you may not use this file except in compliance with the
License. You may obtain a copy of the License at http://www.mozilla.org/MPL/

Software distributed under the License is distributed on an "AS IS" basis,
WITHOUT WARRANTY OF ANY KIND, either express or implied. See the License for
the specific language governing rights and limitations under the License.

The Original Code is the TSOClient.

The Initial Developer of the Original Code is
Mats 'Afr0' Vederhus. All Rights Reserved.

Contributor(s): ______________________________________.
*/

using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace TSOClient.LUI
{
    /// <summary>
    /// Determines at what level a UIElement will be drawn.
    /// </summary>
    public enum DrawLevel
    {
        AlwaysOnTop = 0x00,
        DontGiveAFuck = 0x01,
        AlwaysOnBottom = 0x02
    }

    /// <summary>
    /// Base class for all UIElements.
    /// </summary>
    public abstract class UIElement
    {
        protected UIScreen m_Screen;
        protected string m_StringID;

        protected DrawLevel m_DrawLevel;

        /// <summary>
        /// Determines what level this UIElement will be drawn at.
        /// </summary>
        public DrawLevel DrawingLevel
        {
            get { return m_DrawLevel; }
        }

        public string StrID
        {
            get { return m_StringID; }
        }

        public UIElement(UIScreen Screen, string StrID, DrawLevel Level)
        {
            m_Screen = Screen;
            m_StringID = StrID;
            m_DrawLevel = Level;
        }

        public virtual void Draw(SpriteBatch SBatch) { }

        public virtual void Update(GameTime GTime) { }

        public virtual void Update(GameTime GTime, ref MouseState CurrentMouseState,
            ref MouseState PrevioMouseState) { }

        /// <summary>
        /// Manually replaces a specified color in a texture with transparent black,
        /// thereby masking it.
        /// </summary>
        /// <param name="Texture">The texture on which to apply the mask.</param>
        /// <param name="ColorFrom">The color to mask away.</param>
        protected void ManualTextureMask(ref Texture2D Texture, Color ColorFrom)
        {
            Color ColorTo = Color.TransparentBlack;

            Color[] data = new Color[Texture.Width * Texture.Height];
            Texture.GetData(data);

            for (int i = 0; i < data.Length; i++)
            {
                if (data[i] == ColorFrom)
                    data[i] = ColorTo;
            }

            Texture.SetData(data);
        }

        protected void ManualTextureMask(ref Texture2D Texture, Color[] ColorsFrom)
        {
                        Color ColorTo = Color.TransparentBlack;

            Color[] data = new Color[Texture.Width * Texture.Height];
            Texture.GetData(data);

            for (int i = 0; i < data.Length; i++)
            {
                foreach (Color Clr in ColorsFrom)
                {
                    if (data[i] == Clr)
                        data[i] = ColorTo;
                }
            }

            Texture.SetData(data);
        }
    }
}
