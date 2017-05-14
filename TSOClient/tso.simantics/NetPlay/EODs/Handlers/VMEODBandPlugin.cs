using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Timers;
using FSO.SimAntics.Model;
using FSO.SimAntics.NetPlay.EODs.Model;
using FSO.SimAntics.NetPlay.EODs.Utils;

namespace FSO.SimAntics.NetPlay.EODs.Handlers
{
    public class VMEODBandPlugin : VMEODHandler
    {
        private VMEODClient Controller;
        private VMEODBandStates State;
        private EODLobby<VMEODBandSlot> Lobby;
        private Random IsBuzzNoteRandom = new Random();
        private Random NonBuzzNoteRandom = new Random();
        private Timer SequenceTimer;
        private List<byte> Song;
        private short CurrentSongLength;
        private int CurrentNote;
        private int UITimer = -1;
        private int TimerFrames;
        private int CumulativePayout;
        private decimal CombinedSkillAmount;

        public const int FINALE_TIMER_DFEAULT = 9;
        public const int PRESHOW_TIMER_DEFAULT = 3;
        public const int DECISION_TIMER_DEFAULT = 5;
        public const int NOTE_TIMER_DEFAULT = 5;
        public const int MAX_SONG_LENGTH = 25;
        public const int BUZZ_NOTE_FREQUENCY = 72;
        public const int SKILL_PAYOUT_MULTIPLIER = 18;
        public const double MILLISECONDS_PER_NOTE_IN_SEQUENCE = 1500;

        public VMEODBandPlugin(VMEODServer server) : base(server)
        {
            Lobby = new EODLobby<VMEODBandSlot>(server, 4)
                    .BroadcastPlayersOnChange("Band_Players")
                    .OnFailedToJoinDisconnect();

            State = VMEODBandStates.Idle;
            SequenceTimer = new Timer(MILLISECONDS_PER_NOTE_IN_SEQUENCE);
            SequenceTimer.Elapsed += SequenceTimerElapsedHandler;

            // event listeners
            BinaryHandlers["Band_Decision"] = RockOnOrSellOutHandler;
            BinaryHandlers["Band_Note"] = NoteSelectedHandler;
            SimanticsHandlers[(short)VMEODBandEventTypes.NewGame] = NewGameHandler;
            SimanticsHandlers[(short)VMEODBandEventTypes.AnimationsFinished] = AnimationsFinishedHandler;
        }

