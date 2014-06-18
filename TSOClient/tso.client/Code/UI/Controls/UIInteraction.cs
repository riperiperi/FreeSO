/*The contents of this file are subject to the Mozilla Public License Version 1.1
(the "License"); you may not use this file except in compliance with the
License. You may obtain a copy of the License at http://www.mozilla.org/MPL/

Software distributed under the License is distributed on an "AS IS" basis,
WITHOUT WARRANTY OF ANY KIND, either express or implied. See the License for
the specific language governing rights and limitations under the License.

The Original Code is the TSOClient.

The Initial Developer of the Original Code is
RHY3756547. All Rights Reserved.

Contributor(s): ______________________________________.
*/


using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using TSOClient.Code.UI.Framework;
using TSOClient.Code.UI.Framework.Parser;
using TSOClient.Code.UI.Model;
using TSO.Common.rendering.framework.model;
using TSO.Common.rendering.framework.io;
using TSOClient.Code.Utils;
using TSO.Simantics;
using TSOClient.LUI;
using tso.common.utils;

namespace TSOClient.Code.UI.Controls
{
    /// <summary>
    /// Used to display an interaction. Will eventually have all features like the timer, big huge red x support for cancel etc.
    /// </summary>
    public class UIInteraction : UIElement
    {
        public Texture2D Icon;
        private UITooltipHandler m_TooltipHandler;
        private Texture2D Background;
        private Texture2D Overlay;
        private bool Active;
        private UIMouseEventRef ClickHandler;
        public event ButtonClickDelegate OnMouseEvent;
        public UIIQTrackEntry ParentEntry;

        public void SetCancelled()
        {
            Overlay = GetTexture((ulong)0x000003A100000001);
        }

        public void SetActive(bool active)
        {
            this.Active = active;
            if (active) Background = TextureGenerator.GetInteractionActive(GameFacade.GraphicsDevice);
            else Background = TextureGenerator.GetInteractionInactive(GameFacade.GraphicsDevice);
        }

        public UIInteraction(bool Active)
        {
            SetActive(Active);
            m_TooltipHandler = UIUtils.GiveTooltip(this);
            ClickHandler = ListenForMouse(new Rectangle(0, 0, 45, 45), new UIMouseEvent(MouseEvt));
        }

        private void MouseEvt(UIMouseEventType type, UpdateState state)
        {
            if (type == UIMouseEventType.MouseDown) OnMouseEvent(this); //pass to parents to handle
        }

        public override void Draw(UISpriteBatch batch)
        {
            DrawLocalTexture(batch, Background, new Vector2(0, 0));
            if (Icon != null) DrawLocalTexture(batch, Icon, new Rectangle(0, 0, Icon.Width/2, Icon.Height), new Vector2(4, 4));
            if (Overlay != null) DrawLocalTexture(batch, Overlay, new Vector2(0, 0));
        }

        public override Rectangle GetBounds()
        {
            return new Rectangle(0, 0, ClickHandler.Region.Width, ClickHandler.Region.Height); 
        }
    }
}
