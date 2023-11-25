using System;
using System.Collections.Generic;
using System.Timers;
using FSO.SimAntics.Model;
using FSO.SimAntics.NetPlay.EODs.Model;
using FSO.SimAntics.NetPlay.EODs.Utils;

namespace FSO.SimAntics.NetPlay.EODs.Handlers
{
    public class VMEODBandPlugin : VMEODHandler
    {
        private VMEODClient Controller;
        private VMEODBandStates NextState;
        private VMEODBandStates State;
        private VMEODBandStates NoteState;
        private EODLobby<VMEODBandSlot> Lobby;
        private Random IsBuzzNoteRandom = new Random();
        private Random NonBuzzNoteRandom = new Random();
        private Timer SequenceTimer;
        private List<byte> Song;
        private short CurrentSongLength;
        private int CurrentNote;
        private int UITimer = -1;
        private int TimerFrames;
        private decimal CombinedSkillAmount;
        private int[] PayoutScheme;
        
        public const int PRESHOW_TIMER_DEFAULT = 10;
        public const int DECISION_TIMER_DEFAULT = 10;
        public const int NOTE_TIMER_DEFAULT = 10;
        public const int MAX_SONG_LENGTH = 25;
        public const int BUZZ_NOTE_FREQUENCY = 72;
        public const int SKILL_PAYOUT_MULTIPLIER = 18;
        public const double MILLISECONDS_PER_NOTE_IN_SEQUENCE = 1500;

        private readonly object NoteDecision = new object();

        public VMEODBandPlugin(VMEODServer server) : base(server)
        {
            Lobby = new EODLobby<VMEODBandSlot>(server, 4)
                    .BroadcastPlayersOnChange("Band_Players")
                    .OnFailedToJoinDisconnect();

            State = VMEODBandStates.Lobby;
            SequenceTimer = new Timer(MILLISECONDS_PER_NOTE_IN_SEQUENCE);
            SequenceTimer.Elapsed += SequenceTimerElapsedHandler;

            InitPayoutScheme();

            // event listeners
            BinaryHandlers["Band_Decision"] = RockOnOrSellOutHandler;
            BinaryHandlers["Band_Note"] = NoteSelectedHandler;
            SimanticsHandlers[(short)VMEODBandEventTypes.AnimationsFinished] = AnimationsFinishedHandler;
        }

        public override void OnConnection(VMEODClient client)
        {
            var args = client.Invoker.Thread.TempRegisters;
            // client belongs to a player
            if (client.Avatar != null)
            {
                if ((args[0] > -1) && (args[0] < 4))
                {
                    if (Lobby.Join(client, args[0]))
                    {
                        client.Send("Band_UI_Init", new byte[] { (byte)args[0] });
                        var slot = Lobby.GetSlotData(client);
                        if (slot != null)
                        {
                            slot.AvatarName = client.Avatar.Name;
                            slot.Instrument = (VMEODBandInstrumentTypes)Enum.ToObject(typeof(VMEODBandInstrumentTypes), args[0]);
                            slot.SkillAmount = GetAvatarsCurrentSkill(client);

                            if (Lobby.IsFull())
                                EnqueueGotoState(VMEODBandStates.PreShow);
                        }
                    }
                }
            }
            // client belongs to the smart tile, is contoller
            else
            {
                Controller = client;
            }
            base.OnConnection(client);
        }

        public override void OnDisconnection(VMEODClient client)
        {
            Lobby.Leave(client);
            EnqueueGotoState(VMEODBandStates.Lobby);
            base.OnDisconnection(client);
        }

