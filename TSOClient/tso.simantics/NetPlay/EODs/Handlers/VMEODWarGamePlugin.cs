using System;
using System.Collections.Generic;
using FSO.SimAntics.NetPlay.EODs.Model;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Timers;

namespace FSO.SimAntics.NetPlay.EODs.Handlers
{
    public class VMEODWarGamePlugin : VMEODHandler
    {
        VMEODClient ControllerClient;
        VMEODClient RedPlayerClient;
        VMEODClient BluePlayerClient;
        private int NumberOfPlayers;
        private int BluePlayerVictories;
        private int RedPlayerVictories;
        private List<VMEODWarGamePlayerPieces> Players;
        private VMEODWarGamePiece ChosenBluePiece;
        private VMEODWarGamePiece ChosenRedPiece;
        private Timer GameMessageTimer;
        private Timer RoundMessageTimer;

        public VMEODWarGamePlugin(VMEODServer server) : base(server)
        {
            Players = NewGame();
            BinaryHandlers["WarGame_Piece_Selection"] = PieceSelectionHandler;
            PlaintextHandlers["WarGame_Close_UI"] = OnCloseUIHandler;
            SimanticsHandlers[(short)VMEODWarGameEvents.NextRound] = NextRoundHandler;
            SimanticsHandlers[(short)VMEODWarGameEvents.NextGame] = NextGameHandler;
            GameMessageTimer = new Timer(5000);
            GameMessageTimer.Elapsed += GameTieMessageHandler;
            RoundMessageTimer = new Timer(5000);
            RoundMessageTimer.Elapsed += RoundTieMessageHandler;
        }

