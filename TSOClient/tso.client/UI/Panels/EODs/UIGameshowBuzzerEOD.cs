using System;
using System.Timers;
using FSO.Client.UI.Controls;
using FSO.Client.UI.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace FSO.Client.UI.Panels.EODs
{
    public abstract class UIGameshowBuzzerEOD : UIEOD
    {
        //shared assets
        private Timer InvalidateTimer;
        protected Texture2D PlayerScoreBackTexture = GetTexture(0x95500000001); // eod_buzzer_playerscoreback
        protected Texture2D PlayersVMPersonButtonBackTex = GetTexture(0x000002B300000001); // EOD_PizzaHeadPlaceholder1.bmp
        protected Texture2D Lightsframe1Tex;
        protected Texture2D Lightsframe2Tex;
        protected Texture2D LightsframebackTex;
        protected Texture2D LightsframeblueTex;
        protected Texture2D LightsframeredTex;
        protected bool IsHost { get; private set; }
        protected EODLiveModeOpt EODLiveModeOptions { get; private set; }

        public UIGameshowBuzzerEOD(UIEODController controller) : base(controller)
        {
            InvalidateTimer = new Timer(1000);
            InvalidateTimer.Elapsed += new ElapsedEventHandler((obj, args) => { Parent.Invalidate(); });
            InvalidateTimer.Start();
        }

        public override void OnClose()
        {
            CloseInteraction();
            base.OnClose();
        }

        #region events
        internal virtual void ShowUIHandler(string evt, byte[] eodType)
        {
            EODLiveModeOptions = new EODLiveModeOpt
            {
                Height = EODHeight.Tall,
                Length = EODLength.Full,
                Tips = EODTextTips.Short,
                Timer = EODTimer.Normal
            };

            if (eodType[0] == 1) // VMEODGameshowBuzzerPluginType.Host
            {
                IsHost = true;
                EODLiveModeOptions.Height = EODHeight.ExtraTall;
            }

            EODController.ShowEODMode(EODLiveModeOptions);
        }
        /// <summary>
        /// Enable or disable master buzzer. Affects Player and Host UI.
        /// </summary>
        /// <param name="evt">BuzzerEOD_Master</param>
        /// <param name="args"></param>
        internal abstract void MasterBuzzerHandler(string evt, byte[] args);

        /// <summary>
        /// Flashes the light of the player who buzzed in first. Affects Player and Host UI.
        /// </summary>
        /// <param name="evt">BuzzerEOD_Buzzed</param>
        /// <param name="playerIndex">The index of the player who buzzed in first</param>
        internal abstract void PlayerBuzzedHandler(string evt, byte[] playerIndex);

        /// <summary>
        /// A callback event that updates the tip of the UIEOD, identifying a correct or incorrect answer. Affects players and host.
        /// </summary>
        /// <param name="evt">BuzzerEOD_Answer</param>
        /// <param name="correctAnswer">1 if correct, 0 if not</param>
        internal abstract void PlayerAnswerHandler(string evt, byte[] answer);

        /// <summary>
        /// Continuing the classic trend of sending an integer as a string and then parsing it upon arrival back into an integer. It's a tradition at this point.
        /// </summary>
        /// <param name="evt">BuzzerEOD_Timer, found in derived class instances</param>
        /// <param name="timeString"></param>
        protected void TimerHandler(string evt, string timeString)
        {
            if (Int32.TryParse(timeString, out int time))
                SetTime(time);
        }

        /// <summary>
        /// Handlers registered in derived class instances.
        /// </summary>
        /// <param name="evt">Buzzer_Host_Tip or Buzzer_Player_Tip</param>
        /// <param name="cstIndex">The string index in _f127_gameshoweod.cst</param>
        protected void SetTipHandler(string evt, string cstIndex)
        {
            SetTip(GameFacade.Strings.GetString("f127", cstIndex));
        }

        /// <summary>
        /// Shows the name of the winning player. Handlers registered in derived class instances.
        /// </summary>
        /// <param name="evt">Buzzer_Player_Win</param>
        /// <param name="playerName">Winning player name</param>
        protected void PlayerWinHandler(string evt, string playerName)
        {
            SetTip(GameFacade.Strings.GetString("f127", "36").Replace("%s", playerName)); // "PlayerName is the winner!"
        }
        #endregion

    }
    /// <summary>
    /// A simple encapsulated method of display different light colors in the UI. It relies on the textures being sent to it. Red() makes it red, Blue() makes it blue, Flash() makes it flash
    /// </summary>
    internal class ContestantLightsFrame : UIContainer
    {
        private bool TexturesValid;
        private UIImage Lights1 = new UIImage(); 
        private UIImage Lights2 = new UIImage();
        private UIImage LightsBack = new UIImage();
        private UIImage LightsBlue = new UIImage(); 
        private UIImage LightsRed = new UIImage();
        private System.Timers.Timer FlashTimer;

        internal ContestantLightsFrame(Texture2D light1, Texture2D light2, Texture2D back, Texture2D blue, Texture2D red)
        {
            FlashTimer = new System.Timers.Timer(66);
            if (light1 != null)
            {
                TexturesValid = true;
                LightsBack = new UIImage(back);
                Add(LightsBack);
                Lights1 = new UIImage(light1);
                Add(Lights1);
                Lights2 = new UIImage(light2);
                Add(Lights2);
                LightsBlue = new UIImage(blue);
                Add(LightsBlue);
                LightsRed = new UIImage(red);
                Add(LightsRed);
                FlashTimer.Elapsed += (source, args) => { Lights1.Visible = !Lights1.Visible; Lights2.Visible = !Lights2.Visible; };
                Red();
            }
        }
        internal void Flash()
        {
            LightsRed.Visible = false;
            LightsBlue.Visible = false;
            Lights1.Visible = true;
            Lights2.Visible = false;
            if (TexturesValid)
                FlashTimer.Start();
        }
        internal void Blue()
        {
            FlashTimer.Stop();
            LightsRed.Visible = false;
            LightsBlue.Visible = true;
            Lights1.Visible = false;
            Lights2.Visible = false;
        }
        internal void Red()
        {
            FlashTimer.Stop();
            LightsRed.Visible = true;
            LightsBlue.Visible = false;
            Lights1.Visible = false;
            Lights2.Visible = false;
        }
    }
}