        public override void Tick()
        {
            if (NextState != VMEODBandStates.Invalid)
            {
                var state = NextState;
                NextState = VMEODBandStates.Invalid;
                GotoState(state);
            }

            if (Controller != null)
            {
                if (UITimer > 0)
                {
                    if (++TimerFrames >= 30)
                    {
                        TimerFrames = 0;
                        UITimer--;
                        SendTime();
                    }
                }
                switch (State)
                {
                    case VMEODBandStates.PreShow:
                        {
                            if (UITimer == 0)
                                EnqueueGotoState(VMEODBandStates.Rehearsal);
                            break;
                        }
                    case VMEODBandStates.Performance:
                        {
                            if (UITimer == 0)
                                GameOver(false, "Band_Timeout");
                            break;
                        }
                    case VMEODBandStates.Intermission:
                        {
                            if (ValidateRockOn(false))
                                EnqueueGotoState(VMEODBandStates.Rehearsal);
                            else if (UITimer == 0)
                            {
                                if (ValidateRockOn(true))
                                    EnqueueGotoState(VMEODBandStates.Rehearsal);
                                else
                                    GameOver(true, null);
                            }
                            break;
                        }
                    case VMEODBandStates.Finale:
                        {
                            if (UITimer == 0)
                                EnqueueGotoState(VMEODBandStates.Lobby);
                            break;
                        }
                }
            }
        }
        private void EnqueueGotoState(VMEODBandStates newState)
        {
            if (State != newState)
                NextState = newState;
        }
        private void GotoState(VMEODBandStates newState)
        {
            if (!Lobby.IsFull() && !newState.Equals(VMEODBandStates.Lobby))
                EnqueueGotoState(VMEODBandStates.Lobby);
            else
            {
                State = newState;

                switch (State)
                {
                    case VMEODBandStates.Lobby:
                        {
                            SetTimer(-1);
                            break;
                        }
                    case VMEODBandStates.PreShow:
                        {
                            InitGame(PRESHOW_TIMER_DEFAULT);
                            Lobby.Broadcast("Band_Show", new byte[0]);
                            break;
                        }
                    case VMEODBandStates.Rehearsal:
                        {
                            SetTimer(-1);
                            ResetRockOn();
                            PlayNextSequence();
                            break;
                        }
                    case VMEODBandStates.Performance:
                        {
                            NoteState = VMEODBandStates.Performance;
                            Lobby.Broadcast("Band_Performance", new byte[0]);
                            SetTimer(NOTE_TIMER_DEFAULT);
                            break;
                        }
                    case VMEODBandStates.Intermission:
                        {
                            // ask players if they wish to continue, and update payout string
                            Lobby.Broadcast("Band_Intermission", GetPayoutData());
                            SetTimer(DECISION_TIMER_DEFAULT);
                            break;
                        }
                    case VMEODBandStates.MinPayment:
                        {
                            // reduce the current song length to the closest achieved minimum
                            CurrentSongLength -= (short)(CurrentSongLength % 5);
                            break;
                        }
                    case VMEODBandStates.Electric:
                        {
                            SetTimer(-1);
                            Lobby.Broadcast("Band_Electric", new byte[0]);
                            break;
                        }
                }
            }
        }
        /*
         * This Simantics event only happens after a win or a loss and after each sequence round.
         */
        private void AnimationsFinishedHandler(short evt, VMEODClient client)
        {
            if (State.Equals(VMEODBandStates.MinPayment))
            {
                EnqueueGotoState(VMEODBandStates.Finale);
                Lobby.Broadcast("Band_Win", Data.VMEODGameCompDrawACardData.SerializeStrings(CumulativePayout + "", "" + CurrentSongLength));
                // send the song length for the win animation
                Controller.SendOBJEvent(new VMEODEvent((short)VMEODBandEventTypes.NewSongLength, CurrentSongLength));
                // get the prorated skill payout amount
                short skillPayout = (short)Math.Round(0m + (CombinedSkillAmount * SKILL_PAYOUT_MULTIPLIER * CurrentSongLength) / MAX_SONG_LENGTH);
                Controller.SendOBJEvent(new VMEODEvent((short)VMEODBandEventTypes.NewSkillPayout, skillPayout));
                // send the win amount
                Controller.SendOBJEvent(new VMEODEvent((short)VMEODBandEventTypes.WinRound, (short)CumulativePayout));
            }
            else if (State.Equals(VMEODBandStates.Electric))
            {
                if (Lobby.IsFull())
                    EnqueueGotoState(VMEODBandStates.Intermission);
            }
            else
            {
                if (Lobby.IsFull())
                    EnqueueGotoState(VMEODBandStates.PreShow);
            }
        }
        /*
         * Timed to trigger after each sequence is played for the players, allowing them to now attempt to play it back
         */
        private void SequenceTimerElapsedHandler(object source, ElapsedEventArgs args)
        {
            SequenceTimer.Stop();
            EnqueueGotoState(VMEODBandStates.Performance);
        }
        /*
         * Client has pushed RockOnBtn or SellOutBtn
         */
        private void RockOnOrSellOutHandler(string evt, byte[] playerChoice, VMEODClient client)
        {
            if (playerChoice == null)
                return;
            if (Lobby.GetPlayerSlot(client) == -1)
                return;
            var slot = Lobby.GetSlotData(client);
            if (slot != null)
                slot.RockOn = (playerChoice[0] == 1);
        }
        /*
         * Client has pushed a valid note button
         */
        private void NoteSelectedHandler(string evt, byte[] playerChoice, VMEODClient client)
        {
            if (client == null || Lobby.GetPlayerSlot(client) == -1 || playerChoice.Length == 0 || !State.Equals(VMEODBandStates.Performance))
                return;
            var slot = Lobby.GetSlotData(client);
            if (slot == null)
                return;
            
            byte note = 9;

            lock (NoteDecision)
            {
                if (State.Equals(VMEODBandStates.Performance) && NoteState.Equals(VMEODBandStates.Performance))
                {
                    bool isLegal = false;
                    note = playerChoice[0];

                    if (note == (byte)VMEODBandNoteTypes.Buzz)
                        isLegal = true;
                    else
                    {
                        // can this player even legally play this note?
                        switch (slot.Instrument)
                        {
                            case VMEODBandInstrumentTypes.Trumpet:
                                isLegal = (note == (byte)VMEODBandNoteTypes.Do) || (note == (byte)VMEODBandNoteTypes.Re); break;
                            case VMEODBandInstrumentTypes.Drums:
                                isLegal = (note == (byte)VMEODBandNoteTypes.Mi) || (note == (byte)VMEODBandNoteTypes.Fa); break;
                            case VMEODBandInstrumentTypes.Guitar:  // Creativity 2, Maxis has it backwards. I hate you, Maxis.
                                isLegal = (note == (byte)VMEODBandNoteTypes.So) || (note == (byte)VMEODBandNoteTypes.La); break;
                            case VMEODBandInstrumentTypes.Keyboard: // Creativity 1, Maxis has it backwards. I hate you, Maxis.
                                isLegal = (note == (byte)VMEODBandNoteTypes.Ti) || (note == (byte)VMEODBandNoteTypes.Doh); break;
                        }
                    }
                    if (!isLegal)
                        note = 9; // not legal, do nothing below
                    else
                        NoteState = VMEODBandStates.BlockEvents;
                }
            }
            // handle the legal note
            if (note < 9)
            {
                // play the note back to the other clients
                Lobby.Broadcast("Band_Note_Sync", new byte[] { note });

                // check the result
                if (Song[CurrentNote] == note)
                {
                    // note is correct but it is the buzz note
                    if (Song[CurrentNote] == (byte)VMEODBandNoteTypes.Buzz)
                        GameOver(false, "Band_Buzz");
                    // note is correct and players have reached the end of the sequence
                    else if (CurrentNote == CurrentSongLength - 1)
                        SequenceEndHandler(slot.Instrument);
                    else
                    {
                        // move on to the next note
                        CurrentNote++;
                        NoteState = VMEODBandStates.Performance;
                        Lobby.Broadcast("Band_Continue_Performance", new byte[0]);
                        SetTimer(NOTE_TIMER_DEFAULT);
                    }
                }
                else // wrong note
                {
                    string failuresName = Lobby.GetSlotData(client).AvatarName;
                    if (failuresName != null && failuresName.Length > 0)
                        Lobby.Broadcast("Band_Fail", failuresName);
                    GameOver(false, null);
                }
            }
        }

