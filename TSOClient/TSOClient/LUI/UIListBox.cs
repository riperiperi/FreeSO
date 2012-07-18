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
    /// A box listing various items (mostly used for the UICitySelection dialog).
    /// </summary>
    class UIListBox : UIElement
    {
        private float m_X, m_Y;
        private int m_Width;
        private List<string> m_ListItems;
        private int m_VerticalSpacing;      //Spacing between strings as they are rendered.          

        public UIListBox(float X, float Y, int Width, int Spacing, UIScreen Screen, string StrID, DrawLevel DLevel)
            : base(Screen, StrID, DLevel)
        {
            m_X = X;
            m_Y = Y;
            m_Width = Width;

            m_ListItems = new List<string>();
        }

        /// <summary>
        /// Adds an item to this UIListBox's list of items.
        /// For the time being, items can only be strings.
        /// </summary>
        /// <param name="Item">The item to add.</param>
        public void AddItem(string Item)
        {
            m_ListItems.Add(Item);
        }

        public override void Update(GameTime GTime, ref MouseState CurrentMouseState, ref MouseState PrevioMouseState)
        {
            base.Update(GTime, ref CurrentMouseState, ref PrevioMouseState);
        }

        public override void Draw(SpriteBatch SBatch)
        {
            int VerticalCounter = m_VerticalSpacing;

            float Scale = GlobalSettings.Default.ScaleFactor;

            foreach (string Str in m_ListItems)
            {
                SBatch.DrawString(m_Screen.ScreenMgr.SprFontBig, Str, new Vector2((m_X + (m_Width * Scale)/ 2) * Scale,
                    (m_Y + VerticalCounter) * Scale), Color.Wheat);
                VerticalCounter += m_VerticalSpacing;
            }

            base.Draw(SBatch);
        }
    }
}
