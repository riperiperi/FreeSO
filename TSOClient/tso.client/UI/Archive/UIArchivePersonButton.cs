using FSO.Client.UI.Controls;
using FSO.Client.UI.Framework;
using FSO.Common.Rendering.Framework.Model;
using FSO.Server.Protocol.Electron.Packets;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace FSO.Client.UI.Panels.Neighborhoods
{
    public class UIArchivePersonButton : UIContainer
    {
        private Texture2D NormalImg; //0x83E00000001, blue
        private Texture2D HoverImg; //0x83F00000001, green
        private Texture2D PressedImg; //0xCF200000001, black
        private Texture2D DisabledImg; //0xCEE00000001, gray

        private Texture2D OnlineBg;
        public UISim Sim { get; set; }
        //private UITooltipHandler m_TooltipHandler;
        public UIButton MainButton { get; set; }

        private string _AvatarName = "";
        public string AvatarName
        {
            get
            {
                return _AvatarName;
            }
            set
            {
                MainButton.Tooltip = value;
                _AvatarName = value;
            }
        }

        public UIArchivePersonButton()
        {
            NormalImg = GetTexture(0x83E00000001);
            HoverImg = GetTexture(0x83F00000001);
            PressedImg = GetTexture(0xCF200000001);
            DisabledImg = GetTexture(0xCEE00000001);
            MainButton = new UIButton(NormalImg)
            {
                Size = new Microsoft.Xna.Framework.Vector2(114, 169)
            };
            Add(MainButton);

            Sim = new UISim();
            Sim.Size = new Vector2(80, 150);
            Sim.Position = new Vector2(17, 13);
            Sim.AutoRotate = true;
            Sim.Visible = false;
            Add(Sim);

            //m_TooltipHandler = UIUtils.GiveTooltip(this);

            MainButton.OnButtonClick += _Button_OnButtonClick;
        }

        public void SetSim(ArchiveAvatar? ava)
        {
            if (ava == null)
            {
                Sim.Visible = false;
            }
            else
            {
                AvatarName = ava.Value.Name;
                Sim.Avatar.HeadOutfitId = ava.Value.Head;
                Sim.Avatar.BodyOutfitId = ava.Value.Body;
                Sim.Avatar.Appearance = (Vitaboy.AppearanceType)ava.Value.Type;

                Sim.Visible = true;
            }
        }

        public override void Update(UpdateState state)
        {
            base.Update(state);
            MainButton.Disabled = !Sim.Visible;
            MainButton.BlendColor = Color.Transparent;
        }

        private void _Button_OnButtonClick(UIElement button)
        {

        }

        public override void Draw(UISpriteBatch batch)
        {
            if (!Visible) return;
            //draw relevant button graphic
            var frame = MainButton.CurrentFrame;
            if (MainButton.Disabled) frame = 3;
            switch (frame)
            {
                case 0:
                    DrawLocalTexture(batch, NormalImg, Vector2.Zero);
                    break;
                case 1:
                    DrawLocalTexture(batch, PressedImg, Vector2.Zero);
                    break;
                case 2:
                    DrawLocalTexture(batch, HoverImg, Vector2.Zero);
                    break;
                case 3:
                    DrawLocalTexture(batch, DisabledImg, Vector2.Zero);
                    break;
            }
            base.Draw(batch);
        }
    }
}
