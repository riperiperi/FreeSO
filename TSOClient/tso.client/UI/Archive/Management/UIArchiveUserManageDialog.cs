using FSO.Client.UI.Controls;
using FSO.Client.UI.Framework;
using FSO.Client.UI.Panels;
using FSO.Common.Rendering.Framework.IO;
using FSO.Common.Utils;
using FSO.Server.Embedded;
using FSO.Server.Protocol.Embedded;
using FSO.UI.Controls;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace FSO.Client.UI.Archive.Management
{
    internal class UIArchiveUserManageDialog : UIDialog
    {
        private ArchiveManagement Management;

        private UIGenericTable UserTable;
        private UITextBox SearchBox;
        private UIButton IPBansButton;

        private UIListBoxTextStyle ListBoxColors;
        private Texture2D AdminActionsButtonTexture;

        private List<ArchiveDbUser> Data;

        public UIArchiveUserManageDialog(ArchiveManagement management) : base(UIDialogStyle.Close, true)
        {
            Management = management;

            var gd = GameFacade.GraphicsDevice;

            var ui = Content.Content.Get().CustomUI;
            AdminActionsButtonTexture = ui.Get("archive_burgermenu.png").Get(gd);

            Caption = GameFacade.Strings.GetString("f128", "48");
            var vbox = new UIVBoxContainer() { HorizontalAlignment = UIContainerHorizontalAlignment.Center };

            var searchContainer = new UIHBoxContainer() { VerticalAlignment = UIContainerVerticalAlignment.Middle };

            searchContainer.Add(new UILabel()
            {
                Caption = GameFacade.Strings.GetString("f128", "64")
            });

            searchContainer.Add(SearchBox = new UITextBox() { });
            SearchBox.SetSize(200, 25);

            searchContainer.AutoSize();

            vbox.Add(searchContainer);

            vbox.Add(new UISpacer(1, 8));

            vbox.Add(UserTable = new UIGenericTable([
                new UITableColumn(GameFacade.Strings.GetString("f128", "49"), 128),
                new UITableColumn(GameFacade.Strings.GetString("f128", "50"), 96),
                new UITableColumn(GameFacade.Strings.GetString("f128", "51"), 48),
                new UITableColumn("", 14),
                ])
            { Loading = true });


            var vbox2 = new UIVBoxContainer() { HorizontalAlignment = UIContainerHorizontalAlignment.Right };

            vbox.Add(new UISpacer(1, 8));

            vbox2.Add(IPBansButton = new UIButton()
            {
                Caption = GameFacade.Strings.GetString("f128", "56")
            });

            vbox2.AutoSize(); //TODO: somehow force horiz size from parent?

            vbox.Add(vbox2);

            DynamicOverlay.Add(vbox);

            vbox.AutoSize();
            vbox.Position = new Vector2(20, 35);

            SearchBox.OnChange += (elem) => UpdateUserTable();

            SetSize((int)vbox.Size.X + 40, (int)vbox.Size.Y + 60);

            CloseButton.OnButtonClick += (elem) =>
            {
                UIScreen.RemoveDialog(this);
            };

            IPBansButton.OnButtonClick += OpenIPBans;

            Fetch();
        }

        private void OpenIPBans(UIElement button)
        {
            UIScreen.GlobalShowDialog(new UIArchiveBanManageDialog(Management), true);
        }

        private void Fetch()
        {
            Task.Run(() =>
            {
                var users = Management.GetUsers();

                GameThread.InUpdate(() =>
                { 
                    Data = users; 
                    UserTable.Loading = false;
                    UpdateUserTable();
                });
            });
        }

        private string GetStatusIcon(ArchiveDbUserStatus status)
        {
            return status.ToString();
        }

        private void UpdateUserTable()
        {
            var query = (SearchBox.CurrentText ?? "").ToLower();

            if (Data == null)
            {
                // Empty the list
                UserTable.Items.Clear();
            }
            else
            {
                var myItems = Data
                    .Where(x => x.Name.ToLower().Contains(query))
                    .Select((ArchiveDbUser x) =>
                    {
                        var actionButton = new UIButton(AdminActionsButtonTexture);

                        actionButton.OnButtonClick += (UIElement element) =>
                        {
                            OpenActions(element, x);
                        };

                        return new UIListBoxItem(x, new object[] { x.Name, x.AvatarCount.ToString(), GetStatusIcon(x.Status), actionButton })
                        {
                            CustomStyle = ListBoxColors,
                        };
                    });

                UserTable.Items.Clear();

                UserTable.Items.AddRange(myItems);
            }

            UserTable.Items = UserTable.Items;
            Invalidate();
        }

        private void ViewAvatars(ArchiveDbUser user)
        {
            UIScreen.GlobalShowDialog(new UIArchiveAvatarManageDialog(Management, user), true);
        }

        private void ShowIP(ArchiveDbUser user)
        {
            try
            {
                ClipboardHandler.Default.Set(user.IP);
            }
            catch
            {
                // No error right now.
            }
            
            UIAlert.Alert("", GameFacade.Strings.GetString("f128", "70", [user.Name, user.IP]), true);
        }

        private void BanUser(ArchiveDbUser user)
        {
            UIAlert.Prompt(GameFacade.Strings.GetString("f128", "72", [user.Name]), (result, alert) =>
            {
                if (result)
                {
                    try
                    {
                        Management.BanUser((int)user.ID);
                    }
                    catch
                    {
                        UIAlert.Alert("", GameFacade.Strings.GetString("f128", "78"), true);
                        return;
                    }

                    Fetch();
                }
            });
        }

        private void UnbanUser(ArchiveDbUser user)
        {
            UIAlert.Prompt(GameFacade.Strings.GetString("f128", "71", [user.Name]), (result, alert) =>
            {
                if (result)
                {
                    try
                    {
                        Management.UnbanUser((int)user.ID);
                    }
                    catch
                    {
                        UIAlert.Alert("", GameFacade.Strings.GetString("f128", "78"), true);
                        return;
                    }

                    Fetch();
                }
            });
        }

        private void DeleteUser(ArchiveDbUser user)
        {
            UIAlert.Prompt(GameFacade.Strings.GetString("f128", "73", [user.Name]), (result, alert) =>
            {
                if (result)
                {
                    try
                    {
                        Management.DeleteUser((int)user.ID);
                    }
                    catch
                    {
                        UIAlert.Alert("", GameFacade.Strings.GetString("f128", "78"), true);
                        return;
                    }

                    Fetch();
                }
            });
        }

        private void OpenActions(UIElement anchor, ArchiveDbUser user)
        {
            var items = new List<UIContextMenuItem>
            {
                new(GameFacade.Strings.GetString("f128", "52"), () => { ViewAvatars(user); }),
                new(GameFacade.Strings.GetString("f128", "53"), () => { ShowIP(user); }),
            };

            if (user.Status == ArchiveDbUserStatus.Banned)
            {
                items.Add(new UIContextMenuItem(GameFacade.Strings.GetString("f128", "55"), () => { UnbanUser(user); }));
            }
            else
            {
                items.Add(new UIContextMenuItem(GameFacade.Strings.GetString("f128", "54"), () => { BanUser(user); }));
            }
            
            items.Add(new UIContextMenuItem(GameFacade.Strings.GetString("f128", "66"), () => { DeleteUser(user); }));

            new UIContextMenu(anchor, items, UserTable);
        }
    }
}
