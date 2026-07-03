using FSO.Client.Regulators;
using FSO.Client.UI.Archive;
using FSO.Client.UI.Archive.Management;
using FSO.Client.UI.Controls;
using FSO.Client.UI.Framework;
using FSO.Client.UI.Model;
using FSO.Client.UI.Screens;
using FSO.Common;
using FSO.Common.DatabaseService.Model;
using FSO.Common.Utils;
using FSO.HIT;
using FSO.Server.Embedded;
using FSO.Server.Protocol.CitySelector;
using FSO.Server.Protocol.Electron.Packets;
using FSO.Server.Protocol.Utils;
using FSO.UI.Model;

namespace FSO.Client.Controllers
{
    public enum ConnectArchiveMode
    {
        Landing,
        Create,
        Join,
        JoinRPC
    }

    public class ConnectArchiveController : IDisposable
    {
        private TransitionScreen View;
        private CityConnectionRegulator CityConnectionRegulator;
        private Callback onConnect;
        private Callback onError;
        private UIElement Dialog;
        public LoadAvatarByIDResponse AvatarData;

        private ConnectArchiveMode LastMode;
        private ArchiveAvatarSelectCode LastSelectCode = ArchiveAvatarSelectCode.Success;

        public ShardSelectorServletRequest Shard => CityConnectionRegulator.CurrentShard;

        public ConnectArchiveController(TransitionScreen view,
                                     CityConnectionRegulator cityConnectionRegulator)
        {
            this.View = view;
            this.CityConnectionRegulator = cityConnectionRegulator;
            this.CityConnectionRegulator.OnTransition += CityConnectionRegulator_OnTransition;
            this.CityConnectionRegulator.OnError += CityConnectionRegulator_OnError;

            View.ShowProgress = true;
            View.SetProgress(0, 4);

            View.ShowSandboxMode();
        }

        private void EnsureDisplayName(Action action)
        {
            var clientConfig = ClientArchiveConfiguration.Default;

            if (!ClientArchiveConfiguration.ValidDisplayName(clientConfig.PlayerName))
            {
                ShowMainDialog(null);

                UIArchiveDisplayName.ShowDisplayNameDialog((string newName) =>
                {
                    if (newName == null)
                    {
                        SwitchMode(ConnectArchiveMode.Landing);
                    }
                    else
                    {
                        ClientArchiveConfiguration.Default.PlayerName = newName;
                        ClientArchiveConfiguration.Default.Save();

                        action();
                    }
                });
            }
            else
            {
                action();
            }
        }

        public void SwitchMode(ConnectArchiveMode mode)
        {
            LastMode = mode;
            bool sandboxVisible = false;

            switch (mode)
            {
                case ConnectArchiveMode.Join:
                    EnsureDisplayName(() =>
                    {
                        ShowMainDialog(new UIArchiveJoinDialog());
                    });
                    break;
                case ConnectArchiveMode.JoinRPC:
                    EnsureDisplayName(() =>
                    {
                        ShowMainDialog(new UIArchiveJoinRPCDialog());
                    });
                    break;
                case ConnectArchiveMode.Landing:
                    sandboxVisible = true;
                    ShowMainDialog(new UIArchiveLandingDialog());
                    break;
                case ConnectArchiveMode.Create:
                    if (FSOFacade.Controller.HasServer())
                    {
                        ExistingServerDialog();
                    }
                    else
                    {
                        EnsureDisplayName(() =>
                        {
                            ShowMainDialog(new UIArchiveCreateServer());
                        });
                    }
                    break;
            }

            View.SetSandboxVisibility(sandboxVisible);
        }

