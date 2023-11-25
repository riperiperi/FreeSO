using System.Timers;
using FSO.Client.UI.Framework;
using FSO.Client.UI.Controls;
using FSO.Client.GameContent;

namespace FSO.Client.UI.Screens
{
    public class MaxisLogo : GameScreen
    {
        private UIImage m_MaxisLogo;
        private UIContainer BackgroundCtnr;
        private Timer m_CheckProgressTimer;

        public MaxisLogo() : base()
        {
            /**
             * Scale the whole screen to 1024
             */
            BackgroundCtnr = new UIContainer();
            BackgroundCtnr.ScaleX = BackgroundCtnr.ScaleY = GlobalSettings.Default.GraphicsWidth / 640.0f;

            /** Background image **/
            m_MaxisLogo = new UIImage(GetTexture((ulong)FileIDs.UIFileIDs.maxislogo));
            BackgroundCtnr.Add(m_MaxisLogo);

            this.Add(BackgroundCtnr);

            m_CheckProgressTimer = new Timer();
            m_CheckProgressTimer.Interval = 5000;
            m_CheckProgressTimer.Elapsed += new ElapsedEventHandler(m_CheckProgressTimer_Elapsed);
            m_CheckProgressTimer.Start();
        }

        private void m_CheckProgressTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            m_CheckProgressTimer.Stop();
            GameFacade.Screens.RemoveCurrent();
            GameFacade.Screens.AddScreen(new LoadingScreen());
        }
    }
}
