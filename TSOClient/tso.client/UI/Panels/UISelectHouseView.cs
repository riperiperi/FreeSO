using FSO.Client.UI.Framework;
using FSO.Client.UI.Controls;
using Microsoft.Xna.Framework.Graphics;

namespace FSO.Client.UI.Panels
{
    public class UISelectHouseView : UIContainer
    {
        public event HouseViewSelection OnModeSelection;

        private UIImage Background;
        public UIButton WallsDownButton { get; set; }
        public UIButton WallsUpButton { get; set; }
        public UIButton WallsCutawayButton { get; set; }
        public UIButton RoofButton { get; set; }
        public Texture2D BackgroundImage { get; set; }

        public UISelectHouseView()
        {
            var script = this.RenderScript("selecthouseview.uis");

            Background = new UIImage(BackgroundImage);
            this.AddAt(0, Background);

            WallsDownButton.OnButtonClick += new ButtonClickDelegate(WallsDownClick);
            WallsUpButton.OnButtonClick += new ButtonClickDelegate(WallsUpClick);
            WallsCutawayButton.OnButtonClick += new ButtonClickDelegate(WallsCutClick);
            RoofButton.OnButtonClick += new ButtonClickDelegate(RoofClick);
        }

        void RoofClick(UIElement button)
        {
            OnModeSelection(3);
        }

        void WallsCutClick(UIElement button)
        {
            OnModeSelection(1);
        }

        void WallsUpClick(UIElement button)
        {
            OnModeSelection(2);
        }

        void WallsDownClick(UIElement button)
        {
            OnModeSelection(0);
        }

    }

    public delegate void HouseViewSelection(int mode);
}
