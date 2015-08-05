/*
This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
If a copy of the MPL was not distributed with this file, You can obtain one at
http://mozilla.org/MPL/2.0/.
*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using FSO.Client.UI.Framework;
using FSO.Client.UI.Model;
using FSO.Common.Rendering.Framework.IO;
using FSO.Common.Rendering.Framework.Model;

namespace FSO.Client.UI.Controls
{
    public class UIClickableLabel : UILabel
    {
        private UIMouseEventRef ClickHandler;
        public event ButtonClickDelegate OnButtonClick;

        public UIClickableLabel()
        {
            ClickHandler =
                ListenForMouse(new Rectangle(0, 0, 10, 10), new UIMouseEvent(OnMouseEvent));
        }


        public override Vector2 Size
        {
            get
            {
                return base.Size;
            }
            set
            {
                base.Size = value;
                ClickHandler.Region = new Rectangle(0, 0, (int)value.X, (int)value.Y);
            }
        }



        //private bool m_isOver;
        //todo - use m_isOver to show diff colour for hovering over labels. 
        private bool m_isDown;

        private void OnMouseEvent(UIMouseEventType type, UpdateState state)
        {
            switch (type)
            {
                case UIMouseEventType.MouseOver:
                    //m_isOver = true;
                    break;

                case UIMouseEventType.MouseOut:
                    //m_isOver = false;
                    break;

                case UIMouseEventType.MouseDown:
                    m_isDown = true;
                    break;

                case UIMouseEventType.MouseUp:
                    if (m_isDown)
                    {
                        if (OnButtonClick != null)
                        {
                            OnButtonClick(this);
                            //GameFacade.SoundManager.PlayUISound(1);
                        }
                    }
                    m_isDown = false;
                    break;
            }
        }

    }
}
