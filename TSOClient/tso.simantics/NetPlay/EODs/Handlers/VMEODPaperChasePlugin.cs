using FSO.Common.Utils;
using FSO.SimAntics.NetPlay.EODs.Model;
using FSO.SimAntics.NetPlay.EODs.Utils;
using System;

namespace FSO.SimAntics.NetPlay.EODs.Handlers
{
    public class VMEODPaperChasePlugin : VMEODHandler
    {
        private VMEODClient Controller;
        private EODLobby<VMEODPaperChaseSlot> Lobby;
        private StateMachine<VMEODPaperChaseState> StateMachine;
        private Random Random = new Random();
        private short[] CurrentCombination;

        private int Ticks = 0;
        private short Matches = -1;
        private short PreviousMatches = -1;

        private short[][] Combinations;

        
        public VMEODPaperChasePlugin(VMEODServer server) : base(server)
        {
            Lobby = new EODLobby<VMEODPaperChaseSlot>(server, 3)
                    .BroadcastPlayersOnChange("paperchase_players")
                    .OnJoinSend("paperchase_show")
                    .OnFailedToJoinDisconnect();

            PlaintextHandlers["close"] = Lobby.Close;
            PlaintextHandlers["paperchase_chooseletter"] = SetPlayerLetter;

            StateMachine = new StateMachine<VMEODPaperChaseState>(VMEODPaperChaseState.Lobby);
            StateMachine.OnTransition += StateMachine_OnTransition;

            //The random function is crappy, if we use it to choose single chars it very often chooses the same combination
            //Expanding the list gives us more variance
            Combinations = new short[27][];
            var i = 0;

            for(var x=0; x < 3; x++){
                for(var y=0; y < 3; y++){
                    for(var z=0; z < 3; z++){
                        //1 = a, 2 = b, 3=c
                        Combinations[i] = new short[] {
                            (short)(x+1),
                            (short)(y+1),
                            (short)(z+1)
                        };
                        i++;
                    }
                }
            }
        }

        public override void Tick()
        {
            Ticks++;

            switch (StateMachine.CurrentState)
            {
                case VMEODPaperChaseState.CheckingResult:
                    if(Ticks > (14 * 30)){
                        StateMachine.TransitionTo(VMEODPaperChaseState.CheckResult);
                    }
                    break;
                case VMEODPaperChaseState.CheckResult:
                    if(Ticks > (3 * 30)){
                        if(Matches == 3){
                            //Success, start a new game
                            StateMachine.TransitionTo(VMEODPaperChaseState.StartRound);
                        }else{
                            //Fail, next guess
                            StateMachine.TransitionTo(VMEODPaperChaseState.WaitingForThreeLetters);
                        }
                    }
                    break;
            }
        }

        private void StateMachine_OnTransition(VMEODPaperChaseState from, VMEODPaperChaseState to)
        {
            Lobby.Broadcast("paperchase_state", ((byte)to).ToString());

            if (Controller == null) { return; }

            switch (to)
            {
                case VMEODPaperChaseState.Lobby:
                    ResetGame();
                    BroadcastPlayerLetters();
                    break;

                case VMEODPaperChaseState.StartRound:
                    ResetGame();
                    CurrentCombination = Combinations[Random.Next(0, Combinations.Length)];
                    StateMachine.TransitionTo(VMEODPaperChaseState.WaitingForThreeLetters);
                    break;
                case VMEODPaperChaseState.WaitingForThreeLetters:
                    PreviousMatches = Matches;
                    BroadcastPlayerLetters();
                    Controller.SendOBJEvent(new VMEODEvent((short)VMEODPaperChaseObjEvent.Idle));
                    break;

                case VMEODPaperChaseState.ThreeLettersProvided:
                    Matches = 0;
                    for (var i = 0; i < 3; i++)
                    {
                        var playerData = Lobby.GetSlotData(i);
                        if (playerData.Letter == CurrentCombination[i]){
                            Matches++;
                        }

                        Controller.SendOBJEvent(new VMEODEvent((short)VMEODPaperChaseObjEvent.SetLetter,
                                    (short)(playerData.Letter | (short)((i + 1) << 8)))); //lo: letter, hi: FREESO player id

                        playerData.PreviousLetter = playerData.Letter;
                        playerData.Letter = null;
                    }

                    Controller.SendOBJEvent(new VMEODEvent((short)VMEODPaperChaseObjEvent.SetResult, Matches));
                    StateMachine.TransitionTo(VMEODPaperChaseState.CheckingResult);
                    break;

                case VMEODPaperChaseState.CheckingResult:
                    Ticks = 0;
                    break;

                case VMEODPaperChaseState.CheckResult:
                    Ticks = 0;
                    Controller.SendOBJEvent(new VMEODEvent((short)VMEODPaperChaseObjEvent.ShowResult));
                    Lobby.Broadcast("paperchase_result", Matches.ToString());
                    break;
            }
        }