        private void SequenceEndHandler(VMEODBandInstrumentTypes lastPlayer)
        {
            SetTimer(-1);
            if (CurrentSongLength == 25)
                GameOver(true, null);
            // if players didn't just finish a 25 note sequence, move into the decision round
            else
            {
                // NEW: send a simantics event to play a sound from the last player instrument type since they just finished the sequence
                Controller.SendOBJEvent(new VMEODEvent((short)VMEODBandEventTypes.LastPlayer, new short[] { (short)lastPlayer }));
                EnqueueGotoState(VMEODBandStates.Electric);
            }
        }
        private byte[] GetPayoutData()
        {
            string nextPayout = (CurrentSongLength < MAX_SONG_LENGTH) ? PayoutScheme[CurrentSongLength + 1] + "" : "";
            return Data.VMEODGameCompDrawACardData.SerializeStrings(new string[] { CumulativePayout + "", nextPayout, "" + CurrentSongLength });
        }
        private short CumulativePayout
        {
            get
            {
                return (short)PayoutScheme[CurrentSongLength];
            }
        }
        private void InitPayoutScheme()
        {
            PayoutScheme = new int[MAX_SONG_LENGTH + 1];
            for (int index = 1; index < PayoutScheme.Length; index++)
            {
                if (index == 1)
                    PayoutScheme[1] = 40;
                else
                {
                    PayoutScheme[index] = index * index + (20 - index) * 5 + PayoutScheme[index - 1];
                    if (index % 5 == 0) // bonus for milestone
                        PayoutScheme[index] += 200;
                }
            }
        }
        private void SetTimer(int newValue)
        {
            UITimer = newValue;
            TimerFrames = 0;
            if (UITimer > 0)
                SendTime();
            else
                Lobby.Broadcast("Band_Timer", "" + Byte.MinValue);
        }

