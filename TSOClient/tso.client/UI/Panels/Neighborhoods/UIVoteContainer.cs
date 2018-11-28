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
    public class UIVoteContainer : UIContainer
    {
        public UIButton VoteButton;
        public UIImage VoteImage;

        public NhoodCandidateList Candidates;
        public List<UIVoteCandidate> Elems = new List<UIVoteCandidate>();

        public event Action<uint> OnVote;
        public uint SelectedID = 0;

        public string SelectedName
        {
            get
            {
                return Candidates.Candidates.FirstOrDefault(x => x.ID == SelectedID)?.Name ?? "???";
            }
        }

        public UIVoteContainer(NhoodCandidateList cand)
        {
            Candidates = cand;
            Init(cand.Candidates.Count);

            for (int i = 0; i < Elems.Count; i++)
            {
                Elems[i].ShowCandidate(cand.Candidates[i]);
            }
        }

        public UIVoteContainer()
        {
            Init(5);
        }

        public void InjectClose()
        {
            var parent = (UIDialog)Parent;
            parent.CloseBg = new UIImage(GetTexture((ulong)1477468749826L));
            parent.CloseButton = new UIButton(GetTexture((ulong)8697308774401L));
            parent.Add(parent.CloseBg);
            parent.Add(parent.CloseButton);

            parent.CloseButton.OnButtonClick += CloseButton_OnButtonClick;

            parent.SetSize(parent.Width, parent.Height); //refresh
        }

        public NhoodRequest MakeRequest(NhoodRequest request)
        {
            return new NhoodRequest()
            {
                Type = NhoodRequestType.VOTE,
                TargetAvatar = SelectedID,
                TargetNHood = request.TargetNHood
            };
        }

        private void CloseButton_OnButtonClick(UIElement button)
        {
            OnVote?.Invoke(0);
        }

        public void Init(int candidates)
        {
            var ui = Content.Content.Get().CustomUI;
            var gd = GameFacade.GraphicsDevice;

            for (int i = 0; i < candidates; i++)
            {
                var elem = new UIVoteCandidate(i % 2 == 1, this);
                elem.Position = new Vector2(0, 98 * i);
                Add(elem);
                Elems.Add(elem);
            }

            VoteButton = new UIButton(ui.Get("vote_big_btn.png").Get(gd));
            VoteButton.Width = 200;
            VoteButton.Caption = GameFacade.Strings.GetString("f118", "21");
            VoteButton.CaptionStyle = VoteButton.CaptionStyle.Clone();
            VoteButton.CaptionStyle.Color = Color.White;
            VoteButton.CaptionStyle.Shadow = true;
            VoteButton.CaptionStyle.Size = 22;
            VoteButton.Position = new Vector2((575 - 200) / 2, 98 * candidates + 8);
            Add(VoteButton);

            VoteButton.OnButtonClick += VoteButton_OnButtonClick;

            VoteImage = new UIImage(ui.Get("vote_icon.png").Get(gd));
            VoteImage.Position = VoteButton.Position + new Vector2(float.Parse(GameFacade.Strings.GetString("f118", "22")), 12);
            VoteButton.Disabled = true;
            Add(VoteImage);
        }

        public void SetSelected(UIVoteCandidate cand)
        {
            if (cand != null) SelectedID = cand.AvatarID;
            else SelectedID = 0;
            foreach (var cand2 in Elems)
            {
                cand2.SetChecked(cand == cand2);
            }
            VoteButton.Disabled = SelectedID == 0;
        }

        private void VoteButton_OnButtonClick(UIElement button)
        {
            OnVote?.Invoke(SelectedID);
        }

        public override void Update(UpdateState state)
        {
            base.Update(state);
            if (VoteButton.CurrentFrame == 1) VoteImage.BlendColor = new Color(1, 53, 115);
            else VoteImage.BlendColor = Color.White;
        }

        public override Rectangle GetBounds()
        {
            return new Rectangle(0, 0, 575, 98 * Elems.Count + 24);
        }
    }
}
