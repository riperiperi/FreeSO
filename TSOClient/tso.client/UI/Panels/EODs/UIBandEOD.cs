using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Timers;
using FSO.Client.UI.Controls;
using FSO.Client.UI.Framework;
using FSO.Client.UI.Framework.Parser;
using FSO.HIT;
using FSO.Client.UI.Model;
using FSO.Client.UI.Panels.EODs.Utils;
using FSO.SimAntics;
using FSO.SimAntics.NetPlay.EODs.Handlers;
using Microsoft.Xna.Framework.Graphics;

namespace FSO.Client.UI.Panels.EODs
{
    public class UIBandEOD : UIEOD
    {
        private static bool NoteSent;

        private UIScript Script;
        //private VMEODBandStates State;
        private UIEODLobby Lobby;
        private Timer SequenceNoteTimer;
        private Timer SyncTimer;
        private byte[] CurrentSequence;
        private int CurrentNote;

        // buttons
        public UIButton DOH { get; set; }
        public UIButton RE { get; set; }
        public UIButton MI { get; set; }
        public UIButton FA { get; set; }
        public UIButton SO { get; set; }
        public UIButton LA { get; set; }
        public UIButton TI { get; set; }
        public UIButton DOH2 { get; set; }
        public UIButton BUZZ { get; set; }
        public UIButton CONTINUE { get; set; } // rock on
        public UIButton CASHOUT { get; set; } // sell out
        public UIButton MyFirstButton;
        public UIButton MySecondButton;
        public UIButton LastNote;
        public UIButton SyncButton;
        private UIButton[] NoteButtonArray;
        private UIButton[] MiscButtonArray;

        // images
        public UIImage ButtonBack { get; set; }
        public UIImage Player1 { get; set; }
        public UIImage Player2 { get; set; }
        public UIImage Player3 { get; set; }
        public UIImage Player4 { get; set; }
        public UIImage Player5 { get; set; }
        public UIImage Player6 { get; set; }
        public UIImage Player7 { get; set; }
        public UIImage Player8 { get; set; }
        private UIVMPersonButton[] Players = new UIVMPersonButton[8];
        public UISlotsImage Player1Choice;
        public UISlotsImage Player2Choice;
        public UISlotsImage Player3Choice;
        public UISlotsImage Player4Choice;

        public UIImage WaitPlayer1 { get; set; }
        public UIImage WaitPlayer2 { get; set; }
        public UIImage WaitPlayer3 { get; set; }
        public UIImage WaitPlayer4 { get; set; }
        private UIImage[] WaitPlayers { get; set; }
        private UIVMPersonButton[] PlayersWait = new UIVMPersonButton[4];

        // text
        public UILabel TextMessage { get; set; }
        public UILabel Player1Wait { get; set; }
        public UILabel Player2Wait { get; set; }
        public UILabel Player3Wait { get; set; }
        public UILabel Player4Wait { get; set; }
        private UITextEdit EarningString;
        private string CurrentPayoutString;
        private string CurrentSkillTotalString;

        // textures
        public Texture2D PlayerImage { get; set; }
        public Texture2D ContinueButtonImage { get; set; } // rock on
        public Texture2D CashOutButtonImage { get; set; } // sell out

