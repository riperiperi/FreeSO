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
using TSOClient.Code.UI.Framework;

namespace TSOClient.Code.UI.Controls
{
    /// <summary>
    /// A drawable label containing text.
    /// </summary>
    public class UILabel : UIElement
    {
        /// <summary>
        /// The font to use when rendering the label
        /// </summary>
        public TextStyle CaptionStyle = TextStyle.DefaultLabel;

        private string m_Text = "";

        public string Caption
        {
            get { return m_Text; }
            set { m_Text = value; }
        }

        public UILabel()
        {
        }


        public override void Draw(SpriteBatch SBatch)
        {
            if (!Visible)
            {
                return;
            }

            if (m_Text != null && CaptionStyle != null)
            {
                DrawLocalString(SBatch, m_Text, Vector2.Zero, CaptionStyle);
            }
        }
    }
}
