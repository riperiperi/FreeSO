/*
This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
If a copy of the MPL was not distributed with this file, You can obtain one at
http://mozilla.org/MPL/2.0/.
*/

using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Graphics;

namespace FSO.Client.UI.Framework
{
    public abstract class GameScreen : UIScreen
    {
        private bool m_Scale800x600;

        public bool Scale800x600
        {
            get { return m_Scale800x600; }
            set
            {
                m_Scale800x600 = value;
                if (value)
                {
                    ScaleX = ScaleY = ScreenWidth / 800.0f;
                }
                else
                {
                    ScaleX = ScaleY = 1.0f;
                }
            }
        }

        public override Rectangle GetBounds()
        {
            if (m_Scale800x600)
            {
                return new Rectangle(0, 0, 800, 600);
            }
            return base.GetBounds();
        }
    }
}