        public UIBandEOD(UIEODController controller) : base(controller)
        {
            // render script
            Script = RenderScript("jobobjband.uis");
            SetTip(GameFacade.Strings["UIText", "253", "19"]);
            
            Remove(DOH);
            Remove(RE);
            Remove(MI);
            Remove(FA);
            Remove(SO);
            Remove(LA);
            Remove(TI);
            Remove(DOH2);
            Remove(BUZZ);
            Remove(CONTINUE);
            Remove(CASHOUT);

            SequenceNoteTimer = new Timer(VMEODBandPlugin.MILLISECONDS_PER_NOTE_IN_SEQUENCE);
            SequenceNoteTimer.Elapsed += NextNoteHandler;
            SyncTimer = new Timer(VMEODBandPlugin.MILLISECONDS_PER_NOTE_IN_SEQUENCE);
            SyncTimer.Elapsed += SyncTimerElapsedHandler;

            // get the buttons and put into array in order to recover their references when the client connects
            NoteButtonArray = new UIButton[] { BUZZ, DOH, RE, MI, FA, SO, LA, TI, DOH2}; // matches order of VMEODBandPlugin.VMEODBandNoteTypes
            MiscButtonArray = new UIButton[] { CASHOUT, CONTINUE };
            
            // make the waiting for players images
            WaitPlayer1 = Script.Create<UIImage>("WaitPlayer1");
            WaitPlayer1.Texture = PlayerImage;
            Add(WaitPlayer1);
            WaitPlayer2 = Script.Create<UIImage>("WaitPlayer2");
            WaitPlayer2.Texture = PlayerImage;
            Add(WaitPlayer2);
            WaitPlayer3 = Script.Create<UIImage>("WaitPlayer3");
            WaitPlayer3.Texture = PlayerImage;
            Add(WaitPlayer3);
            WaitPlayer4 = Script.Create<UIImage>("WaitPlayer4");
            WaitPlayer4.Texture = PlayerImage;
            Add(WaitPlayer4);

            WaitPlayers = new UIImage[] { WaitPlayer1, WaitPlayer2, WaitPlayer3, WaitPlayer4 };

            Player1Wait.Alignment = TextAlignment.Left;
            Player2Wait.Alignment = TextAlignment.Left;
            Player3Wait.Alignment = TextAlignment.Left;
            Player4Wait.Alignment = TextAlignment.Left;

            // add lobby
            Lobby = new UIEODLobby(this, 4)
                .WithPlayerUI(new UIEODLobbyPlayer(0, WaitPlayer1, Player1Wait))
                .WithPlayerUI(new UIEODLobbyPlayer(1, WaitPlayer2, Player2Wait))
                .WithPlayerUI(new UIEODLobbyPlayer(2, WaitPlayer3, Player3Wait))
                .WithPlayerUI(new UIEODLobbyPlayer(3, WaitPlayer4, Player4Wait))
                .WithCaptionProvider((player, avatar) => {
                    switch (player.Slot)
                    {
                        case (int)VMEODBandInstrumentTypes.Trumpet:
                            {
                                if (avatar != null)
                                    return GameFacade.Strings["UIText", "253", "25"].Replace("%d.%02d",
                                        avatar.GetPersonData(SimAntics.Model.VMPersonDataVariable.CharismaSkill) / 100m + "");
                                else
                                    return GameFacade.Strings["UIText", "253", "21"];
                            }
                        case (int)VMEODBandInstrumentTypes.Drums:
                            {
                                if (avatar != null)
                                    return GameFacade.Strings["UIText", "253", "26"].Replace("%d.%02d",
                                        avatar.GetPersonData(SimAntics.Model.VMPersonDataVariable.BodySkill) / 100m + "");
                                else
                                    return GameFacade.Strings["UIText", "253", "22"];
                            }
                        case (int)VMEODBandInstrumentTypes.Keyboard:
                            {
                                if (avatar != null)
                                    return GameFacade.Strings["UIText", "253", "27"].Replace("%d.%02d",
                                        avatar.GetPersonData(SimAntics.Model.VMPersonDataVariable.CreativitySkill) / 100m + "");
                                else
                                    return GameFacade.Strings["UIText", "253", "23"];
                            }
                        case (int)VMEODBandInstrumentTypes.Guitar:
                            {
                                if (avatar != null)
                                    return GameFacade.Strings["UIText", "253", "28"].Replace("%d.%02d",
                                        avatar.GetPersonData(SimAntics.Model.VMPersonDataVariable.CreativitySkill) / 100m + "");
                                else
                                    return GameFacade.Strings["UIText", "253", "24"];
                            }
                    }
                    return "";
                });
            Add(Lobby);

            // listeners
            BinaryHandlers["Band_Sequence"] = SequenceHandler;
            BinaryHandlers["Band_UI_Init"] = UIInitHandler;
            BinaryHandlers["Band_Note_Sync"] = NoteSyncHandler;
            PlaintextHandlers["Band_Buzz"] = BuzzNoteHandler;
            PlaintextHandlers["Band_Fail"] = NoteFailHandler;
            PlaintextHandlers["Band_Game_Reset"] = ResetHandler;
            PlaintextHandlers["Band_Intermission"] = IntermissionHandler;
            PlaintextHandlers["Band_Performance"] = InitPerformanceHandler;
            PlaintextHandlers["Band_Continue_Performance"] = PerformanceHandler;
            PlaintextHandlers["Band_Players"] = PlayerRosterHandler;
            PlaintextHandlers["Band_RockOn"] = ForceRockOnHandler;
            PlaintextHandlers["Band_Show"] = ShowGameHandler;
            PlaintextHandlers["Band_Timer"] = TimerHandler;
            PlaintextHandlers["Band_Timeout"] = TimeoutHandler;
            PlaintextHandlers["Band_Win"] = DisplayWinHandler;
        }
        public override void OnClose()
        {
            base.OnClose();
        }

