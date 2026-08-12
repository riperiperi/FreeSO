using FSO.Client.UI.Controls;
using FSO.Client.UI.Framework;
using FSO.Common;
using FSO.Common.Utils;
using Microsoft.Xna.Framework;

namespace FSO.Client.UI.Archive
{
    internal class UIArchiveDisplayName: UIVBoxContainer
    {
        public static void ShowDisplayNameDialog(Callback<string> onResult)
        {
            UIAlert alert = null;
            alert = UIScreen.GlobalShowAlert(new UIAlertOptions()
            {
                Title = GameFacade.Strings.GetString("f128", "143"),
                Message = GameFacade.Strings.GetString("f128", "144"),
                TextEntry = true,
                TextValue = ClientArchiveConfiguration.Default.PlayerName,
                Buttons = UIAlertButton.OkCancel(
                    (btn) =>
                    {
                        if (!ClientArchiveConfiguration.ValidDisplayName(alert.ResponseText))
                        {
                            UIAlert.Alert(
                                GameFacade.Strings.GetString("f128", "82"),
                                GameFacade.Strings.GetString("f128", "83"),
                                true);

                            return;
                        }

                        onResult(alert.ResponseText);
                        UIScreen.RemoveDialog(alert);
                    },
                    (btn) => { onResult(null); UIScreen.RemoveDialog(alert); }
                    )
            }, true);
        }

        private UIHBoxContainer NameBox;
        private UILabel NameLabel;
        private UIButton EditButton;

        public event Action<string> OnChange;

        public UIArchiveDisplayName()
        {
            var gd = GameFacade.GraphicsDevice;
            var ui = Content.Content.Get().CustomUI;

            var titleStyle = TextStyle.DefaultLabel.Clone();
            titleStyle.Size = 8;

            Spacing = 0;

            Add(new UILabel()
            {
                Caption = GameFacade.Strings.GetString("f128", "145"),
                CaptionStyle = titleStyle,
            });

            NameBox = new UIHBoxContainer()
            {
                VerticalAlignment = UIContainerVerticalAlignment.Middle
            };

            NameBox.Add(NameLabel = new UILabel()
            {
                Caption = ClientArchiveConfiguration.Default.PlayerName
            });

            NameBox.Add(EditButton = new UIButton(ui.Get("archive_edit.png").Get(gd))
            {
                Tooltip = GameFacade.Strings.GetString("f128", "146")
            });

            Add(NameBox);

            AutoSize();

            EditButton.OnButtonClick += EditName;
        }

        private void EditName(Framework.UIElement button)
        {
            ShowDisplayNameDialog((newName) =>
            {
                if (newName != null)
                {
                    NameLabel.Caption = newName;
                    NameLabel.Size = Vector2.Zero;
                    AutoSize();

                    OnChange?.Invoke(newName);

                    ClientArchiveConfiguration.Default.PlayerName = newName;
                    ClientArchiveConfiguration.Default.Save();
                }
            });
        }
    }
}
