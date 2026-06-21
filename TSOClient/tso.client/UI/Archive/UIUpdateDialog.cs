using FSO.Client.Controllers;
using FSO.Client.UI.Controls;
using FSO.Common;
using FSO.Files.FSO;
using FSO.Server.Clients;
using Microsoft.Xna.Framework;

namespace FSO.Client.UI.Archive
{
    internal class UIUpdateDialog : UIDialog
    {
        public UITextEdit ChangelogTextEdit;
        private UIButton NoButton;
        private UIButton YesButton;

        private readonly UpdatePathNew Path;

        public UIUpdateDialog(UpdatePathNew path, bool autoUpdate) : base(UIDialogStyle.Close, true)
        {
            Path = path;
            var current = FSOVersionInfo.Current;

            Caption = GameFacade.Strings.GetString("f101", autoUpdate ? "55" : "21");

            var vbox = new UIVBoxContainer()
            {
                HorizontalAlignment = UIContainerHorizontalAlignment.Center
            };

            var targetVersion = path.Destination;

            vbox.Add(new UILabel()
            {
                Caption = GameFacade.Strings.GetString("f101", autoUpdate ? "61" : "41", [targetVersion.id]), //43 for downgrade.
                Size = new Vector2(400, 45),
                Wrapped = true
            });

            var changelogBox = new UIHBoxContainer()
            {
                VerticalAlignment = UIContainerVerticalAlignment.Middle
            };

            changelogBox.Add(ChangelogTextEdit = new UITextEdit()
            {
                BackgroundTextureReference = UITextBox.StandardBackground,
                ScrollbarGutter = 7,
                TextMargin = new Rectangle(12, 10, 12, 10),
                ScrollbarImage = GetTexture(0x4AB00000001),
                Size = new Vector2(400, 300),
                CurrentText = BuildChangelog(path),
                Mode = UITextEditMode.ReadOnly
            });
            ChangelogTextEdit.InitDefaultSlider();

            vbox.Add(changelogBox);

            var buttonBox = new UIHBoxContainer()
            {
                VerticalAlignment = UIContainerVerticalAlignment.Middle
            };

            buttonBox.Add(NoButton = new UIButton()
            {
                Caption = GameFacade.Strings.GetString("f101", autoUpdate ? "35" : "44")
            });

            buttonBox.Add(YesButton = new UIButton()
            {
                Caption = GameFacade.Strings.GetString("f101", "36", [targetVersion?.id ?? "unknown"])
            });

            vbox.Add(buttonBox);
            vbox.AutoSize();
            vbox.Position = new Vector2(20, 40);

            SetSize((int)vbox.Size.X + 40, (int)vbox.Size.Y + 60);
            DynamicOverlay.Add(vbox);

            YesButton.OnButtonClick += Accept;
            NoButton.OnButtonClick += Reject;
            CloseButton.OnButtonClick += Reject;
        }

        private void Reject(Framework.UIElement button)
        {
            FindController<UpdateController>().RejectUpdate();
        }

        private void Accept(Framework.UIElement button)
        {
            FindController<UpdateController>().AcceptUpdate(Path);
        }

        private string BuildChangelog(UpdatePathNew path)
        {
            return string.Join(
                '\n',
                path.Path.Reverse<FSOUpdateMetadata>().Select((x, index) => 
                    $"# {x.id} {GameFacade.Strings.GetString("f101", path.FullZipStart && index == path.Path.Count - 1 ? "24" : "23")} \n{x.changelog}")
            );
        }
    }
}