        private void PlayerRosterHandler(string evt, string msg)
        {
            Lobby.UpdatePlayers(evt, msg);

            // make the 8 gameplay player images but don't add them to UI
            Player1 = Script.Create<UIImage>("Player1");
            Player2 = Script.Create<UIImage>("Player2");
            Player3 = Script.Create<UIImage>("Player3");
            Player4 = Script.Create<UIImage>("Player4");
            Player5 = Script.Create<UIImage>("Player5");
            Player6 = Script.Create<UIImage>("Player6");
            Player7 = Script.Create<UIImage>("Player7");
            Player8 = Script.Create<UIImage>("Player8");

            var players = new UIImage[] { Player1, Player2, Player3, Player4, Player5, Player6, Player7, Player8 };
            int playerNumber = 0;
            for (int index = 0; index < 8; index += 2)
            {
                var lobbyPlayerButton = Lobby.GetPlayerButton(playerNumber);

                if (lobbyPlayerButton == null)
                {
                    if (Players[index] != null)
                        Remove(Players[index]);
                    if (Players[index + 1] != null)
                        Remove(Players[index + 1]);
                    Players[index] = null;
                    Players[index + 1] = null;
                }
                else if (Players[index] == null || !Players[index].Equals(lobbyPlayerButton))
                {
                    if (Players[index] != null)
                        Remove(Players[index]);
                    if (Players[index + 1] != null)
                        Remove(Players[index + 1]);
                    // Make Person buttons for all players
                    Players[index] = new UIVMPersonButton(lobbyPlayerButton.Avatar, lobbyPlayerButton.vm, true);
                    Players[index].Position = players[index].Position;
                    Add(Players[index]);
                    Players[index + 1] = new UIVMPersonButton(lobbyPlayerButton.Avatar, lobbyPlayerButton.vm, true);
                    Players[index + 1].Position = players[index + 1].Position;
                    Add(Players[index + 1]);
                    playerNumber++;
                }
            }
            // goto lobby
            if (ButtonBack != null)
                GotoWaitForPlayerPhase();
        }
        private void NoteSyncHandler(string evt, byte[] noteArray)
        {
            var note = noteArray[0];
            if (Enum.IsDefined(typeof(UIBANDEODSoundNames),note))
            {
                // if this note does not belong to my 1st or 2nd button
                if (!NoteButtonArray[note].Equals(MyFirstButton) && !NoteButtonArray[note].Equals(MySecondButton)) {
                    SyncButton = NoteButtonArray[note];
                    ForceNoteButtonState(SyncButton, (int)UIElementState.Highlighted);
                    PlaySound(note);
                    SyncTimer.Start();
                }
            }
        }
        private void ShowGameHandler(string evt, string msg)
        {
            GotoBandGame();
        }
        private void BuzzNoteHandler(string evt, string msg)
        {
            RemoveListeners();
            SetTip(GameFacade.Strings["UIText", "253", "18"]);
        }
        private void NoteFailHandler(string evt, string avatarName)
        {
            RemoveListeners();
            SetTip(GameFacade.Strings["UIText", "253", "20"].Replace("%s", avatarName));
            PlaySound((byte)UIBANDEODSoundNames.band_note_buzz);
        }
        private void TimeoutHandler(string evt, string msg)
        {
            RemoveListeners();
            SetTip(GameFacade.Strings["UIText", "253", "29"]);
            PlaySound((byte)UIBANDEODSoundNames.band_note_buzz);
        }
        private void TimerHandler(string evt, string timeString)
        {
            int time = 0;
            int.TryParse(timeString, out time);
            SetTime(time);
        }
        private void InitPerformanceHandler(string evt, string msg)
        {
            SetTip(GameFacade.Strings["UIText", "253", "16"]);
            DisableNoteButtons();
            ForceNotesButtonsState((int)UIElementState.Disabled);
            NoteSent = false;

            // allow my buttons
            UnForceNoteButtonState(MyFirstButton);
            UnForceNoteButtonState(MySecondButton);
            UnForceNoteButtonState(BUZZ);
            AddMyListeners();
        }
        private void PerformanceHandler(string evt, string msg)
        {
            NoteSent = false;
            // allow my buttons
            UnForceNoteButtonState(MyFirstButton);
            UnForceNoteButtonState(MySecondButton);
            UnForceNoteButtonState(BUZZ);
            AddMyListeners();
        }
        private void ResetHandler(string evt, string newSkillString)
        {
            CurrentPayoutString = "0";
            CurrentSkillTotalString = newSkillString;
            UpdateEarningString();
        }
        private void SequenceHandler(string evt, byte[] sequence)
        {
            SyncButton = null;
            DisableIntermissionButtons();
            CurrentSequence = sequence;
            InitSequencePhase();
        }
        private void ForceRockOnHandler(string evt, string msg)
        {
            // TODO: replace 2nd useless face icon with chosen button icon
        }
        private void DisplayWinHandler(string evt, string newPayout)
        {
            SetTip(GameFacade.Strings["UIText", "253", "17"]);
            CurrentPayoutString = newPayout;
            UpdateEarningString();
        }
        private void IntermissionHandler(string evt, string newPayout)
        {
            RemoveListeners();
            SetTip(GameFacade.Strings["UIText", "253", "13"] + "?");
            CurrentPayoutString = newPayout;
            UpdateEarningString();
            EnableIntermissionButtons();
        }
        private void SyncTimerElapsedHandler(object source, ElapsedEventArgs args)
        {
            SyncTimer.Stop();
            if (SyncButton != null)
            {
                SyncButton.ForceState = -1;
            }
        }
        private void NextNoteHandler(object source, ElapsedEventArgs args)
        {
            SequenceNoteTimer.Stop();

            // have NOT reached the end of the sequence
            if (CurrentNote < CurrentSequence.Length - 1)
            {
                // restore last hightlighted button to normal
                if (LastNote != null && LastNote.ForceState > -1)
                    ForceNoteButtonState(LastNote, (int)UIElementState.Normal);

                // light up the next note
                CurrentNote++;
                PlayNextNote();
            }
            else
            {
                // restore last hightlighted button to normal
                if (LastNote != null && LastNote.ForceState > -1)
                {
                    if (LastNote.Equals(MyFirstButton) || LastNote.Equals(MySecondButton))
                        UnForceNoteButtonState(LastNote);
                    else
                        ForceNoteButtonState(LastNote, (int)UIElementState.Normal);
                }
            }
        }
        /*
         * When any note button is clicked, send the index in the array, which matches the value of the enum VMEODBandNoteTypes
         */
        private void NoteButtonClickedHandler(UIElement clicked)
        {
            RemoveListeners();
            if (NoteSent == true)
                return;
            var index = Array.IndexOf(NoteButtonArray, clicked);
            if ((index > -1) && (index < 9))
            {
                PlaySound((byte)index);
                NoteSent = true;
                Send("Band_Note", new byte[] { (byte)index });
            }
            else
                AddMyListeners();
        }
        private void IntermissionButtonClickedHandler(UIElement clicked)
        {
            DisableIntermissionButtons();
            // TODO: replace 2nd useless face icon with chosen button icon

            
            var index = Array.IndexOf(MiscButtonArray, clicked);
            Send("Band_Decision", new byte[] { (byte)index });
        }

