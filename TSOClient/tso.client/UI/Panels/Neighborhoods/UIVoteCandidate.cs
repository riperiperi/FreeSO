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
    public class UIVoteCandidate : UIContainer
    {
        private UIImage Background;
        private UIImage AvaBackground;
        private UIBigPersonButton Avatar;
        private UILabel NameLabel;
        private UILabel SubtitleLabel;
        private UIRatingDisplay Rating;
        private UITextEdit MessageLabel;
        private UIButton CheckButton;

        public UIVoteCandidate(bool alignment)
        {
            var ui = Content.Content.Get().CustomUI;
            var gd = GameFacade.GraphicsDevice;

            Background = new UIImage(ui.Get("vote_bg_9slice.png").Get(gd)).With9Slice(15, 15, 15, 15);
            Background.Position = new Vector2(alignment ? 5 : 32, 16);
            Background.SetSize(539, 74);
            Add(Background);

            AvaBackground = new UIImage(ui.Get("vote_ava_bg.png").Get(gd));
            AvaBackground.Position = new Vector2(alignment ? 499 : 0, 0);
            Add(AvaBackground);

            CheckButton = new UIButton(ui.Get("vote_check.png").Get(gd));
            CheckButton.Position = new Vector2(alignment ? 15 : 518, 32);
            Add(CheckButton);

            Avatar = new UIBigPersonButton();
            Avatar.ScaleX = Avatar.ScaleY = 108f / 170;
            Add(Avatar);
            Avatar.Position = new Vector2(alignment ? 502 : 2, 2);

            //range: [75, 501]

            NameLabel = new UILabel();
            NameLabel.Position = new Vector2(75 + 2, 17);
            NameLabel.Size = new Vector2(426 - 4, 1);
            NameLabel.CaptionStyle = NameLabel.CaptionStyle.Clone();
            NameLabel.CaptionStyle.Shadow = true;
            NameLabel.CaptionStyle.Color = Color.White;
            NameLabel.CaptionStyle.Size = 16;
            NameLabel.Alignment = alignment ? TextAlignment.Right : TextAlignment.Left;
            Add(NameLabel);

            SubtitleLabel = new UILabel();
            SubtitleLabel.Position = new Vector2(75 + 6, 41);
            SubtitleLabel.Size = new Vector2(426 - 12, 1);
            SubtitleLabel.Alignment = alignment ? TextAlignment.Right : TextAlignment.Left;
            Add(SubtitleLabel);

            MessageLabel = new UITextEdit();
            MessageLabel.Position = new Vector2(75 + 4, 60);
            MessageLabel.Size = new Vector2(426 - 8, 29);
            MessageLabel.Mode = UITextEditMode.ReadOnly;
            MessageLabel.Alignment = TextAlignment.Top | (alignment ? TextAlignment.Right : TextAlignment.Left);
            MessageLabel.TextStyle = MessageLabel.TextStyle.Clone();
            MessageLabel.TextStyle.Size = 7;
            MessageLabel.TextStyle.Color = Color.White;
            Add(MessageLabel);

            Rating = new UIRatingDisplay(true);
            Rating.Visible = false;
            Add(Rating);

            NameLabel.Caption = "CHESTER BESTERFESTERTEST";
            SubtitleLabel.Caption = "Running for 2nd Term";
            MessageLabel.CurrentText = "If you vote for me, I personally vow to avoid polluting the water supply. It will be hard, but I believe that with your votes I might find any restraint whatsoever.";
            Avatar.AvatarId = 887;
        }

        public override void Update(UpdateState state)
        {
            Invalidate();
            Avatar.ScaleX = Avatar.ScaleY = 108f / 170;
            base.Update(state);
        }

        public void ShowCandidate(NhoodCandidate cand)
        {
            Avatar.AvatarId = cand.ID;
            NameLabel.Caption = cand.Name;
            MessageLabel.CurrentText = cand.Message;

            if (cand.TermNumber > 0)
                SubtitleLabel.Caption = GameFacade.Strings.GetString("f118", "7", new string[] { cand.TermNumber.ToString() }); //consecutive term
            else if (cand.LastNhoodID != 0)
                SubtitleLabel.Caption = GameFacade.Strings.GetString("f118", "8", new string[] { cand.LastNhoodName }); //ex-mayor
            else
                SubtitleLabel.Caption = GameFacade.Strings.GetString("f118", "6"); //newcomer
        }
    }
}
