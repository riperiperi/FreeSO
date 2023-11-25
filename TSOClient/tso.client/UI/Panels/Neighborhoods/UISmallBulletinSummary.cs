using Microsoft.Xna.Framework;

namespace FSO.Client.UI.Panels.Neighborhoods
{
    public class UISmallBulletinSummary : UIMediumBulletinSummary
    {
        public UISmallBulletinSummary(bool system) : base("bulletin_small")
        {
            TitleLabel.X -= 3;
            Body.Visible = false;

            TitleLabel.CaptionStyle.Color = (system) ? new Color(0, 64, 32) : new Color(0, 51, 102);
            DateLabel.CaptionStyle.Color = (system) ? new Color(0, 102, 51) : new Color(0, 70, 140);

            if (!system) HSVMod = new Color(104-30, 255, 255, 255);

            if (system)
            {
                DateLabel.Alignment = Framework.TextAlignment.Center;
                DateLabel.Position = new Vector2(22, 35);
                DateLabel.Size = new Vector2(100, 18);

                PersonButton.Visible = false;
                DateLabel.Caption = "24/12/2018 6:23am";
            } else
            {
                DateLabel.Y -= 60;
                PersonButton.Y -= 60;
            }
        }
    }
}
