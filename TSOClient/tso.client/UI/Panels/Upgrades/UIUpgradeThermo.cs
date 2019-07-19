using FSO.Client.UI.Framework;
using FSO.Common;
using FSO.Common.Rendering.Framework.IO;
using FSO.Common.Rendering.Framework.Model;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FSO.Client.UI.Panels.Upgrades
{
    public class UIUpgradeThermo : UIElement
    {
        private float AnimFill;
        private float TargetFill;
        private float FillSpeed;

        private float HAnimFill;
        private float HTargetFill;
        private float HFillSpeed;

        public Texture2D BgSlice;
        public Texture2D FillSlice;
        public Texture2D Highlight;
        private int TotalLevels;
        private float[] HighlightIntensity;

        private UIMouseEventRef ClickHandler;
        public event Action<int> OnHoveredLevel;
        public event Action<int> OnClickedLevel;

        public int HighlightLevel;
        private bool MouseOver;

        public UIUpgradeThermo()
        {
            var ui = Content.Content.Get().CustomUI;
            var gd = GameFacade.GraphicsDevice;

            BgSlice = ui.Get("up_thermo_slice.png").Get(gd);
            FillSlice = ui.Get("up_thermo_slice_active.png").Get(gd);
            Highlight = ui.Get("up_thermo_highlight.png").Get(gd);

            ClickHandler = ListenForMouse(new Rectangle(-14, -14, (int)28, (int)28), MouseHandler);
        }

        private int GetHoveredLevel(UpdateState state)
        {
            var local = GlobalPoint(state.MouseState.Position.ToVector2());
            int level = (35 - (int)local.Y) / 70;
            level = Math.Max(0, Math.Min(TotalLevels - 1, level));
            return level;
        }

        private void MouseHandler(UIMouseEventType type, UpdateState state)
        {
            switch (type)
            {
                case UIMouseEventType.MouseOver:
                    MouseOver = true;
                    OnHoveredLevel?.Invoke(GetHoveredLevel(state));
                    break;
                case UIMouseEventType.MouseOut:
                    MouseOver = false;
                    OnHoveredLevel?.Invoke(-1);
                    break;
                case UIMouseEventType.MouseDown:
                    OnClickedLevel?.Invoke(GetHoveredLevel(state));
                    break;
            }
        }

        public override void Update(UpdateState state)
        {
            base.Update(state);
            if (Parent.Visible)
            {
                var speed = 60f / FSOEnvironment.RefreshRate;
                FillSpeed += speed * ((TargetFill - AnimFill) / 40f);
                AnimFill += FillSpeed;
                FillSpeed *= (float)Math.Pow(0.93f, speed);

                HFillSpeed += speed * ((HTargetFill - HAnimFill) / 40f);
                HAnimFill += HFillSpeed;
                HFillSpeed *= (float)Math.Pow(0.93f, speed);

                for (int i = 0; i < HighlightIntensity.Length; i++)
                {
                    if (i == HighlightLevel)
                    {
                        if (HighlightIntensity[i] < 1)
                            HighlightIntensity[i] += 3f / FSOEnvironment.RefreshRate;
                        else
                            HighlightIntensity[i] = 1;
                    } 
                else
                    {
                        if (HighlightIntensity[i] > 0)
                            HighlightIntensity[i] -= 3f / FSOEnvironment.RefreshRate;
                        else
                            HighlightIntensity[i] = 0;
                    }
                }

                if (MouseOver)
                {
                    var level = GetHoveredLevel(state);
                    if (HighlightLevel != level) OnHoveredLevel?.Invoke(level);
                }
            }
        }

        public void SetHighlightLevel(int target)
        {
            HighlightLevel = target;
            if (target == -1) HTargetFill = TargetFill;
            else
            {
                if (target == 0) HTargetFill = 0.5f + 8 / 70f;
                else HTargetFill = target + 0.5f + 6 / 70f;
            }
        }

        public void SetTotalLevels(int totalLevels)
        {
            TotalLevels = totalLevels;
            var m1 = totalLevels - 1;
            HighlightIntensity = new float[totalLevels];
            ClickHandler.Region = new Rectangle(-14, -(14 + 70 * m1), 28, 28 + 70 * m1);
        }

        public void SetTargetFill(int target)
        {
            if (target == 0) SetTargetFill(0.5f + 8 / 70f);
            else SetTargetFill(target + 0.5f + 6 / 70f);
        }

        public void SetTargetFill(float target)
        {
            TargetFill = target;
            HTargetFill = target;
        }

        private float HueOff;
        public override void Draw(UISpriteBatch batch)
        {
            HueOff += 0.25f / FSOEnvironment.RefreshRate;
            var color = Color.White * Opacity;
            _BlendColor = Color.White;
            var effect = LotView.WorldContent.SpriteEffect;
            for (int i=0; i<TotalLevels; i++)
            {
                var source = new Rectangle(0, (i == 0) ? 140 : ((i == TotalLevels - 1) ? 0 : 70), 42, 70);
                if (TotalLevels == 1) source.Y += 70;
                var target = new Vector2(-21, -(35 + 70 * i));

                DrawLocalTexture(batch, BgSlice, source, target, Vector2.One, color);
                
                batch.SetEffect(effect);

                effect.CurrentTechnique = effect.Techniques["HSVEffect"];
                effect.Parameters["Highlight"].SetValue(0f);

                var hcolor = new Color(HueOff % 1f, 1f, 1f, Opacity * 0.66f);
                if (HAnimFill > i && AnimFill < i+1 && (Math.Abs(HAnimFill - AnimFill) > 2/70f || Math.Abs(HFillSpeed) > 2/70f))
                {
                    var s = source;
                    var t = target;
                    var pct = (HAnimFill < i + 1) ? (HAnimFill % 1) : 1f;
                    s.Y += (int)((1 - pct) * 70);
                    t.Y += (int)((1 - pct) * 70);
                    s.Height = (int)Math.Ceiling(pct * 70);
                    DrawLocalTexture(batch, FillSlice, s, t, Vector2.One, hcolor);
                }
                hcolor = new Color(HueOff % 1f, 1f, 1f, Opacity);
                if (AnimFill > i)
                {
                    var s = source;
                    var t = target;
                    var pct = (AnimFill < i + 1) ? (AnimFill % 1) : 1f;
                    s.Y += (int)((1 - pct) * 70);
                    t.Y += (int)((1 - pct) * 70);
                    s.Height = (int)Math.Ceiling(pct * 70);
                    DrawLocalTexture(batch, FillSlice, s, t, Vector2.One, hcolor);
                }
                batch.SetEffect();
            }
            
            batch.SetEffect(effect);
            effect.CurrentTechnique = effect.Techniques["HSVEffect"];
            for (int i = 0; i < TotalLevels; i++)
            {
                if (HighlightIntensity[i] > 0)
                {
                    var hcolor = new Color(HueOff % 1f, 1f, 1f, Opacity * HighlightIntensity[i]);
                    DrawLocalTexture(batch, Highlight, null, new Vector2(0, 0 - i * 70), new Vector2((i == 0) ? 1.16667f : 1f), hcolor, 0, new Vector2(29));
                }
            }
            batch.SetEffect();
        }
    }
}