        private void ExistingServerDialog()
        {
            var config = FSOFacade.Controller.GetServerConfig();

            UIAlert alert = null;
            alert = UIScreen.GlobalShowAlert(new UIAlertOptions()
            {
                Message = GameFacade.Strings.GetString("f128", "96"),
                Width = 400,
                Buttons = [
                    new UIAlertButton(UIAlertButtonType.Yes, (btn =>
                    {
                        // User management
                        UIScreen.RemoveDialog(alert);
                        UIScreen.GlobalShowDialog(new UIArchiveUserManageDialog(new ArchiveManagement(config)), true);
                    }), GameFacade.Strings.GetString("f128", "97")),
                    new UIAlertButton(UIAlertButtonType.No, (btn =>
                    {
                        // Close server
                        UIScreen.RemoveDialog(alert);
                        FSOFacade.Controller.CloseServer(() =>
                        {
                            ShowMainDialog(new UIArchiveCreateServer());
                        });
                    }), GameFacade.Strings.GetString("f128", "98")),
                    new UIAlertButton(UIAlertButtonType.Cancel, (btn =>
                    {
                        // Join server
                        UIScreen.RemoveDialog(alert);
                        ShowMainDialog(null);
                        FSOFacade.Controller.ConnectToArchive(ClientArchiveConfiguration.Default.PlayerName, $"127.0.0.1:{config.CityPort}", true);
                    }), GameFacade.Strings.GetString("f128", "99")),
                ],
            }, true);
        }

        public void ReturnToSAS(Callback onConnect, Callback onError)
        {
            this.onConnect = onConnect;
            this.onError = onError;

            if (!CityConnectionRegulator.ReturnToSASArchive())
            {
                // If we can't do this, re-initialize archive mode.
                Initialize();
            }
        }

        public void Connect(string displayName, string address, bool selfHost, Callback onConnect, Callback onError)
        {
            this.onConnect = onConnect;
            this.onError = onError;

            address = PortTransformer.DefaultCityPort(address);

            CityConnectionRegulator.ConnectArchive(new ConnectArchiveRequest
            {
                CityAddress = address,
                DisplayName = displayName,
                SelfHost = selfHost
            });
        }

        public void SetCallbacks(Callback onConnect, Callback onError)
        {
            this.onConnect = onConnect;
            this.onError = onError;
        }

        public void SkipAvatarSelection(uint avatarId)
        {
            CityConnectionRegulator.CurrentShard.AvatarID = avatarId.ToString();
            CityConnectionRegulator.AsyncTransition("AskForAvatarData");
        }

        public void SelectAvatar(uint avatarId, uint lotId = 0)
        {
            if (lotId != 0)
            {
                FSOFacade.Controller.SetArchiveLot(lotId);
            }

            CityConnectionRegulator.AsyncProcessMessage(new ArchiveAvatarSelectRequest()
            {
                AvatarId = avatarId
            });
        }

        public void CreateServer(ArchiveConfiguration config)
        {
            View.SetSandboxVisibility(false);

            var embedded = new EmbeddedServer(config);

            embedded.Start();

            FSOFacade.Controller.RegisterServer(embedded);

            ShowMainDialog(new UIArchiveServerStatusDialog(true, embedded, () =>
            {
                ShowMainDialog(null);
                FSOFacade.Controller.ConnectToArchive(ClientArchiveConfiguration.Default.PlayerName, $"127.0.0.1:{config.CityPort}", true);
            }));
        }

        private void CityConnectionRegulator_OnError(object data)
        {
            onError();
        }

        private void ShowMainDialog(UIElement dialog)
        {
            // if there's currently a dialog, dispose of it
            if (Dialog != null)
            {
                UIScreen.RemoveDialog(Dialog);
            }

            Dialog = dialog;

            if (dialog != null)
            {
                UIScreen.ShowDialog(dialog, false);
            }
        }

        public void Initialize()
        {
            GameFacade.Cursor.SetCursor(Common.Rendering.Framework.CursorType.Normal);

            HITVM.Get().PlaySoundEvent(UIMusic.None);
            GlobalSettings.Default.Save();

            SwitchMode(LastMode);
            View.SetProgressArchive(0, "Awaiting user input");
        }

