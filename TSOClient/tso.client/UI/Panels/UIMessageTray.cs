using FSO.Client.Controllers;
using FSO.Client.Model;
using FSO.Client.UI.Controls;
using FSO.Client.UI.Framework;
using FSO.Client.UI.Framework.Parser;
using FSO.Client.Utils;
using FSO.Common.Utils;
using FSO.Content.Model;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;
using FSO.Common.Rendering.Framework.Model;

namespace FSO.Client.UI.Panels
{
    public class UIMessageTray : UIContainer
    {
        private List<UIMessageIcon> _Items = new List<UIMessageIcon>();

        public UIMessageTray()
        {
        }

        public void SetItems(List<Message> messages)
        {
            foreach (var item in _Items) { this.Remove(item); }

            var y = 0;

            foreach(var message in messages){
                var ui = new UIMessageIcon(message);
                ui.Y = y;
                _Items.Add(ui);
                this.Add(ui);
                y += 45;
            }
        }
    }
    

    public class UIMessageIcon : UIContainer
    {
        public UIButton button;
        public Texture2D BackgroundImageCall { get; set; }
        public Texture2D BackgroundImageLetter { get; set; }
        public Texture2D AvatarThumbOverlay { get; set; }
        public Texture2D EAIconImage { get; set; }
        public Texture2D MaxisIconImage { get; set; }
        public Texture2D MOMIIconImage { get; set; }
        public Texture2D TSOIconImage { get; set; }
        
        private UIMessageIconThumbnail AvatarThumbnail;
        private Binding<UserReference> User;

        private UITooltipHandler m_TooltipHandler;
        private Message Message;

        private uint Tick;

        public UIMessageIcon(Message message)
        {
            Message = message;

            var script = this.RenderScript("messageicon.uis");
            button = new UIButton((message.Type == MessageType.Call) ? BackgroundImageCall : BackgroundImageLetter);
            button.ImageStates = 3;
            button.OnButtonClick += Button_OnButtonClick;
            this.Add(button);

            AvatarThumbnail = script.Create<UIMessageIconThumbnail>("AvatarThumbnail");
            AvatarThumbnail.Button = button;
            AvatarThumbnail.FrameTexture = AvatarThumbOverlay;

            User = new Binding<UserReference>()
                .WithBinding(this, "Icon", "Icon")
                .WithBinding(this, "Tooltip", "Name");

            User.Value = message.User;

            this.Add(AvatarThumbnail);

            m_TooltipHandler = UIUtils.GiveTooltip(this);
        }

        public override void Update(UpdateState state)
        {
            base.Update(state);
            button.Selected = !Message.Read && (Tick > 30);
            if (Tick++ >= 60) Tick -= 60;
        }

        private void Button_OnButtonClick(UIElement button)
        {
            FindController<MessagingController>().ToggleWindow(this.Message);
        }

        private ITextureRef _Icon;
        public ITextureRef Icon {
            get { return _Icon; }
            set { _Icon = value; AvatarThumbnail.Texture = _Icon; }
        }

        private ulong _AvatarAppearance_HeadOutfitID;
        public ulong AvatarAppearance_HeadOutfitID
        {
            get { return _AvatarAppearance_HeadOutfitID; }
            set
            {
                _AvatarAppearance_HeadOutfitID = value;
            }
        }

        public override Rectangle GetBounds()
        {
            return button.GetBounds();
        }
    }

    class UIMessageIconThumbnail : UIElement
    {
        public UIMessageIconThumbnail()
        {
        }

        public UIButton Button;

        private Texture2D _FrameTexture;
        public Texture2D FrameTexture
        {
            get { return _FrameTexture; }
            set
            {
                _FrameTexture = value;
            }
        }

        private ITextureRef _Texture;
        public ITextureRef Texture
        {
            get { return _Texture; }
            set
            {
                _Texture = value;
            }
        }

        private Vector2 _FrameSize;
        [UIAttribute("frameSize")]
        public Vector2 FrameSize
        {
            get { return _FrameSize; }
            set { _FrameSize = value; }
        }

        private Vector2 _ThumbSize;
        [UIAttribute("thumbSize")]
        public Vector2 ThumbSize
        {
            get { return _ThumbSize; }
            set { _ThumbSize = value; }
        }

        private Vector2 _Offset;
        [UIAttribute("offset")]
        public Vector2 Offset
        {
            get { return _Offset; }
            set { _Offset = value; }
        }

        public override void Draw(UISpriteBatch batch)
        {
            var frame = Button.CurrentFrameIndex;
            var frameWidth = _FrameTexture.Width / 3.0f;

            if (_Texture != null)
            {
                var texture = _Texture.Get(batch.GraphicsDevice);

                var thumbScale = new Vector2(_ThumbSize.X / texture.Width, _ThumbSize.Y / texture.Height);
                DrawLocalTexture(batch, texture, new Rectangle(0, 0, texture.Width, texture.Height), _Offset, thumbScale);
            }

            DrawLocalTexture(batch, _FrameTexture, new Rectangle((int)(frame * frameWidth), 0, (int)frameWidth, _FrameTexture.Height), Vector2.Zero);
        }
    }
}
