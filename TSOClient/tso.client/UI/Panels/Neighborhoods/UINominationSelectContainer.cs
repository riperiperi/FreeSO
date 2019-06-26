using FSO.Client.UI.Controls;
using FSO.Client.UI.Framework;
using FSO.Common.Rendering.Framework.Model;
using FSO.Server.Protocol.Electron.Packets;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FSO.Client.UI.Panels.Neighborhoods
{
    public class UINominationSelectContainer : UIContainer
    {
        public UIListBox RoommateListBox { get; set; }
        public UIListBoxTextStyle RoommateListBoxColors { get; set; }
        private UIInboxDropdown Dropdown;

        public UISlider RoommateListSlider { get; set; }
        public UIButton RoommateListScrollUpButton { get; set; }
        public UIButton RoommateScrollDownButton { get; set; }

        public UILabel DonatorsLabel { get; set; }

        public UITextBox SearchBox;
        private NhoodCandidateList Candidates;
        private bool NonPerson;

        public NhoodCandidate SelectedCandidate
        {
            get
            {
                return RoommateListBox.SelectedItem?.Data as NhoodCandidate;
            }
        }
        public UINominationSelectContainer(NhoodCandidateList candidates) : this(candidates, false)
        {

        }

        public UINominationSelectContainer(NhoodCandidateList candidates, bool nonPerson) 
        {
            NonPerson = nonPerson;
            var ui = RenderScript("fsodonatorlist.uis");
            var listBg = ui.Create<UIImage>("ListBoxBackground");
            AddAt(0, listBg);
            listBg.With9Slice(25, 25, 25, 25);
            listBg.Width += 110;
            listBg.Height += 50;

            RoommateListSlider.AttachButtons(RoommateListScrollUpButton, RoommateScrollDownButton, 1);
            RoommateListBox.AttachSlider(RoommateListSlider);
            RoommateListBox.Columns[1].Alignment = Framework.TextAlignment.Left | Framework.TextAlignment.Middle;

            DonatorsLabel.Caption = "Search";
            DonatorsLabel.CaptionStyle = DonatorsLabel.CaptionStyle.Clone();
            DonatorsLabel.CaptionStyle.Shadow = true;
            DonatorsLabel.Y -= 26;

            foreach (var child in GetChildren())
            {
                child.Y -= 45;
            }

            SearchBox = new UITextBox();
            SearchBox.X = RoommateListBox.X;
            SearchBox.Y = 20;
            SearchBox.SetSize(listBg.Width, 25);
            SearchBox.OnChange += SearchBox_OnChange;
            Add(SearchBox);

            Candidates = candidates;
            UpdateCandidateList(candidates);
        }

        private void SearchBox_OnChange(UIElement element)
        {
            UpdateCandidateList(Candidates);
        }

        public override void Update(UpdateState state)
        {
            base.Update(state);
        }

        public override Rectangle GetBounds()
        {
            return new Rectangle(0, 0, 405, 330);
        }

        public NhoodRequest GetRequest(NhoodRequest initial)
        {
            if (SelectedCandidate == null) return null;
            if (NonPerson)
            {
                return new NhoodRequest()
                {
                    Type = NhoodRequestType.FREE_VOTE,
                    TargetNHood = SelectedCandidate.ID
                };
            }
            else
            {
                return new NhoodRequest()
                {
                    Type = NhoodRequestType.NOMINATE,
                    TargetNHood = initial.TargetNHood,
                    TargetAvatar = SelectedCandidate.ID
                };
            }
        }

        public void UpdateCandidateList(NhoodCandidateList candidates)
        {
            IEnumerable<NhoodCandidate> sims = candidates.Candidates;
            var searchString = SearchBox.CurrentText.ToLowerInvariant();
            if (SearchBox.CurrentText != "") sims = sims.Where(x => x.Name.ToLowerInvariant().Contains(searchString));
            RoommateListBox.Items = sims.OrderBy(x => x.Name).Select(x =>
            {
                UIPersonButton personBtn = null;
                if (!NonPerson)
                {
                    personBtn = new UIPersonButton()
                    {
                        AvatarId = x.ID,
                        FrameSize = UIPersonButtonSize.SMALL
                    };
                    personBtn.LogicalParent = this;
                }

                UIRatingDisplay rating = null;
                if (x.Rating != uint.MaxValue)
                {
                    rating = new UIRatingDisplay(true);
                    rating.LogicalParent = this;
                    rating.DisplayStars = x.Rating / 100f;
                    rating.LinkAvatar = x.ID;
                }
                return new UIListBoxItem(
                    x,
                    (object)personBtn ?? "",
                    x.Name,
                    "",
                    (object)rating ?? ""
                    );
            }).ToList();
        }
    }
}
