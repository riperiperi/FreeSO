using FSO.Client.UI.Controls;
using FSO.Client.UI.Framework;
using FSO.Client.UI.Panels;
using FSO.Common.Utils;
using FSO.Server.Embedded;
using FSO.Server.Protocol.Embedded;
using FSO.UI.Controls;
using Microsoft.Xna.Framework;

namespace FSO.Client.UI.Archive.Management
{
    internal class UIArchiveAvatarMigrateDialog : UIDialog
    {
        private ArchiveManagement Management;

        private UIGenericTable UserTable;
        private UITextBox SearchBox;
        private UIButton TransferButton;

        private UIListBoxTextStyle ListBoxColors;

        private ArchiveDbAvatar Avatar;
        private List<ArchiveDbUser> Data;

        public UIArchiveAvatarMigrateDialog(ArchiveManagement management, ArchiveDbAvatar avatar) : base(UIDialogStyle.Close, true)
        {
            Avatar = avatar;
            Management = management;

            var gd = GameFacade.GraphicsDevice;

            var ui = Content.Content.Get().CustomUI;

            Caption = GameFacade.Strings.GetString("f128", "74");
            var vbox = new UIVBoxContainer() { HorizontalAlignment = UIContainerHorizontalAlignment.Center };

            UILabel desc;

            vbox.Add(desc = new UILabel()
            {
                Caption = GameFacade.Strings.GetString("f128", "75", [Avatar.Name]),
                Wrapped = true
            });

            desc.Size = new Vector2(200, 48);

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
                new UITableColumn(GameFacade.Strings.GetString("f128", "51"), 62),
                ])
            { Loading = true });

            var vbox2 = new UIVBoxContainer() { HorizontalAlignment = UIContainerHorizontalAlignment.Right };

            vbox.Add(new UISpacer(1, 8));

            vbox2.Add(TransferButton = new UIButton()
            {
                Caption = GameFacade.Strings.GetString("f128", "76")
            });

            vbox2.AutoSize(); //TODO: somehow force horiz size from parent?

            vbox.Add(vbox2);

            DynamicOverlay.Add(vbox);

            vbox.AutoSize();
            vbox.Position = new Vector2(20, 35);

            SearchBox.OnChange += (elem) => UpdateUserTable();

            SetSize((int)vbox.Size.X + 40, (int)vbox.Size.Y + 60);

            UserTable.OnChange += (elem) =>
            {
                TransferButton.Disabled = UserTable.SelectedIndex == -1;
            };

            CloseButton.OnButtonClick += (elem) =>
            {
                UIScreen.RemoveDialog(this);
            };

            TransferButton.OnButtonClick += Transfer;
            TransferButton.Disabled = true;

            Fetch();
        }

        private void Transfer(UIElement button)
        {
            var selected = UserTable.SelectedItem;

            if (selected == null)
            {
                return;
            }

            var user = (ArchiveDbUser)selected.Data;

            UIAlert.Prompt(GameFacade.Strings.GetString("f128", "77", [Avatar.Name, user.Name]), (result, alert) =>
            {
                if (result)
                {
                    try
                    {
                        Management.MigrateAvatar((int)Avatar.ID, (int)user.ID);
                    }
                    catch
                    {
                        UIAlert.Alert("", GameFacade.Strings.GetString("f128", "78"), true);
                        return;
                    }

                    UIScreen.RemoveDialog(this);
                }
            });
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
                        return new UIListBoxItem(x, [x.Name, x.AvatarCount.ToString(), GetStatusIcon(x.Status)])
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
    }
}
