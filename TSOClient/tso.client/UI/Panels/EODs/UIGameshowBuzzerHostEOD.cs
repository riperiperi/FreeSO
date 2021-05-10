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
    public class UIGameshowBuzzerHostEOD : UIGameshowBuzzerEOD
    {
        private Texture2D ButtonSeatTexture;
        private UIImage EODBuzzersBack;
        private UIContainer Stage;
        private bool PlayerDemandsJudgment;

        // more options panel
        private UIContainer MoreOptionsPanel;
        private UIImage EODMoreOptionsBack;
        private UITextEdit MoreOptionsTitle;
        private Texture2D SetTimeBackTexture;
        private UIImage SetAnswerTimeBack;
        private UITextEdit SetAnswerTimeTextEdit;
        private string SetAnswerTimeCallback;
        private UITextEdit SetAnswerTimeLabel;
        private UIImage SetBuzzerTimeBack;
        private UITextEdit SetBuzzerTimeTextEdit;
        private string SetBuzzerTimeCallback;
        private UITextEdit SetBuzzerTimeLabel;
        private UIButton AutoDeductWrongPointsbtn;
        private UITextEdit AutoDeductWrongPointsLabel;
        private UIButton AutoDisableOnWrongbtn;
        private UITextEdit AutoDisableOnWrongLabel;
        private UIButton AutoEnableAllOnRightbtn;
        private UITextEdit AutoEnableAllOnRightLabel;
        private UITextEdit GoBackLabel;
        private UIButton GoBackbtn;
        private UIImage GoBackbtnBack;
        private bool AutoEnableAllOnRight = true;
        private bool AutoDisableOnWrong = true;
        private bool AutoDeductWrongPoints;

        // master buzzer
        private Texture2D BuzzerTogglebtnTexture;
        private UIInvisibleButton MasterBuzzerbtn;
        private UISlotsImage BuzzerToggledOnImage;
        private UISlotsImage BuzzerToggledOffImage;
        private UITextEdit BuzzerEnabledLabel;
        private UITextEdit BuzzerDisabledLabel;
        private UITextEdit BuzzerMasterLabel;
        private bool MasterBuzzerDisabled;

        // current-question-point-value and more-options assets
        private UITextEdit GlobalScore;
        private string GlobalScoreCallback;
        private UITextEdit GlobalScoreLabel;
        private UIButton Optionsbtn;
        private UITextEdit OptionsButtonLabel;
        private UIButton DeclareWinnerbtn;
        private Texture2D DeclareWinnerbtnTexture;

        // player slot lights
        private ContestantLightsFrame[] PlayerLights;

        // enable player buttons
        private UILabel EnablePlayersLabel;
        private Texture2D CheckboxbtnTexture;
        private UIButton[] EnablePlayer;

        // players' faces
        private UIImage Player1PersonBtnBack;
        private UIImage Player2PersonBtnBack;
        private UIImage Player3PersonBtnBack;
        private UIImage Player4PersonBtnBack;
        private UIVMPersonButton[] PlayerPersonbtn;
        private Vector2[] PlayerPersonbtnPositions;
        private UIAlert DeclareWinnerAlert;

        // player move buttons
        private Texture2D MovePlayerRightbtnTexture;
        private UIButton[] MovePlayerRightbtn;

        private Texture2D MovePlayerLeftbtnTexture;
        private UIButton[] MovePlayerLeftbtn;

        // find new players buttons
        private Texture2D FindNewPlayerbtnTexture;
        private UIButton[] FindNewPlayerbtn;

        // player scores
        private UITextEdit[] PlayerScores;

        // player correct and incorrect buttons
        private Texture2D PlayerCorrectbtnTexture;
        private Texture2D PlayerIncorrectbtnTexture;
        private UIButton[] PlayerCorrectbtn;
        private UIButton[] PlayerIncorrectbtn;

        public UIGameshowBuzzerHostEOD(UIEODController controller) : base(controller)
        {
            BinaryHandlers["BuzzerEOD_Answer"] = PlayerAnswerHandler;
            BinaryHandlers["BuzzerEOD_Buzzed"] = PlayerBuzzedHandler;
            BinaryHandlers["BuzzerEOD_Init"] = base.ShowUIHandler;
            BinaryHandlers["Buzzer_Host_B_Deduct"] = PlayerAutoDeductHandler;
            BinaryHandlers["Buzzer_Host_F_Deduct"] = (evt, data) => { AutoDeductWrongPointsbtn.Selected = AutoDeductWrongPoints = BitConverter.ToBoolean(data, 0); ErrorMsgHandler(evt, "32"); };
            BinaryHandlers["Buzzer_Host_B_Disable"] = PlayerAutoDisableHandler;
            BinaryHandlers["Buzzer_Host_F_Disable"] = (evt, data) => { AutoDisableOnWrongbtn.Selected = AutoDisableOnWrong = BitConverter.ToBoolean(data, 0); ErrorMsgHandler(evt, "32"); };
            BinaryHandlers["Buzzer_Host_B_Enable"] = PlayerAutoEnableHandler;
            BinaryHandlers["Buzzer_Host_F_Enable"] = (evt, data) => { AutoEnableAllOnRightbtn.Selected = AutoEnableAllOnRight = BitConverter.ToBoolean(data, 0); ErrorMsgHandler(evt, "32"); };
            BinaryHandlers["Buzzer_Host_B_ToggleMaster"] = MasterBuzzerHandler;
            BinaryHandlers["Buzzer_Host_B_BuzzerTime"] = BuzzerTimeCallbackHandler;
            BinaryHandlers["Buzzer_Host_B_UnderBuzzerTime"] = BuzzerTimeUnderflowHandler;
            BinaryHandlers["Buzzer_Host_B_OverBuzzerTime"] = BuzzerTimeOverflowHandler;
            BinaryHandlers["Buzzer_Host_B_AnswerTime"] = AnswerTimeCallbackHandler;
            BinaryHandlers["Buzzer_Host_B_UnderAnswerTime"] = AnswerTimeUnderflowHandler;
            BinaryHandlers["Buzzer_Host_B_OverAnswerTime"] = AnswerTimeOverflowHandler;
            BinaryHandlers["Buzzer_Host_B_GlobalScore"] = GlobalScoreCallbackHandler;
            BinaryHandlers["Buzzer_Host_B_UnderGlobalScore"] = GlobalScoreUnderflowHandler;
            BinaryHandlers["Buzzer_Host_B_OverGlobalScore"] = GlobalScoreOverflowHandler;
            BinaryHandlers["Buzzer_Host_B_OverPlayerScore"] = PlayerScoreOverflowHandler; 
            BinaryHandlers["Buzzer_Host_B_UnderPlayerScore"] = PlayerScoreUnderflowHandler;
            BinaryHandlers["Buzzer_Host_Live_Roster"] = LiveRosterHandler;
            BinaryHandlers["Buzzer_Host_Round_Restart"] = RoundRestartHandler;
            BinaryHandlers["Buzzer_Host_Roster"] = RosterHandler;

            PlaintextHandlers["Buzzer_Host_Error"] = ErrorMsgHandler;
            PlaintextHandlers["Buzzer_Host_Error_sic"] = SicErrorMsgHandler;
            
            PlaintextHandlers["BuzzerEOD_Timer"] = base.TimerHandler;
            PlaintextHandlers["Buzzer_Host_Tip"] = base.SetTipHandler;
            PlaintextHandlers["Buzzer_Player_Win"] = base.PlayerWinHandler;

            InitHostUI();
        }

        #region events
        /// <summary>
        /// Enable or disable master buzzer.
        /// </summary>
        /// <param name="evt">Buzzer_Host_B_ToggleMaster</param>
        /// <param name="args"></param>
        internal override void MasterBuzzerHandler(string evt, byte[] args)
        {
            bool enabled = BitConverter.ToBoolean(args, 0);
            if (enabled)
                EnableMasterBuzzer();
            else
            {
                DisableMasterBuzzer();
                SetTip(GameFacade.Strings.GetString("f127", "24")); // "Waiting for host"
                SetTime(0);
            }
            EnableMasterBuzzerSwitch();
        }
        /// <summary>
        /// Flashes the light of the player who buzzed in first.
        /// </summary>
        /// <param name="evt">BuzzerEOD_Buzzed</param>
        /// <param name="playerIndex">The index of the player who buzzed in first</param>
        internal override void PlayerBuzzedHandler(string evt, byte[] playerIndex)
        {
            if (playerIndex.Length > 0)
            {
                var index = playerIndex[0];
                if (index < 4)
                {
                    DisableMasterBuzzer();
                    PlayerLights[index].Flash();
                    // host MUST use correct/incorrect button for buzzed player
                    PlayerDemandsJudgment = true;
                    DisableMasterBuzzerSwitch();
                    DisablePlayerManagement();
                    SetAllPlayersAnswerButtons(true);
                    EnablePlayerAnswerButtons(index);
                    string playerName = PlayerPersonbtn[index]?.Avatar?.Name ?? GameFacade.Strings.GetString("f127", "28"); // default: "Another player"
                    SetTip(GameFacade.Strings.GetString("f127", "39").Replace("%s", playerName)); // "PlayerName buzzed first!"
                }
            }
        }
        /// <summary>
        /// A callback event that updates the tip and time of the UIEOD. It occurs after the host judges a player's answer correct or incorrect.
        /// </summary>
        /// <param name="evt">BuzzerEOD_Answer</param>
        /// <param name="answer">1 if correct, 0 if not</param>
        internal override void PlayerAnswerHandler(string evt, byte[] answer)
        {
            SetTip(GameFacade.Strings.GetString("f127", "42")); // "Please wait..."
            SetTime(0);
        }
        /// <summary>
        /// Same roster method as below but synchronizes whether the master buzzer is enabled on server
        /// </summary>
        /// <param name="evt">Buzzer_Host_Live_Roster</param>
        /// <param name="serialPlayers"></param>
        private void LiveRosterHandler(string evt, byte[] serialPlayers)
        {
            EnableMasterBuzzer();
            RosterHandler("BuzzerEOD_Roster", serialPlayers);
        }
        /// <summary>
        /// Receives a serialized roster, 4 trios of strings: Player Avatar's ObjectID, Their Podium's Current Score, 0 if they're disabled or 1 if enabled. The order of the trio of strings corresponds
        /// to the player's position in the UI, where a zero for the ObjectID indicates an empty position.
        /// </summary>
        /// <param name="evt">BuzzerEOD_Rosterr</param>
        /// <param name="serialPlayers">Serialized roster : 4 trios of strings, 1st - Player Avatar's ObjectID, 2nd - their podium's score (0 - 9999), 3rd - 0/1 for [dis/en]abled</param>
        private void RosterHandler(string evt, byte[] serialPlayers)
        {
            string[] rawRoster = VMEODGameCompDrawACardData.DeserializeStrings(serialPlayers);
            int playerIndex = 0;
            for (int rosterIndex = 0; rosterIndex + 2 < rawRoster.Length; rosterIndex += 3)
            {
                if (Int16.TryParse(rawRoster[rosterIndex], out short playerOBJID) && Int16.TryParse(rawRoster[rosterIndex + 1], out short currentScore) && Int32.TryParse(rawRoster[rosterIndex + 2], out int enabled))
                {
                    if (playerOBJID > 0)
                    {
                        SeatPlayer(playerIndex, playerOBJID, currentScore, enabled == 1);
                    }
                    else
                        UnSeatPlayer(playerIndex);
                }
                playerIndex++;
            }
            if (MasterBuzzerDisabled)
                EnablePlayerManagement();
            EnableMasterBuzzerSwitch();
        }
        /// <summary>
        /// After the callback host event, which happens after the host finishes an interaction wherein they judge a player's answer correct or incorrect.
        /// </summary>
        /// <param name="evt"></param>
        /// <param name="nothing"></param>
        private void RoundRestartHandler(string evt, byte[] nothing)
        {
            PlayerDemandsJudgment = false;
            SetTip(GameFacade.Strings.GetString("f127", "24")); // "Waiting for host"
            SetTime(0);
            EnablePlayerManagement();
            EnableMasterBuzzerSwitch();
            SetAllPlayersAnswerButtons(false);
        }
        /// <summary>
        /// Callback for the option to auto deduct the global points from player(s) for whom the host presses the "Incorrect Answer" button.
        /// </summary>
        /// <param name="evt">Buzzer_Host_B_Deduct</param>
        /// <param name="boolean">GetBytes(bool)</param>
        private void PlayerAutoDeductHandler(string evt, byte[] boolean)
        {
            AutoDeductWrongPointsbtn.Selected = AutoDeductWrongPoints = BitConverter.ToBoolean(boolean, 0);
            AutoDeductWrongPointsbtn.Disabled = false;
        }
        /// <summary>
        /// Callback for the option to automatically disable a player's buzzer after answering incorrectly.
        /// </summary>
        /// <param name="evt">Buzzer_Host_B_Disable</param>
        /// <param name="boolean">GetBytes(bool)</param>
        private void PlayerAutoDisableHandler(string evt, byte[] boolean)
        {
            AutoDisableOnWrongbtn.Selected = AutoDisableOnWrong = BitConverter.ToBoolean(boolean, 0);
            AutoDisableOnWrongbtn.Disabled = false;
        }
        /// <summary>
        /// Callback for the option to automatically enable each connected players' buzzer after any player answers correctly.
        /// </summary>
        /// <param name="evt">Buzzer_Host_B_Enable</param>
        /// <param name="boolean">GetBytes(bool)</param>
        private void PlayerAutoEnableHandler(string evt, byte[] boolean)
        {
            AutoEnableAllOnRightbtn.Selected = AutoEnableAllOnRight = BitConverter.ToBoolean(boolean, 0);
            AutoEnableAllOnRightbtn.Disabled = false;
        }
        /// <summary>
        /// Store the true value for when the user unfocuses the textedit.
        /// </summary>
        /// <param name="evt">Buzzer_Host_B_BuzzerTime</param>
        /// <param name="newTime"></param>
        private void BuzzerTimeCallbackHandler(string evt, byte[] newTime)
        {
            var value = BitConverter.ToInt16(newTime, 0);
            SetBuzzerTimeCallback = value + "";
            SetBuzzerTimeTextEdit.Mode = UITextEditMode.Editor;
        }
        /// <summary>
        /// Unfocus from the textedit resets it to the true value. This is to stop the nausiating jump in text cursor back to the start of the textedit.
        /// </summary>
        /// <param name="field"></param>
        private void BuzzerTimeUnfocusHandler(UIElement field)
        {
            if (SetBuzzerTimeCallback != null)
            {
                SetBuzzerTimeTextEdit.CurrentText = SetBuzzerTimeCallback;
                SetBuzzerTimeCallback = null;
            }
        }
        /// <summary>
        /// Store the true value for when the user unfocuses the textedit.
        /// </summary>
        /// <param name="evt">Buzzer_Host_B_AnswerTime</param>
        /// <param name="newTime"></param>
        private void AnswerTimeCallbackHandler(string evt, byte[] newTime)
        {
            var value = BitConverter.ToInt16(newTime, 0);
            SetAnswerTimeCallback = value + "";
            SetAnswerTimeTextEdit.Mode = UITextEditMode.Editor;
        }
        /// <summary>
        /// Unfocus from the textedit resets it to the true value. This is to stop the nausiating jump in text cursor back to the start of the textedit.
        /// </summary>
        /// <param name="field"></param>
        private void AnswerTimeUnfocusHandler(UIElement field)
        {
            if (SetBuzzerTimeCallback != null)
            {
                SetAnswerTimeTextEdit.CurrentText = SetAnswerTimeCallback;
                SetAnswerTimeCallback = null;
            }
        }
        /// <summary>
        /// Store the true value for when the user unfocuses the textedit.
        /// </summary>
        /// <param name="evt">Buzzer_Host_B_GlobalScore</param>
        /// <param name="newScore"></param>
        private void GlobalScoreCallbackHandler(string evt, byte[] newScore)
        {
            var value = BitConverter.ToInt16(newScore, 0);
            GlobalScoreCallback = value + "";
            GlobalScore.Mode = UITextEditMode.Editor;
        }
        /// <summary>
        /// Unfocus from the textedit resets it to the true value. This is to stop the nausiating jump in text cursor back to the start of the textedit.
        /// </summary>
        /// <param name="field"></param>
        private void GlobalScoreUnfocusHandler(UIElement field)
        {
            if (GlobalScoreCallback != null)
            {
                GlobalScore.CurrentText = GlobalScoreCallback;
                GlobalScoreCallback = null;
            }
        }
        /// <summary>
        /// Can be clicked on any player any time the buzzer is inactive
        /// </summary>
        /// <param name="player">index of player 0-3</param>
        private void FindNewPlayerbtnClicked(int player)
        {
            if (MasterBuzzerDisabled)
            {
                if (!PlayerDemandsJudgment)
                {
                    DisablePlayerManagement();
                    DisableMasterBuzzerSwitch();
                    Send("Buzzer_Host_FindNewPlayer", BitConverter.GetBytes(player));
                }
                else
                    ErrorMsgHandler("", "43"); // "You must judge the buzzed player first."
            }
            else
                ErrorMsgHandler("", "32"); // "You can't change this setting until you disable the master buzzer."
        }
        /// <summary>
        /// Can be clicked on any player any time the buzzer is inactive
        /// </summary>
        /// <param name="player">index of player 0-3</param>
        private void PlayerMoveRightbtnClicked(int player)
        {
            HIT.HITVM.Get().PlaySoundEvent(UISounds.Click);
            if (MasterBuzzerDisabled) {
                if (!PlayerDemandsJudgment)
                {
                    DisablePlayerManagement();
                    DisableMasterBuzzerSwitch();
                    Send("Buzzer_Host_MovePlayerRight", BitConverter.GetBytes(player));
                }
                else
                    ErrorMsgHandler("", "43"); // "You must judge the buzzed player first."
            }
            else
                ErrorMsgHandler("", "32"); // "You can't change this setting until you disable the master buzzer."
        }
        /// <summary>
        /// Can be clicked on any player any time the buzzer is inactive
        /// </summary>
        /// <param name="player">index of player 0-3</param>
        private void PlayerMoveLeftbtnClicked(int player)
        {
            HIT.HITVM.Get().PlaySoundEvent(UISounds.Click);
            if (MasterBuzzerDisabled)
            {
                if (!PlayerDemandsJudgment)
                {
                    DisablePlayerManagement();
                    DisableMasterBuzzerSwitch();
                    Send("Buzzer_Host_MovePlayerLeft", BitConverter.GetBytes(player));
                }
                else
                    ErrorMsgHandler("", "43"); // "You must judge the buzzed player first."
            }
            else
                ErrorMsgHandler("", "32"); // "You can't change this setting until you disable the master buzzer."
        }
        /// <summary>
        /// Can be clicked on any player any time the buzzer is inactive
        /// </summary>
        /// <param name="player">index of player 0-3</param>
        private void EnablePlayerbtnClicked(int player)
        {
            HIT.HITVM.Get().PlaySoundEvent("tv_exp_switch");
            if (MasterBuzzerDisabled)
            {
                DisablePlayerManagement();
                DisableMasterBuzzerSwitch();
                Send("Buzzer_Host_ToggleEnablePlayer", BitConverter.GetBytes(player));
            }
            else
                ErrorMsgHandler("", "32"); // "You can't change this setting until you disable the master buzzer."
        }
        /// <summary>
        /// Can be clicked on any player any time the buzzer is inactive
        /// </summary>
        /// <param name="player">index of player 0-3</param>
        private void PlayerCorrectbtnClicked(int player)
        {
            if (MasterBuzzerDisabled)
            {
                Send("Buzzer_Host_PlayerCorrect", BitConverter.GetBytes(player));
                SetAllPlayersAnswerButtons(true);
                SetTip(GameFacade.Strings.GetString("f127", "42")); // "Please wait..."
            }
            else
                ErrorMsgHandler("", "32"); // "You can't change this setting until you disable the master buzzer."
        }
        /// <summary>
        /// Can be clicked on any player any time the buzzer is inactive
        /// </summary>
        /// <param name="player">index of player 0-3</param>
        private void PlayerIncorrectbtnClicked(int player)
        {
            if (MasterBuzzerDisabled)
            {
                Send("Buzzer_Host_PlayerIncorrect", BitConverter.GetBytes(player));
                SetAllPlayersAnswerButtons(true);
                SetTip(GameFacade.Strings.GetString("f127", "42")); // "Please wait..."
            }
            else
                ErrorMsgHandler("", "32"); // "You can't change this setting until you disable the master buzzer."
        }
        /// <summary>
        /// Go to the options menu. Only able to navigate there if the master buzzer is disabled.
        /// </summary>
        /// <param name="btn"></param>
        private void OptionsbtnClicked(UIElement btn)
        {
            HIT.HITVM.Get().PlaySoundEvent(UISounds.Click);
            if (MasterBuzzerDisabled)
                ShowOptionsPanel();
            else
                ErrorMsgHandler("", "32"); // "You can't change this setting until you disable the master buzzer."
        }
        /// <summary>
        /// Go back to the players screen, from the options menu.
        /// </summary>
        /// <param name="btn"></param>
        private void GoBackbtnClicked(UIElement btn)
        {
            HIT.HITVM.Get().PlaySoundEvent(UISounds.Click);
            HideOptionsPanel();
        }
        /// <summary>
        /// Turn on or off the master buzzer, which once when turned on, all enabled players can buzz in to answer a question.
        /// </summary>
        /// <param name="btn"></param>
        private void MasterBuzzerbtnClicked(UIElement btn)
        {
            DisableMasterBuzzerSwitch();
            HIT.HITVM.Get().PlaySoundEvent("tv_exp_turnon");
            byte enable = 1;
            // if toggling toward disable
            if (!MasterBuzzerDisabled)
            {
                enable = 0;
                SetTip(GameFacade.Strings.GetString("f127", "24")); // "Waiting for host"
            }
            Send("Buzzer_Host_A_ToggleMaster", new byte[] { enable });
        }
        /// <summary>
        /// Host wants to change the time allowed for players to buzz in to answer, so if it's valid, send to the server.
        /// </summary>
        /// <param name="field">SetBuzzerTimeTextEdit triggers OnChange event</param>
        private void ValidateNewBuzzerTime(UIElement field)
        {
            if (Int32.TryParse(SetBuzzerTimeTextEdit.CurrentText, out int time))
            {
                if (time >= VMEODAbstractGameshowBuzzerPlugin.MIN_BUZZER_TIME)
                {
                    if (time <= VMEODAbstractGameshowBuzzerPlugin.MAX_BUZZER_TIME)
                    {
                        SetBuzzerTimeTextEdit.Mode = UITextEditMode.ReadOnly;
                        Send("Buzzer_Host_A_BuzzerTime", BitConverter.GetBytes(time));
                    }
                    else
                    {
                        BuzzerTimeOverflowHandler("", new byte[0]);
                        Send("Buzzer_Host_A_BuzzerTime", BitConverter.GetBytes(VMEODAbstractGameshowBuzzerPlugin.MAX_BUZZER_TIME));
                    }
                    return;
                }
            }
            Send("Buzzer_Host_A_BuzzerTime", BitConverter.GetBytes(VMEODAbstractGameshowBuzzerPlugin.MIN_BUZZER_TIME));
            BuzzerTimeUnderflowHandler("", new byte[0]);
        }
        /// <summary>
        /// Display error message: buzzer time can't be below MIN_BUZZER_TIME
        /// </summary>
        /// <param name="evt">Buzzer_Host_B_UnderBuzzerTime</param>
        /// <param name="data"></param>
        private void BuzzerTimeUnderflowHandler(string evt, byte[] data)
        {
            FixBuzzerTime(VMEODAbstractGameshowBuzzerPlugin.MIN_BUZZER_TIME, "33");
        }
        /// <summary>
        /// Display error message: buzzer time can't be above MAX_BUZZER_TIME
        /// </summary>
        /// <param name="evt">Buzzer_Host_B_OverBuzzerTime</param>
        /// <param name="data"></param>
        private void BuzzerTimeOverflowHandler(string evt, byte[] data)
        {
            FixBuzzerTime(VMEODAbstractGameshowBuzzerPlugin.MAX_BUZZER_TIME, "34");
        }
        /// <summary>
        /// Host wants to change the time allowed for a player to answer after buzzing in, so if it's valid, send to the server.
        /// </summary>
        /// <param name="field">SetAnswerTimeTextEdit triggers OnChange event</param>
        private void ValidateNewAnswerTime(UIElement field)
        {
            if (Int32.TryParse(SetAnswerTimeTextEdit.CurrentText, out int time))
            {
                if (time >= VMEODAbstractGameshowBuzzerPlugin.MIN_ANSWER_TIME)
                {
                    if (time <= VMEODAbstractGameshowBuzzerPlugin.MAX_ANSWER_TIME)
                    {
                        SetAnswerTimeTextEdit.Mode = UITextEditMode.ReadOnly;
                        Send("Buzzer_Host_A_AnswerTime", BitConverter.GetBytes(time));
                    }
                    else
                    {
                        AnswerTimeOverflowHandler("", new byte[0]);
                        Send("Buzzer_Host_A_AnswerTime", BitConverter.GetBytes(VMEODAbstractGameshowBuzzerPlugin.MAX_ANSWER_TIME));
                    }
                    return;
                }
            }
            Send("Buzzer_Host_A_AnswerTime", BitConverter.GetBytes(VMEODAbstractGameshowBuzzerPlugin.MIN_ANSWER_TIME));
            AnswerTimeUnderflowHandler("", new byte[0]);
        }
        /// <summary>
        /// Display error message: answer time can't be below MIN_ANSWER_TIME
        /// </summary>
        /// <param name="evt">Buzzer_Host_B_UnderAnswerTime</param>
        /// <param name="data"></param>
        private void AnswerTimeUnderflowHandler(string evt, byte[] data)
        {
            FixAnswerTime(VMEODAbstractGameshowBuzzerPlugin.MIN_ANSWER_TIME, "33");
        }
        /// <summary>
        /// Display error message: answer time can't be above MAX_ANSWER_TIME
        /// </summary>
        /// <param name="evt">Buzzer_Host_B_OverAnswerTime</param>
        /// <param name="data"></param>
        private void AnswerTimeOverflowHandler(string evt, byte[] data)
        {
            FixAnswerTime(VMEODAbstractGameshowBuzzerPlugin.MAX_ANSWER_TIME, "34");
        }
        /// <summary>
        /// Host wants to change the points awarded for the next question, so if it's valid, send to the server.
        /// </summary>
        /// <param name="field">GlobalScore triggers OnChange event</param>
        private void ValidateGlobalScore(UIElement field)
        {
            if (Int32.TryParse(GlobalScore.CurrentText, out int score))
            {
                if (score > -1)
                {
                    if (score <= VMEODAbstractGameshowBuzzerPlugin.MAX_SCORE)
                    {
                        GlobalScore.Mode = UITextEditMode.ReadOnly;
                        Send("Buzzer_Host_A_GlobalScore", BitConverter.GetBytes(score));
                    }
                    else
                    {
                        GlobalScoreOverflowHandler("", new byte[0]);
                        Send("Buzzer_Host_A_GlobalScore", BitConverter.GetBytes(VMEODAbstractGameshowBuzzerPlugin.MAX_SCORE));
                    }
                    return;
                }
            }
            Send("Buzzer_Host_A_GlobalScore", BitConverter.GetBytes(0));
            GlobalScoreUnderflowHandler("", new byte[0]);
        }
        /// <summary>
        /// Display error message: global score can't be below 0
        /// </summary>
        /// <param name="evt">Buzzer_Host_B_UnderGlobalScore</param>
        /// <param name="data"></param>
        private void GlobalScoreUnderflowHandler(string evt, byte[] data)
        {
            FixGlobalScore(0, "33");
        }
        /// <summary>
        /// Display error message: global score can't be above MAX_SCORE
        /// </summary>
        /// <param name="evt">Buzzer_Host_B_OverGlobalScore</param>
        /// <param name="data"></param>
        private void GlobalScoreOverflowHandler(string evt, byte[] data)
        {
            FixGlobalScore(VMEODGameshowBuzzerHostPlugin.MAX_SCORE, "34");
        }
        /// <summary>
        /// Host wants to change the points allotted to this player, so if it's valid, send to the server.
        /// </summary>
        /// <param name="playerIndex">0-3</param>
        private void ValidatePlayerScore(int playerIndex)
        {
            var field = PlayerScores[playerIndex];
            if (Int32.TryParse(field.CurrentText, out int score))
            {
                if (score > -1)
                {
                    if (score <= VMEODGameshowBuzzerHostPlugin.MAX_SCORE)
                    {
                        field.Mode = UITextEditMode.ReadOnly;
                        Send("Buzzer_Host_A_PlayerScore" + playerIndex, BitConverter.GetBytes(score));
                    }
                    else
                    {
                        PlayerScoreOverflowHandler("", new byte[0]);
                        Send("Buzzer_Host_Request_Roster", new byte[0]);
                    }
                    return;
                }
            }
            PlayerScoreUnderflowHandler("", new byte[0]);
            Send("Buzzer_Host_Request_Roster", new byte[0]);
        }
        /// <summary>
        /// Display error message: player score can't be below 0
        /// </summary>
        /// <param name="evt">Buzzer_Host_B_UnderPlayerScore</param>
        /// <param name="data"></param>
        private void PlayerScoreUnderflowHandler(string evt, byte[] data)
        {
            SicErrorMsgHandler(evt, GameFacade.Strings.GetString("f127", "33").Replace("%d", "0"));
        }
        /// <summary>
        /// Display error message: player score can't be above MAX_SCORE
        /// </summary>
        /// <param name="evt">Buzzer_Host_B_OverPlayerScore</param>
        /// <param name="data"></param>
        private void PlayerScoreOverflowHandler(string evt, byte[] data)
        {
            SicErrorMsgHandler(evt, GameFacade.Strings.GetString("f127", "34").Replace("%d", "" + VMEODAbstractGameshowBuzzerPlugin.MAX_SCORE));
        }
        /// <summary>
        /// The host wants to toggle a checkbox option from the options panel. This event sends the post-toggled version.
        /// </summary>
        /// <param name="evt">Buzzer_Host_A_Deduct or Buzzer_Host_A_Negative or Buzzer_Host_A_Disable or Buzzer_Host_A_Enable</param>
        /// <param name="flag">The new value bool</param>
        private void ToggleOption(string evt, bool flag)
        {
            HIT.HITVM.Get().PlaySoundEvent("tv_exp_switch");
            Send(evt, BitConverter.GetBytes(!flag));
        }
        /// <summary>
        /// Note that SimpleUIAlert is now located in UIEOD.cs - Should have done this a long time ago, because I'm sick of reinventing the wheel with UIAlerts. :superweary:
        /// </summary>
        /// <param name="evt">Buzzer_Host_Error</param>
        /// <param name="errorCST">string number in f127 .cst file</param>
        private void ErrorMsgHandler(string evt, string errorCST)
        {
            SimpleUIAlert(GameFacade.Strings.GetString("f127", "37"), GameFacade.Strings.GetString("f127", errorCST));
        }
        /// <summary>
        /// Note that SimpleUIAlert is now located in UIEOD.cs
        /// </summary>
        /// <param name="evt">Buzzer_Host_Error_sic</param>
        /// <param name="errorText">The [sic] error message</param>
        private void SicErrorMsgHandler(string evt, string errorText)
        {
            SimpleUIAlert(GameFacade.Strings.GetString("f127", "37"), errorText);
        }
        /// <summary>
        /// Declare who winner is of the connected players. Making a valid choice from the buttons invokes a confirmation dialog.
        /// </summary>
        /// <param name="btn">DeclareWinnerbtn</param>
        private void DeclareWinnerHandler(UIElement declareWinnerbtn)
        {
            if (MasterBuzzerDisabled)
            {
                if (!PlayerDemandsJudgment)
                {
                    // get options for choosing winner
                    var players = new List<UIAlertButton>();
                    lock (PlayerPersonbtn)
                    {
                        for (int index = 0; index < 4; index++)
                        {
                            if (PlayerPersonbtn[index]?.Avatar?.Name != null)
                            {
                                var player = PlayerPersonbtn[index];
                                var name = player.Avatar.Name;
                                var trunc = name;
                                if (trunc.Length > 10)
                                    trunc = trunc.Substring(0, 8) + "...";

                                players.Add(new UIAlertButton((UIAlertButtonType)index, (btn) => { UIScreen.RemoveDialog(DeclareWinnerAlert); ConfirmDeclareWinnerDialog(name, Array.IndexOf(PlayerPersonbtn, player)); }, trunc));
                            }
                        }
                    }
                    if (players.Count > 0)
                    {
                        // add a cancel button
                        players.Add(new UIAlertButton(UIAlertButtonType.Cancel, (btn) => { UIScreen.RemoveDialog(DeclareWinnerAlert); }));
                        DeclareWinnerAlert = UIScreen.GlobalShowAlert(new UIAlertOptions()
                        {
                            TextSize = 12,
                            Title = GameFacade.Strings.GetString("f127", "35"), // "Declare Winner"
                            Message = GameFacade.Strings.GetString("f127", "40"), // "Whom would you like to declare the winner?"
                            Width = 250 + (players.Count * 75),
                            Alignment = TextAlignment.Center,
                            Buttons = players.ToArray(), // valid players from above plus cancel button
                        }, true);
                    }
                }
                else
                    ErrorMsgHandler("", "43"); // "You must judge the buzzed player first."
            }
            else
                ErrorMsgHandler("", "32"); // "You can't change this setting until you disable the master buzzer."
        }
        #endregion
        /// <summary>
        /// This is invoked when the user makes a selection from the UIAlert that pops up after they press the DeclareWinnerbtn. This is the confirmation alert before declaring a winner.
        /// </summary>
        /// <param name="winnerName">Name of avatar of the prospective winner</param>
        /// <param name="winnerIndex">Index of player to declare winner</param>
        private void ConfirmDeclareWinnerDialog(string winnerName, int winnerIndex)
        {
            UIAlert confirmAlert = null;
            confirmAlert = UIScreen.GlobalShowAlert(new UIAlertOptions()
            {
                TextSize = 12,
                Title = GameFacade.Strings.GetString("f127", "35"), // "Declare Winner"
                Message = GameFacade.Strings.GetString("f127", "41").Replace("%s", winnerName), // "Declare $winnerName as the winner?"
                Alignment = TextAlignment.Center,
                Buttons = UIAlertButton.YesNo((btn) => { UIScreen.RemoveDialog(confirmAlert); Send("Buzzer_Host_A_DeclareWinner", BitConverter.GetBytes(winnerIndex)); }, (btn) => { UIScreen.RemoveDialog(confirmAlert); })
            }, true);
        }

        private void ShowOptionsPanel()
        {
            Add(MoreOptionsPanel);
            SetBuzzerTimeTextEdit.Mode = UITextEditMode.Editor;
            SetAnswerTimeTextEdit.Mode = UITextEditMode.Editor;
            AutoDeductWrongPointsbtn.Selected = AutoDeductWrongPoints;
            AutoDeductWrongPointsbtn.Disabled = false;
            AutoDisableOnWrongbtn.Selected = AutoDisableOnWrong;
            AutoDisableOnWrongbtn.Disabled = false;
            AutoEnableAllOnRightbtn.Selected = AutoEnableAllOnRight;
            AutoEnableAllOnRightbtn.Disabled = false;
            Remove(Stage);
        }
        private void HideOptionsPanel()
        {
            SetBuzzerTimeTextEdit.Mode = UITextEditMode.ReadOnly;
            SetAnswerTimeTextEdit.Mode = UITextEditMode.ReadOnly;
            AutoDeductWrongPointsbtn.Disabled = true;
            AutoDisableOnWrongbtn.Disabled = true;
            AutoEnableAllOnRightbtn.Disabled = true;
            Remove(MoreOptionsPanel);
            Add(Stage);
        }
        private void EnableMasterBuzzerSwitch()
        {
            MasterBuzzerbtn.Disabled = false;
        }
        private void DisableMasterBuzzerSwitch()
        {
            MasterBuzzerbtn.Disabled = true;
        }
        private void EnableMasterBuzzer()
        {
            BuzzerToggledOffImage.Visible = MasterBuzzerDisabled = false;
            BuzzerToggledOnImage.Visible = true;
            DisablePlayerManagement();
            SetTip(GameFacade.Strings.GetString("f127", "25")); // "Buzzer ready"
        }
        private void DisableMasterBuzzer()
        {
            BuzzerToggledOffImage.Visible = MasterBuzzerDisabled = true;
            BuzzerToggledOnImage.Visible = false;
            EnablePlayerManagement();
        }
        private void DisablePlayerManagement()
        {
            for (int index = 0; index < 4; index++)
            {
                DisablePlayerManagement(index);
                FindNewPlayerbtn[index].Disabled = true;
            }
            GlobalScore.Mode = UITextEditMode.ReadOnly;
        }
        private void DisablePlayerManagement(int index)
        {
            EnablePlayer[index].Disabled = true;
            MovePlayerRightbtn[index].Disabled = true;
            MovePlayerLeftbtn[index].Disabled = true;
            PlayerScores[index].Mode = UITextEditMode.ReadOnly;
        }
        private void EnablePlayerManagement()
        {
            for (int index = 0; index < 4; index++)
            {
                if (PlayerPersonbtn[index] != null)
                {
                    FindNewPlayerbtn[index].Disabled = false;
                    EnablePlayer[index].Disabled = false;
                    MovePlayerRightbtn[index].Disabled = !MovePlayerRightbtn[index].Visible == true;
                    MovePlayerLeftbtn[index].Disabled = !MovePlayerLeftbtn[index].Visible == true;
                    PlayerScores[index].Mode = UITextEditMode.Editor;
                }
                FindNewPlayerbtn[index].Disabled = false;
            }
            GlobalScore.Mode = UITextEditMode.Editor;
        }
        private void SetAllPlayersAnswerButtons(bool disabled)
        {
            lock (PlayerCorrectbtn)
            {
                lock (PlayerIncorrectbtn)
                {
                    for (int index = 0; index < 4; index++)
                    {
                        PlayerCorrectbtn[index].Disabled = disabled | PlayerPersonbtn[index] == null;
                        PlayerIncorrectbtn[index].Disabled = disabled | PlayerPersonbtn[index] == null;
                    }
                }
            }
        }
        private void EnablePlayerAnswerButtons(int player)
        {
            if (player > -1 && player < 4)
                SetPlayerAnswerButtons(player, false);
        }
        private void DisablePlayerAnswerButtons(int player)
        {
            if (player > -1 && player < 4)
                SetPlayerAnswerButtons(player, true);
        }
        private void SetPlayerAnswerButtons(int player, bool disabled)
        {
            lock (PlayerCorrectbtn)
                PlayerCorrectbtn[player].Disabled = disabled;
            lock (PlayerIncorrectbtn)
                PlayerIncorrectbtn[player].Disabled = disabled;
        }
        private void SeatPlayer(int newPlayerIndex, short playerOBJID, short playerScore, bool enabled)
        {
            var avatar = (VMAvatar)EODController.Lot.vm.GetObjectById(playerOBJID);
            if (PlayerPersonbtn[newPlayerIndex] != null)
                Stage.Remove(PlayerPersonbtn[newPlayerIndex]);
            var newPlayer = new UIVMPersonButton(avatar, EODController.Lot.vm, true)
            {
                Position = PlayerPersonbtnPositions[newPlayerIndex]
            };
            Stage.Add(newPlayer);
            PlayerPersonbtn[newPlayerIndex] = newPlayer;
            PlayerScores[newPlayerIndex].CurrentText = "" + playerScore;
            if (enabled)
            {
                PlayerLights[newPlayerIndex].Blue();
                EnablePlayer[newPlayerIndex].Selected = true;
            }
            else
            {
                PlayerLights[newPlayerIndex].Red();
                EnablePlayer[newPlayerIndex].Selected = false;
            }
            if (!PlayerDemandsJudgment)
                EnablePlayerAnswerButtons(newPlayerIndex);
        }
        private void UnSeatPlayer(int playerIndex)
        {
            if (PlayerPersonbtn[playerIndex] != null)
                Stage.Remove(PlayerPersonbtn[playerIndex]);
            PlayerPersonbtn[playerIndex] = null;
            PlayerLights[playerIndex].Red();
            DisablePlayerAnswerButtons(playerIndex);
            DisablePlayerManagement(playerIndex);
            PlayerScores[playerIndex].CurrentText = "";
            EnablePlayer[playerIndex].Selected = false;
        }
        private void FixBuzzerTime(short newValue, string errorCST)
        {
            SetBuzzerTimeTextEdit.CurrentText = newValue + "";
            SetBuzzerTimeCallback = null;
            SetBuzzerTimeTextEdit.Mode = UITextEditMode.Editor;
            var errorMsg = GameFacade.Strings.GetString("f127", errorCST).Replace("%d", "" + newValue);
            SicErrorMsgHandler("", errorMsg);
        }
        private void FixAnswerTime(short newValue, string errorCST)
        {
            SetAnswerTimeTextEdit.CurrentText = newValue + "";
            SetAnswerTimeCallback = null;
            SetAnswerTimeTextEdit.Mode = UITextEditMode.Editor;
            var errorMsg = GameFacade.Strings.GetString("f127", errorCST).Replace("%d", "" + newValue);
            SicErrorMsgHandler("", errorMsg);
        }
        private void FixGlobalScore(short newValue, string errorCST)
        {
            GlobalScore.CurrentText = newValue + "";
            GlobalScoreCallback = null;
            GlobalScore.Mode = UITextEditMode.Editor;
            var errorMsg = GameFacade.Strings.GetString("f127", errorCST).Replace("%d", "" + newValue);
            SicErrorMsgHandler("", errorMsg);
        }
        private void InitHostUI()
        {
            PlayerPersonbtn = new UIVMPersonButton[4];
            PlayerPersonbtnPositions = new Vector2[4];

            // textures
            SetTimeBackTexture = GetTexture(0x95600000001); // eod_buzzer_playertimerback
            PlayerCorrectbtnTexture = GetTexture(0x31700000001); // gizmo_top100listsbtn
            PlayerIncorrectbtnTexture = GetTexture(0x95900000001); // eod_cancelbtn
            ButtonSeatTexture = GetTexture(0x000001A100000002); // eod_buttonseat_transparent.tga
            MovePlayerRightbtnTexture = GetTexture(0x2D400000001); // eod_dc_editcardnextbtn
            MovePlayerLeftbtnTexture = GetTexture(0x2D500000001); // eod_dc_editcardpreviousbtn
            CheckboxbtnTexture = GetTexture(0x49300000001); // options_checkboxbtn
            BuzzerTogglebtnTexture = GetTexture(0x95800000001); // eod_buzzer_toggleonoff
            FindNewPlayerbtnTexture = GetTexture(0x31300000001); // gizmo_searchbtn
            Texture2D optionsbtnTex = GetTexture(0x4C000000001); // ucp_optionsmodebtn
            Texture2D checkbtnTex = GetTexture(0x2D800000001); // eod_dc_sharedcheckbtn
            DeclareWinnerbtnTexture = GetTexture(0x4A900000001); // querypanel_sellasyncbtn

            // custom textures
            var gd = GameFacade.GraphicsDevice;
            Texture2D extraTallBackTex = null;

            // try to get the custom back for extratall; this is a second copy, which will be used for options window
            AbstractTextureRef extraTallBackTexRef = new FileTextureRef("Content/Textures/EOD/lpanel_eodpanelextratallback.png");
            try
            {
                extraTallBackTex = extraTallBackTexRef.Get(gd);
                EODBuzzersBack = new UIImage(extraTallBackTex);
                EODMoreOptionsBack = new UIImage(extraTallBackTex);
            }
            catch (Exception e)
            {
                EODBuzzersBack = new UIImage();
                EODMoreOptionsBack = new UIImage();
            }
            EODBuzzersBack.Position = new Vector2(10, 96);
            AddAt(0, EODBuzzersBack);

            // try to get custom vertical textures for lights
            try
            {
                AbstractTextureRef lightsframe1hRef = new FileTextureRef("Content/Textures/EOD/Buzzer/eod_lightsframe1h.png");
                Lightsframe1Tex = lightsframe1hRef.Get(gd);
                AbstractTextureRef lightsframe2hRef = new FileTextureRef("Content/Textures/EOD/Buzzer/eod_lightsframe2h.png");
                Lightsframe2Tex = lightsframe2hRef.Get(gd);
                AbstractTextureRef lightsframebackhRef = new FileTextureRef("Content/Textures/EOD/Buzzer/eod_lightsframebackh.png");
                LightsframebackTex = lightsframebackhRef.Get(gd);
                AbstractTextureRef lightsframebluehRef = new FileTextureRef("Content/Textures/EOD/Buzzer/eod_lightsframeblueh.png");
                LightsframeblueTex = lightsframebluehRef.Get(gd);
                AbstractTextureRef lightsframeredhRef = new FileTextureRef("Content/Textures/EOD/Buzzer/eod_lightsframeredh.png");
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

            Stage = new UIContainer();
            Add(Stage);

            /*
             * non-player-specific assets
             */
            GlobalScoreLabel = new UITextEdit()
            {
                X = 25,
                Y = 98,
                CurrentText = GameFacade.Strings.GetString("f127", "1"), // "Question Value"
                Alignment = TextAlignment.Center,
                Mode = UITextEditMode.ReadOnly,
                Size = new Vector2(120, 20)
            };
            Stage.Add(GlobalScoreLabel);

            var setGlobalScoreBack = new UIImage(PlayerScoreBackTexture)
            {
                X = 50,
                Y = 118
            };
            Stage.Add(setGlobalScoreBack);

            GlobalScore = new UITextEdit()
            {
                X = setGlobalScoreBack.X + 6,
                Y = setGlobalScoreBack.Y + 6,
                CurrentText = "100",
                MaxChars = 5,
                MaxLines = 1,
                Size = new Vector2(60, 20),
                Mode = UITextEditMode.Editor,
                Alignment = TextAlignment.Center,
                Tooltip = GameFacade.Strings.GetString("f127", "2") // "Player Score"
            };
            Stage.Add(GlobalScore);
            GlobalScore.OnChange += ValidateGlobalScore;
            GlobalScore.OnFocusOut += GlobalScoreUnfocusHandler;

            // Options button
            var optionsbtnBack = new UIImage(ButtonSeatTexture)
            {
                X = 363,
                Y = 122,
                ScaleX = 0.65f,
                ScaleY = 0.65f
            };
            Stage.Add(optionsbtnBack);
            Optionsbtn = new UIButton(optionsbtnTex)
            {
                X = 365,
                Y = 124
            };
            Stage.Add(Optionsbtn);
            Optionsbtn.OnButtonClick += OptionsbtnClicked;
            OptionsButtonLabel = new UITextEdit()
            {
                X = Optionsbtn.X + 25,
                Y = Optionsbtn.Y,
                CurrentText = GameFacade.Strings.GetString("f127", "3"), // "Options"
                Mode = UITextEditMode.ReadOnly,
                Size = new Vector2(100, 20),
                Alignment = TextAlignment.Left
            };
            Stage.Add(OptionsButtonLabel);

            // master button and nearby labels
            BuzzerToggledOnImage = new UISlotsImage(BuzzerTogglebtnTexture)
            {
                X = 220,
                Y = 118,
                Visible = false
            };
            BuzzerToggledOnImage.SetBounds(37, 0, 37, 31);
            Stage.Add(BuzzerToggledOnImage);
            BuzzerToggledOffImage = new UISlotsImage(BuzzerTogglebtnTexture)
            {
                X = 220,
                Y = 118
            };
            BuzzerToggledOffImage.SetBounds(0, 0, 37, 31);
            Stage.Add(BuzzerToggledOffImage);
            MasterBuzzerbtn = new UIInvisibleButton(37, 31, TextureUtils.TextureFromColor(gd, new Color(new Vector4(0, 0, 0, 0)), 1, 1))
            {
                X = 220,
                Y = 118
            };
            Stage.Add(MasterBuzzerbtn);
            MasterBuzzerbtn.OnButtonClick += MasterBuzzerbtnClicked;
            BuzzerEnabledLabel = new UITextEdit()
            {
                X = MasterBuzzerbtn.X - 66,
                Y = MasterBuzzerbtn.Y + 6,
                CurrentText = GameFacade.Strings.GetString("f127", "4"), // "Disabled"
                Alignment = TextAlignment.Right,
                Mode = UITextEditMode.ReadOnly,
                Size = new Vector2(62, 20)
            };
            Stage.Add(BuzzerEnabledLabel);
            BuzzerDisabledLabel = new UITextEdit()
            {
                X = MasterBuzzerbtn.X + 40,
                Y = MasterBuzzerbtn.Y + 6,
                CurrentText = GameFacade.Strings.GetString("f127", "5"), // "Enabled"
                Mode = UITextEditMode.ReadOnly,
                Alignment = TextAlignment.Left,
                Size = new Vector2(60, 20)
            };
            Stage.Add(BuzzerDisabledLabel);
            BuzzerMasterLabel = new UITextEdit()
            {
                X = 175,
                Y = 98,
                CurrentText = GameFacade.Strings.GetString("f127", "6"), // "PLAYER BUZZERS"
                Alignment = TextAlignment.Center,
                Mode = UITextEditMode.ReadOnly,
                Size = new Vector2(120, 20)
            };
            Stage.Add(BuzzerMasterLabel);
            EnablePlayersLabel = new UILabel()
            {
                X = 218,
                Y = 155,
                Alignment = TextAlignment.Center,
                Caption = GameFacade.Strings.GetString("f127", "7") // "Allow?"
            };
            Stage.Add(EnablePlayersLabel);

            /*
             * Players' assets
             */

            var personOffset = new Vector2(2, 2);

            // player 1
            Player1PersonBtnBack = new UIImage(PlayersVMPersonButtonBackTex)
            {
                X = 57,
                Y = 195
            };
            Stage.Add(Player1PersonBtnBack);
            PlayerPersonbtnPositions[0] = new Vector2(Player1PersonBtnBack.X + personOffset.X, Player1PersonBtnBack.Y + personOffset.Y);

            // player 2
            Player2PersonBtnBack = new UIImage(PlayersVMPersonButtonBackTex)
            {
                X = 160,
                Y = 195
            };
            Stage.Add(Player2PersonBtnBack);
            PlayerPersonbtnPositions[1] = new Vector2(Player2PersonBtnBack.X + personOffset.X, Player2PersonBtnBack.Y + personOffset.Y);

            // player 3
            Player3PersonBtnBack = new UIImage(PlayersVMPersonButtonBackTex)
            {
                X = 293,
                Y = 195
            };
            Stage.Add(Player3PersonBtnBack);
            PlayerPersonbtnPositions[2] = new Vector2(Player3PersonBtnBack.X + personOffset.X, Player3PersonBtnBack.Y + personOffset.Y);

            // player 4
            Player4PersonBtnBack = new UIImage(PlayersVMPersonButtonBackTex)
            {
                X = 396,
                Y = 195
            };
            Stage.Add(Player4PersonBtnBack);
            PlayerPersonbtnPositions[3] = new Vector2(Player4PersonBtnBack.X + personOffset.X, Player4PersonBtnBack.Y + personOffset.Y);

            // player light frames
            PlayerLights = new ContestantLightsFrame[4]
            {
                new ContestantLightsFrame(Lightsframe1Tex, Lightsframe2Tex, LightsframebackTex, LightsframeblueTex, LightsframeredTex){ X = Player1PersonBtnBack.X - 37f, Y = Player1PersonBtnBack.Y - 20f },
                new ContestantLightsFrame(Lightsframe1Tex, Lightsframe2Tex, LightsframebackTex, LightsframeblueTex, LightsframeredTex){ X = Player2PersonBtnBack.X - 37f, Y = Player2PersonBtnBack.Y - 20f },
                new ContestantLightsFrame(Lightsframe1Tex, Lightsframe2Tex, LightsframebackTex, LightsframeblueTex, LightsframeredTex){ X = Player3PersonBtnBack.X - 37f, Y = Player3PersonBtnBack.Y - 20f },
                new ContestantLightsFrame(Lightsframe1Tex, Lightsframe2Tex, LightsframebackTex, LightsframeblueTex, LightsframeredTex){ X = Player4PersonBtnBack.X - 37f, Y = Player4PersonBtnBack.Y - 20f }
            };
            foreach (var light in PlayerLights) Stage.Add(light);

            // enable player checkboxes
            EnablePlayer = new UIButton[4]
            {
                new UIButton(CheckboxbtnTexture){X = Player1PersonBtnBack.X + 3, Y = EnablePlayersLabel.Y},
                new UIButton(CheckboxbtnTexture){X = Player2PersonBtnBack.X + 3, Y = EnablePlayersLabel.Y},
                new UIButton(CheckboxbtnTexture){X = Player3PersonBtnBack.X + 3, Y = EnablePlayersLabel.Y},
                new UIButton(CheckboxbtnTexture){X = Player4PersonBtnBack.X + 3, Y = EnablePlayersLabel.Y}
            };


            // Declare winner
            var buttonSeat = new UIImage(ButtonSeatTexture)
            {
                X = 222,
                Y = 238,
                ScaleX = 0.85f,
                ScaleY = 0.85f
            };
            Stage.Add(buttonSeat);
            DeclareWinnerbtn = new UIButton(DeclareWinnerbtnTexture)
            {
                X = 224,
                Y = 240,
                Tooltip = GameFacade.Strings.GetString("f127", "35") // "Declare Winner"
            };
            Stage.Add(DeclareWinnerbtn);
            DeclareWinnerbtn.OnButtonClick += DeclareWinnerHandler;

            /*
             * move players right and left buttons
             */
            MovePlayerLeftbtn = new UIButton[4]
            {
                new UIButton(MovePlayerLeftbtnTexture)
                {
                    X = Player1PersonBtnBack.X - 18,
                    Y = Player1PersonBtnBack.Y - 1,
                    Visible = false
                },
                new UIButton(MovePlayerLeftbtnTexture)
                {
                    X = Player2PersonBtnBack.X - 18,
                    Y = Player2PersonBtnBack.Y - 1
                },
                new UIButton(MovePlayerLeftbtnTexture)
                {
                    X = Player3PersonBtnBack.X - 18,
                    Y = Player3PersonBtnBack.Y - 1
                },
                new UIButton(MovePlayerLeftbtnTexture)
                {
                    X = Player4PersonBtnBack.X - 18,
                    Y = Player4PersonBtnBack.Y - 1
                },
            };
            MovePlayerRightbtn = new UIButton[4]
            {
                new UIButton(MovePlayerRightbtnTexture)
                {
                    X = Player1PersonBtnBack.X + 35,
                    Y = Player1PersonBtnBack.Y - 1
                },
                new UIButton(MovePlayerRightbtnTexture)
                {
                    X = Player2PersonBtnBack.X + 35,
                    Y = Player2PersonBtnBack.Y - 1
                },
                new UIButton(MovePlayerRightbtnTexture)
                {
                    X = Player3PersonBtnBack.X + 35,
                    Y = Player3PersonBtnBack.Y - 1
                },
                new UIButton(MovePlayerRightbtnTexture)
                {
                    X = Player4PersonBtnBack.X + 35,
                    Y = Player4PersonBtnBack.Y - 1,
                    Visible = false
                }
            };
            // listeners; initially disable, add to uicontainer
            for (var i = 0; i < 4; i++)
            {
                var left = MovePlayerLeftbtn[i];
                Stage.Add(left);
                left.OnButtonClick += (element) => { PlayerMoveLeftbtnClicked(Array.IndexOf(MovePlayerLeftbtn, element as UIButton)); };
                left.Disabled = true;

                var right = MovePlayerRightbtn[i];
                Stage.Add(right);
                right.OnButtonClick += (element) => { PlayerMoveRightbtnClicked(Array.IndexOf(MovePlayerRightbtn, element as UIButton)); };
                right.Disabled = true;

                var enable = EnablePlayer[i];
                Stage.Add(enable);
                enable.OnButtonClick += (element) => { EnablePlayerbtnClicked(Array.IndexOf(EnablePlayer, element as UIButton)); };
                enable.Disabled = true;
            }

            /*
             * find new player buttons and backs
             */
            var findNewPlayerbtnBacks = new UIImage[4]
            {
                new UIImage(ButtonSeatTexture)
                {X = Player1PersonBtnBack.X - 1,
                Y = Player1PersonBtnBack.Y + 32,
                ScaleX = 0.72f,
                ScaleY = 0.72f },
                new UIImage(ButtonSeatTexture)
                {X = Player2PersonBtnBack.X - 1,
                Y = Player2PersonBtnBack.Y + 32,
                ScaleX = 0.72f,
                ScaleY = 0.72f },
                new UIImage(ButtonSeatTexture)
                {X = Player3PersonBtnBack.X - 1,
                Y = Player3PersonBtnBack.Y + 32,
                ScaleX = 0.72f,
                ScaleY = 0.72f },
                new UIImage(ButtonSeatTexture)
                {X = Player4PersonBtnBack.X - 1,
                Y = Player4PersonBtnBack.Y + 32,
                ScaleX = 0.72f,
                ScaleY = 0.72f }
            };
            FindNewPlayerbtn = new UIButton[4];
            for (var i = 0; i < findNewPlayerbtnBacks.Length; i++)
            {
                var back = findNewPlayerbtnBacks[i];
                Stage.Add(back);
                var btn = FindNewPlayerbtn[i] = new UIButton(FindNewPlayerbtnTexture)
                {
                    X = back.X + 2,
                    Y = back.Y + 2,
                    ScaleX = 0.80f,
                    ScaleY = 0.80f,
                    Tooltip = GameFacade.Strings.GetString("f127", "8") // "Find new player"
                };
                Stage.Add(btn);
                btn.OnButtonClick += (element) => { FindNewPlayerbtnClicked(Array.IndexOf(FindNewPlayerbtn, element as UIButton)); };
            }

            /*
             * Correct buttons and backs
             */
            var playerCorrectbtnBack = new UIImage[4]
            {
                new UIImage(ButtonSeatTexture)
                {
                    X = Player1PersonBtnBack.X - 20,
                    Y = Player1PersonBtnBack.Y + 60,
                    ScaleX = 0.88f,
                    ScaleY = 0.88f
                },
                new UIImage(ButtonSeatTexture)
                {
                    X = Player2PersonBtnBack.X - 20,
                    Y = Player2PersonBtnBack.Y + 60,
                    ScaleX = 0.88f,
                    ScaleY = 0.88f
                },
                new UIImage(ButtonSeatTexture)
                {
                    X = Player3PersonBtnBack.X - 20,
                    Y = Player3PersonBtnBack.Y + 60,
                    ScaleX = 0.88f,
                    ScaleY = 0.88f
                },
                new UIImage(ButtonSeatTexture)
                {
                    X = Player4PersonBtnBack.X - 20,
                    Y = Player4PersonBtnBack.Y + 60,
                    ScaleX = 0.88f,
                    ScaleY = 0.88f
                }
            };
            PlayerCorrectbtn = new UIButton[4];
            for (var i = 0; i < playerCorrectbtnBack.Length; i++)
            {
                var back = playerCorrectbtnBack[i];
                Stage.Add(back);
                var btn = PlayerCorrectbtn[i] = new UIButton(PlayerCorrectbtnTexture)
                {
                    X = back.X + 2,
                    Y = back.Y + 2,
                    Tooltip = GameFacade.Strings.GetString("f127", "9") // "Correct"
                };
                Stage.Add(btn);
                btn.OnButtonClick += (element) => { PlayerCorrectbtnClicked(Array.IndexOf(PlayerCorrectbtn, element as UIButton)); };
                btn.Disabled = true;
            }

            // Incorrect buttons and backs
            var playerIncorrectbtnBack = new UIImage[4]
            {
                new UIImage(ButtonSeatTexture)
                {
                    X = Player1PersonBtnBack.X + 15,
                    Y = Player1PersonBtnBack.Y + 60,
                    ScaleX = 0.88f,
                    ScaleY = 0.88f
                },
                new UIImage(ButtonSeatTexture)
                {
                    X = Player2PersonBtnBack.X + 15,
                    Y = Player2PersonBtnBack.Y + 60,
                    ScaleX = 0.88f,
                    ScaleY = 0.88f
                },
                new UIImage(ButtonSeatTexture)
                {
                    X = Player3PersonBtnBack.X + 15,
                    Y = Player3PersonBtnBack.Y + 60,
                    ScaleX = 0.88f,
                    ScaleY = 0.88f
                },
                new UIImage(ButtonSeatTexture)
                {
                    X = Player4PersonBtnBack.X + 15,
                    Y = Player4PersonBtnBack.Y + 60,
                    ScaleX = 0.88f,
                    ScaleY = 0.88f
                }
            };

            PlayerIncorrectbtn = new UIButton[4];
            for (var i = 0; i < playerIncorrectbtnBack.Length; i++)
            {
                var back = playerIncorrectbtnBack[i];
                Stage.Add(back);
                var btn = PlayerIncorrectbtn[i] = new UIButton(PlayerIncorrectbtnTexture)
                {
                    X = back.X + 2,
                    Y = back.Y + 2,
                    ScaleX = 0.90f,
                    ScaleY = 0.90f,
                    Tooltip = GameFacade.Strings.GetString("f127", "10") // "Incorrect"
                };
                Stage.Add(btn);
                btn.OnButtonClick += (element) => { PlayerIncorrectbtnClicked(Array.IndexOf(PlayerIncorrectbtn, element as UIButton)); };
                btn.Disabled = true;
            }

            // score backs and score texts
            var playerScoreBack = new UIImage[]
            {
                new UIImage(PlayerScoreBackTexture)
                {
                    X = Player1PersonBtnBack.X - 25,
                    Y = Player1PersonBtnBack.Y + 100
                },
                new UIImage(PlayerScoreBackTexture)
                {
                    X = Player2PersonBtnBack.X - 25,
                    Y = Player2PersonBtnBack.Y + 100
                },
                new UIImage(PlayerScoreBackTexture)
                {
                    X = Player3PersonBtnBack.X - 25,
                    Y = Player3PersonBtnBack.Y + 100
                },
                new UIImage(PlayerScoreBackTexture)
                {
                    X = Player4PersonBtnBack.X - 25,
                    Y = Player4PersonBtnBack.Y + 100
                },
            };
            PlayerScores = new UITextEdit[4];
            for (var i = 0; i < playerScoreBack.Length; i++)
            {
                var back = playerScoreBack[i];
                Stage.Add(back);
                var score = PlayerScores[i] = new UITextEdit()
                {
                    X = back.X + 6,
                    Y = back.Y + 6,
                    CurrentText = "",
                    MaxChars = 4,
                    MaxLines = 1,
                    Size = new Vector2(60, 20),
                    Mode = UITextEditMode.Editor,
                    Alignment = TextAlignment.Center,
                    Tooltip = GameFacade.Strings.GetString("f127", "11") // "Player Score"
                };
                Stage.Add(score);
                PlayerScores[i].OnChange += (field) => { ValidatePlayerScore(Array.IndexOf(PlayerScores, field as UITextEdit)); };
            }

            /*
             * Init flags
             */
            MasterBuzzerDisabled = true;

            /*
             * More options
             */
            MoreOptionsPanel = new UIContainer();

            EODMoreOptionsBack.Position = new Vector2(10, 96);
            MoreOptionsPanel.Add(EODMoreOptionsBack);

            MoreOptionsTitle = new UITextEdit()
            {
                X = 175,
                Y = 98,
                CurrentText = GameFacade.Strings.GetString("f127", "12"), // "MORE OPTIONS"
                Alignment = TextAlignment.Center,
                Mode = UITextEditMode.ReadOnly,
                Size = new Vector2(120, 20)
            };
            MoreOptionsPanel.Add(MoreOptionsTitle);

            SetAnswerTimeLabel = new UITextEdit()
            {
                X = 55,
                Y = 184,
                CurrentText = GameFacade.Strings.GetString("f127", "13"), // "Set Answer Time"
                Alignment = TextAlignment.Right,
                Mode = UITextEditMode.ReadOnly,
                Size = new Vector2(200, 20)
            };
            MoreOptionsPanel.Add(SetAnswerTimeLabel);
            SetAnswerTimeBack = new UIImage(SetTimeBackTexture)
            {
                X = 265,
                Y = SetAnswerTimeLabel.Y + 1
            };
            MoreOptionsPanel.Add(SetAnswerTimeBack);
            SetAnswerTimeTextEdit = new UITextEdit()
            {
                X = SetAnswerTimeBack.X - 5,
                Y = SetAnswerTimeBack.Y,
                MaxLines = 1,
                MaxChars = 3,
                Mode = UITextEditMode.Editor,
                Size = new Vector2(60, 20),
                Alignment = TextAlignment.Center,
                Tooltip = GameFacade.Strings.GetString("f127", "14"), // "Seconds to answer"
                CurrentText = "20"
            };
            MoreOptionsPanel.Add(SetAnswerTimeTextEdit);
            SetAnswerTimeTextEdit.OnChange += ValidateNewAnswerTime;
            SetAnswerTimeTextEdit.OnFocusOut += AnswerTimeUnfocusHandler;

            SetBuzzerTimeLabel = new UITextEdit()
            {
                X = 55,
                Y = 144,
                CurrentText = GameFacade.Strings.GetString("f127", "15"), // "Set Buzzer Time"
                Alignment = TextAlignment.Right,
                Mode = UITextEditMode.ReadOnly,
                Size = new Vector2(200, 20)
            };
            MoreOptionsPanel.Add(SetBuzzerTimeLabel);
            SetBuzzerTimeBack = new UIImage(SetTimeBackTexture)
            {
                X = 265,
                Y = SetBuzzerTimeLabel.Y + 1
            };
            MoreOptionsPanel.Add(SetBuzzerTimeBack);
            SetBuzzerTimeTextEdit = new UITextEdit()
            {
                X = SetBuzzerTimeBack.X - 5,
                Y = SetBuzzerTimeBack.Y,
                MaxLines = 1,
                MaxChars = 3,
                Mode = UITextEditMode.Editor,
                Size = new Vector2(60, 20),
                Alignment = TextAlignment.Center,
                Tooltip = GameFacade.Strings.GetString("f127", "16"), // "Seconds to buzz",
                CurrentText = "10"
            };
            MoreOptionsPanel.Add(SetBuzzerTimeTextEdit);
            SetBuzzerTimeTextEdit.OnChange += ValidateNewBuzzerTime;
            SetBuzzerTimeTextEdit.OnFocusOut += BuzzerTimeUnfocusHandler;

            AutoDeductWrongPointsLabel = new UITextEdit()
            {
                X = 57,
                Y = 224,
                CurrentText = GameFacade.Strings.GetString("f127", "17"), // "Deduct Points on Wrong Answer"
                Alignment = TextAlignment.Right,
                Mode = UITextEditMode.ReadOnly,
                Size = new Vector2(200, 20)
            };
            MoreOptionsPanel.Add(AutoDeductWrongPointsLabel);
            AutoDeductWrongPointsbtn = new UIButton(CheckboxbtnTexture)
            {
                X = SetBuzzerTimeTextEdit.X + 21,
                Y = AutoDeductWrongPointsLabel.Y,
                Selected = AutoDeductWrongPoints
            };
            MoreOptionsPanel.Add(AutoDeductWrongPointsbtn);
            AutoDeductWrongPointsbtn.OnButtonClick += (btn) => { ToggleOption("Buzzer_Host_A_Deduct", AutoDeductWrongPoints); AutoDeductWrongPointsbtn.Disabled = true; };

            AutoDisableOnWrongLabel = new UITextEdit()
            {
                X = 55,
                Y = 264,
                CurrentText = GameFacade.Strings.GetString("f127", "19"), // "Disable Buzzer on Wrong Answer",
                Alignment = TextAlignment.Right,
                Mode = UITextEditMode.ReadOnly,
                Size = new Vector2(200, 20)
            };
            MoreOptionsPanel.Add(AutoDisableOnWrongLabel);
            AutoDisableOnWrongbtn = new UIButton(CheckboxbtnTexture)
            {
                X = SetBuzzerTimeTextEdit.X + 21,
                Y = AutoDisableOnWrongLabel.Y,
                Selected = AutoDisableOnWrong
            };
            MoreOptionsPanel.Add(AutoDisableOnWrongbtn);
            AutoDisableOnWrongbtn.OnButtonClick += (btn) => { ToggleOption("Buzzer_Host_A_Disable", AutoDisableOnWrong); AutoDisableOnWrongbtn.Disabled = true; };

            AutoEnableAllOnRightLabel = new UITextEdit()
            {
                X = 55,
                Y = 304,
                CurrentText = GameFacade.Strings.GetString("f127", "20"), // "Enable All Buzzers on Right Answer"
                Alignment = TextAlignment.Right,
                Mode = UITextEditMode.ReadOnly,
                Size = new Vector2(200, 20)
            };
            MoreOptionsPanel.Add(AutoEnableAllOnRightLabel);
            AutoEnableAllOnRightbtn = new UIButton(CheckboxbtnTexture)
            {
                X = SetBuzzerTimeTextEdit.X + 21,
                Y = AutoEnableAllOnRightLabel.Y,
                Selected = AutoEnableAllOnRight
            };
            MoreOptionsPanel.Add(AutoEnableAllOnRightbtn);
            AutoEnableAllOnRightbtn.OnButtonClick += (btn) => { ToggleOption("Buzzer_Host_A_Enable", AutoEnableAllOnRight); AutoEnableAllOnRightbtn.Disabled = true; };

            GoBackLabel = new UITextEdit()
            {
                Position = OptionsButtonLabel.Position,
                CurrentText = GameFacade.Strings.GetString("f127", "21"), // "Go back"
                Mode = UITextEditMode.ReadOnly,
                Size = new Vector2(100, 20),
                Alignment = TextAlignment.Left
            };
            MoreOptionsPanel.Add(GoBackLabel);
            GoBackbtnBack = new UIImage(ButtonSeatTexture)
            {
                X = 363,
                Y = 122,
                ScaleX = 0.65f,
                ScaleY = 0.65f
            };
            MoreOptionsPanel.Add(GoBackbtnBack);
            GoBackbtn = new UIButton(checkbtnTex)
            {
                Position = Optionsbtn.Position,
                ScaleX = 0.60f,
                ScaleY = 0.60f
            };
            MoreOptionsPanel.Add(GoBackbtn);
            GoBackbtn.OnButtonClick += GoBackbtnClicked;
        }
    }
}
