using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using FSO.Client.UI.Framework;
using FSO.Client.UI.Model;
using FSO.Common.Rendering.Framework.Model;
using FSO.Common.Rendering.Framework.IO;
using FSO.Client.Utils;
using FSO.Common.Utils;
using FSO.Client.UI.Panels;

namespace FSO.Client.UI.Controls
{
    /// <summary>
    /// Used to display an interaction. Will eventually have all features like the timer, big huge red x support for cancel etc.
    /// </summary>
    public class UIInteraction : UIContainer
    {
        public Texture2D Icon;
        private UITooltipHandler m_TooltipHandler;
        private Texture2D Background;
        private Texture2D Overlay;
        private bool Active;
        private UIMouseEventRef ClickHandler;
        public event ButtonClickDelegate OnMouseEvent;
        public delegate void InteractionResultDelegate(UIElement me, bool accepted);
        public event InteractionResultDelegate OnInteractionResult;
        public UIIQTrackEntry ParentEntry;

        public UIButton AcceptButton;
        public UIButton DeclineButton;

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

        /*
          actionfaceselection = 0x1B200000001,
          actionhappy = 0x1B300000001,
          actionmad = 0x1B400000001,
          actionneutral = 0x1B500000001,
          actionsad = 0x1B600000001,
          */

        public void UpdateInteractionResult(sbyte result)
        {
            if (result == -1) return;
            //button widths are both 19. icon width is 40
            //accept at x=0, decline at x = 40-19
            if (AcceptButton == null)
            {
                HIT.HITVM.Get().PlaySoundEvent(UISounds.CallQueueFull);
                AcceptButton = new UIButton();
                AcceptButton.Texture = GetTexture(0x1B300000001);
                AcceptButton.Y = 40 - 24;
                Add(AcceptButton);
                var tween = GameFacade.Screens.Tween.To(AcceptButton, 0.33f, new Dictionary<string, float>()
                    {
                        {"Y", 44.0f },
                    }, TweenQuad.EaseOut);
                AcceptButton.OnButtonClick += (btn) => { OnInteractionResult?.Invoke(this, true); };

                DeclineButton = new UIButton();
                DeclineButton.Texture = GetTexture(0x1B600000001);
                DeclineButton.Y = 45 - 24;
                DeclineButton.X = 45 - 19;
                Add(DeclineButton);
                var tween2 = GameFacade.Screens.Tween.To(DeclineButton, 0.33f, new Dictionary<string, float>()
                    {
                        {"Y", 44.0f },
                    }, TweenQuad.EaseOut);
                DeclineButton.OnButtonClick += (btn) => { OnInteractionResult?.Invoke(this, false); };
            }

            if (result == 0)
            {
                AcceptButton.Disabled = false;
                DeclineButton.Disabled = false;
                AcceptButton.Selected = false;
                DeclineButton.Selected = false;
                AcceptButton.Opacity = 1f;
                DeclineButton.Opacity = 1f;
            }
            else if (result == 1)
            {
                AcceptButton.Disabled = true;
                DeclineButton.Disabled = false;
                AcceptButton.Selected = false;
                DeclineButton.Selected = true;
                AcceptButton.Opacity = 0.5f;
            }
            else if (result == 2)
            {
                AcceptButton.Disabled = false;
                DeclineButton.Disabled = true;
                AcceptButton.Selected = true;
                DeclineButton.Selected = false;
                DeclineButton.Opacity = 0.5f;
            }
            else
            {
                AcceptButton.Disabled = true;
                DeclineButton.Disabled = true;
            }
        }

        private void MouseEvt(UIMouseEventType type, UpdateState state)
        {
            if (type == UIMouseEventType.MouseDown) OnMouseEvent(this); //pass to parents to handle
        }

        public override void Draw(UISpriteBatch batch)
        {
            base.Draw(batch);
            DrawLocalTexture(batch, Background, new Vector2(0, 0));
            if (Icon != null)
            {
                if (Icon.Width <= 45) {
                    DrawLocalTexture(batch, Icon, new Rectangle(0, 0, Icon.Width, Icon.Height), new Vector2(4, 4), new Vector2(37f/Icon.Width, 37f/Icon.Height));
                }
                else DrawLocalTexture(batch, Icon, new Rectangle(0, 0, Icon.Width / 2, Icon.Height), new Vector2(4, 4));
            }
            if (Overlay != null) DrawLocalTexture(batch, Overlay, new Vector2(0, 0));
        }

        public override Rectangle GetBounds()
        {
            return new Rectangle(0, 0, ClickHandler.Region.Width, ClickHandler.Region.Height); 
        }
    }
}
