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
using TSOClient.Code.UI.Framework.Parser;
using TSOClient.Code.Utils;

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
        [UIAttribute("font", typeof(TextStyle))]
        public TextStyle CaptionStyle { get; set; }
        private string m_Text = "";

        [UIAttribute("text", DataType=UIAttributeType.StringTable)]
        public string Caption
        {
            get { return m_Text; }
            set { m_Text = value; }
        }

        /// <summary>
        /// If size is set you can make use of alignment settings
        /// </summary>
        [UIAttribute("size")]
        public virtual Vector2 Size {
            get
            {
                if (m_Size != null)
                {
                    return new Vector2(m_Size.X, m_Size.Y);
                }
                return Vector2.Zero;
            }
            set
            {
                m_Size = new Rectangle(0, 0, (int)value.X, (int)value.Y);
            }
        }
        private Rectangle m_Size;

        public UILabel()
        {
            CaptionStyle = TextStyle.DefaultLabel;
        }

        public TextAlignment Alignment = TextAlignment.Center;

        [UIAttribute("alignment")]
        public int _Alignment
        {
            set
            {
                switch (value)
                {
                    case 3:
                        Alignment = TextAlignment.Center | TextAlignment.Middle;
                        break;
                }
            }
        }


        public override void Draw(UISpriteBatch SBatch)
        {
            if (!Visible)
            {
                return;
            }

            if (m_Text != null && CaptionStyle != null)
            {
                //DrawLocalTexture(SBatch, TextureUtils.TextureFromColor(SBatch.GraphicsDevice, Color.Red), null, Vector2.Zero, new Vector2(10, 10));

                if (m_Size != Rectangle.Empty)
                {
                    DrawLocalString(SBatch, m_Text, Vector2.Zero, CaptionStyle, m_Size, Alignment);
                }
                else
                {
                    DrawLocalString(SBatch, m_Text, Vector2.Zero, CaptionStyle);
                }
            }
        }
    }
}