        public void TickRPC()
        {
            var rpc = DiscordRpcEngine.Secret;

            if (rpc != null)
            {
                if (rpc.Value.ArchiveMode)
                {
                    if (!string.IsNullOrEmpty(rpc.Value.ServerHostname))
                    {
                        SwitchMode(ConnectArchiveMode.JoinRPC);
                    }
                }
                else
                {
                    UIAlert.Alert("", GameFacade.Strings.GetString("f128", "114"), true);
                    DiscordRpcEngine.Secret = null;
                }
            }
        }

        private void CityConnectionRegulator_OnTransition(string state, object data)
        {
            GameThread.NextUpdate((x) =>
            {
                switch (state)
                {
                    case "Disconnected":
                        Initialize();
                        break;
                    case "ArchiveConnect":
                        //4  ^Starting engines^                 # City is Selected...
                        LastSelectCode = ArchiveAvatarSelectCode.Success;
                        View.SetSandboxVisibility(false);
                        ShowMainDialog(null);
                        View.SetProgress((1.0f / 14.0f) * 100, 4);
                        break;
                    case "OpenSocket":
                        //7	  ^Sterilizing TCP/IP sockets^       # Connecting to City...
                        View.SetProgress((4.0f / 14.0f) * 100, 7);
                        break;
                    case "RequestClientSessionArchive":
                        View.SetProgressArchive((4.5f / 14.0f) * 100, "Performing handshake with Archive Server");
                        break;
                    case "PartiallyConnected":
                        View.SetProgressArchive((5.0f / 14.0f) * 100, "Connected, awaiting avatar selection");

                        // Show the avatar selection UI.
                        // Need to force resize due to showing a screen as a dialog.
                        var select = new ArchivePersonSelection() { ScaleX = 1, ScaleY = 1 };
                        select.GameResized();

                        ShowMainDialog(select);

                        if (LastSelectCode != ArchiveAvatarSelectCode.Success)
                        {
                            select.ShowSelectionError(LastSelectCode);
                            LastSelectCode = ArchiveAvatarSelectCode.Success;
                        }

                        break;
                    case "ArchiveSelectAvatar":
                        View.SetProgressArchive((5.5f / 14.0f) * 100, "Selecting avatar");
                        ShowMainDialog(null);
                        break;

                    case "ArchiveSelectedAvatar":
                        if (data is ArchiveAvatarSelectResponse sel)
                        {
                            LastSelectCode = sel.Code;
                        }
                        break;

                    case "AskForAvatarData":
                        //9  ^Reticulating spleens^             # Asking for Avatar data from DB...
                        View.SetProgress((6.0f / 14.0f) * 100, 9);
                        break;
                    case "ReceivedAvatarData":
                        //10 ^Spleens Reticulated^              # Received Avatar data from DB...

                        var dbResponse = (LoadAvatarByIDResponse)data;
                        if (dbResponse != null)
                        {
                            AvatarData = dbResponse;
                        }

                        View.SetProgress((7.0f / 14.0f) * 100, 10);
                        break;
                    case "AskForCharacterData":
                        //11 ^Purging psychographic metrics^    # Asking for Character data from DB...
                        View.SetProgress((8.0f / 14.0f) * 100, 11);
                        break;

                    case "ReceivedCharacterData":
                        //12 ^Metrics Purged^                   # Received Character data from DB...
                        View.SetProgress((9.0f / 14.0f) * 100, 12);
                        break;

                    case "AskForCityData":
                        View.SetProgress((10.0f / 14.0f) * 100, 8, "f100");
                        break;

                    case "ReceivedCityData":
                        View.SetProgress((13.0f / 14.0f) * 100, 9, "f100");
                        break;

                    case "Connected":
                        onConnect();
                        break;
                }
            });
        }

        public void Dispose()
        {
            CityConnectionRegulator.OnTransition -= CityConnectionRegulator_OnTransition;
            CityConnectionRegulator.OnError -= CityConnectionRegulator_OnError;
        }
    }
}