        private void SendTime()
        {
            Lobby.Broadcast("Band_Timer", ""  + UITimer);
        }
        private void InitGame(int Timer)
        {
            StartNewGame();
            SetTimer(Timer);
        }
        private void StartNewGame()
        {
            SetTimer(-1);

            // Get a new song
            Song = GetNewSong();
            CurrentSongLength = 0;

            // Get the UPDATED combined skill values of the players for payout purposes
            CombinedSkillAmount = GetUpdatedSkillAmounts();

            // Reset the payout string and status/help message, essentially
            Lobby.Broadcast("Band_Game_Reset_Skill", Data.VMEODGameCompDrawACardData.SerializeStrings(CombinedSkillAmount + ""));
        }
        /*
         * Demonstrate the sequence to be played back
         */
        private void PlayNextSequence()
        {
            CurrentNote = 0;
            CurrentSongLength++;
            SequenceTimer.Interval = MILLISECONDS_PER_NOTE_IN_SEQUENCE * (CurrentSongLength + 2);
            Lobby.Broadcast("Band_Sequence", GetCurrentSequence());
            SequenceTimer.Start();
        }

        private bool ValidateRockOn(bool timeExpired)
        {
            int rockOnCount = 0;
            int selloutCount = 0;
            foreach (var player in Lobby.Players)
            {
                var slot = Lobby.GetSlotData(player);
                if (slot == null)
                    continue;
                if (slot.RockOn == null)
                {
                    if (timeExpired)
                    {
                        // rockon is chosen for you if you never chose
                        slot.RockOn = true;
                        player.Send("Band_RockOn", "");
                        rockOnCount++;
                    }
                }
                else if (slot.RockOn == true)
                    rockOnCount++;
                else selloutCount++;
            }
            if (selloutCount > 2)
                SetTimer(0);
            return rockOnCount > 1;
        }

        private void ResetRockOn()
        {
            foreach (var player in Lobby.Players)
            {
                var slot = Lobby.GetSlotData(player);
                if (slot != null)
                    slot.RockOn = null;
            }
        }

        /*
         * The object handles the payout, but the payout amount must be sent. The song length is sent for the animations of winning.
         */
        private void GameOver(bool win, string loseEvent)
        {
            SetTimer(-1);
            EnqueueGotoState(VMEODBandStates.Finale);
            if (win)
            {
                Lobby.Broadcast("Band_Win", Data.VMEODGameCompDrawACardData.SerializeStrings(CumulativePayout + "", "" + CurrentSongLength));
                // send the song length for the win animation
                Controller.SendOBJEvent(new VMEODEvent((short)VMEODBandEventTypes.NewSongLength, CurrentSongLength));
                // get the prorated skill payout amount
                short skillPayout = (short)Math.Round(0m + (CombinedSkillAmount * SKILL_PAYOUT_MULTIPLIER * CurrentSongLength) / MAX_SONG_LENGTH);
                Controller.SendOBJEvent(new VMEODEvent((short)VMEODBandEventTypes.NewSkillPayout, skillPayout));
                // send the win amount
                Controller.SendOBJEvent(new VMEODEvent((short)VMEODBandEventTypes.WinRound, (short)CumulativePayout));
            }
            else
            {
                // decrement song length, because this one was a failure, but the last one was a success or 0
                CurrentSongLength--;
                if (loseEvent != null)
                    Lobby.Broadcast(loseEvent, new byte[0]);
                // pay minimum payout, if applicable
                if (CurrentSongLength / 5 > 0)
                    EnqueueGotoState(VMEODBandStates.MinPayment);
                Controller.SendOBJEvent(new VMEODEvent((short)VMEODBandEventTypes.LoseRound));
            }
        }

