using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TSOClient.Code.UI.Framework;
using TSO.Common.rendering.framework.io;
using Microsoft.Xna.Framework.Graphics;
using TSOClient.Code.Utils;
using TSOClient.LUI;
using tso.common.utils;
using Microsoft.Xna.Framework;
using TSO.Common.rendering.framework.model;

namespace TSOClient.Code.UI.Controls.Catalog
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
            if (Icon != null) DrawLocalTexture(batch, Icon, new Rectangle(0, 0, Icon.Width / 2, Icon.Height), new Vector2(2, 2));
        }

        public override Rectangle GetBounds()
        {
            return new Rectangle(0, 0, ClickHandler.Region.Width, ClickHandler.Region.Height);
        }
    }
}
