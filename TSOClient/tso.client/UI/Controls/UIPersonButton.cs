using FSO.Client.UI.Framework;
using FSO.Client.Utils;
using FSO.Common.DataService.Model;
using FSO.Common.Utils;
using Microsoft.Xna.Framework;
using FSO.Client.Model;
using FSO.Content.Model;
using FSO.Client.Controllers;
using FSO.Common.DataService;
using Ninject;

namespace FSO.Client.UI.Controls
{
    /// <summary>
    /// TODO: This and UIPersonIcon are two of the same deal... but we have different desires for each.
    /// This must:
    /// - Open person page when clicked
    /// - Access data service to show icons for people not on lot
    /// VM variant must:
    /// - Center avatar when clicked
    /// - Autogenerate and show icons for pets and some NPCs (via world, TODO)
    /// - Icons for NPCs at all!
    /// - Follow-center when right-clicked.
    /// 
    /// Right now UIVMPersonButton is running the show for all VM related panels.
    /// </summary>
    public class UIPersonButton : UIContainer
    {
        public Binding<UserReference> User { get; internal set; }
        private UITooltipHandler m_TooltipHandler;
        private ITextureRef _Icon;
        private UIButton _Button;
        private UIPersonButtonSize _Size;

        //Mixing concerns here but binding avatar id is much nicer than lots of plumbing each time
        private IClientDataService DataService;

        public UIPersonButton()
        {
            DataService = FSOFacade.Kernel.Get<IClientDataService>();

            User = new Binding<UserReference>()
                .WithBinding(this, "Tooltip", "Name")
                .WithBinding(this, "Icon", "Icon")
                .WithMultiBinding(x => { Invalidate(); }, "Icon", "Name");

            m_TooltipHandler = UIUtils.GiveTooltip(this);

            _Button = new UIButton();
            _Button.OnButtonClick += _Button_OnButtonClick;
            Add(_Button);
        }

        public override void Removed()
        {
            User.Dispose();
        }

        private uint _AvatarId;
        public uint AvatarId
        {
            get { return _AvatarId; }
            set
            {
                _AvatarId = value;
                if (value == uint.MaxValue)
                {
                    GameThread.NextUpdate((x) =>
                    {
                        User.Value = UserReference.Of(Common.Enum.UserReferenceType.MOMI);
                    });
                }
                else
                {
                    DataService.Get<Avatar>(_AvatarId).ContinueWith(x =>
                    {
                        if (x.Result == null) { return; }
                        (User.Value as AvatarUserReference)?.Dispose();
                        User.Value = UserReference.Wrap(x.Result);
                    });
                    DataService.Request(Server.DataService.Model.MaskedStruct.SimPage_Main, _AvatarId);
                }
            }
        }

        private void _Button_OnButtonClick(UIElement button)
        {
            FindController<CoreGameScreenController>()?.ShowPersonPage(User.Value);
        }

        private ITextureRef _FrameTexture;
        public UIPersonButtonSize FrameSize
        {
            get { return _Size; }
            set
            {
                _Size = value;

                if(_Size == UIPersonButtonSize.SMALL)
                {
                    _Button.Texture = FSO.Content.Content.Get().UIGraphics.Get(2564095475713).Get(GameFacade.GraphicsDevice);
                }
                else
                {
                    _Button.Texture = FSO.Content.Content.Get().UIGraphics.Get(2551210573825).Get(GameFacade.GraphicsDevice);
                }
            }
        }

        public ITextureRef Icon
        {
            get { return _Icon; }
            set
            {
                _Icon = value;
            }
        }

        public override Rectangle GetBounds()
        {
            return _Button.GetBounds();
        }

        public override void Draw(UISpriteBatch batch)
        {
            base.Draw(batch);

            if(_Icon != null)
            {
                var texture = _Icon.Get(batch.GraphicsDevice);

                if (_Size == UIPersonButtonSize.SMALL)
                {
                    var scale = new Vector2(16.0f / texture.Width, 16.0f / texture.Height);
                    DrawLocalTexture(batch, texture, null, new Vector2(2, 2), scale);
                }else if(_Size == UIPersonButtonSize.LARGE)
                {
                    var scale = new Vector2(30.0f / texture.Width, 30.0f / texture.Height);
                    DrawLocalTexture(batch, texture, null, new Vector2(2, 2), scale);
                }
            }
        }
    }

    public enum UIPersonButtonSize
    {
        SMALL,
        LARGE
    }

    public enum UIPersonButtonStyle
    {
        Default,
        Friend,
        Enemy,
        Roommate,
        NPC
    }
}
