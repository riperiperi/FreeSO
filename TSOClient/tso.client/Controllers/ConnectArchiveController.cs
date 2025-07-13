using FSO.Client.Regulators;
using FSO.Client.UI.Archive;
using FSO.Client.UI.Controls;
using FSO.Client.UI.Framework;
using FSO.Client.UI.Model;
using FSO.Client.UI.Screens;
using FSO.Common;
using FSO.Common.DatabaseService.Model;
using FSO.Common.Utils;
using FSO.HIT;
using FSO.Server.Embedded;
using FSO.Server.Protocol.Electron.Packets;
using System;

namespace FSO.Client.Controllers
{
    public enum ConnectArchiveMode
    {
        Landing,
        Create,
        Join
    }

    public class ConnectArchiveController : IDisposable
    {
        private TransitionScreen View;
        private CityConnectionRegulator CityConnectionRegulator;
        private Callback onConnect;
        private Callback onError;
        private UIDialog Dialog;
        public LoadAvatarByIDResponse AvatarData;

        private ConnectArchiveMode LastMode;

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

        public void SwitchMode(ConnectArchiveMode mode)
        {
            LastMode = mode;
            bool sandboxVisible = false;

            switch (mode)
            {
                case ConnectArchiveMode.Join:
                    ShowMainDialog(new UIArchiveJoinDialog());
                    break;
                case ConnectArchiveMode.Landing:
                    sandboxVisible = true;
                    ShowMainDialog(new UIArchiveLandingDialog());
                    break;
                case ConnectArchiveMode.Create:
                    ShowMainDialog(new UIArchiveCreateServer());
                    break;
            }

            View.SetSandboxVisibility(sandboxVisible);
        }

        public void Connect(string displayName, string address, bool selfHost, Callback onConnect, Callback onError)
        {
            this.onConnect = onConnect;
            this.onError = onError;

            if (address.IndexOf(":") == -1)
            {
                address = address + ":33101";
            }

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

        public void SelectAvatar(uint avatarId)
        {
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
                // TODO: passthru display name, ports
                ShowMainDialog(null);
                FSOFacade.Controller.ConnectToArchive("riperiperi", "127.0.0.1", true);
            }));
        }

        private void CityConnectionRegulator_OnError(object data)
        {
            onError();
        }

        private void ShowMainDialog(UIDialog dialog)
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
                        ShowMainDialog(new UIArchiveCharacterSelector());

                        break;
                    case "ArchiveSelectAvatar":
                        View.SetProgressArchive((5.5f / 14.0f) * 100, "Selecting avatar");
                        ShowMainDialog(null);
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