        private void ResetGame()
        {
            PreviousMatches = -1;
            Matches = -1;

            CurrentCombination = null;
            for (var i = 0; i < 3; i++)
            {
                var playerData = Lobby.GetSlotData(i);
                if (playerData != null)
                {
                    playerData.Letter = null;
                    playerData.PreviousLetter = null;
                }
            }
        }

        private void SetPlayerLetter(string evt, string body, VMEODClient client)
        {
            //Only the controller updates this
            if (Controller == null) { return; }

            //If we don't recognise the letter, ignore the request
            short letter;
            if (!short.TryParse(body, out letter) || letter < VMEODPaperChaseLetters.A || letter > VMEODPaperChaseLetters.C)
            {
                return;
            }

            //Make sure they are still a player
            var playerSlot = Lobby.GetPlayerSlot(client);
            if(playerSlot == -1){
                return;
            }

            var data = Lobby.GetSlotData(playerSlot);
            if(data.Letter != null){
                //You can't change your mind!
                return;
            }

            Controller.SendOBJEvent(new VMEODEvent((short)VMEODPaperChaseObjEvent.SetLetter,
                                    (short)(letter | (short)((playerSlot+1) << 8)))); //lo: letter, hi: FREESO player id

            data.Letter = letter;
            BroadcastPlayerLetters();
            CheckAllLettersSet();
        }

        private void CheckAllLettersSet()
        {
            for (var i = 0; i < 3; i++)
            {
                var playerData = Lobby.GetSlotData(i);
                if (playerData != null)
                {
                    if (!playerData.Letter.HasValue)
                    {
                        return;
                    }
                }else
                {
                    return;
                }
            }

            //All set!
            if(StateMachine.CurrentState == VMEODPaperChaseState.WaitingForThreeLetters){
                StateMachine.TransitionTo(VMEODPaperChaseState.ThreeLettersProvided);
            }
        }

        private void BroadcastPlayerLetters()
        {
            var letters = new short[7];

            for(var i=0; i < 3; i++){
                var playerData = Lobby.GetSlotData(i);
                if(playerData != null && playerData.Letter.HasValue){
                    letters[i] = playerData.Letter.Value;
                }else{
                    letters[i] = -1;
                }
            }

            for (var i = 0; i < 3; i++){
                var playerData = Lobby.GetSlotData(i);
                if (playerData != null && playerData.PreviousLetter.HasValue){
                    letters[i+3] = playerData.PreviousLetter.Value;
                }else{
                    letters[i+3] = -1;
                }
            }

            letters[6] = PreviousMatches;

            var msg = String.Join("\n", letters);
            Lobby.Broadcast("paperchase_letters", msg);
        }

        public override void OnConnection(VMEODClient client)
        {
            var param = client.Invoker.Thread.TempRegisters;
            if (client.Avatar != null)
            {
                var slot = param[0];
                if(Lobby.Join(client, (short)(slot - 1)))
                {
                    client.SendOBJEvent(new VMEODEvent((short)VMEODPaperChaseObjEvent.Idle));

                    if (Lobby.IsFull())
                    {
                        StateMachine.TransitionTo(VMEODPaperChaseState.StartRound);
                    }
                }
            }
            else
            {
                Controller = client;
            }
        }

        public override void OnDisconnection(VMEODClient client)
        {
            Lobby.Leave(client);
            if (!Lobby.IsFull())
            {
                StateMachine.TransitionTo(VMEODPaperChaseState.Lobby);
            }
        }
    }

    public class VMEODPaperChaseSlot
    {
        public Nullable<short> Letter;
        public Nullable<short> PreviousLetter;
    }

    public class VMEODPaperChaseLetters
    {
        public const short A = 1;
        public const short B = 2;
        public const short C = 3;

        public static short[] All = new short[] { A, B, C };
        
    }

    public enum VMEODPaperChaseState : byte
    {
        Lobby = 1, //end when have 3 players. return to when less.
        StartRound = 2,
        WaitingForThreeLetters = 3,
        ThreeLettersProvided = 4,
        CheckingResult = 5,
        CheckResult = 6
    }

    public enum VMEODPaperChaseObjEvent : short
    {
        SetResult = 1,
        SetLetter = 2,
        ShowResult = 3,
        Idle = 4
    }

    public enum VMEODPaperChaseSlots : byte
    {
        Body = 1,
        Mechanical = 2,
        Logic = 3
    }
}
