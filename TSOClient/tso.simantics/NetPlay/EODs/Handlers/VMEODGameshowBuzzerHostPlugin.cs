using System;
using System.Collections.Generic;
using System.Linq;
using FSO.SimAntics.NetPlay.EODs.Handlers.Data;
using System.Text;
using System.Threading.Tasks;

namespace FSO.SimAntics.NetPlay.EODs.Handlers
{
    public class VMEODGameshowBuzzerHostPlugin : VMEODAbstractGameshowBuzzerPlugin
    {
        private int _AnsweringPlayerIndex;
        private int _EngagedTimer;
        private int _Tock;
        private int _UbiqitousTimer;
        private VMEODGameshowBuzzerStates _BuzzerState;
        private VMEODGameshowBuzzerHostOptions _Options;
        private Random _Random;
        private Action WinnerAction;
        internal double ClientListRefreshedStamp { get; set; }
        internal double SessionStamp { get; set; }
        private VMEODGameshowBuzzerPlayerPlugin[] ConnectedContestants = new VMEODGameshowBuzzerPlayerPlugin[4];
        internal VMEODClient MyClient { get; private set; }

        private readonly object PlayerConfigLock = new object();
        private readonly object PlayerBuzzerLock = new object();

        private bool BuzzerEnabled
        {
            get { return _BuzzerState.Equals(VMEODGameshowBuzzerStates.Ready); }
        }

        public VMEODGameshowBuzzerHostPlugin(VMEODServer server) : base(server)
        {
            BinaryHandlers["Buzzer_Host_FindNewPlayer"] = FindNewPlayerHandler;
            BinaryHandlers["Buzzer_Host_MovePlayerRight"] = MovePlayerRightHandler;
            BinaryHandlers["Buzzer_Host_MovePlayerLeft"] = MovePlayerLeftHandler;
            BinaryHandlers["Buzzer_Host_ToggleEnablePlayer"] = ToggleEnablePlayerHandler;
            BinaryHandlers["Buzzer_Host_PlayerCorrect"] = PlayerCorrectHandler;
            BinaryHandlers["Buzzer_Host_PlayerIncorrect"] = PlayerIncorrectHandler;
            BinaryHandlers["Buzzer_Host_A_ToggleMaster"] = ToggleMasterHandler;
            BinaryHandlers["Buzzer_Host_A_DeclareWinner"] = DeclareWinnerHandler;
            BinaryHandlers["Buzzer_Host_A_Deduct"] = (evt, newFlag, client) => { ToggleOptionHandler(_Options, nameof(VMEODGameshowBuzzerHostOptions.AutoDeductWrongPoints), newFlag, evt); };
            BinaryHandlers["Buzzer_Host_A_Disable"] = (evt, newFlag, client) => { ToggleOptionHandler(_Options, nameof(VMEODGameshowBuzzerHostOptions.AutoDisableOnWrong), newFlag, evt); };
            BinaryHandlers["Buzzer_Host_A_Enable"] = (evt, newFlag, client) => { ToggleOptionHandler(_Options, nameof(VMEODGameshowBuzzerHostOptions.AutoEnableAllOnRight), newFlag, evt); };
            BinaryHandlers["Buzzer_Host_A_BuzzerTime"] = (evt, newTime, client) =>
            { ChangeValueHandler(_Options, nameof(VMEODGameshowBuzzerHostOptions.BuzzerTimeLimit), newTime, MIN_BUZZER_TIME, MAX_BUZZER_TIME, evt, "Buzzer_Host_B_OverBuzzerTime", "Buzzer_Host_B_UnderBuzzerTime"); };
            BinaryHandlers["Buzzer_Host_A_AnswerTime"] = (evt, newTime, client) =>
            { ChangeValueHandler(_Options, nameof(VMEODGameshowBuzzerHostOptions.AnswerTimeLimit), newTime, MIN_ANSWER_TIME, MAX_ANSWER_TIME, evt, "Buzzer_Host_B_OverAnswerTime", "Buzzer_Host_B_UnderAnswerTime"); };
            BinaryHandlers["Buzzer_Host_A_PlayerScore0"] = (evt, newScore, client) => { ChangePlayerScoreHandler(0, newScore); };
            BinaryHandlers["Buzzer_Host_A_PlayerScore1"] = (evt, newScore, client) => { ChangePlayerScoreHandler(1, newScore); };
            BinaryHandlers["Buzzer_Host_A_PlayerScore2"] = (evt, newScore, client) => { ChangePlayerScoreHandler(2, newScore); };
            BinaryHandlers["Buzzer_Host_A_PlayerScore3"] = (evt, newScore, client) => { ChangePlayerScoreHandler(3, newScore); };
            BinaryHandlers["Buzzer_Host_A_GlobalScore"] = (evt, newAmount, client) =>
            { ChangeValueHandler(_Options, nameof(VMEODGameshowBuzzerHostOptions.CorrectAnswerScore), newAmount, 0, MAX_SCORE, evt, "Buzzer_Host_B_OverGlobalScore", "Buzzer_Host_B_UnderGlobalScore"); };
            BinaryHandlers["Buzzer_Host_Request_Roster"] = RequestRosterHandler;

            SimanticsHandlers[(short)VMEODGameshowHostPluginEvents.Host_Judgment_Callback] = JudgmentCallbackHandler;
            SimanticsHandlers[(short)VMEODGameshowHostPluginEvents.Execute_Declare_Winner] = ExecuteDeclareWinnerHandler;
        }

