using FSO.Files.Formats.tsodata;
using Microsoft.Xna.Framework;

namespace FSO.Client.UI.Panels.Neighborhoods
{
    public class UILargeBulletinSummary : UIMediumBulletinSummary
    {
        public UILotThumbButtonAuto LotThumb;

        public UILargeBulletinSummary() : base("bulletin_large")
        {
            var ui = Content.Content.Get().CustomUI;
            var gd = GameFacade.GraphicsDevice;

            TitleLabel.CaptionStyle.Size = 12;
            TitleLabel.Size = new Vector2(168, 39);
            TitleLabel.Position = new Vector2(36, 5);

            Body.CaptionStyle.Size = 9;
            Body.CaptionStyle.LineHeightModifier = -3;
            Body.MaxLines = 9;
            Body.Position = new Vector2(36, 44);
            Body.Size = new Vector2(167, 65);

            PersonButton.Position = new Vector2(186, 157);
            DateLabel.Position = new Vector2(32, 165);
            PromotedStar.Position = new Vector2(190, -17);

            LotThumb = new UILotThumbButtonAuto().WithDefaultClick();
            var lottex = ui.Get("bulletin_lot_btn.png").Get(gd);
            LotThumb.Init(lottex, lottex);
            LotThumb.Position = new Vector2(30, 107);
            Add(LotThumb);

            OffsetMultiplier = 0.75f;
        }

        public override void SetItem(BulletinItem item)
        {
            base.SetItem(item);
            if (item == null)
            {
                Visible = false;
            }
            else
            {
                if (item.LotID != 0)
                {
                    Body.MaxLines = 5;
                    DateLabel.Alignment = Framework.TextAlignment.Top | Framework.TextAlignment.Right;
                    DateLabel.Position = new Vector2(32 + 168, 165);
                    PersonButton.Position = new Vector2(186, 142);
                    LotThumb.LotId = item.LotID;
                    LotThumb.Visible = true;
                }
                else
                {
                    Body.MaxLines = 9;
                    DateLabel.Alignment = Framework.TextAlignment.Top | Framework.TextAlignment.Left;
                    DateLabel.Position = new Vector2(32, 165);
                    PersonButton.Position = new Vector2(186, 157);
                    LotThumb.Visible = false;
                }
            }
        }
    }
}
