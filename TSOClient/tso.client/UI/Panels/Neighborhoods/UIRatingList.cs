using FSO.Client.Controllers.Panels;
using FSO.Client.UI.Controls;
using FSO.Client.UI.Framework;
using FSO.Client.Utils;
using FSO.Common.DataService.Model;
using FSO.Common.Rendering.Framework.Model;
using FSO.Common.Utils;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Immutable;
using System.Linq;

namespace FSO.Client.UI.Panels.Neighborhoods
{
    public class UIRatingList : UIContainer
    {
        public UIImage InnerBackground;
        public UIListBox RatingList;
        public Binding<Avatar> CurrentAvatar { get; set; }

        public string Name
        {
            set
            {
                var dialog = this.Parent as UIAlert;
                if (dialog == null) return;
                dialog.Caption = GameFacade.Strings.GetString("f118", "23", new string[] { value });
                dialog.Body = GameFacade.Strings.GetString("f118", "24", new string[] { value });
                dialog.RefreshSize();
                dialog.InvalidateMatrix();
            }
        }

        public ImmutableList<uint> RatingIDs {
            set
            {
                var items = value.Reverse();
                RatingList.Items = items.Select(x =>
                    new UIListBoxItem(x, new UIFullRatingItem(x))
                    ).ToList();
                if (items.Count > 5)
                    RatingList.ScrollOffset = Math.Max(0, Math.Min(items.IndexOf(FocusID), items.Count - 5));
            }
        }

        private uint FocusID;
        private uint AvatarID;

        public UIRatingList(uint avatarID)
        {
            InnerBackground = new UIImage(GetTexture((ulong)0x7A400000001)).With9Slice(13, 13, 13, 13);
            InnerBackground.SetSize(475, 349);
            Add(InnerBackground);

            RatingList = new UIListBox()
            {
                RowHeight = 69,
                Size = new Vector2(475, 352)
            };
            RatingList.Columns.Add(new UIListBoxColumn() { Width = 475 });
            RatingList.ScrollbarGutter = 5;
            Add(RatingList);
            RatingList.ScrollbarImage = GetTexture(0x31000000001);
            RatingList.InitDefaultSlider();

            Size = new Vector2(490, 352);
            AvatarID = avatarID;

            ControllerUtils.BindController<RatingListController>(this);
        }

        public void Focus(uint focus)
        {
            FocusID = focus;
        }

        public override Rectangle GetBounds()
        {
            return new Rectangle(0, 0, 495, 356);
        }

        public override void Update(UpdateState state)
        {
            if (CurrentAvatar == null)
            {
                CurrentAvatar = new Binding<Avatar>()
                    .WithBinding(this, "RatingIDs", "Avatar_ReviewIDs")
                    .WithBinding(this, "Name", "Avatar_Name");
                var cont = ControllerUtils.BindController<RatingListController>(this);
                cont.SetAvatar(AvatarID);

                RatingList.SetSize(475, 349);
                RatingList.VisibleRows = 5;
                RatingList.PositionChildSlider();
            }
            base.Update(state);
        }

        public override void Removed()
        {
            FindController<RatingListController>()?.Dispose();

            foreach (var item in RatingList.Items)
            {
                ((UIFullRatingItem)item.Columns[0]).FindController<RatingSummaryController>()?.Dispose();
            }
        }
    }
}
