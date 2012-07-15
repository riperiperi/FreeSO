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

namespace TSOClient.LUI
{
    /// <summary>
    /// A drawable image that is part of the GUI.
    /// Cannot be clicked.
    /// </summary>
    public class UIImage : UIElement
    {
        private int m_X, m_Y;
        private Texture2D m_Texture;
        private string m_StrID;

        public int X
        {
            get { return m_X; }
        }

        public int Y
        {
            get { return m_Y; }
        }

        public Texture2D Texture
        {
            get { return m_Texture; }
        }

        public UIImage(int X, int Y, string StrID, Texture2D Texture, UIScreen Screen)
            : base(Screen, StrID, DrawLevel.DontGiveAFuck)
        {
            m_X = X;
            m_Y = Y;
            m_Texture = Texture;
            m_StrID = StrID;
        }

        public override void Draw(SpriteBatch SBatch)
        {
            int Scale = GlobalSettings.Default.ScaleFactor;

            SBatch.Draw(m_Texture, new Rectangle(m_X, m_Y, m_Texture.Width * Scale, 
                m_Texture.Height * Scale), Color.White);
            base.Draw(SBatch);
        }
    }
}