        public override void OnConnection(VMEODClient client)
        {
            var args = client.Invoker.Thread.TempRegisters;
            // client belongs to a player
            if (client.Avatar != null)
            {
                if ((args != null) && (args[0] > -1) && (args[0] < 4))
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
                                InitGame();
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
            State = VMEODBandStates.Idle;
            base.OnDisconnection(client);
            SetTimer(-1);
        }

        public override void Tick()
        {
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
                            {
                                Lobby.Broadcast("Band_Show", "");
                                PlayNextSequence();
                            }
                            break;
                        }
                    case VMEODBandStates.Performance:
                        {
                            if (UITimer == 0)
                            {
                                Lobby.Broadcast("Band_Timeout", "");
                                GameOver(false);
                            }
                            break;
                        }
                    case VMEODBandStates.Intermission:
                        {
                            if (UITimer == 0)
                            {
                                if (RockOn())
                                    PlayNextSequence();
                                else
                                    GameOver(true);
                            }
                            break;
                        }
                    case VMEODBandStates.Finale:
                        {
                            if (UITimer == 0)
                                InitGame();
                            break;
                        }
                }
            }
        }
        /*
         * This Simantics event only happens after a win or a loss, not after each sequence round.
         */
        private void AnimationsFinishedHandler(short evt, VMEODClient client)
        {
            //ResetGame();
        }
        /*
         * This Simantics event happens after a win or a loss and each time a new player joins.
         */
        private void NewGameHandler(short evt, VMEODClient client)
        {
            if (State.Equals(VMEODBandStates.Finale))
            {
                InitGame(FINALE_TIMER_DFEAULT);
            }
        }
        /*
         * Timed to trigger after each sequence is played for the players, allowing them to now attempt to play it back
         */
        private void SequenceTimerElapsedHandler(object source, ElapsedEventArgs args)
        {
            SequenceTimer.Stop();
            State = VMEODBandStates.Performance;
            Lobby.Broadcast("Band_Performance", "");
            SetTimer(NOTE_TIMER_DEFAULT);
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
            if (playerChoice == null)
                return;
            if (Lobby.GetPlayerSlot(client) == -1)
                return;
            var slot = Lobby.GetSlotData(client);
            if (slot == null)
                return;
            
            byte note = 9;

            lock (this)
            {
                if (State.Equals(VMEODBandStates.Performance))
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
                                {
                                    if ((note == (byte)VMEODBandNoteTypes.Do) || (note == (byte)VMEODBandNoteTypes.Re))
                                        isLegal = true;
                                    break;
                                }
                            case VMEODBandInstrumentTypes.Drums:
                                {
                                    if ((note == (byte)VMEODBandNoteTypes.Mi) || (note == (byte)VMEODBandNoteTypes.Fa))
                                        isLegal = true;
                                    break;
                                }
                            case VMEODBandInstrumentTypes.Guitar:  // Creativity 2, Maxis has it backwards. I hate you, Maxis.
                                {
                                    if ((note == (byte)VMEODBandNoteTypes.So) || (note == (byte)VMEODBandNoteTypes.La))
                                        isLegal = true;
                                    break;
                                }
                            case VMEODBandInstrumentTypes.Keyboard: // Creativity 1, Maxis has it backwards. I hate you, Maxis.
                                {
                                    if ((note == (byte)VMEODBandNoteTypes.Ti) || (note == (byte)VMEODBandNoteTypes.Doh))
                                        isLegal = true;
                                    break;
                                }
                        }
                    }
                    if (!isLegal)
                        note = 9;
                    else
                        State = VMEODBandStates.BlockEvents;
                }
                else return;
            }
            if (note < 9)
            {
                // check the result
                if (Song[CurrentNote] == note)
                {
                    // note is correct but it is the buzz note
                    if (Song[CurrentNote] == (byte)VMEODBandNoteTypes.Buzz)
                    {
                        Lobby.Broadcast("Band_Buzz", "");
                        GameOver(false);
                    }
                    // note is correct and players have reached the end of the sequence
                    else if (CurrentNote == CurrentSongLength - 1)
                        SequenceEndHandler();
                    else
                    {
                        // move on to the next note
                        CurrentNote++;
                        Lobby.Broadcast("Band_Performance", "");
                        State = VMEODBandStates.Performance;
                        SetTimer(NOTE_TIMER_DEFAULT);
                    }
                }
                else // wrong note
                {
                    string failuresName = Lobby.GetSlotData(client).AvatarName;
                    Lobby.Broadcast("Band_Fail", failuresName);
                    GameOver(false);
                }
            }
        }

        private void SequenceEndHandler()
        {
            SetTimer(-1);

            // update payout
            CumulativePayout += CurrentSongLength * CurrentSongLength;

            if (CurrentSongLength == 25)
                GameOver(true);
            // if players didn't just finish a 25 note sequence, move into the decision round
            else
            {
                State = VMEODBandStates.Intermission;
                // ask players if they wish to continue, and update payout string
                Lobby.Broadcast("Band_Intermission", CumulativePayout + "");
                SetTimer(DECISION_TIMER_DEFAULT);
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
        private void InitGame()
        {
            InitGame(PRESHOW_TIMER_DEFAULT);
        }
        private void InitGame(int Timer)
        {
            ResetGame();

            // PRESHOW_TIMER_DEFAULT seconds to see the player data before moving into first sequence
            State = VMEODBandStates.PreShow;
            SetTimer(Timer);
        }
        private void ResetGame()
        {
            SetTimer(-1);

            // Get a new song
            Song = GetNewSong();
            CurrentSongLength = 0;
            CumulativePayout = 0;

            // Get the UPDATED combined skill values of the players for payout purposes
            CombinedSkillAmount = GetUpdatedSkillAmounts();

            // Reset the payout string and status/help message, essentially
            Lobby.Broadcast("Band_Game_Reset", "" + CombinedSkillAmount);
        }
        /*
         * Demonstrate the sequence to be played back
         */
        private void PlayNextSequence()
        {
            SetTimer(-1);
            ResetRockOn();
            CurrentNote = 0;
            State = VMEODBandStates.Rehearsal;
            CurrentSongLength++;
            SequenceTimer.Interval = MILLISECONDS_PER_NOTE_IN_SEQUENCE * (CurrentSongLength + 1);
            Lobby.Broadcast("Band_Sequence", GetCurrentSequence());
            SequenceTimer.Start();
        }

        private bool RockOn()
        {
            SetTimer(-1);
            int rockOnCount = 0;
            int sellOutCount = 0;
            foreach (var player in Lobby.Players)
            {
                var slot = Lobby.GetSlotData(player);
                if (slot == null)
                    continue;
                if (slot.RockOn == null)
                {
                    // rockon is chosen for you if you never chose
                    slot.RockOn = true;
                    player.Send("Band_RockOn", "");
                    rockOnCount++;
                }
                else if (slot.RockOn == true)
                    rockOnCount++;
                else
                    sellOutCount++;
            }
            if (sellOutCount > rockOnCount)
                return false;
            else // tie goes to rock on
                return true;
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
        private void GameOver(bool win)
        {
            SetTimer(-1);
            State = VMEODBandStates.Finale;
            if (win)
            {
                Lobby.Broadcast("Band_Win", CumulativePayout + "");
                // send the song length for the win animation
                Controller.SendOBJEvent(new VMEODEvent((short)VMEODBandEventTypes.NewSongLength, CurrentSongLength));
                // get the prorated skill payout amount
                short skillPayout = (short)Math.Round(0m + (CombinedSkillAmount * SKILL_PAYOUT_MULTIPLIER * CurrentSongLength) / MAX_SONG_LENGTH);
                Controller.SendOBJEvent(new VMEODEvent((short)VMEODBandEventTypes.NewSkillPayout, skillPayout));
                // send the win amount
                Controller.SendOBJEvent(new VMEODEvent((short)VMEODBandEventTypes.WinRound, (short)CumulativePayout));
            }
            else
                Controller.SendOBJEvent(new VMEODEvent((short)VMEODBandEventTypes.LoseRound));
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
                var avatar = client.Avatar;
                var slot = Lobby.GetSlotData(client);
                slot.SkillAmount = GetAvatarsCurrentSkill(client);
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
        Idle = 0,
        PreShow = 1, // 5 seconds before the start of the game
        Rehearsal = 2, // demonstrating the note pattern, memorize this
        Performance = 3, // play the note pattern back
        Intermission = 4, // do you rock on, or do you sell out
        Finale = 5, // winning or losing animations
        BlockEvents = 6
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
        NewSkillPayout = 6
    }
}
