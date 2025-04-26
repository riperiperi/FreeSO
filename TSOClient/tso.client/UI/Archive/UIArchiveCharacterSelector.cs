using FSO.Client.Controllers;
using FSO.Client.UI.Controls;
using FSO.Client.UI.Framework;
using FSO.Client.UI.Panels.Neighborhoods;
using FSO.Client.Utils;
using FSO.Server.Protocol.Electron.Packets;
using Microsoft.Xna.Framework;
using System.Linq;

namespace FSO.Client.UI.Archive
{
    internal class UIArchiveCharacterSelector : UIDialog
    {
        public UITextBox SearchBox;
        public UIButton SearchButton;
        public UIListBox AvatarListBox;
        public UIButton CASButton;
        public UIButton SelectButton;
        public UIImage ListBackground;
        public UIArchivePersonButton AvatarButton;

        private UIListBoxTextStyle ListBoxColors;

        private ArchiveAvatarsResponse Data;

        public UIArchiveCharacterSelector() : base(UIDialogStyle.Standard, true)
        {
            var ui = Content.Content.Get().CustomUI;
            var gd = GameFacade.GraphicsDevice;

            Caption = "Select a Sim";
            var vbox = new UIVBoxContainer();

            var hboxSearch = new UIHBoxContainer() { VerticalAlignment = UIContainerVerticalAlignment.Middle };

            hboxSearch.Add(SearchBox = new UITextBox()
            {
                Size = new Microsoft.Xna.Framework.Vector2(178, 25)
            });

            hboxSearch.Add(SearchButton = new UIButton(ui.Get("cat_search.png").Get(gd)));

            hboxSearch.AutoSize();

            vbox.Add(hboxSearch);

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
            ListBackground.SetSize(200, 180);
            vbox.Add(ListBackground);

            var actions = new UIHBoxContainer();

            actions.Add(CASButton = new UIButton()
            {
                Caption = "Create a Sim",
            });

            actions.Add(SelectButton = new UIButton()
            {
                Caption = "Select",
                Disabled = true
            });

            actions.AutoSize();

            vbox.Add(actions);

            vbox.AutoSize();
            vbox.Position = new Vector2(20, 45);

            Add(vbox);

            Add(AvatarListBox = new UIListBox()
            {
                Size = ListBackground.Size - new Vector2(20, 20),
                Position = vbox.Position + ListBackground.Position + new Vector2(10, 10),
                Mask = true,
                VisibleRows = 12,
                Columns = new UIListBoxColumnCollection()
                {
                    new UIListBoxColumn() { Width = 25, Alignment = TextAlignment.Left },
                    new UIListBoxColumn() { Width = 163, Alignment = TextAlignment.Left }
                },
                FontStyle = searchFont,
                SelectionFillColor = new Color(250, 200, 140),
                ScrollbarImage = GetTexture(0x31000000001),
                ScrollbarGutter = 12,
            });

            AvatarListBox.InitDefaultSlider();

            SetSize((int)vbox.Size.X + 40 + 130, (int)vbox.Size.Y + 70);

            AvatarButton = new UIArchivePersonButton();
            AvatarButton.Position = new Vector2(vbox.Size.X + 34, 79);
            DynamicOverlay.Add(AvatarButton);

            CASButton.OnButtonClick += OpenCAS;
            SelectButton.OnButtonClick += SelectAvatar;
            SearchBox.OnChange += (elem) =>
            {
                RefreshList();
            };

            AvatarListBox.OnChange += ChangedSelectedAvatar;

            ControllerUtils.BindController<ArchiveCharactersSelectorController>(this);

            FindController<ArchiveCharactersSelectorController>().Refresh();
        }

        private void ChangedSelectedAvatar(UIElement element)
        {
            SelectButton.Disabled = AvatarListBox.SelectedItem == null;

            if (SelectButton.Disabled)
            {
                AvatarButton.SetSim(null);
            }
            else
            {
                var ava = (ArchiveAvatar)AvatarListBox.SelectedItem.Data;
                AvatarButton.SetSim(ava);
            }
        }

        private void SelectAvatar(Framework.UIElement button)
        {
            var ava = (ArchiveAvatar)AvatarListBox.SelectedItem.Data;
            FindController<ConnectArchiveController>().SelectAvatar(ava.AvatarId);
        }

        private void OpenCAS(Framework.UIElement button)
        {
            FSOFacade.Controller.GotoCAS();
        }

        public void SetData(ArchiveAvatarsResponse data)
        {
            Data = data;
            RefreshList();
        }

        public void RefreshList()
        {
            var query = (SearchBox.CurrentText ?? "").ToLower();

            if (Data == null)
            {
                // Empty the list
                AvatarListBox.Items.Clear();
            } else
            {
                var myItems = Data.UserAvatars
                    .Where(x => x.Name.ToLower().Contains(query))
                    .Select((ArchiveAvatar x) =>
                {
                    return new UIListBoxItem(x, new object[] { "*", x.Name })
                    {
                        CustomStyle = ListBoxColors,
                    };
                });

                var sharedItems = Data.SharedAvatars
                    .Where(x => x.Name.ToLower().Contains(query))
                    .Select((ArchiveAvatar x) =>
                {
                    return new UIListBoxItem(x, new object[] { "", x.Name })
                    {
                        CustomStyle = ListBoxColors,
                    };
                });

                AvatarListBox.Items.Clear();

                AvatarListBox.Items.AddRange(myItems);
                AvatarListBox.Items.AddRange(sharedItems);
            }

            AvatarListBox.Items = AvatarListBox.Items;
            Invalidate();
        }
    }
}
