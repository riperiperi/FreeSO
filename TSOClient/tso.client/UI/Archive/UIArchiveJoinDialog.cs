using FSO.Client.Controllers;
using FSO.Client.UI.Archive.Management;
using FSO.Client.UI.Controls;
using FSO.Client.UI.Framework;
using FSO.Client.UI.Panels;
using FSO.Common;
using FSO.Common.Rendering.Framework.IO;
using FSO.Common.Rendering.Framework.Model;
using FSO.Common.Utils;
using FSO.Server.Clients;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace FSO.Client.UI.Archive
{
    internal class UIJoinServerEntry
    {
        private readonly UIArchiveJoinDialog Parent;
        public readonly ClientArchiveHistoryItem Item;

        public ClientArchiveHistoryType ServerType => Item.ServerType;
        public string Name => Item.Name;
        public string Address => Item.Address;

        public bool IsFetching = true;

        public StatusCheckResult? Result;

        public UIJoinServerEntry(UIArchiveJoinDialog parent, ClientArchiveHistoryItem item)
        {
            Parent = parent;
            Item = item;

            Task.Run(RefreshStatus);
        }

        public async Task RefreshStatus()
        {
            StatusCheckResult result;
            switch (Item.ServerType)
            {
                case ClientArchiveHistoryType.FreeSO:
                    // If the server is FreeSO, try and request the `/userapi/status.json`.07
                    result = await StatusChecker.FreeSOStatus(Item.Address);
                    break;
                case ClientArchiveHistoryType.Archive:
                case ClientArchiveHistoryType.DiscordArchive:
                    // If it's archive, start a connection to the server, then disconnect after getting the RequestClientSessionArchive packet.
                    // Disconnect after two seconds of not receiving this packet.
                    result = await StatusChecker.ArchiveStatus(FSOFacade.Kernel, Item.Address);
                    break;
                default:
                    return;
            }

            GameThread.InUpdate(() =>
            {
                IsFetching = false;
                Result = result;

                // If the result's name/address doesn't match the saved one, we need to update it.
                // TODO: save back to the config?
                if (result.IsOnline)
                {
                    if (Item.Name != result.Name)
                    {
                        Item.Name = result.Name;
                    }
                }

                Parent?.UpdateServerTable(); // TODO: update just this item?
            });
        }
    }

    internal class UIArchiveJoinDialog : UIDialog
    {
        public UIArchiveDisplayName DisplayName;
        public UIButton AddServerButton;
        public UIButton JoinButton;

        private UIHBoxContainer ButtonBox;
        private UIVBoxContainer CurrentLayout;

        private UIJoinServerEntry[] Servers;
        private UIGenericTable ServerTable;

        private readonly Texture2D ActionsButtonTexture;
        private readonly Texture2D ServerFreeSOIcon;
        private readonly Texture2D ServerArchiveIcon;
        private readonly Texture2D ServerDiscordIcon;

        public UIArchiveJoinDialog() : base(UIDialogStyle.Close, true)
        {
            Caption = "Join Server";

            var gd = GameFacade.GraphicsDevice;

            var ui = Content.Content.Get().CustomUI;
            ActionsButtonTexture = ui.Get("archive_burgermenu.png").Get(gd);

            ServerArchiveIcon = ui.Get("archive_simuser.png").Get(gd);
            ServerFreeSOIcon = ui.Get("archive_simshared.png").Get(gd);
            ServerDiscordIcon = ui.Get("archive_discordserver.png").Get(gd);

            ButtonBox = new UIHBoxContainer() { VerticalAlignment = UIContainerVerticalAlignment.Middle };
            ButtonBox.Add(AddServerButton = new UIButton() { Caption = "Add Server" });
            ButtonBox.Add(JoinButton = new UIButton() { Caption = "Join", Disabled = true });
            ButtonBox.AutoSize();

            ServerTable = new UIGenericTable([
                new UITableColumn("", 22),
                new UITableColumn(GameFacade.Strings.GetString("f128", "133"), 192),
                new UITableColumn(GameFacade.Strings.GetString("f128", "134"), 64),
                new UITableColumn(GameFacade.Strings.GetString("f128", "135"), 64),
                new UITableColumn("", 14),
                ], 250)
            { Loading = false };

            DisplayName = new UIArchiveDisplayName();

            AddServerButton.OnButtonClick += AddServer;
            JoinButton.OnButtonClick += Submit;
            CloseButton.OnButtonClick += Close;

            BuildLayout();

            var config = ClientArchiveConfiguration.Default;

            Servers = [..config.JoinHistory.Select(x =>
            {
                return new UIJoinServerEntry(this, x);
            })];

            UpdateServerTable();

            ServerTable.OnChange += SelectionChanged;
        }

        private void AddServer(UIElement button)
        {
            var dialog = new UIArchiveAddServerDialog(AddServerResult);

            GameScreen.ShowDialog(dialog, true);
        }

        private void AddServerResult(UIAddServerResult info)
        {
            var newServerInfo = new ClientArchiveHistoryItem(info.IsFreeSO ? ClientArchiveHistoryType.FreeSO : ClientArchiveHistoryType.Archive, info.Status.Name, info.Address, 0);

            var config = ClientArchiveConfiguration.Default;
            config.RegisterJoin(newServerInfo);

            Servers = [..config.JoinHistory.Select(x =>
                Servers.FirstOrDefault(y => y.ServerType == x.ServerType && y.Address == x.Address) ?? new UIJoinServerEntry(this, x))];

            UpdateServerTable();
        }

        private void SelectionChanged(UIElement element)
        {
            JoinButton.Disabled = ServerTable.SelectedIndex == -1 || (ServerTable.SelectedItem.Data as UIJoinServerEntry)?.Result?.IsOnline != true;
        }

        private Texture2D GetTypeIcon(ClientArchiveHistoryType type)
        {
            switch (type)
            {
                case ClientArchiveHistoryType.Archive:
                    return ServerArchiveIcon;
                case ClientArchiveHistoryType.FreeSO:
                    return ServerFreeSOIcon;
                case ClientArchiveHistoryType.DiscordArchive:
                    return ServerDiscordIcon;
            }

            return null;
        }

        private void Refresh(UIJoinServerEntry server)
        {
            server.IsFetching = true;
            Task.Run(server.RefreshStatus);

            UpdateServerTable();
        }

        private void Forget(UIJoinServerEntry server)
        {
            // Remove the server from this list (and the saved history)

            Servers = [.. Servers.Where(item => item != server)];

            // TODO: save history

            UpdateServerTable();
        }

        private void CopyIP(UIJoinServerEntry server)
        {
            ClipboardHandler.Default.Set(server.Address);
            UIScreen.GlobalShowAlert(new UIAlertOptions()
            {
                Message = GameFacade.Strings.GetString("f128", "34"), // Copied to clipboard
            }, true);
        }

        private void OpenActions(UIElement anchor, UIJoinServerEntry server)
        {
            var items = new List<UIContextMenuItem>
            {
                new(GameFacade.Strings.GetString("f128", "139"), () => { Refresh(server); }),
                new(GameFacade.Strings.GetString("f128", "136"), () => { Forget(server); }),
                new(GameFacade.Strings.GetString("f128", "137"), () => { CopyIP(server); }),
            };

            new UIContextMenu(anchor, items, ServerTable);
        }

        public void UpdateServerTable()
        {
            ServerTable.Items.Clear();
            var items = ServerTable.Items;

            // First, stable sort the servers by online status, so the online servers always appear at the top.

            var orderedServers = Servers.OrderBy(server => !(server.Result?.IsOnline ?? false));

            foreach (var server in orderedServers)
            {
                var actionButton = new UIButton(ActionsButtonTexture);

                actionButton.OnButtonClick += (UIElement element) =>
                {
                    OpenActions(element, server);
                };

                var status = server.IsFetching ? null : server.Result;

                items.Add(new UIListBoxItem(
                    server,
                    GetTypeIcon(server.ServerType),
                    server.Name,
                    status?.Version?.id ?? "",
                    status == null ? "--" : (status.Value.IsOnline ? status.Value.Players.ToString() : GameFacade.Strings.GetString("f128", "138")),
                    actionButton)
                {
                    Disabled = status?.IsOnline != true
                });
            }

            ServerTable.Items = items;
        }

        private void BuildLayout()
        {
            if (CurrentLayout != null)
                Remove(CurrentLayout);

            CurrentLayout = new UIVBoxContainer() { HorizontalAlignment = UIContainerHorizontalAlignment.Right };
            CurrentLayout.Add(ServerTable);
            CurrentLayout.Add(ButtonBox);
            CurrentLayout.AutoSize();
            CurrentLayout.Position = new Vector2(20, 40);
            SetSize((int)CurrentLayout.Size.X + 40, (int)CurrentLayout.Size.Y + 60);
            Add(CurrentLayout);

            DisplayName.AutoSize();

            CurrentLayout.Add(DisplayName);
            DisplayName.Position = new Vector2(0, ButtonBox.Y + (ButtonBox.Size.Y - DisplayName.Size.Y) / 2);

            //JoinButton.Caption = server ? "Connect" : "Join";
        }

        private void Close(UIElement button)
        {
            FindController<ConnectArchiveController>().SwitchMode(ConnectArchiveMode.Landing);
        }
        
        private FSOVersionInfo GetTargetUpdate(UIJoinServerEntry server)
        {
            var current = FSOVersionInfo.Current;

            if (server.Result == null)
            {
                return null;
            }

            var target = server.Result.Value.Version;

            return current.Equals(target) ? null : target;
        }

        private void Join(UIJoinServerEntry selected)
        {
            ClientArchiveConfiguration.Default.RegisterJoin(selected.Item);

            if (selected.ServerType == ClientArchiveHistoryType.FreeSO)
            {
                var url = selected.Address;

                UIScreen.RemoveDialog(this);
                FSOFacade.Controller.ShowServerLogin(url);
            }
            else
            {
                var displayName = ClientArchiveConfiguration.Default.PlayerName;
                FSOFacade.Controller.ConnectToArchive(displayName, selected.Address, false);
            }
        }

        private void Submit(UIElement button)
        {
            var item = ServerTable.SelectedItem;
            if (JoinButton.Disabled || item == null)
                return;

            var selected = item.Data as UIJoinServerEntry;
            var update = GetTargetUpdate(selected);

            if (update != null)
            {
                var controller = new UpdateController((bool skip) =>
                {
                    if (skip)
                    {
                        Join(selected);
                    }
                });

                controller.PromptUpdate(update);
            }
            else
            {
                Join(selected);
            }
        }

        public override void Update(UpdateState state)
        {
            base.Update(state);
            FindController<ConnectArchiveController>().TickRPC();
        }
    }
}
