using FSO.Client.UI.Controls;
using FSO.Client.UI.Framework;
using FSO.Client.UI.Panels;
using FSO.Common.Utils;
using FSO.Server.Embedded;
using FSO.Server.Protocol.Embedded;
using FSO.UI.Controls;
using Microsoft.Xna.Framework.Graphics;
using System.Numerics;

namespace FSO.Client.UI.Archive.Management
{
    internal class UIArchiveAvatarManageDialog : UIDialog
    {
        private ArchiveManagement Management;

        private UIGenericTable AvatarTable;
        private UITextBox SearchBox;
        private UIButton IPBansButton;

        private UIListBoxTextStyle ListBoxColors;
        private Texture2D AdminActionsButtonTexture;

        private ArchiveDbUser User;
        private List<ArchiveDbAvatar> Data;
        public UIArchiveAvatarManageDialog(ArchiveManagement management, ArchiveDbUser user) : base(UIDialogStyle.Close, true)
        {
            User = user;
            Management = management;

            var gd = GameFacade.GraphicsDevice;

            var ui = Content.Content.Get().CustomUI;
            AdminActionsButtonTexture = ui.Get("archive_burgermenu.png").Get(gd);

            Caption = GameFacade.Strings.GetString("f128", "57", [ user.Name ]);
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

            vbox.Add(AvatarTable = new UIGenericTable([
                new UITableColumn(GameFacade.Strings.GetString("f128", "58"), 128),
                new UITableColumn(GameFacade.Strings.GetString("f128", "59"), 128),
                new UITableColumn("", 14),
                ])
            { Loading = true });

            /*
            var vbox2 = new UIVBoxContainer() { HorizontalAlignment = UIContainerHorizontalAlignment.Right };

            vbox.Add(new UISpacer(1, 8));

            vbox2.Add(IPBansButton = new UIButton()
            {
                Caption = GameFacade.Strings.GetString("f128", "56")
            });

            vbox2.AutoSize(); //TODO: somehow force horiz size from parent?

            vbox.Add(vbox2);
            */

            DynamicOverlay.Add(vbox);

            vbox.AutoSize();
            vbox.Position = new Vector2(20, 35);

            SearchBox.OnChange += (elem) => UpdateAvatarTable();

            SetSize((int)vbox.Size.X + 40, (int)vbox.Size.Y + 60);

            CloseButton.OnButtonClick += (elem) =>
            {
                UIScreen.RemoveDialog(this);
            };

            Fetch();
        }


        private void Fetch()
        {
            Task.Run(() =>
            {
                var avatars = Management.GetAvatars(User.ID);

                GameThread.InUpdate(() =>
                {
                    Data = avatars;
                    AvatarTable.Loading = false;
                    UpdateAvatarTable();
                });
            });
        }

        private void UpdateAvatarTable()
        {
            var query = (SearchBox.CurrentText ?? "").ToLower();

            if (Data == null)
            {
                // Empty the list
                AvatarTable.Items.Clear();
            }
            else
            {
                var myItems = Data
                    .Where(x => x.Name.ToLower().Contains(query))
                    .Select((ArchiveDbAvatar x) =>
                    {
                        var actionButton = new UIButton(AdminActionsButtonTexture);

                        actionButton.OnButtonClick += (UIElement element) =>
                        {
                            OpenActions(element, x);
                        };

                        return new UIListBoxItem(x, new object[] { x.Name, x.LotName, actionButton })
                        {
                            CustomStyle = ListBoxColors,
                        };
                    });

                AvatarTable.Items.Clear();

                AvatarTable.Items.AddRange(myItems);
            }

            AvatarTable.Items = AvatarTable.Items;
            Invalidate();
        }

        private void DeleteAvatar(ArchiveDbAvatar avatar)
        {
            UIAlert.Prompt(GameFacade.Strings.GetString("f128", "69", [avatar.Name]), (result, alert) =>
            {
                if (result)
                {
                    try
                    {
                        Management.DeleteAvatar((int)avatar.ID);
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

        private void TransferAvatar(ArchiveDbAvatar avatar)
        {
            UIScreen.GlobalShowDialog(new UIArchiveAvatarMigrateDialog(Management, avatar), true);
        }

        private void OpenActions(UIElement anchor, ArchiveDbAvatar avatar)
        {
            var items = new List<UIContextMenuItem>
            {
                new UIContextMenuItem(GameFacade.Strings.GetString("f128", "65"), () => { DeleteAvatar(avatar); }),
                new UIContextMenuItem(GameFacade.Strings.GetString("f128", "74"), () => { TransferAvatar(avatar); })
            };

            new UIContextMenu(anchor, items, AvatarTable);
        }
    }
}
