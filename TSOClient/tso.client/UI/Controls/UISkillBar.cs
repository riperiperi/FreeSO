using FSO.Client.UI.Framework;
using FSO.Client.UI.Framework.Parser;
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

namespace FSO.Client.UI.Controls
{
    public class UISkillBar : UIElement
    {
        public static Texture2D LockedTex;
        public static Texture2D LockTex;
        public static Texture2D[] SkillLevelTex;

        public delegate void SkillLockChoiceDelegate(int level);
        public event SkillLockChoiceDelegate OnSkillLock;

        public bool DisableLock;
        public int SkillLevel;
        public int LockLevel;
        public int HoverLevel;

        public int FreeLocks;
        public int SkillID;

        private int m_Width;
        private int m_Height;

        public float Width
        {
            get { return m_Width; }
            set
            {
                m_Width = (int)value;
                if (ClickHandler != null)
                {
                    ClickHandler.Region.Width = (int)value;
                }
            }
        }

        public float Height
        {
            get { return m_Height; }
            set
            {
                m_Height = (int)value;
                if (ClickHandler != null)
                {
                    ClickHandler.Region.Width = (int)value;
                }
            }
        }

        [UIAttribute("size")]
        public override Vector2 Size
        {
            get
            {
                return new Vector2(m_Width, m_Height);
            }
            set
            {
                Width = value.X;
                Height = value.Y;
            }
        }

        private UIMouseEventRef ClickHandler;
        private UITooltipHandler m_TooltipHandler;
        private bool m_isOver;

        public UISkillBar()
        {
            if (SkillLevelTex == null)
            {
                SkillLevelTex = new Texture2D[9];
                for (int i=0; i<9; i++)
                {
                    SkillLevelTex[i] = GetTexture(0x00000CB600000001 + (ulong)i *0x100000000);
                }
            }

            ClickHandler =
                ListenForMouse(new Rectangle(0, 0, m_Width, m_Height), new UIMouseEvent(OnMouseEvent));

            m_TooltipHandler = UIUtils.GiveTooltip(this); //buttons can have tooltips
        }

        public override Rectangle GetBounds()
        {
            return new Rectangle(new Point(), Size.ToPoint());
        }

        public override void Update(UpdateState state)
        {
            base.Update(state);
            if (m_isOver && !DisableLock)
            {
                HoverLevel = (int)Math.Round(GlobalPoint(state.MouseState.Position.ToVector2()).X/6);
                if (HoverLevel > SkillLevel / 100) HoverLevel = SkillLevel / 100;
                if (HoverLevel > LockLevel+FreeLocks) HoverLevel = LockLevel + FreeLocks;
            } else
            {
                HoverLevel = 0;
            }

            if (Visible) {
                var nameStr = GameFacade.Strings.GetString("189", (23 + SkillID).ToString());
                Tooltip = nameStr.Substring(0, nameStr.Length - 7)+(SkillLevel / 100f).ToString("0.00");
            }

            ClickHandler.Region.Width = m_Width;
            ClickHandler.Region.Height = m_Height;
        }

        private void OnMouseEvent(UIMouseEventType type, UpdateState state)
        {
            switch (type)
            {
                case UIMouseEventType.MouseOver:
                    m_isOver = true;
                    break;

                case UIMouseEventType.MouseOut:
                    m_isOver = false;
                    break;

                case UIMouseEventType.MouseDown:
                    if (!DisableLock)
                    {
                        LockLevel = HoverLevel;
                        OnSkillLock?.Invoke(HoverLevel);
                    }
                    break;

                case UIMouseEventType.MouseUp:
                    break;
            }
        }

        public override void Draw(UISpriteBatch batch)
        {
            if (!Visible) return;
            for (int i=0; i<20; i++)
            {
                var tex = SkillLevelTex[Math.Min((i * 8) / 20, 8)];

                DrawLocalTexture(batch, tex, new Rectangle(18, 0, 6, 18), new Vector2(i * 6, 0), new Vector2(1));
                Rectangle src;
                if (i < LockLevel)
                {
                    src = new Rectangle(6, 0, 6, 18);
                }
                else if (i < HoverLevel)
                {
                    src = new Rectangle(12, 0, 6, 18);
                }
                else if (i < (SkillLevel+99)/100)
                {
                    src = new Rectangle(0, 0, 6, 18);
                }
                else
                {
                    continue;
                }

                if (i >= SkillLevel / 100) src.Width = ((SkillLevel%100) * 6) / 100;
                DrawLocalTexture(batch, tex, src, new Vector2(i * 6, 0), new Vector2(1));
            }
        }

    }
}
