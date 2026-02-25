using FSO.Client.Controllers;
using FSO.Client.UI.Controls;
using FSO.Client.UI.Framework;
using FSO.Common;
using FSO.Common.Rendering.Framework.Model;
using FSO.Server.Clients;
using Microsoft.Xna.Framework;
using Ninject;

namespace FSO.Client.UI.Archive
{
    internal class UIArchiveJoinDialog : UIDialog
    {
        public UITextBox NameInput;
        public UITextBox AddressInput;
        public UIButton JoinButton;

        private UIHBoxContainer modeBox;
        private UIRadioButton ArchiveRadio;
        private UIRadioButton ServerRadio;
        private UILabel DescriptionLabel;
        private UILabel NameLabel;
        private UILabel AddressLabel;
        private UIVBoxContainer ButtonBox;
        private UIVBoxContainer CurrentLayout;

        private bool IsServerMode => ServerRadio.Selected;

        public UIArchiveJoinDialog() : base(UIDialogStyle.Close, true)
        {
            Caption = "Join Server";

            // Mode selection
            modeBox = new UIHBoxContainer();
            modeBox.Add(ArchiveRadio = new UIRadioButton() { RadioGroup = "joinMode", Selected = true });
            modeBox.Add(new UILabel() { Caption = "Archive" });
            modeBox.Add(ServerRadio = new UIRadioButton() { RadioGroup = "joinMode" });
            modeBox.Add(new UILabel() { Caption = "Online" });
            modeBox.AutoSize();

            DescriptionLabel = new UILabel()
            {
                Caption = "Join a server hosted by another player by using their IP address or URL. Depending on the server settings, you might need an admin to verify you, so use a display name that makes it clear who you are.",
                Size = new Vector2(300, 100),
                Wrapped = true
            };

            NameLabel = new UILabel() { Caption = "Display name:" };
            NameInput = new UITextBox() { Size = new Vector2(150, 25) };
            AddressLabel = new UILabel() { Caption = "Server address:" };
            AddressInput = new UITextBox() { Size = new Vector2(300, 25) };

            ButtonBox = new UIVBoxContainer() { HorizontalAlignment = UIContainerHorizontalAlignment.Right };
            ButtonBox.Add(JoinButton = new UIButton() { Caption = "Join", Disabled = true });
            ButtonBox.AutoSize();

            NameInput.CurrentText = ClientArchiveConfiguration.Default.PlayerName;

            NameInput.OnChange += ValidateInputs;
            AddressInput.OnChange += ValidateInputs;
            JoinButton.OnButtonClick += Submit;
            CloseButton.OnButtonClick += Close;
            ArchiveRadio.OnButtonClick += ModeChanged;
            ServerRadio.OnButtonClick += ModeChanged;

            BuildLayout();
            ValidateInputs(NameInput);
        }

        private void BuildLayout()
        {
            if (CurrentLayout != null)
                Remove(CurrentLayout);

            bool server = IsServerMode;

            CurrentLayout = new UIVBoxContainer();
            CurrentLayout.Add(modeBox);

            if (!server)
            {
                CurrentLayout.Add(DescriptionLabel);
                CurrentLayout.Add(NameLabel);
                CurrentLayout.Add(NameInput);
            }

            CurrentLayout.Add(AddressLabel);
            CurrentLayout.Add(AddressInput);
            CurrentLayout.Add(ButtonBox);

            CurrentLayout.AutoSize();
            CurrentLayout.Position = new Vector2(20, 45);
            SetSize((int)CurrentLayout.Size.X + 40, (int)CurrentLayout.Size.Y + 70);
            Add(CurrentLayout);

            JoinButton.Caption = server ? "Connect" : "Join";
            AddressInput.CurrentText = server
                ? (GlobalSettings.Default.GameEntryUrl ?? "")
                : ClientArchiveConfiguration.Default.LastJoinedHost;
        }

        private void ModeChanged(UIElement button)
        {
            BuildLayout();
            ValidateInputs(AddressInput);
        }

        private void ValidateInputs(UIElement element)
        {
            if (IsServerMode)
                JoinButton.Disabled = AddressInput.CurrentText.Length == 0;
            else
                JoinButton.Disabled = NameInput.CurrentText.Length == 0 || AddressInput.CurrentText.Length == 0;
        }

        private void SaveArchiveConfig()
        {
            var clientConfig = ClientArchiveConfiguration.Default;

            clientConfig.PlayerName = NameInput.CurrentText;
            clientConfig.LastJoinedHost = AddressInput.CurrentText;
            clientConfig.Save();
        }

        private void Close(UIElement button)
        {
            if (!IsServerMode)
                SaveArchiveConfig();

            FindController<ConnectArchiveController>().SwitchMode(ConnectArchiveMode.Landing);
        }

        private void Submit(UIElement button)
        {
            if (IsServerMode)
            {
                var url = AddressInput.CurrentText;
                GlobalSettings.Default.GameEntryUrl = url;
                GlobalSettings.Default.CitySelectorUrl = url;
                GlobalSettings.Default.Save();

                var kernel = FSOFacade.Kernel;
                kernel.Get<AuthClient>().SetBaseUrl(url);
                kernel.Get<CityClient>().SetBaseUrl(url);

                UIScreen.RemoveDialog(this);
                FSOFacade.Controller.ShowServerLogin();
            }
            else
            {
                SaveArchiveConfig();
                FSOFacade.Controller.ConnectToArchive(NameInput.CurrentText, AddressInput.CurrentText, false);
            }
        }

        public override void Update(UpdateState state)
        {
            base.Update(state);
            FindController<ConnectArchiveController>().TickRPC();
        }
    }
}
