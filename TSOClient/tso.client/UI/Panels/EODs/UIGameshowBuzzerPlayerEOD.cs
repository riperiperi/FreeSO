using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FSO.Client.UI.Controls;
using FSO.Client.UI.Model;
using FSO.Client.UI.Framework;
using FSO.Client.UI.Panels.EODs.Utils;
using FSO.Common.Utils;
using FSO.Content.Model;
using FSO.SimAntics.NetPlay.EODs.Handlers;
using FSO.SimAntics.NetPlay.EODs.Handlers.Data;
using FSO.SimAntics;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework;
using OpenTK.Graphics.OpenGL;
using FSO.Files.HIT;

namespace FSO.Client.UI.Panels.EODs
{
    public class UIGameshowBuzzerPlayerEOD : UIGameshowBuzzerEOD
    {
        private Texture2D BuzzerbtnBackTexture = GetTexture(0x95400000001); // eod_buzzer_playerbuzzerbtnback
        private Texture2D BuzzerbtnTexture = GetTexture(0x95100000001); // eod_buzzer_buzzerbtn
        private UIImage PlayerBuzzerBack;
        private UIImage PlayerScoreBack;
        private UIButton PlayerBuzzer;
        private UITextEdit PlayerScore;
        private ContestantLightsFrame PlayerLightFrame;

        public UIGameshowBuzzerPlayerEOD(UIEODController controller) : base(controller)
        {
            BinaryHandlers["BuzzerEOD_Answer"] = PlayerAnswerHandler;
            BinaryHandlers["BuzzerEOD_Buzzed"] = PlayerBuzzedHandler;
            BinaryHandlers["BuzzerEOD_Master"] = MasterBuzzerHandler;
            BinaryHandlers["BuzzerEOD_Init"] = ShowUIHandler;
            BinaryHandlers["Buzzer_Player_Score"] = UpdateMyScoreHandler;
            PlaintextHandlers["Buzzer_Player_Other_Correct"] = OtherCorrectHandler;
            PlaintextHandlers["Buzzer_Player_Other_Incorrect"] = OtherIncorrectHandler;

            PlaintextHandlers["BuzzerEOD_Timer"] = TimerHandler;
            PlaintextHandlers["Buzzer_Player_Tip"] = SetTipHandler;
            PlaintextHandlers["Buzzer_Player_Win"] = PlayerWinHandler;

            InitPlayerUI();
        }

        #region events
        internal override void ShowUIHandler(string evt, byte[] eodType)
        {
            base.ShowUIHandler(evt, eodType);
            MasterBuzzerHandler(evt, new byte[] { 0 });
        }
        /// <summary>
        /// Enable or disable master buzzer.
        /// </summary>
        /// <param name="evt">BuzzerEOD_Master</param>
        /// <param name="args"></param>
        internal override void MasterBuzzerHandler(string evt, byte[] args)
        {
            if (args[0] == 0)
            {
                DisableMyBuzzer();
                SetTip(GameFacade.Strings.GetString("f127", "24")); // "Waiting for host"
                SetTime(0);
            }
            else
                EnableMyBuzzer();
        }
        /// <summary>
        /// Flashes the light of the player if they buzzed in first. Disable their buzzer if they didn't
        /// </summary>
        /// <param name="evt">BuzzerEOD_Buzzed</param>
        /// <param name="thisPlayerBuzzedFlag">The index of the player who buzzed in first</param>
        internal override void PlayerBuzzedHandler(string evt, byte[] thisPlayerBuzzedFlag)
        {
            if (thisPlayerBuzzedFlag.Length > 0)
            {
                if (thisPlayerBuzzedFlag[0] > 0)
                {
                    SetTip(GameFacade.Strings.GetString("f127", "23")); // "You buzzed first!"
                    PlayerLightFrame.Flash();
                    PlayerBuzzer.Disabled = true;
                }
                // Make player's light frame red if they were not first
                else
                {
                    SetTip(GameFacade.Strings.GetString("f127", "22")); // "You did not buzz first"
                    DisableMyBuzzer();
                }
            }
        }
        /// <summary>
        /// A callback event that updates the tip of the UIEOD, identifying a correct or incorrect answer.
        /// </summary>
        /// <param name="evt">BuzzerEOD_Answer</param>
        /// <param name="correctAnswer">1 if correct, 0 if not</param>
        internal override void PlayerAnswerHandler(string evt, byte[] answer)
        {
            if (answer.Length > 0)
            {
                PlayerLightFrame.Red();

                bool correct = answer[0] == 1;
                if (correct)
                    SetTip(GameFacade.Strings.GetString("f127", "26")); // "You answered correctly!!"
                else
                    SetTip(GameFacade.Strings.GetString("f127", "27")); // "You answered incorrectly"
            }
        }
        /// <summary>
        /// Update the score of the player.
        /// </summary>
        /// <param name="evt">Buzzer_Player_Score</param>
        /// <param name="newScore">The score, 0  to 9999</param>
        private void UpdateMyScoreHandler(string evt, byte[] newScore)
        {
            if (newScore != null && newScore.Length > 0)
                PlayerScore.CurrentText = "" + BitConverter.ToInt16(newScore, 0);
        }
        /// <summary>
        /// When the Player pushes their buzzer.
        /// </summary>
        /// <param name="btn">PlayerBuzzer</param>
        private void PlayerBuzzerClickedHandler(UIElement btn)
        {
            HIT.HITVM.Get().PlaySoundEvent("scoreboard_button");
            PlayerBuzzer.Disabled = true;
            Send("Buzzer_Player_Buzzed", new byte[0]);
        }
        /// <summary>
        /// Another player answered correctly
        /// </summary>
        /// <param name="evt">Buzzer_Player_Other_Correct</param>
        /// <param name="playerName"></param>
        private void OtherCorrectHandler(string evt, string playerName)
        {
            if (playerName.Length < 1)
                playerName = GameFacade.Strings.GetString("f127", "28"); // "Another player"
            SetTip(GameFacade.Strings.GetString("f127", "29").Replace("%s", playerName)); // "%s answered correctly!"
        }
        /// <summary>
        /// Another player answered incorrectly
        /// </summary>
        /// <param name="evt">Buzzer_Player_Other_Incorrect</param>
        /// <param name="playerName"></param>
        private void OtherIncorrectHandler(string evt, string playerName)
        {
            if (playerName.Length < 1)
                playerName = GameFacade.Strings.GetString("f127", "28"); // "Another player"
            SetTip(GameFacade.Strings.GetString("f127", "30").Replace("%s", playerName)); // "%s answered incorrectly"
        }

