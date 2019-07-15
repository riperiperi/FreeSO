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
using FSO.Content.Model;
using FSO.SimAntics;
using FSO.SimAntics.NetPlay.EODs.Handlers;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework;

namespace FSO.Client.UI.Panels.EODs
{
    public class UIBandEOD : UIEOD
    {
        private static bool NoteSent;
        private UIScript Script;
        private UIEODLobby Lobby;
        private Timer SequenceNoteTimer;
        private Timer SyncTimer;
        private byte[] CurrentSequence;
        private int CurrentNote;

        // NEW for FreeSO Band aka Band 2 aka Band Redux
        private Vector2 TallTallOffset = new Vector2(0, 188);
        private string MetronomeBackTexPath = "Content/Textures/EOD/Band/bandmetronomeback.png";
        private string MetronomeSliderTexPath = "Content/Textures/EOD/Band/bandmetronomeslider.png";
        private string MetronomeCaseTexPath = "Content/Textures/EOD/Band/bandmetronomecase.png";
        private string SkillLevelTexPath = "Content/Textures/EOD/Band/bandskilllevels.png";
        private float ActivePayoutY;
        private float LeftPayoutX;
        private float ZeroNotesY;
        private UISlotsImage[] SkillLevels; // Charisma, Body, Creativity 1, Creativity 2
        private UISlotsImage BehindSkillsBG;
        private UISlotsImage MetronomeCaseEdge;
        private UIImage MetronomeSlider;
        private UIImage MetronomeBack;
        private UIImage MetronomeCase;
        private UIMaskedContainer Metronome;
        private UITextEdit TotalSkillLevelsTextEdit;
        private UITextEdit CharismaLevelTextEdit;
        private UITextEdit BodyLevelTextEdit;
        private UITextEdit Creativity1LevelTextEdit;
        private UITextEdit Creativity2LevelTextEdit;
        private UITextEdit CurrentPayoutLabelTextEdit;
        private UITextEdit CurrentPayoutTextEdit;
        private UITextEdit MinimumPayoutLabelTextEdit;
        private UITextEdit MinimumPayoutTextEdit;
        private UITextEdit PayoutsTextEdit;
        private UITextEdit[] PayoutTextEdits;
        private UITween TweenQueue;
        private int CharismaSkill;
        private int CurrentDisplayedCharismaLevel;
        private int BodySkill;
        private int CurrentDisplayedBodyLevel;
        private int Creative1Skill;
        private int CurrentDisplayedCreative1Level;
        private int Creative2Skill;
        private int CurrentDisplayedCreative2Level;
        private List<UIElement> UpperUIElements;
        private Timer LevelTimer;
        private int LevelTimerTicks;

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
            CurrentPayoutString = "$%d";
            CurrentSkillTotalString = "%d.%02d";

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
            LevelTimer = new Timer(250);
            LevelTimer.Elapsed += SkillLevelHandler;

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
            