        private void UIInitHandler(string evt, byte[] instrument)
        {
            // add background
            ButtonBack = Script.Create<UIImage>("ButtonBack");
            AddAt(0, ButtonBack);

            EarningString = new UITextEdit();
            EarningString.TextStyle.Size = 12;
            EarningString.Size = TextMessage.Size;
            EarningString.Position = TextMessage.Position;
            EarningString.CurrentText = GameFacade.Strings["UIText", "253", "14"];
            EarningString.Mode = UITextEditMode.ReadOnly;
            Remove(TextMessage);
            Add(EarningString);

            // recover the buttons but hide them by going to the "lobby" to wait for players
            RecoverButtonRefs();
            MyFirstButton = NoteButtonArray[instrument[0] * 2 + 1];
            MySecondButton = NoteButtonArray[(instrument[0] * 2) + 2];
            GotoWaitForPlayerPhase();
        }
        /*
         *  This is idiosyncratic behavior: When any client connects to the server, they don't have references to items created by the Script
         *  unless they were the very first client to join. Even moving the RenderScript to OnConnection doesn't seem to fix this. So the references
         *  need to be recovered.
         */
        private void RecoverButtonRefs()
        {
            BUZZ = NoteButtonArray[Array.LastIndexOf(NoteButtonArray, BUZZ)];
            if (BUZZ == null)
                BUZZ = Script.Create<UIButton>("BUZZ");
            DOH = NoteButtonArray[Array.LastIndexOf(NoteButtonArray, DOH)];
            if (DOH == null)
                DOH = Script.Create<UIButton>("DOH");
            RE = NoteButtonArray[Array.LastIndexOf(NoteButtonArray, RE)];
            if (RE == null)
                RE = Script.Create<UIButton>("RE");
            MI = NoteButtonArray[Array.LastIndexOf(NoteButtonArray, MI)];
            if (MI == null)
                MI = Script.Create<UIButton>("MI");
            FA = NoteButtonArray[Array.LastIndexOf(NoteButtonArray, FA)];
            if (FA == null)
                FA = Script.Create<UIButton>("FA");
            SO = NoteButtonArray[Array.LastIndexOf(NoteButtonArray, SO)];
            if (SO == null)
                SO = Script.Create<UIButton>("SO");
            LA = NoteButtonArray[Array.LastIndexOf(NoteButtonArray, LA)];
            if (LA == null)
                LA = Script.Create<UIButton>("LA");
            TI = NoteButtonArray[Array.LastIndexOf(NoteButtonArray, TI)];
            if (TI == null)
                TI = Script.Create<UIButton>("TI");
            DOH2 = NoteButtonArray[Array.LastIndexOf(NoteButtonArray, DOH2)];
            if (DOH2 == null)
                DOH2 = Script.Create<UIButton>("DOH2");

            CONTINUE = MiscButtonArray[Array.LastIndexOf(MiscButtonArray, CONTINUE)];
            if (CONTINUE == null)
                CONTINUE = Script.Create<UIButton>("CONTINUE");
            CASHOUT = MiscButtonArray[Array.LastIndexOf(MiscButtonArray, CASHOUT)];
            if (CASHOUT == null)
                CASHOUT = Script.Create<UIButton>("CASHOUT");

            // in case a new button was created
            NoteButtonArray = new UIButton[] { BUZZ, DOH, RE, MI, FA, SO, LA, TI, DOH2 }; // matches order of VMEODBandPlugin.VMEODBandNoteTypes
            MiscButtonArray = new UIButton[] { CASHOUT, CONTINUE };

            DOH.Tooltip = DOH.Tooltip.Replace("h","");
            Add(DOH);
            Add(RE);
            Add(MI);
            Add(FA);
            Add(SO);
            Add(LA);
            Add(TI);
            DOH2.Tooltip = DOH2.Tooltip.Replace("h", "");
            Add(DOH2);
            Add(BUZZ);
            Add(CONTINUE);
            Add(CASHOUT);

            DisableNoteButtons();
            DisableIntermissionButtons();
        }
        
