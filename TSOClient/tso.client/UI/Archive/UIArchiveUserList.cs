using FSO.Client.Controllers;
using FSO.Client.UI.Controls;
using FSO.Client.UI.Framework;
using FSO.Client.UI.Panels;
using FSO.Common;
using FSO.Common.Rendering.Framework.Model;
using FSO.Server.Protocol.Electron.Model;
using FSO.Server.Protocol.Electron.Packets;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace FSO.Client.UI.Archive
{
    public class UIArchiveUserList : UIArchiveDialog
    {
        private ArchiveClientList LastList;
        private UIImage ListBackground;
        private UIListBoxTextStyle ListBoxColors;
        private UIListBox UserListBox;

        private Texture2D AdminActionsButtonTexture;

        private Texture2D UserAdminIcon;
        private Texture2D UserModIcon;
        private Texture2D UserVerifyIcon;

        private int FrameCount;

        public UIArchiveUserList() : base(UIDialogStyle.Close, true)
        {
            Caption = GetString("270");

            var gd = GameFacade.GraphicsDevice;

            var ui = Content.Content.Get().CustomUI;
            AdminActionsButtonTexture = ui.Get("archive_burgermenu.png").Get(gd);

            UserAdminIcon = ui.Get("archive_useradmin.png").Get(gd);
            UserModIcon = ui.Get("archive_usermod.png").Get(gd);
            UserVerifyIcon = ui.Get("archive_userverify.png").Get(gd);

            var vbox = new UIVBoxContainer();

            var searchFont = TextStyle.DefaultLabel.Clone();
            searchFont.Size = 8;

            ListBoxColors = new UIListBoxTextStyle(searchFont)
            {
                NormalColor = new Color(247, 232, 145),
                SelectedColor = new Color(0, 0, 0),
                HighlightedColor = new Color(255, 255, 255),
                DisabledColor = new Color(150, 150, 150)
            };

            ListBackground = new UIImage(ui.Get("archive_translist.png").Get(gd)).With9Slice(13, 13, 13, 13);
            ListBackground.SetSize(180, 300);
            vbox.Add(ListBackground);

            vbox.AutoSize();
            vbox.Position = new Vector2(15, 40);
            Add(vbox);

            DynamicOverlay.Add(UserListBox = new UIListBox()
            {
                Size = ListBackground.Size - new Vector2(20, 20),
                Position = vbox.Position + ListBackground.Position + new Vector2(10, 10),
                Mask = true,
                VisibleRows = 12,
                Columns = new UIListBoxColumnCollection()
                {
                    new UIListBoxColumn() { Width = 25, Alignment = TextAlignment.Left }, // Avatar button
                    new UIListBoxColumn() { Width = 99, Alignment = TextAlignment.Left | TextAlignment.Middle }, // Display name, unique ID
                    new UIListBoxColumn() { Width = 20, Alignment = TextAlignment.Left | TextAlignment.Middle }, // Admin status
                    new UIListBoxColumn() { Width = 15, Alignment = TextAlignment.Left | TextAlignment.Middle }, // Admin actions
                },
                RowHeight = 20,
                FontStyle = searchFont,
                SelectionFillColor = new Color(250, 200, 140),
                ScrollbarImage = GetTexture(0x31000000001),
                ScrollbarGutter = 12,
                UseChildElements = true,
            });

            UserListBox.InitDefaultSlider();

            SetSize((int)vbox.Size.X + 30 + 16, (int)vbox.Size.Y + 60);

            CloseButton.OnButtonClick += Close;
        }

        private bool FlashActive()
        {
            return (FrameCount % FSOEnvironment.RefreshRate) < FSOEnvironment.RefreshRate / 2;
        }

        private void Close(UIElement button)
        {
            Visible = false;
        }

        public override void Update(UpdateState state)
        {
            if (Visible)
            {
                var controller = FindController<UserListController>();
                ArchiveClientList list = controller?.UserList;

                if (LastList != list)
                {
                    UpdateList(list);
                }

                FrameCount++;
                if (FrameCount % (FSOEnvironment.RefreshRate / 2) == 0)
                {
                    bool flash = FlashActive();
                    foreach (var item in UserListBox.Items)
                    {
                        if (item.Data is ArchivePendingVerification)
                        {
                            item.UseSelectedStyleByDefault = flash;
                        }
                    }
                }
            }

            base.Update(state);
        }

        private void Approve(ArchivePendingVerification client)
        {
            var controller = FindController<FSO.Client.Controllers.CoreGameScreenController>();
            controller?.ArchiveModRequest(client.UserId, ArchiveModerationRequestType.APPROVE_USER);
        }

        private void Reject(ArchivePendingVerification client)
        {
            var controller = FindController<FSO.Client.Controllers.CoreGameScreenController>();
            controller?.ArchiveModRequest(client.UserId, ArchiveModerationRequestType.REJECT_USER);
        }

        private void Kick(ArchiveClient client)
        {
            UIAlert.YesNo(GetString("271", client.DisplayName), GetString("272", client.DisplayName), true, (bool result) =>
            {
                if (result)
                {
                    var controller = FindController<FSO.Client.Controllers.CoreGameScreenController>();
                    controller?.ArchiveModRequest(client.UserId, ArchiveModerationRequestType.KICK_USER);
                }
            });
        }

        private void Ban(ArchiveClient client)
        {
            UIAlert.YesNo(GetString("273", client.DisplayName), GetString("274", client.DisplayName), true, (bool result) =>
            {
                if (result)
                {
                    var controller = FindController<FSO.Client.Controllers.CoreGameScreenController>();
                    controller?.ArchiveModRequest(client.UserId, ArchiveModerationRequestType.BAN_USER);
                }
            });
        }

        private void Ban(ArchivePendingVerification client)
        {
            UIAlert.YesNo(GetString("273", client.DisplayName), GetString("275", client.DisplayName), true, (bool result) =>
            {
                if (result)
                {
                    var controller = FindController<FSO.Client.Controllers.CoreGameScreenController>();
                    controller?.ArchiveModRequest(client.UserId, ArchiveModerationRequestType.BAN_USER);
                }
            });
        }

        private Texture2D GetModIcon(uint level)
        {
            switch (level)
            {
                case 0:
                    return null;
                case 1:
                    return UserModIcon;
                case 2:
                case 3:
                    return UserAdminIcon;
            }

            return null;
        }

        private string GetModString(int level)
        {
            // TODO: localization

            switch (level)
            {
                case 0:
                    return GetString("276");
                case 1:
                    return GetString("277");
                case 2:
                    return GetString("278");
            }

            return level.ToString(); //TODO
        }

        private void ChangePermissions(ArchiveClient client, int currentLevel, int targetLevel)
        {
            string before = GetModString(currentLevel);
            string after = GetModString(targetLevel);

            UIAlert.YesNo(GetString("273", client.DisplayName), GetString("279", client.DisplayName, before, after), true, (bool result) =>
            {
                if (result)
                {
                    var controller = FindController<FSO.Client.Controllers.CoreGameScreenController>();
                    controller?.ArchiveModRequest(client.UserId, ArchiveModerationRequestType.CHANGE_MOD_LEVEL, targetLevel);
                }
            });
        }

        private void OpenActions(UIElement anchor, ArchivePendingVerification client)
        {
            int myLevel = 2;
            var items = new List<UIContextMenuItem>();

            if (myLevel > 0)
            {
                items.Add(new UIContextMenuItem(GetString("280"), () => { Approve(client); }));
                items.Add(new UIContextMenuItem(GetString("281"), () => { Reject(client); }));
                items.Add(new UIContextMenuItem(GetString("282"), () => { Ban(client); }));
            }

            new UIContextMenu(anchor, items, this);
        }

        private void OpenActions(UIElement anchor, ArchiveClient client, int myLevel)
        {
            int theirLevel = (int)client.ModerationLevel;

            var items = new List<UIContextMenuItem>();

            if (myLevel > theirLevel)
            {
                if (myLevel >= 2)
                {
                    // Change moderation level for this user
                    if (theirLevel != 2)
                    {
                        items.Add(new UIContextMenuItem(GetString("283"), () => { ChangePermissions(client, theirLevel, 2); }));
                    }

                    if (theirLevel != 1)
                    {
                        items.Add(new UIContextMenuItem(GetString("284"), () => { ChangePermissions(client, theirLevel, 1); }));
                    }

                    if (theirLevel != 0)
                    {
                        items.Add(new UIContextMenuItem(GetString("285"), () => { ChangePermissions(client, theirLevel, 0); }));
                    }
                }

                if (myLevel > 0)
                {
                    items.Add(new UIContextMenuItem(GetString("286"), () => { Kick(client); }));
                    items.Add(new UIContextMenuItem(GetString("282"), () => { Ban(client); }));
                }
            }

            new UIContextMenu(anchor, items, this);
        }

        public void UpdateList(ArchiveClientList list)
        {
            LastList = list;

            Caption = GetString("287", (list?.Clients?.Length ?? 0).ToString());

            bool flash = FlashActive();

            var items = new List<UIListBoxItem>();

            if (list != null)
            {
                foreach (var client in list.Pending)
                {
                    var actionButton = new UIButton(AdminActionsButtonTexture);

                    actionButton.OnButtonClick += (UIElement element) =>
                    {
                        OpenActions(element, client);
                    };

                    items.Add(new UIListBoxItem(
                        client,
                        "",
                        client.DisplayName,
                        UserVerifyIcon,
                        actionButton)
                    {
                        CustomStyle = ListBoxColors,
                        UseSelectedStyleByDefault = flash
                    });
                }

                var screen = FindController<CoreGameScreenController>();

                var myId = screen.MyID();
                var myClient = list.Clients.FirstOrDefault(x => myId == x.AvatarId);
                int myLevel = (int)(myClient.AvatarId != 0 ? myClient.ModerationLevel : screen.ModerationLevel);

                foreach (var client in list.Clients)
                {
                    var actionButton = new UIButton(AdminActionsButtonTexture);

                    var hasActions = myLevel > client.ModerationLevel;

                    actionButton.OnButtonClick += (UIElement element) =>
                    {
                        OpenActions(element, client, myLevel);
                    };

                    items.Add(new UIListBoxItem(
                        client,
                        client.AvatarId == 0
                            ? (object)""
                            : new UIPersonButton() { FrameSize = UIPersonButtonSize.SMALL, AvatarId = client.AvatarId },
                        client.DisplayName,
                        hasActions ? GetModIcon(client.ModerationLevel) : null,
                        hasActions ? actionButton : GetModIcon(client.ModerationLevel))
                    {
                        CustomStyle = ListBoxColors
                    });
                }
            }

            UserListBox.Items = items;
        }
    }
}
