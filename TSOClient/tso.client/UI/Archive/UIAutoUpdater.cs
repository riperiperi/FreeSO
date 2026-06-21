using FSO.Client.Controllers;
using FSO.Client.UI.Controls;
using FSO.Client.UI.Framework;
using FSO.Common;
using FSO.Common.Utils;
using FSO.Server.Clients;
using Microsoft.Xna.Framework;

namespace FSO.Client.UI.Archive
{
    internal class UIAutoUpdater : UIContainer
    {
        private UIButton InfoButton;
        private UILabel InfoLabel;
        private UIHBoxContainer RootBox;
        private FSOVersionInfo TargetVersion;
        private UpdatePathNew Path;
        private bool Failed = false;

        public UIAutoUpdater()
        {
            var ui = Content.Content.Get().CustomUI;
            var btnTex = ui.Get("chat_cat.png").Get(GameFacade.GraphicsDevice);

            var updateStatusCaption = TextStyle.DefaultLabel.Clone();
            updateStatusCaption.Size = 9;

            var btnCaption = TextStyle.DefaultLabel.Clone();
            btnCaption.Size = 8;
            btnCaption.Shadow = true;

            RootBox = new UIHBoxContainer()
            {
                VerticalAlignment = UIContainerVerticalAlignment.Middle,
                Spacing = 2
            };

            RootBox.Add(InfoButton = new UIButton(btnTex)
            {
                Caption = "i",
                Width = btnTex.Height,
                CaptionStyle = btnCaption
            });

            RootBox.Add(InfoLabel = new UILabel()
            {
                Caption = GameFacade.Strings.GetString("f101", "62"),
                CaptionStyle = updateStatusCaption,
            });

            RootBox.AutoSize();
            RootBox.Position = new Vector2(0, -RootBox.Size.Y);
            ScaleX = ScaleY = 0.75f;

            InfoButton.OnButtonClick += Info;

            Add(RootBox);

            FetchUpdate();
        }

        private void Info(UIElement button)
        {
            var current = FSOVersionInfo.Current;
            if (string.IsNullOrEmpty(current.channelUrl))
            {
                UIAlert.Alert(
                    GameFacade.Strings.GetString("f101", "72"),
                    GameFacade.Strings.GetString("f101", "73"),
                    true
                    );
            }
            else if (Failed)
            {
                UIAlert.Alert(
                    GameFacade.Strings.GetString("f101", "55"),
                    GameFacade.Strings.GetString("f101", "64", [
                        string.IsNullOrEmpty(current.channelUrl) ? "(no update source)" : current.channelUrl
                        ]),
                    true
                    );
            }
            else
            {
                if (TargetVersion != null && Path != null)
                {
                    ShowUpdate(TargetVersion, Path);
                }
                else
                {
                    UIAlert.Alert(
                        GameFacade.Strings.GetString("f101", "55"),
                        GameFacade.Strings.GetString("f101", "65", [current.channel, current.id]),
                        true
                        );
                }
            }
        }

        private void ShowUpdate(FSOVersionInfo targetVersion, UpdatePathNew path)
        {
            var controller = new UpdateController(skip =>
            {
                // If the update was rejected here, it was ignored and the dialog shouldn't pop up again.
                GlobalSettings.Default.IgnoreVersion = targetVersion.id;
                GlobalSettings.Default.Save();
            });

            controller.ShowUpdateDialogNew(path, true);
        }

        private void SetLabel(string label, Color color)
        {
            InfoLabel.Size = Vector2.Zero;
            InfoLabel.Caption = label;
            InfoLabel.CaptionStyle.Color = color;

            RootBox.AutoSize();
        }

        private void FetchUpdate()
        {
            var current = FSOVersionInfo.Current;
            if (string.IsNullOrEmpty(current.channelUrl))
            {
                SetLabel(GameFacade.Strings.GetString("f101", "72"), TextStyle.DefaultLabel.Color);
                return;
            }

            UpdateController.TryGetAutoUpdate((bool success, FSOVersionInfo targetVersion, UpdatePathNew path) =>
            {
                GameThread.InUpdate(() =>
                {
                    var myScreen = this.FindParent<UIScreen>();

                    // A bit of a hack - if we're not on the active screen anymore, then we shouldn't do anything if update info comes back.
                    if (myScreen == null || myScreen != UIScreen.Current)
                    {
                        return;
                    }

                    Path = path;
                    if (targetVersion != null)
                    {
                        SetLabel(GameFacade.Strings.GetString("f101", "60", [targetVersion.id]), Color.White);
                        TargetVersion = targetVersion;

                        // If the user hasn't ignored this update, show the update dialog.
                        if (success && targetVersion.id != GlobalSettings.Default.IgnoreVersion)
                        {
                            ShowUpdate(targetVersion, path);
                        }

                        Failed = !success;
                    }
                    else
                    {
                        SetLabel(GameFacade.Strings.GetString("f101", success ? "59" : "63"), success ? TextStyle.DefaultLabel.Color : Color.LightGray);
                        TargetVersion = null;
                        Failed = !success;
                    }
                });
            });
        }
    }
}
