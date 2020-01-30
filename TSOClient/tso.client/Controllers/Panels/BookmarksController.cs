using FSO.Client.UI.Panels;
using FSO.Common.DataService;
using FSO.Common.DataService.Model;
using FSO.Common.Utils;
using FSO.Server.DataService.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FSO.Client.Controllers.Panels
{
    public class BookmarksController : IDisposable
    {
        private Network.Network Network;
        private IClientDataService DataService;
        private UIBookmarks View;
        private Binding<Avatar> Binding;
        private BookmarkType CurrentType = BookmarkType.AVATAR;

        public BookmarksController(UIBookmarks view, IClientDataService dataService, Network.Network network)
        {
            this.Network = network;
            this.DataService = dataService;
            this.View = view;
            this.Binding = new Binding<Avatar>().WithMultiBinding(x => { RefreshResults(); }, "Avatar_BookmarksVec");

            Init();
        }

        private void Init()
        {
            DataService.Get<Avatar>(Network.MyCharacter).ContinueWith(x =>
            {
                Binding.Value = x.Result;
            });
        }

        public void ChangeType(BookmarkType type)
        {
            CurrentType = type;
            RefreshResults();
        }

        public void RefreshResults()
        {
            var list = new List<BookmarkListItem>();
            if(Binding.Value != null && Binding.Value.Avatar_BookmarksVec != null)
            {
                var bookmarks = Binding.Value.Avatar_BookmarksVec.Where(x => x.Bookmark_Type == (byte)CurrentType).ToList();
                var enriched = DataService.EnrichList<BookmarkListItem, Bookmark, Avatar>(bookmarks, x => x.Bookmark_TargetID, (bookmark, avatar) =>
                {
                    return new BookmarkListItem {
                        Avatar = avatar,
                        Bookmark = bookmark
                    };
                });

                list = enriched;
            }

            View.SetResults(list);
        }

        /**
            var list = new List<BookmarkListItem>();

            if(Binding.Value != null && Binding.Value.Avatar_BookmarksVec != null)
            {
                var bookmarks = Binding.Value.Avatar_BookmarksVec;
                var ids = bookmarks.Select(x => x.Bookmark_TargetID);
                var avatars = 
            }**/




        public void Toggle()
        {
            if (View.Visible)
            {
                Close();
            }
            else
            {
                Show();
            }
        }

        public void Close()
        {
            View.Visible = false;
        }

        public void Show()
        {
            RefreshResults();
            View.Parent.Add(View);
            View.Visible = true;
        }

        public void Dispose()
        {
        }
    }
}