        private byte[] GetCurrentSequence()
        {
            var sequence = new byte[CurrentSongLength];
            Song.CopyTo(0, sequence, 0, CurrentSongLength);
            return sequence;
        }

        private List<byte> GetNewSong()
        {
            List<byte> songList = new List<byte>(MAX_SONG_LENGTH);

            while (songList.Count < songList.Capacity)
            {
                int noteValue = (byte)VMEODBandNoteTypes.Buzz;
                // small % chance to be a buzz note
                if (IsBuzzNoteRandom.Next(1, short.MaxValue) > BUZZ_NOTE_FREQUENCY) // if not a buzz note
                {
                    // any note from 1 to 8
                    noteValue = NonBuzzNoteRandom.Next((byte)VMEODBandNoteTypes.Do, ((byte)(VMEODBandNoteTypes.Doh) + 1));
                }
                songList.Add((byte)noteValue);
            }
            return songList;
        }

        private decimal GetAvatarsCurrentSkill(VMEODClient client)
        {
            var avatar = client.Avatar;
            var slot = Lobby.GetSlotData(client);
            if (slot.Instrument.Equals(VMEODBandInstrumentTypes.Trumpet))
                return avatar.GetPersonData(VMPersonDataVariable.CharismaSkill) / 100m;
            else if (slot.Instrument.Equals(VMEODBandInstrumentTypes.Drums))
                return avatar.GetPersonData(VMPersonDataVariable.BodySkill) / 100m;
            else
                return avatar.GetPersonData(VMPersonDataVariable.CreativitySkill) / 100m;
        }

        private void UpdatePlayersSkills()
        {
            foreach (var client in Lobby.Players)
            {
                if (client != null)
                {
                    var avatar = client.Avatar;
                    var slot = Lobby.GetSlotData(client);
                    slot.SkillAmount = GetAvatarsCurrentSkill(client);
                }
            }
        }

        private decimal GetUpdatedSkillAmounts()
        {
            UpdatePlayersSkills();
            decimal newAmount = 0;
            for (var index = 0; index < 4; index++)
            {
                newAmount += (Lobby.GetSlotData(index)).SkillAmount;
            }
            return newAmount;
        }
    }

    public class VMEODBandSlot
    {
        public string AvatarName;
        public VMEODBandInstrumentTypes Instrument;
        public decimal SkillAmount;
        public Nullable<bool> RockOn;
    }

    public enum VMEODBandNoteTypes : byte
    {
        Buzz = 0,
        Do = 1,
        Re = 2,
        Mi = 3,
        Fa = 4,
        So = 5,
        La = 6,
        Ti = 7,
        Doh = 8
    }

    public enum VMEODBandInstrumentTypes : short
    {
        Trumpet = 0,
        Drums = 1,
        Guitar = 2, // Creativity 2, Maxis has it backwards. I hate you, Maxis.
        Keyboard = 3 // Creativity 1, Maxis has it backwards. I hate you, Maxis.
    }

    public enum VMEODBandStates : short
    {
        Invalid = -1,
        Lobby = 0,
        PreShow = 1, // 5 seconds before the start of the game
        Rehearsal = 2, // demonstrating the note pattern, memorize this
        Performance = 3, // play the note pattern back
        Intermission = 4, // do you rock on, or do you sell out
        Finale = 5, // winning or losing animations
        BlockEvents = 6,
        MinPayment = 7,
        Electric = 8
    }

    public enum VMEODBandEventTypes : short
    {
        ConnectToServer = -2,
        DisconnectFromServer = -1,
        NewGame = 0,
        LoseRound = 2,
        WinRound = 3,
        AnimationsFinished = 4,
        NewSongLength = 5,
        NewSkillPayout = 6,
        LastPlayer = 7
    }
}
