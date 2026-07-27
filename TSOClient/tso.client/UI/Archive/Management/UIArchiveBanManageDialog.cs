using FSO.Client.UI.Controls;
using FSO.Client.UI.Framework;
using FSO.Client.UI.Panels;
using FSO.Common.Utils;
using FSO.Server.Embedded;
using FSO.Server.Protocol.Embedded;
using FSO.UI.Controls;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace FSO.Client.UI.Archive.Management
{
    internal class UIArchiveBanManageDialog : UIDialog
    {
        private ArchiveManagement Management;

        private UIGenericTable IpTable;
        private UITextBox SearchBox;
        private UIButton AddIpButton;

        private UIListBoxTextStyle ListBoxColors;
        private Texture2D AdminActionsButtonTexture;

        private List<ArchiveDbIpBan> Data;

        public UIArchiveBanManageDialog(ArchiveManagement management) : base(UIDialogStyle.Close, true)
        {
            Management = management;

            var gd = GameFacade.GraphicsDevice;

            var ui = Content.Content.Get().CustomUI;
            AdminActionsButtonTexture = ui.Get("archive_burgermenu.png").Get(gd);

            Caption = GameFacade.Strings.GetString("f128", "56");
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

            vbox.Add(IpTable = new UIGenericTable([
                new UITableColumn(GameFacade.Strings.GetString("f128", "60"), 128),
                new UITableColumn(GameFacade.Strings.GetString("f128", "61"), 128),
                new UITableColumn("", 14),
                ])
            { Loading = true });


            var vbox2 = new UIVBoxContainer() { HorizontalAlignment = UIContainerHorizontalAlignment.Right };

            vbox.Add(new UISpacer(1, 8));

            vbox2.Add(AddIpButton = new UIButton()
            {
                Caption = GameFacade.Strings.GetString("f128", "63")
            });

            vbox2.AutoSize(); //TODO: somehow force horiz size from parent?

            vbox.Add(vbox2);

            DynamicOverlay.Add(vbox);

            vbox.AutoSize();
            vbox.Position = new Vector2(20, 35);

            SearchBox.OnChange += (elem) => UpdateIpTable();

            SetSize((int)vbox.Size.X + 40, (int)vbox.Size.Y + 60);

            CloseButton.OnButtonClick += (elem) =>
            {
                UIScreen.RemoveDialog(this);
            };

            AddIpButton.OnButtonClick += BanIp;

            Fetch();
        }

        private void BanIp(UIElement button)
        {
            UIAlert.Prompt("", GameFacade.Strings.GetString("f128", "67"), true, (string ip) =>
            {
                if (ip != null)
                {
                    try
                    {
                        Management.BanIp(ip);
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

        private void Fetch()
        {
            Task.Run(() =>
            {
                var ips = Management.GetIpBans();

                GameThread.InUpdate(() =>
                {
                    Data = ips;
                    IpTable.Loading = false;
                    UpdateIpTable();
                });
            });
        }

        private void UpdateIpTable()
        {
            var query = (SearchBox.CurrentText ?? "").ToLower();

            if (Data == null)
            {
                // Empty the list
                IpTable.Items.Clear();
            }
            else
            {
                var myItems = Data
                    .Where(x => x.IP.ToLower().Contains(query))
                    .Select((ArchiveDbIpBan x) =>
                    {
                        var actionButton = new UIButton(AdminActionsButtonTexture);

                        actionButton.OnButtonClick += (UIElement element) =>
                        {
                            OpenActions(element, x);
                        };

                        return new UIListBoxItem(x, new object[] { x.IP, "", actionButton })
                        {
                            CustomStyle = ListBoxColors,
                        };
                    });

                IpTable.Items.Clear();

                IpTable.Items.AddRange(myItems);
            }

            IpTable.Items = IpTable.Items;
            Invalidate();
        }

        private void UnbanIp(ArchiveDbIpBan ip)
        {
            UIAlert.Prompt(GameFacade.Strings.GetString("f128", "68", [ip.IP]), (result, alert) =>
            {
                if (result)
                {
                    try
                    {
                        Management.UnbanIp(ip.IP);
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

        private void OpenActions(UIElement anchor, ArchiveDbIpBan ip)
        {
            var items = new List<UIContextMenuItem>
            {
                new UIContextMenuItem(GameFacade.Strings.GetString("f128", "62"), () => { UnbanIp(ip); })
            };

            new UIContextMenu(anchor, items, IpTable);
        }
    }
}
