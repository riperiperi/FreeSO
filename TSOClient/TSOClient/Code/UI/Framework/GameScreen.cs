/*The contents of this file are subject to the Mozilla Public License Version 1.1
(the "License"); you may not use this file except in compliance with the
License. You may obtain a copy of the License at http://www.mozilla.org/MPL/

Software distributed under the License is distributed on an "AS IS" basis,
WITHOUT WARRANTY OF ANY KIND, either express or implied. See the License for
the specific language governing rights and limitations under the License.

The Original Code is the TSOClient.

The Initial Developer of the Original Code is
ddfczm. All Rights Reserved.

Contributor(s): ______________________________________.
*/

using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Graphics;

namespace TSOClient.Code.UI.Framework
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