        public override void OnConnection(VMEODClient client)
        {
            if (client.Avatar != null)
            {
                _Random = new Random(DateTime.Now.Millisecond + client.Avatar.ObjectID);
                EODType = VMEODGameshowBuzzerPluginType.Host;
                MyClient = client;
                SessionStamp = ClientListRefreshedStamp = _Random.NextDouble();
                _BuzzerState = VMEODGameshowBuzzerStates.Disabled;
                _Options = new VMEODGameshowBuzzerHostOptions();

                // subscribe to player connection events
                ClientConnect += NewClientConnectedHandler;
                ClientDisconnect += ClientDisconnectedHandler;
                PlayerBuzzed += PlayerBuzzedHandler;

                // find players already connected
                FillContestantSpots();

                client.Send("BuzzerEOD_Init", new byte[] { (byte)VMEODGameshowBuzzerPluginType.Host });
            }
            base.OnConnection(client);
        }
        public override void OnDisconnection(VMEODClient client)
        {
            ClientConnect -= NewClientConnectedHandler;
            ClientDisconnect -= ValidateConnectedContestants;
            PlayerBuzzed -= PlayerBuzzedHandler;
            EODType = VMEODGameshowBuzzerPluginType.Unknown;
            var players = GetConnectedPlayers(-1, false);
            foreach (var player in players)
                player.SessionStamp = 0;
            base.OnDisconnection(client);
        }

        public override void Tick()
        {
            switch (_BuzzerState)
            {
                case VMEODGameshowBuzzerStates.Engaged:
                    {
                        if (++_Tock >= 30)
                        {
                            _Tock = 0;
                            SetUTimer(--_UbiqitousTimer);
                        }
                        if (--_EngagedTimer <= 0)
                            ChangeBuzzerState(VMEODGameshowBuzzerStates.Locked);
                        break;
                    }
                case VMEODGameshowBuzzerStates.Ready:
                    {
                        if (++_Tock >= 30)
                        {
                            _Tock = 0;
                            SetUTimer(--_UbiqitousTimer);
                            if (_UbiqitousTimer <= 0)
                                ChangeBuzzerState(VMEODGameshowBuzzerStates.Expired);
                        }
                        break;
                    }
                case VMEODGameshowBuzzerStates.Locked:
                    {
                        if (++_Tock >= 30)
                        {
                            _Tock = 0;
                            SetUTimer(--_UbiqitousTimer);
                            if (_UbiqitousTimer <= 0)
                                ChangeBuzzerState(VMEODGameshowBuzzerStates.Expired);
                        }
                        break;
                    }
            }
            base.Tick();
        }

        #region events

        private void JudgmentCallbackHandler(short evt, VMEODClient controller)
        {
            MyClient.Send("Buzzer_Host_Round_Restart", new byte[0]);
        }

        private void ExecuteDeclareWinnerHandler(short evt, VMEODClient controller)
        {
            WinnerAction?.Invoke();
            WinnerAction = null;
        }

