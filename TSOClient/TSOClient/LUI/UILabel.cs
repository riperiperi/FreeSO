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
    /// A drawable label containing text.
    /// </summary>
    public class UILabel : UIElement
    {
        private float m_X, m_Y;

        private string m_Text = "";
        private string m_StrID = "";

        public string Caption
        {
            get { return m_Text; }
            set { m_Text = value; }
        }

        /// <summary>
        /// This label's X position.
        /// Set from Lua, so needs to have a set accessor.
        /// </summary>
        public float X
        {
            get { return m_X; }
            set { m_X = value; }
        }
        /// <summary>
        /// This label's Y position.
        /// Set from Lua, so needs to have a set accessor.
        /// </summary>
        public float Y
        {
            get { return m_Y; }
            set { m_Y = value; }
        }

        public UILabel(int CaptionID, string StrID, float X, float Y, UIScreen Screen)
            : base(Screen, StrID, DrawLevel.DontGiveAFuck)
        {
            m_X = X;
            m_Y = Y;

            if (Screen.ScreenMgr.TextDict.ContainsKey(CaptionID))
                m_Text = Screen.ScreenMgr.TextDict[CaptionID];

            m_StrID = StrID;
        }

        public UILabel(string Caption, string StrID, float X, float Y, UIScreen Screen)
            : base(Screen, StrID, DrawLevel.DontGiveAFuck)
        {
            m_X = X;
            m_Y = Y;

            m_Text = Caption;

            m_StrID = StrID;
        }

        public override void Draw(SpriteBatch SBatch)
        {
            base.Draw(SBatch);

            float Scale = GlobalSettings.Default.ScaleFactor;

            if (m_Text != null)
                SBatch.DrawString(m_Screen.ScreenMgr.SprFontBig, m_Text, new Vector2(m_X * Scale, m_Y * Scale), Color.Wheat);
        }
    }
}
