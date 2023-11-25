using FSO.Client.UI.Framework;
using FSO.Client.UI.Panels.Neighborhoods;
using FSO.Client.Utils;
using FSO.Common.Rendering.Framework.IO;
using FSO.Common.Rendering.Framework.Model;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;

namespace FSO.Client.UI.Controls
{
    public class UIRatingDisplay : UIElement
    {
        public int StarStride;

        public Texture2D StarTexture;

        private float _DisplayStars;
        public float DisplayStars
        {
            get
            {
                return _DisplayStars;
            }
            set
            {
                Tooltip = GameFacade.Strings.GetString("f115", "27", new string[] { value.ToString() });
                _DisplayStars = value;
            }
        }
        
        private int HoverHalfStars;
        public bool Settable;

        public Color StarColor = new Color(255, 247, 153);
        public Color InsetColor = Color.Black * 0.33f;

        public event Action<int> OnStarChange;

        public uint LinkAvatar = 0;

        private UIMouseEventRef ClickHandler;
        private UITooltipHandler m_TooltipHandler;
        private bool IsOver;

        public int HalfStars
        {
            get
            {
                return (int)(DisplayStars * 2);
            }
            set
            {
                DisplayStars = value / 2f;
            }
        }

        public UIRatingDisplay(bool big)
        {
            var ui = Content.Content.Get().CustomUI;
            var gd = GameFacade.GraphicsDevice;
            if (big)
            {
                StarTexture = ui.Get("neighp_rate_star.png").Get(gd);
                StarStride = 12;
            }
            else
            {
                StarTexture = ui.Get("neighp_mayor_star.png").Get(gd);
                InsetColor = new Color(0, 51, 102);
                StarStride = 9;
            }
            InitCommon();
        }

        public override Rectangle GetBounds()
        {
            return new Rectangle(0, 0, StarStride * 5, StarTexture.Height);
        }

        public UIRatingDisplay(Texture2D tex)
        {
            StarTexture = tex;
            StarStride = tex.Width;
            InitCommon();
        }

        private void InitCommon()
        {
            ClickHandler =
                ListenForMouse(GetBounds(), new UIMouseEvent(OnMouseEvent));

            m_TooltipHandler = UIUtils.GiveTooltip(this); //buttons can have tooltips
        }

        private void OnMouseEvent(UIMouseEventType type, UpdateState state)
        {
            switch (type)
            {
                case UIMouseEventType.MouseOver:
                    IsOver = true;
                    break;

                case UIMouseEventType.MouseOut:
                    IsOver = false;
                    break;

                case UIMouseEventType.MouseDown:
                    if (LinkAvatar > 0)
                    {
                        var ratingList = new UIRatingList(LinkAvatar);
                        UIScreen.GlobalShowAlert(new UIAlertOptions()
                        {
                            Title = GameFacade.Strings.GetString("f118", "23", new string[] { "Retrieving..." }),
                            Message = GameFacade.Strings.GetString("f118", "24", new string[] { "Retrieving..." }),
                            GenericAddition = ratingList,
                            Width = 530
                        }, true);
                    }
                    if (Settable)
                    {
                        HalfStars = HoverHalfStars;
                        OnStarChange?.Invoke(HalfStars);
                        Invalidate();
                    }
                    break;

                case UIMouseEventType.MouseUp:
                    break;
            }
        }

        public override void Update(UpdateState state)
        {
            if (IsOver)
            {
                if (Settable)
                {
                    HoverHalfStars = Math.Min(10, Math.Max(0, (int)Math.Round((GlobalPoint(state.MouseState.Position.ToVector2()).X / StarStride) * 2)));
                    Invalidate();
                }
            } else
            {
                HoverHalfStars = 0;
            }
            base.Update(state);
        }

        public override void Draw(UISpriteBatch batch)
        {
            if (!Visible) return;
            for (int i=0; i<5; i++)
            {
                if (i <= DisplayStars - 1f)
                {
                    //star completely set
                    DrawLocalTexture(batch, StarTexture, null, new Vector2(StarStride*i, 0), Vector2.One, StarColor);
                }
                else
                {
                    //star at least partially unset
                    DrawLocalTexture(batch, StarTexture, null, new Vector2(StarStride * i, 0), Vector2.One, InsetColor);
                    if (i < DisplayStars)
                    {
                        //this star is partially set
                        DrawLocalTexture(batch, StarTexture, new Rectangle(0, 0, (int)(StarTexture.Width*(DisplayStars%1)), StarTexture.Height), 
                            new Vector2(StarStride * i, 0), Vector2.One, StarColor);
                    }
                }
            }

            var hoverHalf = HoverHalfStars;
            if (LinkAvatar != 0 && IsOver) hoverHalf = 10;
            if (hoverHalf > 0)
            {
                var hoverStars = hoverHalf / 2f;
                for (int i = 0; i < hoverStars; i++)
                {
                    if (i <= hoverStars - 1f)
                    {
                        //star completely set
                        DrawLocalTexture(batch, StarTexture, null, new Vector2(StarStride * i, 0), Vector2.One, Color.White * 0.66f);
                    }
                    else
                    {
                        //this star is partially set
                        DrawLocalTexture(batch, StarTexture, new Rectangle(0, 0, (int)(StarTexture.Width * (hoverStars % 1)), StarTexture.Height),
                            new Vector2(StarStride * i, 0), Vector2.One, Color.White*0.66f);
                    }
                }
            }
        }
    }
}
