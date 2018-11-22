using FSO.Client.UI.Controls;
using FSO.Client.UI.Framework;
using FSO.Common;
using FSO.Common.DataService.Model;
using FSO.Common.Rendering.Framework.Model;
using FSO.Common.Utils;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FSO.Client.UI.Panels.Neighborhoods
{
    public class UIRatingSummaryPanel : UICachedContainer
    {
        public UITextEdit Body;
        public UILabel NameLabel;
        public UIRatingDisplay Rating;
        public Binding<MayorRating> CurrentRating;

        private string NextMessage;
        private string NextName;
        private uint NextStars;
        public int FadeTimer = 0;
        public bool SwitchToNext;

        public string Message
        {
            get
            {
                return Body.CurrentText;
            }
            set
            {
                NextMessage = GameFacade.Emojis.EmojiToBB(value);
                SwitchToNext = true;
            }
        }

        public string Name
        {
            get
            {
                return NameLabel.Caption;
            }
            set
            {
                NextName = "- " + value;
                SwitchToNext = true;
            }
        }

        public uint HalfStars
        {
            get
            {
                return (uint)Rating.HalfStars;
            }
            set
            {
                NextStars = value;
                SwitchToNext = true;
            }
        }

        public UIRatingSummaryPanel()
        {
            Body = new UITextEdit();
            Body.TextStyle = Body.TextStyle.Clone();
            Body.TextStyle.Size = 9;
            Body.TextStyle.LineHeightModifier = -4;
            Body.Position = new Vector2(5, 5);
            Body.SetSize(118, 81);
            Body.MaxLines = 5;
            Body.Mode = UITextEditMode.ReadOnly;
            Body.Position = new Vector2(7, 3);
            Body.BBCodeEnabled = true;
            Body.RemoveMouseEvent();
            Add(Body);

            NameLabel = new UILabel();
            NameLabel.CaptionStyle = NameLabel.CaptionStyle.Clone();
            NameLabel.CaptionStyle.Size = 9;
            NameLabel.Position = new Vector2(122, 77);
            NameLabel.Alignment = TextAlignment.Right | TextAlignment.Top;
            NameLabel.Size = new Vector2(1, 1);
            NameLabel.CaptionStyle.Size = 9;
            Add(NameLabel);

            Rating = new UIRatingDisplay(true);
            Rating.Position = new Vector2(7, 80);
            Add(Rating);

            CurrentRating = new Binding<MayorRating>()
               .WithBinding(this, "Message", "MayorRating_Comment")
               .WithBinding(this, "HalfStars", "MayorRating_HalfStars")
               .WithBinding(this, "Name", "MayorRating_FromAvatar", (object id) =>
               {
                   return ((uint)id == 0) ? "Anon" : "unknown";
               });

            Size = new Vector2(128, 96);
        }

        public override void Update(UpdateState state)
        {
            base.Update(state);
            if (!Visible) FadeTimer = (SwitchToNext) ? FSOEnvironment.RefreshRate : 0;

            if (SwitchToNext)
            {
                if (FadeTimer++ >= FSOEnvironment.RefreshRate)
                {
                    FadeTimer = FSOEnvironment.RefreshRate;

                    if (NextStars == uint.MaxValue)
                    {
                        if (Body.Tooltip != null)
                        {
                            Body.CurrentText = "";
                            Body.Tooltip = null;
                            NameLabel.Caption = "";
                            Rating.HalfStars = (int)0;
                        }
                    }
                    else
                    {
                        Body.CurrentText = NextMessage;
                        Body.Tooltip = NextMessage;
                        NameLabel.Caption = NextName;
                        Rating.HalfStars = (int)NextStars;
                        SwitchToNext = false;
                    }
                }
            }
            else
            {
                if (FadeTimer > 0 && NextStars != uint.MaxValue) FadeTimer--;
            }

            Opacity = 1-(FadeTimer / (float)FSOEnvironment.RefreshRate);
        }
    }
}
