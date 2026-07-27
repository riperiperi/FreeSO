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
        public Texture2D LogoImage { get; set; }
        public UIButton MaxisButton { get; set; }
        public UILabel TitleLabel { get; set; }
        public UILabel EALabel { get; set; }
        public UIButton BackButton { get; set; }
        public UIButton OkButton { get; set; }
        public UIButton ExitButton { get; set; }
        public UICreditsPanel CreditsArea;

        // FreeSO Credits additions
        public UIImage TSOLogo { get; set; }
        public UIImage FSOLogo { get; set; }
        public UIButton TSOButton { get; set; }
        public UIButton FSOButton { get; set; }

        private string TSOTitle;
        private string FSOTitle;

        public Credits()
        {
            var gd = GameFacade.GraphicsDevice;
            var custom = Content.Content.Get().CustomUI;
            var ui = this.RenderScript("credits.uis");

            this.AddAt(0, new UIImage(BackgroundImage));
            this.Add(TSOLogo = ui.Create<UIImage>("TSOLogoImage"));

            this.Add(CenterAt(FSOLogo = new UIImage(custom.Get("credits_fsologo.png").Get(gd)), new Vector2(140, 194)));

            this.Add(CenterAt(TSOButton = new UIButton(custom.Get("credits_tsobutton.png").Get(gd)), new Vector2(140, 486)));
            this.Add(CenterAt(FSOButton = new UIButton(custom.Get("credits_fsobutton.png").Get(gd)), new Vector2(140, 320)));

            Add(CreditsArea = ui.Create<UICreditsPanel>("CreditsArea"));

            TSOTitle = ui.GetString("TitleLabelText");
            FSOTitle = GameFacade.Strings.GetString("f128", "122");

            SetCreditsType(true);

            TSOButton.OnButtonClick += (btn) => SetCreditsType(false);
            TSOButton.Tooltip = GameFacade.Strings.GetString("f128", "123");
            FSOButton.OnButtonClick += (btn) => SetCreditsType(true);
            FSOButton.Tooltip = GameFacade.Strings.GetString("f128", "124");

            BackButton.OnButtonClick += new ButtonClickDelegate(BackButton_OnButtonClick);
            OkButton.OnButtonClick += new ButtonClickDelegate(BackButton_OnButtonClick);
            ExitButton.OnButtonClick += ExitButton_OnButtonClick;

            GameResized();
        }

        private void SetCreditsType(bool fso)
        {
            TSOLogo.Visible = !fso;
            MaxisButton.Visible = !fso;
            EALabel.Visible = !fso;
            FSOButton.Visible = !fso;

            FSOLogo.Visible = fso;
            TSOButton.Visible = fso;

            TitleLabel.Caption = fso ? FSOTitle : TSOTitle;

            CreditsArea.Init(fso);
        }

        private UIElement CenterAt(UIElement elem, Vector2 point)
        {
            elem.Position = point - elem.Size / 2;

            return elem;
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