        private void GotoWaitForPlayerPhase()
        {
            SetTip(GameFacade.Strings["UIText", "253", "19"]);

            // hide game-related elements
            ButtonBack.Visible = false;
            foreach (var btn in NoteButtonArray)
                btn.Visible = false;
            foreach (var btn in MiscButtonArray)
                btn.Visible = false;
            foreach (var player in Players)
            {
                if (player != null)
                {
                    try
                    {
                        Remove(player);
                    }
                    catch (Exception) { }
                }
            }
            EarningString.Visible = false;

            try
            {
                Remove(Lobby);
            }
            catch (Exception) { }
            Add(Lobby);
            foreach (var player in WaitPlayers)
                player.Visible = true;
            Player1Wait.Visible = true;
            Player2Wait.Visible = true;
            Player3Wait.Visible = true;
            Player4Wait.Visible = true;

            Controller.ShowEODMode(new EODLiveModeOpt
            {
                Buttons = 0,
                Expandable = false,
                Height = EODHeight.Tall,
                Length = EODLength.Full,
                Timer = EODTimer.Normal,
                Tips = EODTextTips.Short
            });
        }

        private void GotoBandGame()
        {
            // show game-related elements
            ButtonBack.Visible = true;
            foreach (var btn in NoteButtonArray)
                btn.Visible = true;
            foreach (var btn in MiscButtonArray)
                btn.Visible = true;
            foreach (var player in Players)
            {
                if (player != null)
                {
                    try
                    {
                        Remove(player);
                    }
                    catch (Exception) { }
                }
                Add(player);
            }
            EarningString.Visible = true;
            UpdateEarningString();

            try
            {
                Remove(Lobby);
            }
            catch (Exception) { }
            foreach (var player in WaitPlayers)
                player.Visible = false;
            Player1Wait.Visible = false;
            Player2Wait.Visible = false;
            Player3Wait.Visible = false;
            Player4Wait.Visible = false;

            Controller.ShowEODMode(new EODLiveModeOpt
            {
                Buttons = 2,
                Expandable = false,
                Height = EODHeight.Tall,
                Length = EODLength.Full,
                Timer = EODTimer.Normal,
                Tips = EODTextTips.Short
            });
        }
        /*
         * Before the sequence is played
         */
        private void InitSequencePhase()
        {
            SetTip(GameFacade.Strings["UIText", "253", "15"]);
            DisableNoteButtons();
            ForceNotesButtonsState((int)UIElementState.Normal);
            CurrentNote = -1;
            SequenceNoteTimer.Start();
        }

