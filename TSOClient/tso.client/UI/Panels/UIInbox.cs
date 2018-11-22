/*
This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
If a copy of the MPL was not distributed with this file, You can obtain one at
http://mozilla.org/MPL/2.0/.
*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework.Graphics;
using FSO.Client.UI.Controls;
using FSO.Client.UI.Framework;
using FSO.Client.UI.Model;
using FSO.Client.UI.Screens;
using FSO.Client.Utils;
using FSO.Client.UI.Framework.Parser;
using FSO.Client.Controllers;
using FSO.Common.Utils;
using FSO.Files.Formats.tsodata;
using FSO.Client.UI.Panels.MessageStore;
using System.Globalization;

namespace FSO.Client.UI.Panels
{
    public class UIInbox : UIContainer
    {
        public UIListBox InboxListBox { get; set; }
        private UIImage Background;
        public Texture2D backgroundImage { get; set; }
        public UIButton CloseButton { get; set; }
        public UIButton MessageButton { get; set; }
        private UIInboxDropdown Dropdown;
        public UIButton DeleteButton { get; set; }

        //images
        //(message/vote/club/maxis/tso/house/roommate/call)
        public Texture2D letterIconImage { get; set; }
        public Texture2D voteIconImage { get; set; }
        public Texture2D clubIconImage { get; set; }
        public Texture2D maxisIconImage { get; set; }
        public Texture2D tsoIconImage { get; set; }
        public Texture2D houseIconImage { get; set; }
        public Texture2D neighborIconImage { get; set; }
        public Texture2D callIconImage { get; set; }

        //sort buttons
        public UIButton SubjectSortButton { get; set; }
        public UIButton NameSortButton { get; set; }
        public UIButton TimeSortButton { get; set; }
        public UIButton IconSortButton { get; set; }
        public UIButton AlertsSortButton { get; set; }

        public int SortMode = 2; //defaults to time

        public UISlider InboxSlider { get; set; }
        public UIButton InboxScrollUpButton { get; set; }
        public UIButton InboxScrollDownButton { get; set; }

        public Texture2D[] TypeIcons;
        public UIListBoxTextStyle[] TypeStyles;
        public bool SortOrder = false; //true inverts order.
        public Func<MessageItem, object>[] SortingFunctions;
        public UIButton[] SortButtons;

        public UILabel SummaryInfoTextLabel { get; set; }

        public UIInbox()
        {
            var script = this.RenderScript("messageinbox.uis");

            Background = new UIImage(backgroundImage);
            this.AddAt(0, Background);
            CloseButton.OnButtonClick += new ButtonClickDelegate(Close);
            UIUtils.MakeDraggable(Background, this);

            MessageButton.OnButtonClick += new ButtonClickDelegate(MessageButton_OnButtonClick);

            var msgStyleSim = script.Create<UIListBoxTextStyle>("SimMessageColors", InboxListBox.FontStyle);
            var msgStyleClub = script.Create<UIListBoxTextStyle>("ClubMessageColors", InboxListBox.FontStyle);
            var msgStyleCSR = script.Create<UIListBoxTextStyle>("CSRMessageColors", InboxListBox.FontStyle);
            var msgStyleServer = script.Create<UIListBoxTextStyle>("ServerMessageColors", InboxListBox.FontStyle);
            var msgStyleGame = script.Create<UIListBoxTextStyle>("GameMessageColors", InboxListBox.FontStyle);
            var msgStyleProperty = script.Create<UIListBoxTextStyle>("PropertyMessageColors", InboxListBox.FontStyle);
            var msgStyleNeighborhood = script.Create<UIListBoxTextStyle>("NeighborhoodMessageColors", InboxListBox.FontStyle);

            TypeIcons = new Texture2D[]
            {
                letterIconImage,
                voteIconImage,
                clubIconImage,
                maxisIconImage,
                tsoIconImage,
                houseIconImage,
                neighborIconImage,
                callIconImage
            };

            TypeStyles = new UIListBoxTextStyle[]
            {
                msgStyleSim,
                msgStyleServer,
                msgStyleClub,
                msgStyleCSR,
                msgStyleGame,
                msgStyleProperty,
                msgStyleNeighborhood,
                msgStyleServer
            };

            SortingFunctions = new Func<MessageItem, object>[]
            {
                x => x.Subject,
                x => x.SenderName,
                x => -x.Time,
                x => x.Type,
                x => x.Subtype,
            };

            SortButtons = new UIButton[]
            {
                SubjectSortButton,
                NameSortButton,
                TimeSortButton,
                IconSortButton,
                AlertsSortButton,
            };

            //swap these to give subject field a bit more space
            var posSwap = NameSortButton.Position;
            NameSortButton.Position = SubjectSortButton.Position;
            SubjectSortButton.Position = posSwap;

            for (int i=0; i<5; i++)
            {
                var btni = i;
                SortButtons[i].OnButtonClick += (btn) => { Sort(btni); };
            }

            Dropdown = new UIInboxDropdown();
            Dropdown.OnSearch += (query) =>
            {
                (Parent.Controller as InboxController)?.Search(query, false);
            };
            Dropdown.OnSelect += (id, name) =>
            {
                FindController<CoreGameScreenController>()?.WriteEmail(id, "");
            };
            Dropdown.X = 162;
            Dropdown.Y = 13;
            this.Add(Dropdown);

            SummaryInfoTextLabel.Alignment = TextAlignment.Center | TextAlignment.Middle;

            InboxListBox.AttachSlider(InboxSlider);
            InboxSlider.AttachButtons(InboxScrollUpButton, InboxScrollDownButton, 1f);

            InboxListBox.OnDoubleClick += OpenMessage;
            InboxListBox.Columns[2].TextureSelectedFrame = 1;
            InboxListBox.Columns[2].TextureHoverFrame = 2;

            DeleteButton.OnButtonClick += Delete;

            Sort(2);
            SortOrder = false;
        }

        private void Delete(UIElement button)
        {
            var item = InboxListBox.SelectedItem;
            if (item != null)
            {
                var msg = (MessageItem)item.Data;
                FindController<InboxController>()?.DeleteEmail(msg);
                UpdateInbox();
            }
        }

        private void Sort(int mode)
        {
            if (SortMode == mode)
                SortOrder = !SortOrder;
            else
                SortOrder = false;

            for (int i=0; i<5; i++)
            {
                SortButtons[i].Selected = mode == i;
            }

            SortMode = mode;
            UpdateInbox();
        }

        private void OpenMessage(UIElement button)
        {
            var item = InboxListBox.SelectedItem;
            if (item != null)
            {
                var msg = (MessageItem)item.Data;
                if (msg.ReadState == 0)
                {
                    msg.ReadState = 1;
                    UIMessageStore.Store.Save(msg);
                }
                FindController<CoreGameScreenController>()?.DisplayEmail(msg);
                UpdateInbox();
            }
        }

        public void UpdateInbox()
        {
            InboxListBox.Items.Clear();
            var inboxItems = FindController<InboxController>()?.GetSortedInbox(SortingFunctions[SortMode], SortOrder);
            if (inboxItems != null)
            {
                //populate list box
                foreach (var it in inboxItems) {
                    var time = new DateTime(it.Time);
                    time = time.ToLocalTime();
                    var dateformat = CultureInfo.CurrentCulture.DateTimeFormat.ShortDatePattern;
                    var tuple = MessagingController.CSTReplace(it.SenderName, it.Subject, it.Body);
                    var item = new UIListBoxItem(it, "", "", TypeIcons[it.Type], "", time.ToString("HH:mm ddd "+dateformat), "", tuple.Item2, "", tuple.Item1);
                    item.CustomStyle = TypeStyles[it.Type];
                    item.UseDisabledStyleByDefault = it.ReadState > 0;
                    InboxListBox.Items.Add(item);
                }
            }
            InboxListBox.Items = InboxListBox.Items;

            SummaryInfoTextLabel.Caption = GameFacade.Strings.GetString("200", "31", new string[] {
                InboxListBox.Items.Count.ToString(),
                InboxListBox.Items.Count(x => ((MessageItem)x.Data).ReadState == 0).ToString(),
            });
        }

        public void SetAvatarResults(List<GizmoAvatarSearchResult> results, bool exact)
        {
            if (exact)
            {
                if (results.Count == 0) HIT.HITVM.Get().PlaySoundEvent(UISounds.Error); //couldnt find avatar
                else
                {
                    //open the letter
                    FindController<CoreGameScreenController>()?.WriteEmail(results.First().Result.EntityId, "");
                }
            }
            else
            {
                Dropdown.SetResults(results);
            }
        }

        /// <summary>
        /// User wanted to compose a new message!
        /// </summary>
        private void MessageButton_OnButtonClick(UIElement button)
        {
            FindController<InboxController>()?.Search(Dropdown.MenuTextEdit.CurrentText, true);
        }

        /// <summary>
        /// User wanted to close the inbox.
        /// </summary>
        private void Close(UIElement button)
        {
            var screen = (CoreGameScreen)UIScreen.Current;
            UpdateInbox();
            screen.CloseInbox();
        }
    }

    public class UIInboxDropdown : UIContainer
    {
        public Texture2D backgroundCollapsedImage { get; set; }
        public Texture2D backgroundExpandedImage { get; set; }

        public UIButton DropDownButton { get; set; }

        public UIButton MenuScrollUpButton { get; set; }
        public UIButton MenuScrollDownButton { get; set; }
        public UISlider MenuSlider { get; set; }

        public UIListBox MenuListBox { get; set; }
        public UITextEdit MenuTextEdit { get; set; }

        public UIImage Background;
        public bool open;

        UIScript Script;

        public event Action<string> OnSearch;
        public event Action<uint, string> OnSelect;

        public UIInboxDropdown()
        {
            Script = this.RenderScript("messageinboxmenu.uis");
            Background = new UIImage(backgroundCollapsedImage);
            this.AddAt(0, Background);

            open = true;
            ToggleOpen();

            DropDownButton.OnButtonClick += new ButtonClickDelegate(DropDownButton_OnButtonClick);
            MenuTextEdit.OnEnterPress += Search;
            MenuTextEdit.OnChange += MenuTextEdit_OnChange;

            MenuListBox.AttachSlider(MenuSlider);
            MenuSlider.AttachButtons(MenuScrollUpButton, MenuScrollDownButton, 1f);

            MenuListBox.OnDoubleClick += SendMessageSelected;
        }

        private void SendMessageSelected(UIElement button)
        {
            var selected = MenuListBox.SelectedItem;
            if (selected == null) return;
            var data = ((Common.DatabaseService.Model.SearchResponseItem)selected.Data);
            OnSelect(data.EntityId, data.Name);
        }

        private void MenuTextEdit_OnChange(UIElement element)
        {

        }

        private void Search(UIElement element)
        {
            if (element != null && !open) ToggleOpen();
            else OnSearch(MenuTextEdit.CurrentText);
        }

        void DropDownButton_OnButtonClick(UIElement button)
        {
            ToggleOpen();
        }

        public void ToggleOpen()
        {
            if (open)
            {
                Background.Texture = backgroundCollapsedImage;
                Background.SetSize(backgroundCollapsedImage.Width, backgroundCollapsedImage.Height);
                MenuListBox.Items.Clear();
            }
            else
            {
                Background.Texture = backgroundExpandedImage;
                Background.SetSize(backgroundExpandedImage.Width, backgroundExpandedImage.Height);
                UIListBoxTextStyle Style = Script.Create<UIListBoxTextStyle>("SimMessageColors", MenuListBox.FontStyle);
                Search(null);

                //TODO: This should eventually be made to show only a player's friends.
                /*foreach (UISim Avatar in Network.NetworkFacade.AvatarsInSession)
                {
                    UIListBoxItem AvatarItem = new UIListBoxItem(Avatar.GUID, Avatar.Name);
                    AvatarItem.CustomStyle = Style;
                    MenuListBox.Items.Add(AvatarItem);
                }*/
            }

            open = !open;
            MenuSlider.Visible = open;
            MenuScrollUpButton.Visible = open;
            MenuScrollDownButton.Visible = open;
            MenuListBox.Visible = open;
        }

        public void SetResults(List<GizmoAvatarSearchResult> results)
        {
            MenuListBox.Items.Clear();
            if (results != null)
            {
                MenuListBox.Items.AddRange(results.Select(x =>
                {
                    return new UIListBoxItem(x.Result, new object[] { x.Result.Name })
                    {
                        //CustomStyle = ListBoxColors,
                        UseDisabledStyleByDefault = new ValuePointer(x, "IsOffline")
                    };
                }));
            }
            MenuListBox.Items = MenuListBox.Items;
        }
    }
}