            /*foreach (var player in WaitPlayers)
                player.Position += TallTallOffset;
            Player1Wait.Position += TallTallOffset;
            Player2Wait.Position += TallTallOffset;
            Player3Wait.Position += TallTallOffset;
            Player4Wait.Position += TallTallOffset;*/

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
                                {
                                    var skill = avatar.GetPersonData(SimAntics.Model.VMPersonDataVariable.CharismaSkill) / 100m;
                                    CharismaSkill = (int)Math.Truncate(skill);
                                    CharismaLevelTextEdit.CurrentText = GameFacade.Strings["UIText", "253", "21"] + ": " + CharismaSkill;
                                    return GameFacade.Strings["UIText", "253", "25"].Replace("%d.%02d", skill + "");
                                }
                                else
                                    return GameFacade.Strings["UIText", "253", "21"];
                            }
                        case (int)VMEODBandInstrumentTypes.Drums:
                            {
                                if (avatar != null)
                                {
                                    var skill = avatar.GetPersonData(SimAntics.Model.VMPersonDataVariable.BodySkill) / 100m;
                                    BodySkill = (int)Math.Truncate(skill);
                                    BodyLevelTextEdit.CurrentText = GameFacade.Strings["UIText", "253", "22"] + ": " + BodySkill;
                                    return GameFacade.Strings["UIText", "253", "26"].Replace("%d.%02d", skill + "");
                                }
                                else
                                    return GameFacade.Strings["UIText", "253", "22"];
                            }
                        case (int)VMEODBandInstrumentTypes.Keyboard:
                            {
                                if (avatar != null)
                                {
                                    var skill = avatar.GetPersonData(SimAntics.Model.VMPersonDataVariable.CreativitySkill) / 100m;
                                    Creative1Skill = (int)Math.Truncate(skill);
                                    Creativity1LevelTextEdit.CurrentText = GameFacade.Strings["UIText", "253", "23"] + ": " + Creative1Skill;
                                    return GameFacade.Strings["UIText", "253", "27"].Replace("%d.%02d", skill + "");
                                }
                                else
                                    return GameFacade.Strings["UIText", "253", "23"];
                            }
                        case (int)VMEODBandInstrumentTypes.Guitar:
                            {
                                if (avatar != null)
                                {
                                    var skill = avatar.GetPersonData(SimAntics.Model.VMPersonDataVariable.CreativitySkill) / 100m;
                                    Creative2Skill = (int)Math.Truncate(skill);
                                    Creativity2LevelTextEdit.CurrentText = GameFacade.Strings["UIText", "253", "24"] + ": " + Creative2Skill;
                                    return GameFacade.Strings["UIText", "253", "28"].Replace("%d.%02d", skill + "");
                                }
                                else
                                    return GameFacade.Strings["UIText", "253", "24"];
                            }
                    }
                    return "";
                });
            Add(Lobby);

            InitUpperUI();

            // listeners
            BinaryHandlers["Band_Buzz"] = BuzzNoteHandler;
            BinaryHandlers["Band_Sequence"] = SequenceHandler;
            BinaryHandlers["Band_UI_Init"] = UIInitHandler;
            BinaryHandlers["Band_Note_Sync"] = NoteSyncHandler;
            BinaryHandlers["Band_Performance"] = InitPerformanceHandler;
            BinaryHandlers["Band_Intermission"] = IntermissionHandler;
            BinaryHandlers["Band_Continue_Performance"] = PerformanceHandler;
            BinaryHandlers["Band_Game_Reset_Skill"] = ResetSkillHandler;
            BinaryHandlers["Band_Show"] = ShowGameHandler;
            BinaryHandlers["Band_Electric"] = SequenceSuccessMessage;
            PlaintextHandlers["Band_Fail"] = NoteFailHandler;
            PlaintextHandlers["Band_Players"] = PlayerRosterHandler;
            PlaintextHandlers["Band_RockOn"] = ForceRockOnHandler;
            PlaintextHandlers["Band_Timer"] = TimerHandler;
            PlaintextHandlers["Band_Timeout"] = TimeoutHandler;
            BinaryHandlers["Band_Win"] = DisplayWinHandler;
        }
        public override void OnClose()
        {
            CloseInteraction();
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
                    Players[index].Position = players[index].Position = players[index].Position + TallTallOffset;
                    Add(Players[index]);
                    Players[index + 1] = new UIVMPersonButton(lobbyPlayerButton.Avatar, lobbyPlayerButton.vm, true);
                    Players[index + 1].Position = players[index + 1].Position = players[index + 1].Position + TallTallOffset;
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
        private void ShowGameHandler(string evt, byte[] nothing)
        {
            GotoBandGame();
        }
        private void BuzzNoteHandler(string evt, byte[] nothing)
        {
            RemoveListeners();
            SetTip(GameFacade.Strings["UIText", "253", "18"]);
            Parent.Invalidate();
        }
        private void NoteFailHandler(string evt, string avatarName)
        {
            RemoveListeners();
            SetTip(GameFacade.Strings["UIText", "253", "20"].Replace("%s", avatarName));
            Parent.Invalidate();
            PlaySound((byte)UIBANDEODSoundNames.band_note_buzz);
        }
        private void TimeoutHandler(string evt, string msg)
        {
            RemoveListeners();
            SetTip(GameFacade.Strings["UIText", "253", "29"]);
            Parent.Invalidate();
            PlaySound((byte)UIBANDEODSoundNames.band_note_buzz);
        }
        private void TimerHandler(string evt, string timeString)
        {
            int time = 0;
            int.TryParse(timeString, out time);
            SetTime(time);
        }
        private void InitPerformanceHandler(string evt, byte[] nothing)
        {
            SetTip(GameFacade.Strings["UIText", "253", "16"]);
            Parent.Invalidate();
            DisableNoteButtons();
            ForceNotesButtonsState((int)UIElementState.Disabled);
            NoteSent = false;

            // allow my buttons
            UnForceNoteButtonState(MyFirstButton);
            UnForceNoteButtonState(MySecondButton);
            UnForceNoteButtonState(BUZZ);
            AddMyListeners();
        }
        private void PerformanceHandler(string evt, byte[] nothing)
        {
            NoteSent = false;
            // allow my buttons
            UnForceNoteButtonState(MyFirstButton);
            UnForceNoteButtonState(MySecondButton);
            UnForceNoteButtonState(BUZZ);
            AddMyListeners();
        }
        private void ResetSkillHandler(string evt, byte[] newSkillString)
        {
            var data = SimAntics.NetPlay.EODs.Handlers.Data.VMEODGameCompDrawACardData.DeserializeStrings(newSkillString);
            if (data != null && data.Length == 1)
                CurrentSkillTotalString = data[0];

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
        private void DisplayWinHandler(string evt, byte[] serializedPayoutString)
        {
            var data = SimAntics.NetPlay.EODs.Handlers.Data.VMEODGameCompDrawACardData.DeserializeStrings(serializedPayoutString);
            if (data != null && data.Length == 2 && Int32.TryParse(data[1], out int index))
            {
                UpdatePayouts(data[0], null, index);
            }
            SetTip(GameFacade.Strings["UIText", "253", "17"]);
            Parent.Invalidate();
        }
        private void SequenceSuccessMessage(string evt, byte[] nothing)
        {
            SetTip(GameFacade.Strings.GetString("f124", "5")); // "That was electric! Great job!"
            Parent.Invalidate();
        }
        private void IntermissionHandler(string evt, byte[] serializedPayoutStrings)
        {
            RemoveListeners();
            var data = SimAntics.NetPlay.EODs.Handlers.Data.VMEODGameCompDrawACardData.DeserializeStrings(serializedPayoutStrings);
            SetTip(GameFacade.Strings["UIText", "253", "13"] + "?");
            Parent.Invalidate();
            if (data != null && data.Length == 3 && Int32.TryParse(data[2], out int index))
                UpdatePayouts(data[0], data[1], index);
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
        private void SkillLevelHandler(object source, ElapsedEventArgs args)
        {
            if (++LevelTimerTicks == 20)
                LevelTimer.Stop();
            if (CurrentDisplayedCharismaLevel < CharismaSkill)
                SkillLevels[0].SetBounds(0, 0, ++CurrentDisplayedCharismaLevel * 6, 12);
            if (CurrentDisplayedBodyLevel < BodySkill)
                SkillLevels[1].SetBounds(0, 0, ++CurrentDisplayedBodyLevel * 6, 12);
            if (CurrentDisplayedCreative1Level < Creative1Skill)
                SkillLevels[2].SetBounds(0, 0, ++CurrentDisplayedCreative1Level * 6, 12);
            if (CurrentDisplayedCreative2Level < Creative2Skill)
                SkillLevels[3].SetBounds(0, 0, ++CurrentDisplayedCreative2Level * 6, 12);
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
            EarningString.Position = TextMessage.Position + TallTallOffset;
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

            // move everything down for talltall
            foreach (var btn in NoteButtonArray)
                btn.Position += TallTallOffset;
            foreach (var btnn in MiscButtonArray)
                btnn.Position += TallTallOffset;
            ButtonBack.Position += TallTallOffset;
            
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

        private void UpdatePayouts(string currentPayout, string nextPayout, int payoutFieldIndex)
        {
            float sliderYChange = -40f;
            float time = 1f;
            var fieldsToTween = new List<UITextEdit>();

            fieldsToTween.Add(PayoutTextEdits[payoutFieldIndex]);
            PayoutTextEdits[payoutFieldIndex].CurrentText = currentPayout;
            if (payoutFieldIndex > 0)
                fieldsToTween.Add(PayoutTextEdits[payoutFieldIndex - 1]);

            var y = PayoutTextEdits[payoutFieldIndex].Y - ActivePayoutY;
            var numOfMovesAway = Math.Abs((int)y / 40);
            if (numOfMovesAway == 0)
            {
                sliderYChange = 0f;
            }
            // standard, move to next payout scheme
            else if (numOfMovesAway == 1)
            {
                if (payoutFieldIndex - 1 > 0)
                    fieldsToTween.Add(PayoutTextEdits[payoutFieldIndex - 2]);
                if (payoutFieldIndex < PayoutTextEdits.Length - 1)
                {
                    fieldsToTween.Insert(0, PayoutTextEdits[payoutFieldIndex + 1]);
                    if (nextPayout != null)
                        PayoutTextEdits[payoutFieldIndex + 1].CurrentText = nextPayout;
                }
            }
            // either reverting to closest minimum more than one away or resetting to 0
            else
            {
                time += 0.5f * numOfMovesAway;
                sliderYChange = 40f * numOfMovesAway;
                for (int count = 1; count <= numOfMovesAway; count++)
                {
                    var index = payoutFieldIndex - 1 - count;
                    if (index > 0)
                        fieldsToTween.Add(PayoutTextEdits[index]);
                }
            }
            // add the tween for the slider
            TweenQueue = GameFacade.Screens.Tween.Queue(UITweenQueueTypes.Synchronous, new UITweenInstanceMembers(GameFacade.Screens.Tween, MetronomeBack, time,
                new Dictionary<string, float>() { { "AbstractY", MetronomeBack.AbstractY + sliderYChange } }, TweenQuad.EaseOut).OnUpdateAction(() => Metronome.UpdateChildMask(MetronomeBack)));

            // make the tweens + the other fields need their Y value updated still
            var fieldsNotToTween = new List<UITextEdit>(PayoutTextEdits);
            lock (fieldsNotToTween)
            {
                foreach (var field in fieldsToTween)
                {
                    TweenQueue = GameFacade.Screens.Tween.Queue(UITweenQueueTypes.Synchronous, new UITweenInstanceMembers(GameFacade.Screens.Tween, field, time,
                        new Dictionary<string, float>() { { "Y", field.Y + sliderYChange } }, TweenQuad.EaseOut).OnUpdateAction(() => { PayoutVisibility(field); Parent.Invalidate(); }));
                    fieldsNotToTween.Remove(field);
                }
            }
            foreach (var field in fieldsNotToTween)
            {
                field.Y += sliderYChange;
                field.Visible = false;
            }
            TweenQueue.CompleteAction = () =>
            {
                CurrentPayoutString = "" + currentPayout;
                UpdateSkillAndPayoutDisplays();
                CurrentPayoutTextEdit.CurrentText = "$" + currentPayout;
                if (payoutFieldIndex % 5 == 0)
                    MinimumPayoutTextEdit.CurrentText = "$" + currentPayout;
            };
            TweenQueue.PlayQueue();
        }

        private void ResetPayouts()
        {
            CurrentPayoutString = "" + 0;
            UpdateSkillAndPayoutDisplays();
            CurrentPayoutTextEdit.CurrentText = "$" + 0;
            MinimumPayoutTextEdit.CurrentText = "$" + 0;
            for (int index = 0; index < PayoutTextEdits.Length; index++)
            {
                PayoutTextEdits[index].Visible = false;
                PayoutTextEdits[index].Y = ActivePayoutY + 40 * index;
            }
            MetronomeBack.AbstractY = ZeroNotesY;
            Metronome.UpdateChildMasks();
            PayoutTextEdits[0].Visible = true;
        }

        private void GotoWaitForPlayerPhase()
        {
            SetTip(GameFacade.Strings["UIText", "253", "19"]);
            Parent.Invalidate();

            // hide game-related elements
            ButtonBack.Visible = false;
            foreach (var btn in NoteButtonArray)
                btn.Visible = false;
            foreach (var btn in MiscButtonArray)
                btn.Visible = false;
            foreach (var player in Players)
            {
                if (player != null)
                    Remove(player);
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

            // hide the upper UI items
            foreach (var element in UpperUIElements)
                element.Visible = false;
            foreach (var item in SkillLevels)
                item.Visible = false;
            foreach (var payout in PayoutTextEdits)
                payout.Visible = false;

            EODController.ShowEODMode(new EODLiveModeOpt
            {
                Buttons = 0,
                Expandable = false,
                Expanded = true,
                Height = EODHeight.Tall,
                Length = EODLength.Full,
                Timer = EODTimer.Normal,
                Tips = EODTextTips.Short
            });
        }

        private void GotoBandGame()
        {
            SetTip(GameFacade.Strings.GetString("f124", "6")); // "Are you ready to rock?!"
            Parent.Invalidate();
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

            // show the upper UI stuff
            foreach (var element in UpperUIElements)
                element.Visible = true;

            AnimateLevels();
            ResetPayouts();
            //UpdatePayouts("0", "1", 0);

            Remove(Lobby);

            foreach (var player in WaitPlayers)
                player.Visible = false;
            Player1Wait.Visible = false;
            Player2Wait.Visible = false;
            Player3Wait.Visible = false;
            Player4Wait.Visible = false;

            EODController.ShowEODMode(new EODLiveModeOpt
            {
                Buttons = 2,
                Expandable = false,
                Expanded = true,
                Height = EODHeight.TallTall,
                Length = EODLength.Full,
                Timer = EODTimer.Normal,
                Tips = EODTextTips.Short
            });
        }
        private void AnimateLevels()
        {
            CurrentDisplayedCharismaLevel = CurrentDisplayedBodyLevel = CurrentDisplayedCreative1Level = CurrentDisplayedCreative2Level = 0;
            foreach (var skill in SkillLevels)
            {
                skill.Visible = true;
                skill.SetBounds(0, 0, 0, 12);
            }
            LevelTimer.Start();
        }
        /*
         * Before the sequence is played
         */
        private void InitSequencePhase()
        {
            SetTip(GameFacade.Strings["UIText", "253", "15"]);
            Parent.Invalidate();
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
        private void UpdateSkillAndPayoutDisplays()
        {
            EarningString.CurrentText = GameFacade.Strings["UIText", "253", "14"].Replace("%d.%02d", CurrentSkillTotalString + "        ");
            EarningString.CurrentText = EarningString.CurrentText.Replace("$%d", "$" + CurrentPayoutString);
            TotalSkillLevelsTextEdit.CurrentText = GameFacade.Strings.GetString("f124", "1").Replace("%d.%02d", CurrentSkillTotalString);
        }
        // new for FreeSO Band, Band 2.0
        private void InitUpperUI()
        {
            UpperUIElements = new List<UIElement>();
            // get external textures
            var gd = GameFacade.GraphicsDevice;

            var metroBackRef = new FileTextureRef(MetronomeBackTexPath);
            var metrosliderRef = new FileTextureRef(MetronomeSliderTexPath);
            var metroCaseRef = new FileTextureRef(MetronomeCaseTexPath);
            var skillLevelRef = new FileTextureRef(SkillLevelTexPath);

            Texture2D metroBackTex = null;
            Texture2D metroSliderTex = null;
            Texture2D metroCaseTex = null;
            Texture2D skillLevelTex = null;
            Texture2D behindSkillsTex = GetTexture(0x28700000001); // "eod_costumetrunkback"

            try
            {
                metroBackTex = metroBackRef.Get(gd);
                metroSliderTex = metrosliderRef.Get(gd);
                metroCaseTex = metroCaseRef.Get(gd);
                skillLevelTex = skillLevelRef.Get(gd);
            }
            catch (Exception e)
            {

            }

            MetronomeCase = new UIImage(metroCaseTex)
            {
                X = 34,
                Y = 92,
                Visible = true
            };
            Add(MetronomeCase);
            UpperUIElements.Add(MetronomeCase);

            ActivePayoutY = MetronomeCase.Y + 48;
            LeftPayoutX = MetronomeCase.X + 6;

            BehindSkillsBG = new UISlotsImage(behindSkillsTex)
            {
                X = MetronomeCase.X + 232,
                Y = MetronomeCase.Y + 29,
                ScaleX = 0.75f,
                ScaleY = 0.75f
            };
            BehindSkillsBG.SetBounds(111, 10, 306, 116);
            Add(BehindSkillsBG);
            UpperUIElements.Add(BehindSkillsBG);

            // textfields for the skill names and eventual values
            CharismaLevelTextEdit = new UITextEdit()
            {
                X = BehindSkillsBG.X + 9,
                Y = BehindSkillsBG.Y + 6,
                Mode = UITextEditMode.ReadOnly,
                Alignment = TextAlignment.Left,
                Size = new Vector2(150, 14),
                CurrentText = GameFacade.Strings["UIText", "253", "21"] + ": %d"
            };

            var newStyle = CharismaLevelTextEdit.TextStyle.Clone();
            newStyle.Size = 11;
            var newStyleAqua = newStyle.Clone();
            newStyleAqua.Color = Color.Aqua;
            var newStyleSmall = newStyle.Clone();
            newStyleSmall.Size = 8;
            var newStyleSmallAqua = newStyleSmall.Clone();
            newStyleSmallAqua.Color = Color.Aqua;

            CharismaLevelTextEdit.TextStyle = newStyle;
            Add(CharismaLevelTextEdit);
            UpperUIElements.Add(CharismaLevelTextEdit);

            BodyLevelTextEdit = new UITextEdit()
            {
                X = BehindSkillsBG.X + 9,
                Y = CharismaLevelTextEdit.Y + 18,
                Mode = UITextEditMode.ReadOnly,
                Alignment = TextAlignment.Left,
                Size = new Vector2(150, 14),
                CurrentText = GameFacade.Strings["UIText", "253", "22"] + ": %d",
                TextStyle = newStyle
            };
            Add(BodyLevelTextEdit);
            UpperUIElements.Add(BodyLevelTextEdit);

            Creativity1LevelTextEdit = new UITextEdit()
            {
                X = BehindSkillsBG.X + 9,
                Y = BodyLevelTextEdit.Y + 18,
                Mode = UITextEditMode.ReadOnly,
                Alignment = TextAlignment.Left,
                Size = new Vector2(150, 14),
                CurrentText = GameFacade.Strings["UIText", "253", "23"] + ": %d",
                TextStyle = newStyle
            };
            Add(Creativity1LevelTextEdit);
            UpperUIElements.Add(Creativity1LevelTextEdit);

            Creativity2LevelTextEdit = new UITextEdit()
            {
                X = BehindSkillsBG.X + 9,
                Y = Creativity1LevelTextEdit.Y + 18,
                Mode = UITextEditMode.ReadOnly,
                Alignment = TextAlignment.Left,
                Size = new Vector2(150, 14),
                CurrentText = GameFacade.Strings["UIText", "253", "24"] + ": %d",
                TextStyle = newStyle
            };
            Add(Creativity2LevelTextEdit);
            UpperUIElements.Add(Creativity2LevelTextEdit);

            SkillLevels = new UISlotsImage[4];
            for (int index = 0; index < SkillLevels.Length; index++)
            {
                SkillLevels[index] = new UISlotsImage(skillLevelTex)
                {
                    X = CharismaLevelTextEdit.X + 110,
                    Y = CharismaLevelTextEdit.Y + index * 18 + 4,
                    ScaleX = 0.85f
                };
                SkillLevels[index].SetBounds(0, 0, SkillLevels[index].Texture.Width, SkillLevels[index].Texture.Height);
                Add(SkillLevels[index]);
            }

            TotalSkillLevelsTextEdit = new UITextEdit()
            {
                X = CharismaLevelTextEdit.X,
                Y = MetronomeCase.Y + 10,
                Size = new Vector2(214, 14),
                Alignment = TextAlignment.Center,
                Mode = UITextEditMode.ReadOnly,
                CurrentText = GameFacade.Strings.GetString("f124", "1") // "Total Skill Levels: %d.%02d"
            };
            Add(TotalSkillLevelsTextEdit);
            UpperUIElements.Add(TotalSkillLevelsTextEdit);

            PayoutsTextEdit = new UITextEdit()
            {
                X = MetronomeCase.X + 130,
                Y = MetronomeCase.Y + 10,
                Size = new Vector2(100, 14),
                Alignment = TextAlignment.Center,
                Mode = UITextEditMode.ReadOnly,
                CurrentText = GameFacade.Strings.GetString("f124", "2") // "Payout"
            };
            Add(PayoutsTextEdit);
            UpperUIElements.Add(PayoutsTextEdit);

            CurrentPayoutLabelTextEdit = new UITextEdit()
            {
                X = PayoutsTextEdit.X,
                Y = CharismaLevelTextEdit.Y,
                Size = new Vector2(100, 14),
                Mode = UITextEditMode.ReadOnly,
                Alignment = TextAlignment.Center,
                TextStyle = newStyle,
                CurrentText = GameFacade.Strings.GetString("f124", "3") // "Current"
            };
            Add(CurrentPayoutLabelTextEdit);
            UpperUIElements.Add(CurrentPayoutLabelTextEdit);

            CurrentPayoutTextEdit = new UITextEdit()
            {
                X = CurrentPayoutLabelTextEdit.X,
                Y = BodyLevelTextEdit.Y,
                Size = new Vector2(100, 14),
                Mode = UITextEditMode.ReadOnly,
                Alignment = TextAlignment.Center,
                TextStyle = newStyle,
                CurrentText = "$0"
            };
            Add(CurrentPayoutTextEdit);
            UpperUIElements.Add(CurrentPayoutTextEdit);

            MinimumPayoutLabelTextEdit = new UITextEdit()
            {
                X = CurrentPayoutLabelTextEdit.X,
                Y = Creativity1LevelTextEdit.Y,
                Size = new Vector2(100, 14),
                Mode = UITextEditMode.ReadOnly,
                Alignment = TextAlignment.Center,
                TextStyle = newStyleAqua,
                CurrentText = GameFacade.Strings.GetString("f124", "4") // "Minimum"
            };
            Add(MinimumPayoutLabelTextEdit);
            UpperUIElements.Add(MinimumPayoutLabelTextEdit);

            MinimumPayoutTextEdit = new UITextEdit()
            {
                X = CurrentPayoutLabelTextEdit.X,
                Y = Creativity2LevelTextEdit.Y,
                Size = new Vector2(100, 14),
                Mode = UITextEditMode.ReadOnly,
                Alignment = TextAlignment.Center,
                TextStyle = newStyleAqua,
                CurrentText = "$0"
            };
            Add(MinimumPayoutTextEdit);
            UpperUIElements.Add(MinimumPayoutTextEdit);

            Metronome = new UIMaskedContainer(new Rectangle(0, 0, 114, 100))
            {
                X = MetronomeCase.X + 9,
                Y = MetronomeCase.Y + 12
            };
            Add(Metronome);
            UpperUIElements.Add(Metronome);

            MetronomeBack = new UIImage(metroBackTex)
            {
                AbstractY = ZeroNotesY = metroBackTex.Height / 2f - 50f
            };
            Metronome.Add(MetronomeBack);
            Metronome.UpdateChildMasks();

            PayoutTextEdits = new UITextEdit[26];

            for (int index = 0; index < PayoutTextEdits.Length; index++)
            {
                int payout = index * index;
                if (index > 0 && Int32.TryParse(PayoutTextEdits[index - 1].CurrentText, out int prior))
                {
                    payout += prior;
                }
                PayoutTextEdits[index] = new UITextEdit()
                {
                    X = (index % 2 == 0) ? LeftPayoutX + 60 : LeftPayoutX,
                    Y = ActivePayoutY + 40 * index,
                    Size = new Vector2(60, 14),
                    Mode = UITextEditMode.ReadOnly,
                    Alignment = TextAlignment.Center,
                    TextStyle = (index % 5 == 0) ? newStyleSmallAqua : newStyleSmall,
                    CurrentText = "" + payout,
                    Visible = index == 0
                };
                Add(PayoutTextEdits[index]);
            }

            // Add these last to cover the UITextEdits as they change their Y position, allow for a hiding effect
            MetronomeCaseEdge = new UISlotsImage(metroCaseTex)
            {
                Position = MetronomeCase.Position
            };
            MetronomeCaseEdge.SetBounds(0, 0, 134, 12);
            Add(MetronomeCaseEdge);
            UpperUIElements.Add(MetronomeCaseEdge);

            MetronomeSlider = new UIImage(metroSliderTex)
            {
                X = Metronome.X + 7,
                Y = Metronome.Y
            };
            Add(MetronomeSlider);
            UpperUIElements.Add(MetronomeSlider);
        }
        private void PayoutVisibility(UITextEdit payout)
        {
            payout.Visible = payout.Y < ActivePayoutY + 16 && payout.Y > MetronomeCaseEdge.Y;
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
