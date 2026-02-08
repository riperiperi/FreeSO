using FSO.Client.Controllers;
using FSO.Client.UI.Controls;
using FSO.Client.UI.Framework;
using FSO.Client.Utils;
using FSO.Common;
using FSO.Common.Rendering.Framework.Model;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace FSO.Client.UI.Archive
{
    internal class UIArchiveLandingDialog : UIDialog
    {
        public UIButton CreateButton;
        public UIButton JoinButton;
        public UIButton QuickStartButton;
        public Texture2D FreeSOLogoImage;
        public Texture2D HostServerButtonImage;
        public Texture2D JoinServerButtonImage;
        public Texture2D QuickStartButtonImage;

        public TextStyle LargeButtonTextStyle;
        public UILabel CreateButtonText;
        public UILabel JoinButtonText;
        public UILabel QuickStartButtonText;

        public UIImage FreeSOLogo;

        public UIArchiveLandingDialog() : base(UIDialogStyle.Standard, true)
        {
            var ui = Content.Content.Get().CustomUI;

            SetSize(496, 293);

            LargeButtonTextStyle = TextStyle.DefaultLabel.Clone();

            LargeButtonTextStyle.Size = 17;
            LargeButtonTextStyle.Shadow = true;
            LargeButtonTextStyle.Color = Color.White;

            FreeSOLogoImage = ui.Get("archive_logo_1x.png").Get(GameFacade.GraphicsDevice);
            QuickStartButtonImage = ui.Get("archive_quickstartbtn.png").Get(GameFacade.GraphicsDevice);
            HostServerButtonImage = ui.Get("archive_hostbtn.png").Get(GameFacade.GraphicsDevice);
            JoinServerButtonImage = ui.Get("archive_joinbtn.png").Get(GameFacade.GraphicsDevice);

            int margin = 2;

            FreeSOLogo = new UIImage(FreeSOLogoImage)
            {
                Position = new Vector2((Width - FreeSOLogoImage.Width) / 2, -31)
            };

            DynamicOverlay.Add(FreeSOLogo);

            QuickStartButton = new UIButton(QuickStartButtonImage)
            {
                Position = new Vector2((Width - QuickStartButtonImage.Width / 4) / 2, Height - 36),
                Size = new Vector2(QuickStartButtonImage.Width / 4, QuickStartButtonImage.Height),
                CaptionStyle = LargeButtonTextStyle,
                Caption = "Quick Start"
            };

            DynamicOverlay.Add(QuickStartButton);

            Add(CreateButton = new UIButton(HostServerButtonImage)
            {
                Position = new Vector2(Width / 2 - (HostServerButtonImage.Width / 4 + margin), 80)
            });

            Add(JoinButton = new UIButton(JoinServerButtonImage)
            {
                Position = new Vector2(Width / 2 + margin, 80)
            });

            Add(CreateButtonText = new UILabel()
            {
                Position = new Vector2(CreateButton.X + HostServerButtonImage.Width / 8, CreateButton.Y + 10),
                Size = new Vector2(0, 1),
                Alignment = Framework.TextAlignment.Center | Framework.TextAlignment.Top,
                CaptionStyle = LargeButtonTextStyle,
                Caption = "Host Server"
            });

            Add(JoinButtonText = new UILabel()
            {
                Position = new Vector2(JoinButton.X + JoinServerButtonImage.Width / 8, JoinButton.Y + 10),
                Size = new Vector2(0, 1),
                Alignment = Framework.TextAlignment.Center | Framework.TextAlignment.Top,
                CaptionStyle = LargeButtonTextStyle,
                Caption = "Join Server"
            });

            Add(new UILabel()
            {
                Caption = "Host or join a server to get started.",
                Position = new Vector2(Width / 2, 59),
                Alignment = Framework.TextAlignment.Center | Framework.TextAlignment.Top,
                Size = new Vector2(0, 1)
            });

            CreateButton.OnButtonClick += Create;
            JoinButton.OnButtonClick += Join;
            QuickStartButton.OnButtonClick += QuickStart;

            QuickStartButton.Tooltip = "Quick Start will begin a singleplayer session of the last used archive data.";
        }

        private void QuickStart(Framework.UIElement button)
        {
            Visible = false;

            var config = FSOFacade.Controller.GetServerConfig();

            if (config == null)
            {
                var factory = new ArchiveServerFactory(
                    ArchiveServerFactory.GetQuickStartConfig(),
                    FindController<ConnectArchiveController>());

                factory.Start((success) =>
                {
                    if (!success)
                    {
                        Visible = true;
                    }
                });
            }
            else
            {
                FSOFacade.Controller.ConnectToArchive(ClientArchiveConfiguration.Default.PlayerName, $"127.0.0.1:{config.CityPort}", true);
            }
        }

        private void Join(Framework.UIElement button)
        {
            FindController<ConnectArchiveController>().SwitchMode(ConnectArchiveMode.Join);
        }

        private void Create(Framework.UIElement button)
        {
            FindController<ConnectArchiveController>().SwitchMode(ConnectArchiveMode.Create);
        }

        public override void Update(UpdateState state)
        {
            base.Update(state);
            FindController<ConnectArchiveController>().TickRPC();
        }
    }
}
