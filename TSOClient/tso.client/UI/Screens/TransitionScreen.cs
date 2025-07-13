using FSO.Client.UI.Framework;
using FSO.Client.UI.Controls;
using FSO.Client.UI.Panels;

namespace FSO.Client.UI.Screens
{
    public class TransitionScreen : GameScreen
    {
        private UISetupBackground m_Background;
        private UILoginProgress m_LoginProgress;
        private UIButton SandboxModeButton;

        /// <summary>
        /// Creates a new CityTransitionScreen.
        /// </summary>
        /// <param name="SelectedCity">The city being transitioned to.</param>
        /// <param name="CharacterCreated">If transitioning from CreateASim, this should be true.
        /// A CharacterCreateCity packet will be sent to the CityServer. Otherwise, this should be false.
        /// A CityToken packet will be sent to the CityServer.</param>
        public TransitionScreen()
        {
            /** Background image **/
            GameFacade.Cursor.SetCursor(Common.Rendering.Framework.CursorType.Hourglass);
            m_Background = new UISetupBackground();

            var lbl = new UILabel();
            lbl.Caption = "Version " + GlobalSettings.Default.ClientVersion;
            lbl.X = 20;
            lbl.Y = 558;
            m_Background.BackgroundCtnr.Add(lbl);
            this.Add(m_Background);

            m_LoginProgress = new UILoginProgress();
            m_LoginProgress.X = (ScreenWidth - (m_LoginProgress.Width + 20));
            m_LoginProgress.Y = (ScreenHeight - (m_LoginProgress.Height + 20));
            m_LoginProgress.Opacity = 0.9f;
            this.Add(m_LoginProgress);
        }

        public override void GameResized()
        {
            base.GameResized();
            m_LoginProgress.X = (ScreenWidth - (m_LoginProgress.Width + 20));
            m_LoginProgress.Y = (ScreenHeight - (m_LoginProgress.Height + 20));
        }

        public bool ShowProgress
        {
            get
            {
                return m_LoginProgress.Visible;
            }
            set
            {
                m_LoginProgress.Visible = value;
            }
        }

        public void SetProgress(float progress, int stringIndex)
        {
            m_LoginProgress.ProgressCaption = GameFacade.Strings.GetString("251", (stringIndex).ToString());
            m_LoginProgress.Progress = progress;
        }

        public void SetProgressArchive(float progress, string message)
        {
            // TODO: localization
            m_LoginProgress.ProgressCaption = message;
            m_LoginProgress.Progress = progress;
        }

        public void ShowSandboxMode()
        {
            SandboxModeButton = new UIButton()
            {
                Caption = "Sandbox Mode",
                Y = 10,
                Width = 125,
                X = 10
            };
            this.Add(SandboxModeButton);
            SandboxModeButton.OnButtonClick += new ButtonClickDelegate(gameplayButton_OnButtonClick);
        }

        void gameplayButton_OnButtonClick(UIElement button)
        {
            UIScreen.GlobalShowDialog(new UISandboxSelector(), true);
            return;
        }

        public void SetSandboxVisibility(bool visible)
        {
            if (SandboxModeButton != null)
            {
                SandboxModeButton.Visible = visible;
            }
        }
    }
}