        private void PlayNextNote()
        {
            byte noteValue = CurrentSequence[CurrentNote];

            // play the sound effect
            PlaySound(noteValue);

            // light up the button
            LastNote = NoteButtonArray[noteValue];
            ForceNoteButtonState(LastNote, (int)UIElementState.Highlighted);

            // start the timer
            SequenceNoteTimer.Start();
        }

        private void PlaySound(byte note)
        {
            var soundString = Enum.GetName(typeof(UIBANDEODSoundNames), note);
            HIT.HITVM.Get().PlaySoundEvent(soundString);
        }

        private void AddMyListeners()
        {
            MyFirstButton.OnButtonClick += NoteButtonClickedHandler;
            MySecondButton.OnButtonClick += NoteButtonClickedHandler;
            BUZZ.OnButtonClick += NoteButtonClickedHandler;
        }
        private void RemoveListeners()
        {
            foreach (var btn in NoteButtonArray)
            {
                try
                {
                    btn.OnButtonClick -= NoteButtonClickedHandler;
                }
                catch (Exception) { }
            }
            try
            {
                BUZZ.OnButtonClick -= NoteButtonClickedHandler;
            }
            catch (Exception) { }
        }
        private void DisableNoteButtons()
        {
            foreach (var btn in NoteButtonArray)
            {
                btn.Disabled = true;
            }
        }
        private void DisableIntermissionButtons()
        {
            foreach (var btn in MiscButtonArray)
            {
                btn.Disabled = true;
                btn.OnButtonClick -= IntermissionButtonClickedHandler;
            }
        }

        private void EnableIntermissionButtons()
        {
            foreach (var btn in MiscButtonArray)
            {
                btn.Disabled = false;
                btn.OnButtonClick += IntermissionButtonClickedHandler;
            }
        }
        private void ForceNotesButtonsState(int state)
        {
            foreach (var btn in NoteButtonArray)
            {
                ForceNoteButtonState(btn, state);
            }
        }
        private void ForceNoteButtonState(UIButton btn, int state)
        {
            if (btn != null)
                btn.ForceState = state;
        }
        private void UnForceNoteButtonState(UIButton btn)
        {
            btn.ForceState = -1;
            btn.Disabled = false;
            btn.CurrentFrame = 0;
        }
        private void UpdateEarningString()
        {
            EarningString.CurrentText = GameFacade.Strings["UIText", "253", "14"].Replace("%d.%02d", CurrentSkillTotalString + "        ");
            EarningString.CurrentText = EarningString.CurrentText.Replace("$%d", "$" + CurrentPayoutString);
        }
    }
    [Flags]
    public enum UIBANDEODSoundNames: byte
    {
        band_note_buzz = 0,
        band_note_a = 1,
        band_note_b = 2,
        band_note_c = 3,
        band_note_d = 4,
        band_note_e = 5,
        band_note_f = 6,
        band_note_g = 7,
        band_note_h = 8,
    }
}
