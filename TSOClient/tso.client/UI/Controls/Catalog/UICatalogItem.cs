/*
This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
If a copy of the MPL was not distributed with this file, You can obtain one at
http://mozilla.org/MPL/2.0/.
*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using FSO.Client.UI.Framework;
using FSO.Common.Rendering.Framework.IO;
using Microsoft.Xna.Framework.Graphics;
using FSO.Client.Utils;
using FSO.Client.UI.Controls;
using FSO.Common.Utils;
using Microsoft.Xna.Framework;
using FSO.Common.Rendering.Framework.Model;

namespace FSO.Client.UI.Controls.Catalog
{
    public class UICatalogItem : UIElement
    {
        public Texture2D Icon;
        private UITooltipHandler m_TooltipHandler;
        private bool Active;
        private Texture2D Background;
        private UIMouseEventRef ClickHandler;
        public event ButtonClickDelegate OnMouseEvent;
        public UICatalog ParentCatalog;
        public UICatalogElement Info;
        public int Index;

        public void SetActive(bool active)
        {
            this.Active = active;
            if (active) Background = TextureGenerator.GetCatalogActive(GameFacade.GraphicsDevice);
            else Background = TextureGenerator.GetCatalogInactive(GameFacade.GraphicsDevice);
        }

        public UICatalogItem(bool Active)
        {
            SetActive(Active);
            m_TooltipHandler = UIUtils.GiveTooltip(this);
            ClickHandler = ListenForMouse(new Rectangle(0, 0, 45, 45), new UIMouseEvent(MouseEvt));
        }

        private void MouseEvt(UIMouseEventType type, UpdateState state)
        {
            if (type == UIMouseEventType.MouseDown && OnMouseEvent != null) OnMouseEvent(this); //pass to parents to handle
        }

        public override void Draw(UISpriteBatch batch)
        {
            DrawLocalTexture(batch, Background, new Vector2(0, 0));
            if (Icon != null)
            {
                if (Icon.Height > 48) //poor mans way of saying "special icon" eg floors
                {
                    float scale = 37.0f / Math.Max(Icon.Height, Icon.Width);
                    DrawLocalTexture(batch, Icon, new Rectangle(0, 0, Icon.Width, Icon.Height), new Vector2(2+((37-Icon.Width*scale)/2), 2 + ((37 - Icon.Height * scale) / 2)), new Vector2(scale, scale));
                }
                else
                    DrawLocalTexture(batch, Icon, new Rectangle(0, 0, Icon.Width / 2, Icon.Height), new Vector2(2, 2));
            }
        }

        public override Rectangle GetBounds()
        {
            return new Rectangle(0, 0, ClickHandler.Region.Width, ClickHandler.Region.Height);
        }
    }
}
