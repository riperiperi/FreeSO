using FSO.Client.UI.Framework;
using Microsoft.Xna.Framework.Graphics;
using FSO.Client.UI.Controls;

namespace FSO.Client.UI.Screens
{
    public class Credits : GameScreen
    {
        public Texture2D BackgroundImage { get; set; }
        public UIButton BackButton { get; set; }
        public UIButton OkButton { get; set; }

        public Credits()
        {
            var ui = this.RenderScript("credits.uis");

            this.X = (float)((double)(ScreenWidth - 800)) / 2;
            this.Y = (float)((double)(ScreenHeight - 600)) / 2;

            this.AddAt(0, new UIImage(BackgroundImage));
            this.Add(ui.Create<UIImage>("TSOLogoImage"));


            BackButton.OnButtonClick += new ButtonClickDelegate(BackButton_OnButtonClick);
            OkButton.OnButtonClick += new ButtonClickDelegate(BackButton_OnButtonClick);
        }

        void BackButton_OnButtonClick(UIElement button)
        {
            GameFacade.Screens.RemoveScreen(this);
        }
    }
}
