using FSO.Client.UI.Controls;
using FSO.Client.UI.Framework;
using FSO.Common;
using FSO.Files.Formats.tsodata;
using Microsoft.Xna.Framework;
using System;

namespace FSO.Client.UI.Panels.Neighborhoods
{
    public class UIMediumBulletinSummary : UIAbstractStickyContainer
    {
        public UILabel TitleLabel;
        public UILabel Body;

        public UILabel DateLabel;
        public UIPersonButton PersonButton;

        public BulletinItem Item;
        private Color TitleColor = new Color(102, 0, 0);
        private Color BodyColor = new Color(140, 35, 0);

        protected UIImage PromotedStar;

        public UIMediumBulletinSummary() : this("bulletin_med")
        {

        }

        public UIMediumBulletinSummary(bool community) : this("bulletin_med")
        {
            if (community)
            {
                HSVMod = new Color(104, 255, 255, 255);
                TitleLabel.CaptionStyle.Color = new Color(0, 51, 102);
                Body.CaptionStyle.Color = new Color(0, 70, 140);
                DateLabel.CaptionStyle.Color = new Color(0, 51, 102);
            }
        }

        public virtual void SetItem(BulletinItem item)
        {
            Item = item;
            if (item == null)
            {
                Visible = false;
            }
            else
            {
                Visible = true;
                TitleLabel.Caption = item.Subject;
                Body.Caption = item.Body;
                if (item.SenderID == 0)
                {
                    PersonButton.Visible = false;
                }
                else
                {
                    PersonButton.Visible = true;
                    PersonButton.AvatarId = item.SenderID;
                }
                if (item.Time == 0) DateLabel.Caption = GameFacade.Strings.GetString("f120", "38");
                else DateLabel.Caption = ClientEpoch.ToDate((uint)item.Time).ToLocalTime().ToShortDateString();
                PromotedStar.Visible = (item.Flags & BulletinFlags.PromotedByMayor) > 0;
            }
        }

        public UIMediumBulletinSummary(string type) : base(type)
        {
            var ui = Content.Content.Get().CustomUI;
            var gd = GameFacade.GraphicsDevice;

            var titleCaption = TextStyle.DefaultTitle.Clone();
            titleCaption.Size = 9;
            titleCaption.Color = TitleColor;
            TitleLabel = new UILabel()
            {
                Wrapped = true,
                Size = new Vector2(107, 30),
                Alignment = TextAlignment.Center | TextAlignment.Middle,
                Position = new Vector2(22, 4),
                CaptionStyle = titleCaption,
                Caption = "",
                MaxLines = 2,
            };
            Add(TitleLabel);

            var bodyCaption = TextStyle.DefaultTitle.Clone();
            bodyCaption.Size = 8;
            bodyCaption.Color = BodyColor;
            bodyCaption.LineHeightModifier = -2;

            Body = new UILabel()
            {
                Wrapped = true,
                Size = new Vector2(108, 57),
                Position = new Vector2(21, 35),
                CaptionStyle = bodyCaption,
                Caption = "",
                MaxLines = 5,
            };
            Add(Body);

            var dateCaption = titleCaption.Clone();
            dateCaption.Size = 8;

            DateLabel = new UILabel()
            {
                Position = new Vector2(22, 97),
                CaptionStyle = dateCaption,
                Caption = DateTime.Now.ToShortDateString(),
                Alignment = TextAlignment.Left | TextAlignment.Top,
                Size = Vector2.One
            };
            Add(DateLabel);

            PersonButton = new UIPersonButton()
            {
                FrameSize = UIPersonButtonSize.SMALL,
                Position = new Vector2(109, 93),
            };
            Add(PersonButton);

            PromotedStar = new UIImage(ui.Get((type == "bulletin_large")? "bulletin_promote_l.png":"bulletin_promote_s.png").Get(gd)) { Position = new Vector2(114, -12) };
            PromotedStar.UseTooltip();
            PromotedStar.Tooltip = GameFacade.Strings.GetString("f120", "17");
            PromotedStar.Visible = false;
            DynamicOverlay.Add(PromotedStar);
        }
    }
}