        public override void OnConnection(VMEODClient client)
        {
            base.OnConnection(client);
            if (client.Avatar != null)
            {
                // get the params, temp 0 is player type
                var local = client.Invoker.Thread.TempRegisters;
                if ((local != null) && (local[0] == (short)VMEODWarGamePlayers.Blue))
                {
                    BluePlayerClient = client;
                    BluePlayerClient.Send("WarGame_Init", BluePlayerClient.Avatar.ObjectID + "%blue");
                    NumberOfPlayers++;
                }
                else
                {
                    RedPlayerClient = client;
                    RedPlayerClient.Send("WarGame_Init", RedPlayerClient.Avatar.ObjectID + "%red");
                    NumberOfPlayers++;
                }
            }
            else
            {
                ControllerClient = client;
            }
            if ((NumberOfPlayers == 2) && (ControllerClient != null))
            {
                BluePlayerClient.Send("WarGame_Draw_Opponent", RedPlayerClient.Avatar.ObjectID + "");
                RedPlayerClient.Send("WarGame_Draw_Opponent", BluePlayerClient.Avatar.ObjectID + "");
                NextGameHandler((short)VMEODWarGameEvents.NextGame, ControllerClient);
            }
        }
        public override void OnDisconnection(VMEODClient client)
        {
            NumberOfPlayers--;
            if (RedPlayerClient != null)
            {
                RedPlayerClient.Send("WarGame_Reset", "");
                RedPlayerClient.Send("WarGame_Draw_Opponent", "");
            }
            if (BluePlayerClient != null)
            {
                BluePlayerClient.Send("WarGame_Reset", "");
                BluePlayerClient.Send("WarGame_Draw_Opponent", "");
            }
            base.OnDisconnection(client);
        }
        private void OnCloseUIHandler(string evt, string msg, VMEODClient closedClient)
        {
            Server.Disconnect(closedClient);
        }
        private void PieceSelectionHandler(string evt, Byte[] chosenPieceNum, VMEODClient playerClient)
        {
            // is this an active player client?
            short playerNum = -1;
            if (BluePlayerClient.Equals(playerClient))
                playerNum = (short)VMEODWarGamePlayers.Blue;
            else if (RedPlayerClient.Equals(playerClient))
                playerNum = (short)VMEODWarGamePlayers.Red;
            else
                return;

            // verify that the piece is a legal selection
            bool isLegal = Enum.IsDefined(typeof(VMEODWarGamePieceTypes), chosenPieceNum[0]);

            // finally, make sure that the valid player's valid choice is not already defeated
            if (isLegal)
            {
                foreach (var piece in Players[playerNum].Pieces)
                {
                    if ((byte)piece.PieceType == chosenPieceNum[0])
                    {
                        SetPlayerPiece(playerNum, piece);
                        return;
                    }
                }
            }
            // either the chosen piece is illegal or the chosen piece is already defeated, force a piece choice
            ForcePieceChoice(playerNum);
        }
        private void NextRoundHandler(short eventID, VMEODClient eventSource)
        {
            byte remainingBluePieces = (byte)Players[(byte)VMEODWarGamePlayers.Blue].Pieces.Count;
            byte remainingRedPieces = (byte)Players[(byte)VMEODWarGamePlayers.Red].Pieces.Count;

            // the game is over as one of the players has no more available piece choices
            if ((remainingBluePieces == 0) || (remainingRedPieces == 0))
            {
                GameOver();
            }
            // check to make sure the game isn't a stalemate - both players have the same remaining piece
            else if ((remainingBluePieces == 1) && (remainingRedPieces == 1))
            {
                if (Players[(byte)VMEODWarGamePlayers.Blue].Pieces[0].PieceType.Equals(Players[(byte)VMEODWarGamePlayers.Red].Pieces[0].PieceType))
                {
                    BluePlayerClient.Send("WarGame_Stalemate", new Byte[] { (byte)Players[(byte)VMEODWarGamePlayers.Blue].Pieces[0].PieceType });
                    RedPlayerClient.Send("WarGame_Stalemate", new Byte[] { (byte)Players[(byte)VMEODWarGamePlayers.Red].Pieces[0].PieceType });
                    GameMessageTimer.Start();
                }
            }
            else if (NumberOfPlayers == 2)
            {
                BluePlayerClient.Send("WarGame_Resume", new Byte[] { remainingBluePieces, remainingRedPieces });
                RedPlayerClient.Send("WarGame_Resume", new Byte[] { remainingBluePieces, remainingRedPieces });
            }
        }
        private void NextGameHandler(short eventID, VMEODClient eventSource)
        {
            // reset EOD for both players
            BluePlayerClient.Send("WarGame_Reset", "");
            RedPlayerClient.Send("WarGame_Reset", "");

            // get a new set of game pieces
            Players = NewGame();

            // start the round
            NextRoundHandler((short)VMEODWarGameEvents.NextRound, eventSource);
        }
        private void GameTieMessageHandler(object source, ElapsedEventArgs args)
        {
            GameMessageTimer.Stop();
            // reset player round wins in prim
            ControllerClient.SendOBJEvent(new VMEODEvent((short)VMEODWarGameEvents.ResetWins));
            NextGameHandler((short)VMEODWarGameEvents.NextGame, ControllerClient);
        }
        private void RoundTieMessageHandler(object source, ElapsedEventArgs args)
        {
            RoundMessageTimer.Stop();
            NextRoundHandler((short)VMEODWarGameEvents.NextRound, ControllerClient);
        }
        private void ForcePieceChoice(short playerNumber)
        {
            // this should never happen, but the prim will reset the game if it comes to here and the player has no valid pieces left
            if (Players[playerNumber].Pieces.Count == 0)
                Server.Disconnect((playerNumber == (short)VMEODWarGamePlayers.Blue) ? BluePlayerClient : RedPlayerClient);
            else
                SetPlayerPiece(playerNumber, Players[playerNumber].Pieces[0]);
        }
        private void SetPlayerPiece(int playerNumber, VMEODWarGamePiece chosenPiece)
        {
            if (playerNumber == (short)VMEODWarGamePlayers.Blue)
                ChosenBluePiece = chosenPiece;
            else
                ChosenRedPiece = chosenPiece;

            if ((ChosenBluePiece != null) && (ChosenRedPiece != null))
                DetermineRoundResult();
        }
        private void DetermineRoundResult()
        {
            short defeatedPlayer = -1;
            // the round is a tie
            if (ChosenBluePiece.PieceType.Equals(ChosenRedPiece.PieceType)) { }
            else
            {
                // check to see if blue is defeated
                foreach (var defeatablePiece in ChosenRedPiece.Defeats)
                {
                    // blue's chosen piece matches a defeatable piece of red's chosen piece
                    if (ChosenBluePiece.PieceType.Equals(defeatablePiece.PieceType))
                        defeatedPlayer = (short)VMEODWarGamePlayers.Blue;
                }
                // if blue is not defeated, check to see if red is defeated
                if (defeatedPlayer == -1)
                {
                    foreach (var defeatablePiece in ChosenBluePiece.Defeats)
                    {
                        // red's chosen piece matches a defeatable piece of blue's chosen piece
                        if (ChosenRedPiece.PieceType.Equals(defeatablePiece.PieceType))
                            defeatedPlayer = (short)VMEODWarGamePlayers.Red;
                    }
                }
            }
            // blue is defeated
            if (defeatedPlayer == 0)
            {
                RedPlayerVictories++;
                Players[defeatedPlayer].Pieces.Remove(ChosenBluePiece);
                RedPlayerClient.Send("WarGame_Victory", new Byte[] { (byte)ChosenBluePiece.PieceType, (byte)ChosenRedPiece.PieceType });
                BluePlayerClient.Send("WarGame_Defeat", new Byte[] { (byte)ChosenBluePiece.PieceType, (byte)ChosenRedPiece.PieceType });
                ControllerClient.SendOBJEvent(new VMEODEvent((short)VMEODWarGameEvents.RoundOver, (short)VMEODWarGamePlayers.Red));
            }
            // red is defeated
            else if (defeatedPlayer == 1)
            {
                BluePlayerVictories++;
                Players[defeatedPlayer].Pieces.Remove(ChosenRedPiece);
                BluePlayerClient.Send("WarGame_Victory", new Byte[] { (byte)ChosenBluePiece.PieceType, (byte)ChosenRedPiece.PieceType });
                RedPlayerClient.Send("WarGame_Defeat", new Byte[] { (byte)ChosenBluePiece.PieceType, (byte)ChosenRedPiece.PieceType });
                ControllerClient.SendOBJEvent(new VMEODEvent((short)VMEODWarGameEvents.RoundOver, (short)VMEODWarGamePlayers.Blue));
            }
            // round is a tie
            else
            {
                BluePlayerClient.Send("WarGame_Tie", new Byte[] { (byte)ChosenBluePiece.PieceType, (byte)ChosenRedPiece.PieceType });
                RedPlayerClient.Send("WarGame_Tie", new Byte[] { (byte)ChosenBluePiece.PieceType, (byte)ChosenRedPiece.PieceType });
                RoundMessageTimer.Start();
            }
            ChosenBluePiece = null;
            ChosenRedPiece = null;
        }
        private List<VMEODWarGamePlayerPieces> NewGame()
        {
            var newPlayers = new List<VMEODWarGamePlayerPieces>(2);
            newPlayers.Add(new VMEODWarGamePlayerPieces());
            newPlayers.Add(new VMEODWarGamePlayerPieces());
            return newPlayers;
        }
        private void GameOver()
        {
            short winner = -1;
            // Determine the winner based on the number of wins. The prim has its own record of wins stored internally
            if (BluePlayerVictories > RedPlayerVictories)
                winner = (short)VMEODWarGamePlayers.Blue;
            else
                winner = (short)VMEODWarGamePlayers.Red;
            if (winner > -1)
            {
                ControllerClient.SendOBJEvent(new VMEODEvent((short)VMEODWarGameEvents.GameOver, winner));
                // get new pieces now so a duplicate win can't be executed during server event delay
                Players = NewGame();
            }
        }
    }
    class VMEODWarGamePlayerPieces
    {
        public List<VMEODWarGamePiece> Pieces;

