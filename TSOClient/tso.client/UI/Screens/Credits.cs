using FSO.Client.UI.Framework;
using Microsoft.Xna.Framework.Graphics;
using FSO.Client.UI.Controls;
using FSO.Client.UI.Panels;
using Microsoft.Xna.Framework;

namespace FSO.Client.UI.Screens
{
    public class Credits : UIContainer
    {
        public Texture2D BackgroundImage { get; set; }
        public UIButton BackButton { get; set; }
        public UIButton OkButton { get; set; }
        public UIButton ExitButton { get; set; }
        public UICreditsPanel CreditsArea;

        public Credits()
        {
            var ui = this.RenderScript("credits.uis");

            this.AddAt(0, new UIImage(BackgroundImage));
            this.Add(ui.Create<UIImage>("TSOLogoImage"));

            Add(CreditsArea = ui.Create<UICreditsPanel>("CreditsArea"));

            BackButton.OnButtonClick += new ButtonClickDelegate(BackButton_OnButtonClick);
            OkButton.OnButtonClick += new ButtonClickDelegate(BackButton_OnButtonClick);
            ExitButton.OnButtonClick += ExitButton_OnButtonClick;

            GameResized();
        }

        private void ExitButton_OnButtonClick(UIElement button)
        {
            UIScreen.ShowDialog(new UIExitDialog(), true);
        }

        public override void GameResized()
        {
            base.GameResized();
            Position = new Vector2((GlobalSettings.Default.GraphicsWidth - 800) / 2, (GlobalSettings.Default.GraphicsHeight - 600) / 2);
            InvalidateMatrix();
        }

        void BackButton_OnButtonClick(UIElement button)
        {
            UIScreen.RemoveDialog(this);
        }
    }
}
