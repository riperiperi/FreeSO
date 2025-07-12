using FSO.Client.Controllers;
using FSO.Client.UI.Controls;
using FSO.Client.UI.Framework;
using FSO.Common.Rendering.Framework.Model;
using FSO.Server.Protocol.Electron.Packets;
using Microsoft.Xna.Framework;
using System.Collections.Generic;

namespace FSO.Client.UI.Archive
{
    public class UIArchiveUserList : UIDialog
    {
        private ArchiveClientList LastList;
        private UIImage ListBackground;
        private UIListBoxTextStyle ListBoxColors;
        private UIListBox UserListBox;

        public UIArchiveUserList() : base(UIDialogStyle.Close, true)
        {
            Caption = "User List";

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

            ListBackground = new UIImage(GetTexture((ulong)0x7A400000001)).With9Slice(13, 13, 13, 13);
            ListBackground.SetSize(180, 300);
            vbox.Add(ListBackground);

            vbox.AutoSize();
            vbox.Position = new Vector2(15, 40);
            Add(vbox);

            Add(UserListBox = new UIListBox()
            {
                Size = ListBackground.Size - new Vector2(20, 20),
                Position = vbox.Position + ListBackground.Position + new Vector2(10, 10),
                Mask = true,
                VisibleRows = 12,
                Columns = new UIListBoxColumnCollection()
                {
                    new UIListBoxColumn() { Width = 25, Alignment = TextAlignment.Left }, // Avatar button
                    new UIListBoxColumn() { Width = 115, Alignment = TextAlignment.Left | TextAlignment.Middle }, // Display name, unique ID
                    new UIListBoxColumn() { Width = 20, Alignment = TextAlignment.Left | TextAlignment.Middle }, // Admin status
                },
                RowHeight = 20,
                FontStyle = searchFont,
                SelectionFillColor = new Color(250, 200, 140),
                ScrollbarImage = GetTexture(0x31000000001),
                ScrollbarGutter = 12,
            });

            UserListBox.InitDefaultSlider();

            SetSize((int)vbox.Size.X + 30 + 16, (int)vbox.Size.Y + 60);

            CloseButton.OnButtonClick += Close;
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
            }

            base.Update(state);
        }

        public void UpdateList(ArchiveClientList list)
        {
            LastList = list;

            Caption = $"User List ({list?.Clients?.Length ?? 0})";

            var items = new List<UIListBoxItem>();

            if (list != null)
            {
                foreach (var client in list.Clients)
                {
                    items.Add(new UIListBoxItem(
                        client,
                        client.AvatarId == 0
                            ? (object)""
                            : new UIPersonButton() { FrameSize = UIPersonButtonSize.SMALL, AvatarId = client.AvatarId },
                        client.DisplayName,
                        client.ModerationLevel)
                    {
                        CustomStyle = ListBoxColors,
                    });
                }
            }

            UserListBox.Items = items;
        }
    }
}
