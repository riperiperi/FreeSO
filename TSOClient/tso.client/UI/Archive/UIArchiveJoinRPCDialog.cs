using FSO.Client.Controllers;
using FSO.Client.UI.Controls;
using FSO.Client.UI.Framework;
using FSO.Common;
using FSO.Common.Rendering.Framework.Model;
using FSO.Common.Utils;
using FSO.Server.Clients;
using FSO.UI.Model;
using Microsoft.Xna.Framework;

namespace FSO.Client.UI.Archive
{
    internal class UIArchiveJoinRPCDialog : UIDialog
    {
        private readonly UILabel JoinLabel;
        private readonly UIVBoxContainer VBox;

        public UIArchiveJoinRPCDialog() : base(UIDialogStyle.Close, true)
        {
            Caption = GameFacade.Strings.GetString("f128", "117");
            var vbox = new UIVBoxContainer() { HorizontalAlignment = UIContainerHorizontalAlignment.Right };
            VBox = vbox;

            var clientConfig = ClientArchiveConfiguration.Default;

            vbox.Add(JoinLabel = new UILabel()
            {
                Caption = GameFacade.Strings.GetString("f128", "118"),
                Size = new Vector2(300, 35),
                Wrapped = true
            });

            vbox.AutoSize();
            vbox.Position = new Vector2(20, 45);

            SetSize((int)vbox.Size.X + 40, (int)vbox.Size.Y + 70);

            Add(vbox);
            CloseButton.OnButtonClick += Close;

            CheckStatusAndJoin();
        }

        private void CheckStatusAndJoin()
        {
            var rpc = DiscordRpcEngine.Secret;
            var hostname = rpc.Value.ServerHostname;

            if (rpc.HasValue)
            {
                Task<StatusCheckResult> task;

                if (rpc.Value.ArchiveMode)
                {
                    task = StatusChecker.ArchiveStatus(FSOFacade.Kernel, hostname);
                }
                else
                {
                    task = StatusChecker.FreeSOStatus(hostname);
                }

                task.ContinueWith(x =>
                {
                    GameThread.InUpdate(() =>
                    {
                        // If we're not active anymore, don't go through with the join.

                        var myScreen = this.FindParent<UIScreen>();
                        if (myScreen == null || myScreen != UIScreen.Current)
                        {
                            return;
                        }

                        if (x.IsFaulted || x.IsCanceled || !x.Result.IsOnline)
                        {
                            JoinLabel.Caption = GameFacade.Strings.GetString("f128", "152");
                            JoinLabel.Size = new Vector2(300, 60);

                            VBox.AutoSize();
                            SetSize((int)VBox.Size.X + 40, (int)VBox.Size.Y + 70);
                        }
                        else
                        {
                            var historyItem = new ClientArchiveHistoryItem(
                                rpc.Value.ArchiveMode ? ClientArchiveHistoryType.DiscordArchive : ClientArchiveHistoryType.FreeSO,
                                x.Result.Name,
                                hostname,
                                0);

                            ClientArchiveConfiguration.Default.RegisterJoin(historyItem);

                            if (rpc.Value.ArchiveMode)
                            {
                                FSOFacade.Controller.ConnectToArchive(ClientArchiveConfiguration.Default.PlayerName, rpc.Value.ServerHostname, false);
                            }
                            else
                            {
                                FSOFacade.Controller.ShowServerLogin(rpc.Value.ServerHostname);
                            }
                        }
                    });
                });
            }
        }

        public override void Update(UpdateState state)
        {
            base.Update(state);

            var rpc = DiscordRpcEngine.Secret;

            if (rpc == null || !rpc.Value.ArchiveMode || string.IsNullOrEmpty(rpc.Value.ServerHostname))
            {
                FindController<ConnectArchiveController>().SwitchMode(ConnectArchiveMode.Landing);
            }
        }

        private void Close(Framework.UIElement button)
        {
            DiscordRpcEngine.Secret = null;
            FindController<ConnectArchiveController>().SwitchMode(ConnectArchiveMode.Landing);
        }
    }
}