        public VMEODWarGamePlayerPieces()
        {
            Pieces = new List<VMEODWarGamePiece>();
            Pieces.Add(new VMEODWarGamePiece(VMEODWarGamePieceTypes.Artillery));
            Pieces.Add(new VMEODWarGamePiece(VMEODWarGamePieceTypes.Cavalry));
            Pieces.Add(new VMEODWarGamePiece(VMEODWarGamePieceTypes.Command));
            Pieces.Add(new VMEODWarGamePiece(VMEODWarGamePieceTypes.Infantry));
            Pieces.Add(new VMEODWarGamePiece(VMEODWarGamePieceTypes.Intelligence));

            // Add the children to each piece type, to be used to check the winner.
            // The children of each piece are those which the root piece can destory
            foreach (var piece in Pieces)
            {
                piece.AddChildren();
            }
        }
    }
    class VMEODWarGamePiece
    {
        private VMEODWarGamePieceTypes m_PieceType;
        public List<VMEODWarGamePiece> Defeats;

        public VMEODWarGamePiece(VMEODWarGamePieceTypes type) {
            m_PieceType = type;
        }

        public void AddChildren()
        {
            Defeats = new List<VMEODWarGamePiece>(2);
            switch (m_PieceType)
            {
                case VMEODWarGamePieceTypes.Artillery:
                    {
                        Defeats.Add(new VMEODWarGamePiece(VMEODWarGamePieceTypes.Infantry));
                        Defeats.Add(new VMEODWarGamePiece(VMEODWarGamePieceTypes.Cavalry));
                        break;
                    }
                case VMEODWarGamePieceTypes.Cavalry:
                    {
                        Defeats.Add(new VMEODWarGamePiece(VMEODWarGamePieceTypes.Intelligence));
                        Defeats.Add(new VMEODWarGamePiece(VMEODWarGamePieceTypes.Command));
                        break;
                    }
                case VMEODWarGamePieceTypes.Command:
                    {
                        Defeats.Add(new VMEODWarGamePiece(VMEODWarGamePieceTypes.Artillery));
                        Defeats.Add(new VMEODWarGamePiece(VMEODWarGamePieceTypes.Intelligence));
                        break;
                    }
                case VMEODWarGamePieceTypes.Infantry:
                    {
                        Defeats.Add(new VMEODWarGamePiece(VMEODWarGamePieceTypes.Cavalry));
                        Defeats.Add(new VMEODWarGamePiece(VMEODWarGamePieceTypes.Command));
                        break;
                    }
                case VMEODWarGamePieceTypes.Intelligence:
                    {
                        Defeats.Add(new VMEODWarGamePiece(VMEODWarGamePieceTypes.Infantry));
                        Defeats.Add(new VMEODWarGamePiece(VMEODWarGamePieceTypes.Artillery));
                        break;
                    }
            }
        }
        public VMEODWarGamePieceTypes PieceType
        {
            get { return m_PieceType; }
        }
    }
    public enum VMEODWarGamePlayers : short
    {
        Blue = 0,
        Red = 1
    }
    [Flags]
    public enum VMEODWarGamePieceTypes : byte
    {
        Artillery = 0,
        Cavalry = 1,
        Command = 2,
        Infantry = 3,
        Intelligence = 4
    }
    public enum VMEODWarGameEvents : short
    {
        PlayerJoin = -2,
        PlayerDisconnects = -1,
        RoundOrGameReset = 0,
        RoundOver = 1, // @param 0: Blue wins OR @param 1: Red wins round
        GameOver = 2, // @param 0: Blue wins OR @param 1: Red wins round
        ResetWins = 3, /* added this event in the .pif so round wins from unfinished games or stalemates don't carry over to next new game;
        cf. pizzamaker "internal object variable: contributions" issue */
        NextRound = 10,
        NextGame = 11
    }
}
