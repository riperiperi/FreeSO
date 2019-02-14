using FSO.Client.UI.Controls;
using FSO.Client.UI.Framework;
using FSO.Client.Utils;
using FSO.Common.Rendering.Framework.IO;
using FSO.Common.Rendering.Framework.Model;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FSO.Client.UI.Panels.Neighborhoods
{
    public class UIAbstractStickyContainer : UICachedContainer
    {
        public Texture2D ShadowImg;
        public Texture2D BackgroundImg;
        public UIMouseEventRef MouseEvent;
        public Vector2 ContainerSize = new Vector2(600, 600);
        public bool IgnoreMouse;

        public Action<UIAbstractStickyContainer> OnClick;
        private float Highlight = 0f;
        public Color HSVMod = new Color(0, 255, 255, 255); //hue, saturation, value, alpha

        public UIAbstractStickyContainer(string texName)
        {
            var ui = Content.Content.Get().CustomUI;
            var gd = GameFacade.GraphicsDevice;

            ShadowImg = ui.Get(texName + "_shad.png").Get(gd);
            BackgroundImg = ui.Get(texName + "_bg.png").Get(gd);

            Size = new Vector2(ShadowImg.Width, ShadowImg.Height);
            MouseEvent = ListenForMouse(new Rectangle(ShadowImg.Width/8, 0, (int)(ShadowImg.Width * 0.75f), ShadowImg.Height), MouseEvt);
            InternalBefore = true;
        }

        protected override void CalculateOpacity()
        {
            base.CalculateOpacity();
        }

        public override Rectangle GetBounds()
        {
            return new Rectangle(ShadowImg.Width / 8, 0, (int)(ShadowImg.Width * 0.75f), ShadowImg.Height);
        }

        private void MouseEvt(UIMouseEventType type, UpdateState state)
        {
            if (Opacity > 0 && type == UIMouseEventType.MouseDown)
            {
                OnClick?.Invoke(this);
            }

            if (type == UIMouseEventType.MouseOver)
            {
                Highlight = 0.1f;
                Invalidate();
            }
            else if (type == UIMouseEventType.MouseOut)
            {
                Highlight = 0;
                Invalidate();
            }
            /*
            if (type == UIMouseEventType.MouseDown && OnMouseEvent != null) OnMouseEvent(this); //pass to parents to handle
            if (type == UIMouseEventType.MouseOver) SetHover(true);
            if (type == UIMouseEventType.MouseOut) SetHover(false);
            */
        }

        public override void InternalDraw(UISpriteBatch batch)
        {
            var effect = LotView.WorldContent.SpriteEffect;
            batch.SetEffect(effect);

            effect.CurrentTechnique = effect.Techniques["HSVEffect"];
            effect.Parameters["Highlight"].SetValue(Highlight);
            _BlendColor = Color.White;
            //Convert the opacity percentage into a byte (0-255)
            _BlendColor.A = (byte)Math.Round(Opacity * 255);
            DrawLocalTexture(batch, BackgroundImg, null, new Vector2(), Vector2.One, HSVMod);
            CalculateOpacity();
            batch.SetEffect();
            effect.Parameters["Highlight"].SetValue(0f);
        }

        public override void Update(UpdateState state)
        {
            var oldVisible = Visible;
            if (IgnoreMouse || Opacity == 0) Visible = false;
            base.Update(state);
            if (IgnoreMouse || Opacity == 0) Visible = oldVisible;
        }

        public override void Draw(UISpriteBatch batch)
        {
            if (!Visible || Opacity == 0) return;

            DrawLocalTexture(batch, ShadowImg, null, Vector2.Zero, Vector2.One, Color.Black * 0.5f);
            if (Target != null)
            {
                var effect = LotView.WorldContent.SpriteEffect;
                batch.SetEffect(effect);

                effect.CurrentTechnique = effect.Techniques["StickyEffect"];
                var off = ((Position + Size * Scale / 2) - ContainerSize / 2).X * -0.001f * (Size.Y/240f);
                effect.Parameters["stickyOffset"].SetValue(off);
                effect.Parameters["stickyPersp"].SetValue((Size.Y/240f) * -0.2f);

                DrawLocalTexture(batch, Target, 
                    new Rectangle((int)(-100*ScaleX), 0, (int)(Target.Width + 200 * ScaleX), Target.Height), 
                    - (BackOffset.ToVector2() + new Vector2(100, 0)), new Vector2(1 / (Scale.X), 1 / (Scale.Y)));
                batch.SetEffect();
            }
            DynamicOverlay.GetChildren().ForEach(x =>
            {
                if (x.Opacity != Opacity) x.Opacity = Opacity;
            });
            DynamicOverlay.Draw(batch);
        }
    }
}