        /// <summary>
        /// Toggles the MasterBuzzer, allowing or disallowing the players to buzz in and the host to make setting changes.
        /// </summary>
        /// <param name="callbackEvent">Buzzer_Host_A_ToggleMaster</param>
        /// <param name="newFlag">1 is enabled, 0 is disabled</param>
        /// <param name="host"></param>
        private void ToggleMasterHandler(string callbackEvent, byte[] newFlag, VMEODClient host)
        {
            bool isNowEnabled = (newFlag[0] > 0);
            ToggleMasterBuzzer(isNowEnabled);
            // Send callback event to host client
            MyClient.Send(callbackEvent.Replace("_A_", "_B_"), BitConverter.GetBytes(isNowEnabled));
        }
        /// <summary>
        /// Enables or Disables one specfic player from buzzing in when the MasterBuzzer is enabled. This cannot be changed when the MasterBuzzer is enabled.
        /// </summary>
        /// <param name="evt">Buzzer_Host_ToggleEnablePlayer</param>
        /// <param name="data">1 if enabled, 0 if disabled</param>
        /// <param name="host"></param>
        private void ToggleEnablePlayerHandler(string evt, byte[] data, VMEODClient host)
        {
            lock (ConnectedContestants)
            {
                if (!BuzzerEnabled)
                    TogglePlayerBuzzer(BitConverter.ToInt32(data, 0));
                else
                {
                    // buzzer enabled error
                    host.Send("Buzzer_Host_Error", new byte[(byte)VMEODGameshowBuzzerPluginErrors.H_NoChangesAllowed]);
                }
            }
            lock (PlayerConfigLock)
                SendContestantRoster();
        }
        /// <summary>
        /// Toggles one of the few true/false game options. This cannot be changed when the MasterBuzzer is enabled.
        /// </summary>
        /// <param name="options">_Options</param>
        /// <param name="propertyName">String name of a property in VMEODGameshowBuzzerHostOptions</param>
        /// <param name="newValue">1 if enabled, 0 if disabled</param>
        /// <param name="callbackEvent">Buzzer_Host_B_Deduct, Buzzer_Host_B_Disable, or Buzzer_Host_B_Enable</param>
        private void ToggleOptionHandler(object options, string propertyName, byte[] newValue, string callbackEvent)
        {
            var prop = options.GetType().GetProperty(propertyName, System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (prop != null)
            {
                if (!BuzzerEnabled)
                {
                    // set the new value
                    prop.SetValue(options, BitConverter.ToBoolean(newValue, 0));
                    // Send callback event to host client
                    MyClient.Send(callbackEvent.Replace("_A_", "_B_"), BitConverter.GetBytes((bool)prop.GetValue(options)));
                }
                else
                {
                    // buzzer enabled error
                    MyClient.Send(callbackEvent.Replace("_A_", "_F_"), BitConverter.GetBytes((bool)prop.GetValue(options)));
                }
            }
        }
        /// <summary>
        /// Sets a new value for the buzzer/answer timers or the Global Score. This cannot be changed when the MasterBuzzer is enabled.
        /// </summary>
        /// <param name="options">_Options</param>
        /// <param name="propertyName">String name of a property in VMEODGameshowBuzzerHostOptions</param>
        /// <param name="newValue">The new value</param>
        /// <param name="minValue">Always 0</param>
        /// <param name="maxValue">Found in VMEODAbstractGameshowBuzzerPlugin readonly static variables</param>
        /// <param name="callbackEvent">Buzzer_Host_B_BuzzerTime, Buzzer_Host_B_AnswerTime, or Buzzer_Host_B_GlobalScore</param>
        /// <param name="overErrorEvent">The error event in the case of over flow</param>
        /// <param name="underErrorEvent">The error event in the case of under flow</param>
        private void ChangeValueHandler(object options, string propertyName, byte[] newValue, int minValue, int maxValue, string callbackEvent, string overErrorEvent, string underErrorEvent)
        {
            var prop = options.GetType().GetProperty(propertyName, System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (prop != null)
            {
                if (!BuzzerEnabled)
                {
                    short value = -1;
                    try
                    {
                        value = BitConverter.ToInt16(newValue, 0);
                    }
                    catch (Exception)
                    {

                    }
                    if (value >= minValue)
                    {
                        if (value <= maxValue)
                        {
                            // set the new value
                            prop.SetValue(options, value);
                        }
                        else
                        {
                            // overflow error
                            MyClient.Send(overErrorEvent, new byte[0]);
                        }
                    }
                    else
                    {
                        // underflow error
                        MyClient.Send(underErrorEvent, new byte[0]);
                    }
                }
                else
                {
                    // buzzer enabled error
                    MyClient.Send("Buzzer_Host_Error", new byte[(byte)VMEODGameshowBuzzerPluginErrors.H_NoChangesAllowed]);
                }
                // Send callback event to host client
                MyClient.Send(callbackEvent.Replace("_A_", "_B_"), BitConverter.GetBytes((short)prop.GetValue(options)));
            }
        }
        /// <summary>
        /// Change the specified player's score to the new value.
        /// </summary>
        /// <param name="playerIndex">Buzzer_Host_A_PlayerScore '0 - 3'</param>
        /// <param name="newScore">The new score</param>
        private void ChangePlayerScoreHandler(int playerIndex, byte[] newScore)
        {
            lock (ConnectedContestants)
            {
                if (!BuzzerEnabled)
                {
                    var player = ConnectedContestants[playerIndex];
                    if (player != null && player.EODType.Equals(VMEODGameshowBuzzerPluginType.Player))
                    {
                        short value = -1;
                        try
                        {
                            value = BitConverter.ToInt16(newScore, 0);
                        }
                        catch (Exception)
                        {

                        }
                        if (value >= 0)
                        {
                            if (value <= MAX_SCORE)
                            {
                                // set the new score
                                player.ChangeMyScore(value, true);
                            }
                            else
                            {
                                // overflow error
                                MyClient.Send("Buzzer_Host_B_OverPlayerScore", new byte[ 0 ]);
                            }
                        }
                        else
                        {
                            // underflow error
                            MyClient.Send("Buzzer_Host_B_UnderPlayerScore", new byte[ 0 ]);
                        }
                        
                    }
                }
                else
                {
                    // buzzer enabled error
                    MyClient.Send("Buzzer_Host_Error", new byte[(byte)VMEODGameshowBuzzerPluginErrors.H_NoChangesAllowed]);
                }
                lock (PlayerConfigLock)
                    SendContestantRoster();
            }
        }
        /// <summary>
        /// Execute a Correct Answer Sequence on the specified player index.
        /// </summary>
        /// <param name="evt">Buzzer_Host_PlayerCorrect</param>
        /// <param name="playerBytes">player index 0-3</param>
        /// <param name="host"></param>
        private void PlayerCorrectHandler(string evt, byte[] playerBytes, VMEODClient host)
        {
            lock (PlayerConfigLock)
            {
                var playerIndex = BitConverter.ToInt32(playerBytes, 0);
                lock (ConnectedContestants)
                {
                    if (!BuzzerEnabled)
                    {
                        string playerName = null;
                        var player = ConnectedContestants[playerIndex];
                        if (player != null && player.EODType.Equals(VMEODGameshowBuzzerPluginType.Player))
                        {
                            playerName = player.AvatarName;
                            player.ExecuteCorrectAnswer(_Options.CorrectAnswerScore);
                            if (_Options.AutoEnableAllOnRight)
                                EnableAllPlayerBuzzers();

                            // other players should react
                            var players = GetConnectedPlayers(playerIndex, true);
                            foreach (var otherPlayers in players)
                                otherPlayers.ExecuteOthersAnswer(1, playerName);

                            // update host UI
                            host.Send("BuzzerEOD_Answer", new byte[0]);
                            // execute host event for host avatar to acknowledge player avatar's answer
                            Controller.SendOBJEvent(new Model.VMEODEvent((short)VMEODGameshowHostPluginEvents.Judge_Answer_Correct, player.MyClient?.Avatar?.ObjectID ?? 0));
                        }
                        ChangeBuzzerState(VMEODGameshowBuzzerStates.Disabled);
                    }
                    else
                    {
                        // buzzer enabled error
                        host.Send("Buzzer_Host_Error", new byte[ (byte)VMEODGameshowBuzzerPluginErrors.H_NoChangesAllowed ]);
                    }
                    SendContestantRoster();
                }
            }
        }
        /// <summary>
        /// Execute an Incorrect Answer Sequence on the specified player index.
        /// </summary>
        /// <param name="evt">Buzzer_Host_PlayerIncorrect</param>
        /// <param name="playerBytes">player index 0-3</param>
        /// <param name="host"></param>
        private void PlayerIncorrectHandler(string evt, byte[] playerBytes, VMEODClient host)
        {
            lock (PlayerConfigLock)
            { 
                var playerIndex = BitConverter.ToInt32(playerBytes, 0);
                lock (ConnectedContestants)
                {
                    if (!BuzzerEnabled)
                    {
                        string playerName = null;
                        var player = ConnectedContestants[playerIndex];
                        if (player != null && player.EODType.Equals(VMEODGameshowBuzzerPluginType.Player))
                        {
                            playerName = player.AvatarName;
                            player.ExecuteIncorrectAnswer((short)(_Options.AutoDeductWrongPoints ? _Options.CorrectAnswerScore : 0));
                            if (_Options.AutoDisableOnWrong)
                                SetPlayerBuzzer(playerIndex, false);

                            // other players should react
                            var players = GetConnectedPlayers(playerIndex, true);
                            foreach (var otherPlayers in players)
                                otherPlayers.ExecuteOthersAnswer(0, playerName);

                            // update host UI
                            host.Send("BuzzerEOD_Answer", new byte[0]);
                            // execute host event for host avatar to acknowledge player avatar's answer
                            Controller.SendOBJEvent(new Model.VMEODEvent((short)VMEODGameshowHostPluginEvents.Judge_Answer_Incorrect, player.MyClient?.Avatar?.ObjectID ?? 0));
                        }
                        ChangeBuzzerState(VMEODGameshowBuzzerStates.Disabled);
                    }
                    else
                    {
                        // buzzer enabled error
                        host.Send("Buzzer_Host_Error", new byte[(byte)VMEODGameshowBuzzerPluginErrors.H_NoChangesAllowed]);
                    }
                    SendContestantRoster();
                }
            }
        }
        /// <summary>
        /// Execute a search for a new player to replace the specificed index.
        /// </summary>
        /// <param name="evt">Buzzer_Host_FindNewPlayer</param>
        /// <param name="playerIndex">0-3</param>
        /// <param name="host"></param>
        private void FindNewPlayerHandler(string evt, byte[] playerIndex, VMEODClient host)
        {
            if (!BuzzerEnabled)
            {
                var index = BitConverter.ToInt32(playerIndex, 0);
                if (index > -1 && index < 4)
                {
                    lock (PlayerConfigLock)
                        FillContestantSpot(index, true);
                }
            }
            else
            {
                // buzzer enabled error
                host.Send("Buzzer_Host_Error", new byte[(byte)VMEODGameshowBuzzerPluginErrors.H_NoChangesAllowed]);
            }
        }
        /// <summary>
        /// Move the player at the specified index to the left.
        /// </summary>
        /// <param name="evt">Buzzer_Host_MovePlayerLeft</param>
        /// <param name="sourcePlayerIndex">1-3</param>
        /// <param name="host"></param>
        private void MovePlayerLeftHandler(string evt, byte[] sourcePlayerIndex, VMEODClient host)
        {
            if (!BuzzerEnabled)
            {
                lock (PlayerConfigLock)
                {
                    var sourceIndex = BitConverter.ToInt32(sourcePlayerIndex, 0);
                    lock (ConnectedContestants)
                    {
                        var targetIndex = sourceIndex - 1;
                        if (targetIndex < 0)
                            targetIndex = 3;
                        if (IsValidConnectedContestant(sourceIndex))
                        {
                            var sourcePlayer = ConnectedContestants[sourceIndex];
                            if (IsValidConnectedContestant(targetIndex))
                                ConnectedContestants[sourceIndex] = ConnectedContestants[targetIndex];
                            else
                                ConnectedContestants[sourceIndex] = null;
                            ConnectedContestants[targetIndex] = sourcePlayer;
                        }
                        SendContestantRoster();
                    }
                }
            }
            else
            {
                // buzzer enabled error
                host.Send("Buzzer_Host_Error", new byte[(byte)VMEODGameshowBuzzerPluginErrors.H_NoChangesAllowed]);
            }
        }
        /// <summary>
        /// Move the player at the specified index to the right.
        /// </summary>
        /// <param name="evt">Buzzer_Host_MovePlayerRight</param>
        /// <param name="sourcePlayerIndex">0-2</param>
        /// <param name="host"></param>
        private void MovePlayerRightHandler(string evt, byte[] sourcePlayerIndex, VMEODClient host)
        {
            if (!BuzzerEnabled)
            {
                lock (PlayerConfigLock)
                {
                    var sourceIndex = BitConverter.ToInt32(sourcePlayerIndex, 0);
                    lock (ConnectedContestants)
                    {
                        var targetIndex = sourceIndex + 1;
                        if (targetIndex > 3)
                            targetIndex = 0;
                        if (IsValidConnectedContestant(sourceIndex))
                        {
                            var sourcePlayer = ConnectedContestants[sourceIndex];
                            if (IsValidConnectedContestant(targetIndex))
                                ConnectedContestants[sourceIndex] = ConnectedContestants[targetIndex];
                            else
                                ConnectedContestants[sourceIndex] = null;
                            ConnectedContestants[targetIndex] = sourcePlayer;
                        }
                        SendContestantRoster();
                    }
                }
            }
            else
            {
                // buzzer enabled error
                host.Send("Buzzer_Host_Error", new byte[(byte)VMEODGameshowBuzzerPluginErrors.H_NoChangesAllowed]);
            }
        }
        /// <summary>
        /// Handler for the event triggered from VMEODGameshowBuzzerPlayerPlugin, which occurs when a player presses their buzzer in their UIEOD. (Subscribed Event)
        /// </summary>
        /// <param name="buzzingPlayer">The server handler (VMEODGameshowBuzzerPlayerPlugin) belonging to the buzzing player (VMEODClient)</param>
        private void PlayerBuzzedHandler(VMEODGameshowBuzzerPlayerPlugin buzzingPlayer)
        {
            lock (PlayerBuzzerLock)
            {
                // player buzzes first
                if (BuzzerEnabled)
                {
                    ChangeBuzzerState(VMEODGameshowBuzzerStates.Engaged);
                    _AnsweringPlayerIndex = GetPlayerIndex(buzzingPlayer);
                    if (_AnsweringPlayerIndex > -1)
                    {
                        // alert the buzzing player
                        buzzingPlayer.ActivateMyBuzzer(1);
                        // alert the host
                        MyClient.Send("BuzzerEOD_Buzzed", BitConverter.GetBytes(_AnsweringPlayerIndex));
                        // execute Simantics event for host avatar to face buzzing player avatar
                        Controller.SendOBJEvent(new Model.VMEODEvent((short)VMEODGameshowHostPluginEvents.Acknolwedge_Buzzer, buzzingPlayer.MyClient?.Avatar?.ObjectID ?? 0));
                    }
                    else
                    {
                        // error: who is this player?
                    }
                }
                // player buzzes, but not quite first (before _EngagedTimer runs out)
                else if (_BuzzerState.Equals(VMEODGameshowBuzzerStates.Engaged))
                    buzzingPlayer.ActivateMyBuzzer(0);
            }
        }
        /// <summary>
        /// Notifies each connected contestant of the winner chosen by the host.
        /// </summary>
        /// <param name="evt">Buzzer_Host_A_DeclareWinner</param>
        /// <param name="playerIndex"></param>
        private void DeclareWinnerHandler(string evt, byte[] playerIndex, VMEODClient host)
        {
            var index = BitConverter.ToInt32(playerIndex, 0);
            if (IsValidConnectedContestant(index))
            {
                var winner = ConnectedContestants[index];
                if (winner != null)
                {
                    var name = winner.AvatarName;

                    // queue for player
                    WinnerAction = () => { winner?.DeclareWinner(2, name); }; 

                    // notify other players
                    var players = GetConnectedPlayers(index, false);
                    foreach (var player in players)
                        player.DeclareWinner(1, name); // you lose
                    // host callback
                    MyClient.Send("Buzzer_Player_Win", name);
                    // execute Simantics event for host to declare winner
                    Controller.SendOBJEvent(new Model.VMEODEvent((short)VMEODGameshowHostPluginEvents.Declare_Winner, winner.MyClient?.Avatar?.ObjectID ?? 0));
                }
            }
        }
        /// <summary>
        /// Requested a callback of the roster. Just send it.
        /// </summary>
        /// <param name="evt">Buzzer_Host_Request_Roster</param>
        /// <param name="nothing"></param>
        /// <param name="host"></param>
        private void RequestRosterHandler(string evt, byte[] nothing, VMEODClient host)
        {
            lock (PlayerBuzzerLock)
                lock (ConnectedContestants)
                    SendContestantRoster();
        }
        #endregion

        private void ChangeBuzzerState(VMEODGameshowBuzzerStates newState)
        {
            switch (newState)
            {
                case VMEODGameshowBuzzerStates.Ready:
                    {
                        SetUTimer(_Options.BuzzerTimeLimit);
                        BroadcastToPlayers("BuzzerEOD_Master", new byte[] { 1 }, true);
                        _Tock = 0;
                        break;
                    }
                case VMEODGameshowBuzzerStates.Engaged:
                    {
                        SetUTimer(_Options.AnswerTimeLimit);
                        _EngagedTimer = ACTIVE_BUZZER_WINDOW_TICKS;
                        _Tock = 0;
                        break;
                    }
                case VMEODGameshowBuzzerStates.Expired:
                    {
                        MyClient.Send("Buzzer_Host_Tip", (byte)VMEODGameshowBuzzerPluginEODTips.H_Player_Timeout + "");
                        break;
                    }
                case VMEODGameshowBuzzerStates.Disabled:
                    {
                        _AnsweringPlayerIndex = -1;
                        BroadcastToPlayers("BuzzerEOD_Master", new byte[] { 0 }, false);
                        _Tock = 0;
                        break;
                    };
                case VMEODGameshowBuzzerStates.Locked:
                    {
                        // other players should react
                        var players = GetConnectedPlayers(_AnsweringPlayerIndex, true);
                        foreach (var otherPlayers in players)
                            otherPlayers.ActivateOtherBuzzer();
                        break;
                    }
            }
            _BuzzerState = newState;
        }
        private void SetUTimer(int newTime)
        {
            _UbiqitousTimer = newTime;
            BroadcastToPlayers("BuzzerEOD_Timer", _UbiqitousTimer + "", false);
            MyClient.Send("BuzzerEOD_Timer", _UbiqitousTimer + "");
        }
        private void BroadcastToPlayers(string evt, string args, bool onlyActiveBuzzers)
        {
            var players = GetConnectedPlayers(-1, onlyActiveBuzzers);
            foreach (var player in players)
            {
                player?.Send(evt, args);
            }
        }
        private void BroadcastToPlayers(string evt, byte[] args, bool onlyActiveBuzzers)
        {
            var players = GetConnectedPlayers(-1, onlyActiveBuzzers);
            foreach (var player in players)
            {
                player?.Send(evt, args);
            }
        }
        /// <summary>
        /// Note: This occurs under locks: ConnectedContestants and PlayerConfigLock
        /// </summary>
        /// <param name="playerToSkip"></param>
        /// <returns></returns>
        private List<VMEODGameshowBuzzerPlayerPlugin> GetConnectedPlayers(int playerIndexToSkip, bool onlyActiveBuzzers)
        {
            var players = new List<VMEODGameshowBuzzerPlayerPlugin>();
            for (int index = 0; index < 4; index++)
            {
                if (index == playerIndexToSkip)
                    continue;
                var player = ConnectedContestants[index];
                if (player != null)
                {
                    if (player.EODType.Equals(VMEODGameshowBuzzerPluginType.Player))
                    {
                        if (!onlyActiveBuzzers || onlyActiveBuzzers && player.MyBuzzerEnabled)
                            players.Add(player);
                    }
                }
            }
            return players;
        }
        private int GetPlayerIndex(VMEODGameshowBuzzerPlayerPlugin player)
        {
            int playerIndex = -1;
            lock (PlayerConfigLock)
            {
                lock (ConnectedContestants)
                    playerIndex = Array.IndexOf(ConnectedContestants, player);
            }
            return playerIndex;
        }
        /// <summary>
        /// Subscribed event when a new VMEODGameshowBuzzerPlayerPlugin is added (VMEODClient Connects)
        /// </summary>
        private void NewClientConnectedHandler()
        {
            lock (PlayerConfigLock)
                ClientListRefreshedStamp = System.DateTime.Now.ToOADate();
            FillContestantSpots();
        }
        /// <summary>
        /// Subscribed event when a VMEODClient disconnects
        /// </summary>
        private void ClientDisconnectedHandler()
        {
            ValidateConnectedContestants();
            lock (PlayerConfigLock)
                lock (ConnectedContestants)
                    SendContestantRoster();
            if (_AnsweringPlayerIndex > -1 && ConnectedContestants[_AnsweringPlayerIndex] == null) // player who buzzed in has disconnected
            {
                // force players to react to wrong answer
                var players = GetConnectedPlayers(-1, true);
                foreach (var otherPlayers in players)
                    otherPlayers.ExecuteAnswererDisconnect();
                // force hosts to react to wrong answer
                Controller.SendOBJEvent(new Model.VMEODEvent((short)VMEODGameshowHostPluginEvents.Judge_Answer_Incorrect, 0));
                ChangeBuzzerState(VMEODGameshowBuzzerStates.Disabled);
            }
        }
        /// <summary>
        /// Fill only empty player spots. This occurs when a new host connects or each time a new VMEODGameshowBuzzerPlayerPlugin is created.
        /// </summary>
        private void FillContestantSpots()
        {
            ValidateConnectedContestants();
            lock (PlayerConfigLock)
            {
                for (int index = 0; index < ConnectedContestants.Length; index++)
                {
                    if (ConnectedContestants[index] == null) // if there's an empty spot
                        FillContestantSpot(index, false);
                }
                lock (ConnectedContestants)
                    SendContestantRoster();
            }
        }
        /// <summary>
        /// Looks for available contestants not already connected to this host.
        /// </summary>
        /// <param name="index">The player index to fill with a new awaiting contestant</param>
        /// <param name="viaSearchbtn">Host evoked this by pressing their FindNewPlayer Button</param>
        private void FillContestantSpot(int index, bool viaSearchbtn)
        {
            lock (ConnectedContestants)
            {
                var spot = ConnectedContestants[index];
                if (spot != null)
                    spot.SearchStamp = ClientListRefreshedStamp;
                var players = new List<VMEODGameshowBuzzerPlayerPlugin>(PotentialContestants.Where((player) => (player.SessionStamp == 0)).ToList());
                if (players.Count == 0)
                {
                    // no new players were found
                    if (viaSearchbtn)
                        MyClient.Send("Buzzer_Host_Error", (byte)VMEODGameshowBuzzerPluginErrors.H_NoNewPlayerError + "");
                }
                else // 1 or more players found
                {
                    int newPlayersIndex = 0;
                    bool exhaustedSearchList = true;
                    if (players.Count > 1)
                    {
                        // check the SearchStamp to give priority to unvisited players
                        for (int i = 0; i < players.Count; i++)
                        {
                            if (players[i]?.SearchStamp < ClientListRefreshedStamp && players[i].MyClient != null)
                            {
                                newPlayersIndex = i;
                                exhaustedSearchList = false;
                                break;
                            }
                        }
                        if (exhaustedSearchList) // all unconnected contestants have already been iterated through, reset their search stamp
                            foreach (var eod in players)
                                eod.SearchStamp = 0;
                    }
                    // 1 or more valid players found
                    if (spot != null)
                        spot.SessionStamp = 0; // reset prior player's session stamp
                    ConnectedContestants[index] = spot = players[newPlayersIndex];
                    spot.SessionStamp = this.SessionStamp;
                }
                if (viaSearchbtn)
                    SendContestantRoster();
            }
        }
        /// <summary>
        /// 
        /// Note: Always called within locked PlayerConfigLock and ConnectedContestants.
        /// </summary>
        private void SendContestantRoster()
        {
            bool atLeastOnePlayer = false;
            string evt = "Buzzer_Host_Roster";
            if (BuzzerEnabled)
                evt = "Buzzer_Host_Live_Roster";
            var data = new List<string>();

            for (int index = 0; index < 4; index++)
            {
                var player = ConnectedContestants[index];
                if (player != null && player.EODType.Equals(VMEODGameshowBuzzerPluginType.Player) && player.MyClient.Avatar != null)
                {
                    atLeastOnePlayer = true;
                    data.Add(player.MyClient.Avatar.ObjectID + "");
                    data.Add(player.MyScore + "");
                    data.Add((player.MyBuzzerEnabled ? "1" : "0"));
                }
                else
                    data.AddRange(new string[] { "0", "0", "0" });
            }
            // send to host UI
            MyClient.Send(evt, VMEODGameCompDrawACardData.SerializeStrings(data.ToArray()));
            // sent to object via Simantics
            Controller.SendOBJEvent(new Model.VMEODEvent((short)VMEODGameshowHostPluginEvents.Update_Players_Connected, (short)(atLeastOnePlayer ? 1 : 0)));
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="isEnabled"></param>
        private void ToggleMasterBuzzer(bool isEnabled)
        {
            if (isEnabled)
                ChangeBuzzerState(VMEODGameshowBuzzerStates.Ready);
            else
                ChangeBuzzerState(VMEODGameshowBuzzerStates.Disabled);
        }
        /// <summary>
        /// 
        /// </summary>
        private void ValidateConnectedContestants()
        {
            lock (PlayerConfigLock)
            {
                lock (ConnectedContestants)
                {
                    for (int index = 0; index < ConnectedContestants.Length; index++)
                        IsValidConnectedContestant(index);
                }
            }
        }
        /// <summary>
        /// Removes invalid or stale references to VMEODGameshowBuzzerPlayerPlugin instances at the specified index.
        /// </summary>
        /// <param name="index">0-3</param>
        /// <returns></returns>
        private bool IsValidConnectedContestant(int index)
        {
            bool valid = false;
            var contestant = ConnectedContestants[index];
            if (contestant != null)
            {
                if (contestant.MyClient == null || !contestant.EODType.Equals(VMEODGameshowBuzzerPluginType.Player) || contestant.SessionStamp != SessionStamp)
                    ConnectedContestants[index] = null;
                else
                    valid = true;
            }
            return valid;
        }
        /// <summary>
        /// These are specific flags for each player, not the MasterBuzzer. Both must be enabled for any individual player to be able to buzz in.
        /// </summary>
        private void EnableAllPlayerBuzzers()
        {
            lock (ConnectedContestants)
            {
                for (int i = 0; i < 3; i++)
                {
                    SetPlayerBuzzer(i, true);
                }
            }
        }
        /// <summary>
        /// Sets the flag of the specified player to allow or disallow them personally to buzz in.
        /// </summary>
        /// <param name="playerIndex">0-3</param>
        /// <param name="enabled"></param>
        private void SetPlayerBuzzer(int playerIndex, bool enabled)
        {
            if (IsValidConnectedContestant(playerIndex))
            {
                var player = ConnectedContestants[playerIndex];
                if (player != null)
                    player.MyBuzzerEnabled = enabled;
            }
        }
        /// <summary>
        /// Toggles the flag of the specified player to allow or disallow them personally to buzz in.
        /// </summary>
        /// <param name="playerIndex"></param>
        private void TogglePlayerBuzzer(int playerIndex)
        {
            if (IsValidConnectedContestant(playerIndex))
            {
                var player = ConnectedContestants[playerIndex];
                if (player != null)
                    player.MyBuzzerEnabled = !player.MyBuzzerEnabled;
            }
        }
    }

    internal class VMEODGameshowBuzzerHostOptions
    {
        internal bool AutoEnableAllOnRight { get; set; }
        internal bool AutoDisableOnWrong { get; set; }
        internal bool AutoDeductWrongPoints { get; set; }
        internal short CorrectAnswerScore { get; set; }
        internal short AnswerTimeLimit { get; set; }
        internal short BuzzerTimeLimit { get; set; }

        internal VMEODGameshowBuzzerHostOptions()
        {
            AutoEnableAllOnRight = true;
            AutoDisableOnWrong = true;
            CorrectAnswerScore = 100;
            AnswerTimeLimit = 20;
            BuzzerTimeLimit = 10;
        }

    }

    internal enum VMEODGameshowBuzzerStates
    {
        Disabled,
        Ready,
        Engaged,
        Locked,
        Expired
    }

    public enum VMEODGameshowHostPluginEvents : short
    {
        Acknolwedge_Buzzer = 1,
        Judge_Answer_Correct = 2,
        Judge_Answer_Incorrect = 3,
        Declare_Winner = 4,
        Update_Players_Connected = 5, // value sent in temp0
        Host_Judgment_Callback = 101,
        Execute_Declare_Winner = 102
    }
}