        #endregion
        private void DisableMyBuzzer()
        {
            PlayerLightFrame.Red();
            PlayerBuzzer.Disabled = true;
        }
        private void EnableMyBuzzer()
        {
            SetTip(GameFacade.Strings.GetString("f127", "25")); // "Buzzer ready"
            PlayerLightFrame.Blue();
            PlayerBuzzer.Disabled = false;
        }
        private void InitPlayerUI()
        {
            // custom textures
            var gd = GameFacade.GraphicsDevice;

            // try to get custom horizontal textures for lights
            try
            {
                AbstractTextureRef lightsframe1hRef = new FileTextureRef("Content/Textures/EOD/Buzzer/eod_lightsframe1.png");
                Lightsframe1Tex = lightsframe1hRef.Get(gd);
                AbstractTextureRef lightsframe2hRef = new FileTextureRef("Content/Textures/EOD/Buzzer/eod_lightsframe2.png");
                Lightsframe2Tex = lightsframe2hRef.Get(gd);
                AbstractTextureRef lightsframebackhRef = new FileTextureRef("Content/Textures/EOD/Buzzer/eod_lightsframeback.png");
                LightsframebackTex = lightsframebackhRef.Get(gd);
                AbstractTextureRef lightsframebluehRef = new FileTextureRef("Content/Textures/EOD/Buzzer/eod_lightsframeblue.png");
                LightsframeblueTex = lightsframebluehRef.Get(gd);
                AbstractTextureRef lightsframeredhRef = new FileTextureRef("Content/Textures/EOD/Buzzer/eod_lightsframered.png");
                LightsframeredTex = lightsframeredhRef.Get(gd);
            }
            catch (Exception e)
            {
                Lightsframe1Tex = null;
                Lightsframe2Tex = null;
                LightsframebackTex = null;
                LightsframeblueTex = null;
                LightsframeredTex = null;
            }

            PlayerLightFrame = new ContestantLightsFrame(Lightsframe1Tex, Lightsframe2Tex, LightsframebackTex, LightsframeblueTex, LightsframeredTex)
            {
                X = 110,
                Y = 52
            };
            Add(PlayerLightFrame);

            // buzzer
            PlayerBuzzerBack = new UIImage(BuzzerbtnBackTexture)
            {
                X = 173,
                Y = 85
            };
            Add(PlayerBuzzerBack);
            PlayerBuzzer = new UIButton(BuzzerbtnTexture)
            {
                X = PlayerBuzzerBack.X + 1,
                Y = PlayerBuzzerBack.Y - 10
            };
            Add(PlayerBuzzer);
            PlayerBuzzer.OnButtonClick += PlayerBuzzerClickedHandler;

            // score - read only
            PlayerScoreBack = new UIImage(PlayerScoreBackTexture)
            {
                X = 310,
                Y = 88,
                Tooltip = GameFacade.Strings.GetString("f127", "2") // "Player Score"
            };
            PlayerScoreBack.UseTooltip();
            Add(PlayerScoreBack);

            PlayerScore = new UITextEdit()
            {
                Mode = UITextEditMode.ReadOnly,
                Alignment = TextAlignment.Center,
                Size = new Vector2(80, 20),
                X = PlayerScoreBack.X - 2,
                Y = PlayerScoreBack.Y + 5,
                CurrentText = "0",
                Tooltip = GameFacade.Strings.GetString("f127", "2") // "Player Score"
            };
            Add(PlayerScore);
        }
    }
}