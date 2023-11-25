using FSO.Client.Controllers;
using FSO.Client.UI.Controls;
using FSO.Client.UI.Framework;
using FSO.Common.DataService;
using FSO.Common.DataService.Model;
using FSO.Common.Rendering.Framework.Model;
using FSO.Common.Utils;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Ninject;
using System;

namespace FSO.Client.UI.Panels.Neighborhoods
{
    public class UIBigPersonButton : UIContainer
    {
        public Binding<Avatar> User { get; internal set; }

        private Texture2D NormalImg; //0x83E00000001, blue
        private Texture2D HoverImg; //0x83F00000001, green
        private Texture2D PressedImg; //0xCF200000001, black
        private Texture2D DisabledImg; //0xCEE00000001, gray

        private Texture2D OnlineBg;
        public UISim Sim { get; set; }
        //private UITooltipHandler m_TooltipHandler;
        public UIButton MainButton { get; set; }

        //Mixing concerns here but binding avatar id is much nicer than lots of plumbing each time
        private IClientDataService DataService;
        public event Action<uint, string> OnNameChange;

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
                OnNameChange?.Invoke(AvatarId, value);
                _AvatarName = value;
            }
        }

        public UIBigPersonButton()
        {
            NormalImg = GetTexture(0x83E00000001);
            HoverImg = GetTexture(0x83F00000001);
            PressedImg = GetTexture(0xCF200000001);
            DisabledImg = GetTexture(0xCEE00000001);
            DataService = FSOFacade.Kernel.Get<IClientDataService>();
            MainButton = new UIButton(NormalImg)
            {
                Size = new Microsoft.Xna.Framework.Vector2(114, 169)
            };
            Add(MainButton);

            User = new Binding<Avatar>()
                .WithBinding(this, "AvatarName", "Avatar_Name")
                .WithBinding(this, "Sim.Avatar.HeadOutfitId", "Avatar_Appearance.AvatarAppearance_HeadOutfitID")
                .WithBinding(this, "Sim.Avatar.BodyOutfitId", "Avatar_Appearance.AvatarAppearance_BodyOutfitID")
                .WithBinding(this, "Sim.Avatar.Appearance", "Avatar_Appearance.AvatarAppearance_SkinTone", (x) => (Vitaboy.AppearanceType)((byte)(x ?? (byte)0)));

            Sim = new UISim();
            Sim.Size = new Vector2(80, 150);
            Sim.Position = new Vector2(17, 13);
            Sim.AutoRotate = true;
            Add(Sim);

            //m_TooltipHandler = UIUtils.GiveTooltip(this);

            MainButton.OnButtonClick += _Button_OnButtonClick;
        }

        public override void Update(UpdateState state)
        {
            base.Update(state);
            MainButton.Disabled = AvatarId == 0;
            MainButton.BlendColor = Color.Transparent;
        }

        public override void Removed()
        {
            User.Dispose();
        }


        private uint _AvatarId = uint.MaxValue;
        public uint AvatarId
        {
            get { return _AvatarId; }
            set
            {
                _AvatarId = value;
                if (value == uint.MaxValue || value == 0)
                {
                    GameThread.NextUpdate((x) =>
                    {
                        User.Value = null;
                    });
                    Sim.Visible = false;
                }
                else
                {
                    DataService.Get<Avatar>(_AvatarId).ContinueWith(x =>
                    {
                        if (x.Result == null) { return; }
                        User.Value = x.Result;
                    });
                    DataService.Request(Server.DataService.Model.MaskedStruct.SimPage_Main, _AvatarId);
                    Sim.Visible = true;
                }
            }
        }

        private void _Button_OnButtonClick(UIElement button)
        {
            FindController<CoreGameScreenController>()?.ShowPersonPage(_AvatarId);
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
