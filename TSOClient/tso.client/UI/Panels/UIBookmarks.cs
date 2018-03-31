using FSO.Client.Controllers;
using FSO.Client.Controllers.Panels;
using FSO.Client.UI.Controls;
using FSO.Client.UI.Framework;
using FSO.Client.Utils;
using FSO.Common.DataService.Model;
using FSO.Common.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FSO.Client.UI.Panels
{
    public class UIBookmarks : UIContainer
    {
        public UIButton CloseButton { get; set; }
        public UIListBox BookmarkListBox { get; set; }
        public UIListBoxTextStyle BookmarkListBoxColors { get; set; }

        public UISlider BookmarkListSlider { get; set; }
        public UIButton BookmarkListScrollUpButton { get; set; }
        public UIButton BookmarkScrollDownButton { get; set; }
        public UIButton SimsTabButton { get; set; }
        public UIButton IgnoreTabButton { get; set; }

        public UIImage SimsTab { get; set; }
        public UIImage IgnoreTab { get; set; }

        public Binding<Avatar> Binding;

        public UIBookmarks()
        {
            var ui = this.RenderScript("bookmarks.uis");

            var background = ui.Create<UIImage>("BookmarkBackground");
            SimsTab = ui.Create<UIImage>("SimsTab");
            AddAt(0, SimsTab);
            IgnoreTab = ui.Create<UIImage>("IgnoreTab");
            AddAt(0, IgnoreTab);
            IgnoreTab.Visible = false;

            AddAt(0, ui.Create<UIImage>("Tab1Background"));
            AddAt(0, ui.Create<UIImage>("Tab2Background"));
            AddAt(0, ui.Create<UIImage>("ListBoxBackground"));
            AddAt(0, background);


            UIUtils.MakeDraggable(background, this, true);

            BookmarkListSlider.AttachButtons(BookmarkListScrollUpButton, BookmarkScrollDownButton, 1);
            BookmarkListBox.AttachSlider(BookmarkListSlider);
            BookmarkListBox.OnDoubleClick += BookmarkListBox_OnDoubleClick;
            BookmarkListBoxColors = ui.Create<UIListBoxTextStyle>("BookmarkListBoxColors", BookmarkListBox.FontStyle);
            CloseButton.OnButtonClick += CloseButton_OnButtonClick;
            IgnoreTabButton.OnButtonClick += (btn) => { ChangeType(BookmarkType.IGNORE_AVATAR); };
            SimsTabButton.OnButtonClick += (btn) => { ChangeType(BookmarkType.AVATAR); };
        }

        private void ChangeType(BookmarkType type)
        {
            FindController<BookmarksController>().ChangeType(type);

            var bookmark = type == BookmarkType.AVATAR;
            SimsTabButton.Selected = bookmark;
            SimsTab.Visible = bookmark;
            IgnoreTabButton.Selected = !bookmark;
            IgnoreTab.Visible = !bookmark;
        }

        private void BookmarkListBox_OnDoubleClick(UIElement button)
        {
            if (BookmarkListBox.SelectedItem == null) { return; }
            var item = BookmarkListBox.SelectedItem.Data as BookmarkListItem;
            if (item == null) { return; }
            FindController<CoreGameScreenController>().ShowPersonPage(item.Avatar.Avatar_Id);
        }

        public void SetResults(List<BookmarkListItem> items)
        {
            BookmarkListBox.Items = items.Select(x =>
            {
                return new UIListBoxItem(x, new object[] { null, new ValuePointer(x.Avatar, "Avatar_Name") }) {
                    CustomStyle = BookmarkListBoxColors,
                    UseDisabledStyleByDefault = new ValuePointer(x.Avatar, "Avatar_IsOffline")
                };
            }).ToList();
        }

        private void CloseButton_OnButtonClick(UIElement button)
        {
            FindController<BookmarksController>().Close();
        }
    }


    public class BookmarkListItem
    {
        public Avatar Avatar;
        public Bookmark Bookmark;
    }
}
