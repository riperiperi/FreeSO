using System;
using System.Collections.Generic;
using System.Text;
using System.Timers;
using TSOClient.Code.UI.Framework;
using TSOClient.Code.UI.Controls;

namespace TSOClient.Code.UI.Screens
{
    public class EALogo : GameScreen
    {
        private UIImage m_EALogo;
        private UIContainer BackgroundCtnr;
        private Timer m_CheckProgressTimer;

        public EALogo()
            : base()
        {
            PlayBackgroundMusic(new string[] { GameFacade.GameFilePath("music\\stations\\latin\\latin3_7df26b84.mp3") });
            /**
             * Scale the whole screen to 1024
             */
            BackgroundCtnr = new UIContainer();
            BackgroundCtnr.ScaleX = BackgroundCtnr.ScaleY = GlobalSettings.Default.GraphicsWidth / 800.0f;

            /** Background image **/
            m_EALogo = new UIImage(GetTexture((ulong)FileIDs.UIFileIDs.eagames));
            BackgroundCtnr.Add(m_EALogo);

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
            GameFacade.Screens.AddScreen(new MaxisLogo());
        }
    }
}
