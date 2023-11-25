using System;
using System.Collections.Generic;
using System.Linq;
using FSO.SimAntics.NetPlay.EODs.Model;
using FSO.SimAntics.NetPlay.EODs.Utils;

namespace FSO.SimAntics.NetPlay.EODs.Handlers
{
    public class VMEODTwoPersonJobObjectMazePlugin : VMEODHandler
    {
        private List<ColorPool> AllColorPools;
        private VMEODClient CharismaPlayer;
        private bool CharismaPlayerHasInit;
        private AbstractMazeCell<TSOMazeData> CharismaPlayerOrigin;
        private List<AbstractMazeCell<TSOMazeData>>[] ColorCells;
        private VMEODClient Controller;
        private int CooldownTimer;
        private AbstractMaze<TSOMazeData> CurrentMaze;
        private VMEODTwoPersonJobObjectMazePluginStates GameState;
        private VMEODClient LogicPlayer;
        private bool LogicPlayerHasInit;
        private AbstractMazeCell<TSOMazeData>[,] MazeArray;
        private bool MazeGenerated;
        private bool MazeSolved;
        private VMEODTwoPersonJobObjectMazePluginStates NextState;
        private Random Rng;
        private int RoundTimeRemaining;
        private AbstractMazeCell<TSOMazeData> SolutionCell;
        private List<AbstractMazeCell<TSOMazeData>> SolutionPath;
        private int ThisRoundOriginColor;
        private int Tock;

        public static readonly int MAX_ROWS = 8;
        public static readonly int MAX_COLUMNS = 36;
        public readonly int MAX_COLORS = 14;
        public readonly int MIN_MOVES_TO_EXIT = 20;
        public readonly int MAX_MOVES_TO_EXIT = 40;
        public readonly int ReactionSeconds = 10;
        public readonly int RoundSeconds = 300;

        public VMEODTwoPersonJobObjectMazePlugin(VMEODServer server) : base(server)
        {
            GameState = VMEODTwoPersonJobObjectMazePluginStates.Waiting_For_Player;
            Rng = new Random();

            // listeners
            BinaryHandlers["TSOMaze_Button_Click"] = CharismaButtonClickedHandler;
        }
        public override void Tick()
        {
            if (NextState != VMEODTwoPersonJobObjectMazePluginStates.Invalid)
            {
                var state = NextState;
                NextState = VMEODTwoPersonJobObjectMazePluginStates.Invalid;
                GotoState(state);
            }

            switch (GameState)
            {
                case VMEODTwoPersonJobObjectMazePluginStates.Waiting_For_Player:
                    {
                        // only send data to charisma player if logic player exists
                        if (LogicPlayer != null)
                        {
                            if (!LogicPlayerHasInit && MazeGenerated)
                                SendLogicPlayerData();
                            if (CharismaPlayer != null)
                            {
                                if (!CharismaPlayerHasInit && MazeGenerated)
                                    SendCharismaPlayerData(true);

                                // failsafe, since both players are not null
                                EnqueueGotoState(VMEODTwoPersonJobObjectMazePluginStates.Ready);
                            }
                        }
                        break;
                    }
                case VMEODTwoPersonJobObjectMazePluginStates.Ready:
                    {
                        if (MazeGenerated)
                        {
                            if (LogicPlayerHasInit && CharismaPlayerHasInit)
                            {
                                if (CooldownTimer > 0)
                                {
                                    if (++Tock >= 30)
                                    {
                                        CooldownTimer--;
                                        Tock = 0;
                                        BroadcastSharedEvent("TSOMaze_Update_Timer", BitConverter.GetBytes(CooldownTimer));
                                    }
                                }
                                else
                                    EnqueueGotoState(VMEODTwoPersonJobObjectMazePluginStates.Solving);
                            }
                            else
                            {
                                if (!LogicPlayerHasInit)
                                    SendLogicPlayerData();
                                else
                                    SendCharismaPlayerData(true);
                            }
                        }
                        else
                        {
                            // should never happen, very bad things help
                            if (++Tock >= 30)
                            {
                                RoundTimeRemaining--; // intially 6 seconds for the maze to finish generating and send
                                Tock = 0;
                            }
                            else // better than they stand waiting forever
                                Server.Shutdown();
                        }
                        break;
                    }
                case VMEODTwoPersonJobObjectMazePluginStates.Solving:
                    {
                        if (RoundTimeRemaining > 0)
                        {
                            if (++Tock >= 30)
                            {
                                RoundTimeRemaining--;
                                Tock = 0;
                                BroadcastSharedEvent("TSOMaze_Update_Timer", BitConverter.GetBytes(RoundTimeRemaining));
                            }
                        }
                        else // force loss
                            EnqueueGotoState(VMEODTwoPersonJobObjectMazePluginStates.Reacting);
                        break;
                    }
                case VMEODTwoPersonJobObjectMazePluginStates.Reacting:
                    {
                        if (RoundTimeRemaining > 0)
                        {
                            if (++Tock >= 30)
                            {
                                RoundTimeRemaining--;
                                Tock = 0;
                            }
                        }
                        else
                            EnqueueGotoState(VMEODTwoPersonJobObjectMazePluginStates.Waiting_For_Player);
                        break;
                    }
            }
            base.Tick();
        }
        public override void OnConnection(VMEODClient client)
        {
            if (client.Avatar != null)
            {
                var args = client.Invoker.Thread.TempRegisters;
                if (args[0] == 1)
                {
                    LogicPlayer = client;
                    client.Send("TSOMaze_Show_Logic", new byte[0]);
                    CooldownTimer = 6;
                }
                else
                {
                    CharismaPlayer = client;
                    client.Send("TSOMaze_Show_Charisma", new byte[0]);
                    CooldownTimer = 8;
                }
                if (LogicPlayer != null && CharismaPlayer != null)
                    EnqueueGotoState(VMEODTwoPersonJobObjectMazePluginStates.Ready);
            }
            else
                Controller = client;
            base.OnConnection(client);
        }
        public override void OnDisconnection(VMEODClient client)
        {
            if (client.Equals(LogicPlayer))
                LogicPlayer = null;
            else if (client.Equals(CharismaPlayer))
                CharismaPlayer = null;
            if (LogicPlayer == null && CharismaPlayer == null)
                Server.Shutdown();
            else
                EnqueueGotoState(VMEODTwoPersonJobObjectMazePluginStates.Waiting_For_Player);
            base.OnDisconnection(client);
        }
        private void CharismaButtonClickedHandler(string evt, byte[] direction, VMEODClient charPlayer)
        {
            // validate move, make move, check for solution
            if (charPlayer.Equals(CharismaPlayer) && direction.Length == 1 && GameState.Equals(VMEODTwoPersonJobObjectMazePluginStates.Solving))
            {
                var directionByte = direction[0];
                AbstractMazeCell<TSOMazeData> nextCell = null;
                switch (directionByte)
                {
                    case (byte)AbstractMazeCellCardinalDirections.North:
                        {
                            nextCell = CharismaPlayerOrigin.North_Neighbor;
                            break;
                        }
                    case (byte)AbstractMazeCellCardinalDirections.West:
                        {
                            nextCell = CharismaPlayerOrigin.West_Neighbor;
                            break;
                        }
                    case (byte)AbstractMazeCellCardinalDirections.East:
                        {
                            nextCell = CharismaPlayerOrigin.East_Neighbor;
                            break;
                        }
                    case (byte)AbstractMazeCellCardinalDirections.South:
                        {
                            nextCell = CharismaPlayerOrigin.South_Neighbor;
                            break;
                        }
                }
                // if it's a valid move
                if (nextCell != null)
                {
                    CharismaPlayerOrigin = nextCell;
                    // if it's the solution/exit
                    if (CharismaPlayerOrigin.CellData.IsExit)
                    {
                        MazeSolved = true;
                        EnqueueGotoState(VMEODTwoPersonJobObjectMazePluginStates.Reacting);
                    }
                }
                if (MazeSolved)
                    SendCharismaPlayerCellData("TSOMaze_Final_Cell");
                else
                    SendCharismaPlayerCellData("TSOMaze_Update_Cell");
            }
        }
        private void BroadcastSharedEvent(string evt, byte[] data)
        {
            if (LogicPlayer != null)
                LogicPlayer.Send(evt, data);
            if (CharismaPlayer != null)
                CharismaPlayer.Send(evt, data);
        }
        private void EnqueueGotoState(VMEODTwoPersonJobObjectMazePluginStates newState)
        {
            NextState = newState;
        }
        private void GotoState(VMEODTwoPersonJobObjectMazePluginStates state)
        {
            switch (state)
            {
                case VMEODTwoPersonJobObjectMazePluginStates.Waiting_For_Player:
                    {
                        Reset(); // get a maze
                        GameState = state;
                        BroadcastSharedEvent("TSOMaze_Show_Waiting", new byte[0]);
                        break;
                    }
                case VMEODTwoPersonJobObjectMazePluginStates.Ready:
                    {
                        Tock = 0;
                        RoundTimeRemaining = ReactionSeconds;
                        GameState = state;
                        break;
                    }
                case VMEODTwoPersonJobObjectMazePluginStates.Solving:
                    {
                        Tock = 0;
                        RoundTimeRemaining = RoundSeconds;
                        GameState = state;
                        SendCharismaPlayerData(false); // finally allow input
                        break;
                    }
                case VMEODTwoPersonJobObjectMazePluginStates.Reacting:
                    {
                        if (MazeSolved)
                        {
                            Controller.SendOBJEvent(new VMEODEvent((short)VMEODTwoPersonObjectMazePluginEvents.Success));
                            BroadcastSharedEvent("TSOMaze_Show_Result", new byte[] { 1 }); // win dialog as UIAlert
                            // send the Logic Player the solution by sending (y,x) coords
                            List<byte> solutionCoords = new List<byte>();
                            // but send the cell color first
                            solutionCoords.Add((byte)ThisRoundOriginColor);
                            for (int index = 0; index < SolutionPath.Count - 1; index++)
                            {
                                solutionCoords.Add((byte)SolutionPath[index].Row);
                                solutionCoords.Add((byte)SolutionPath[index].Column);
                            }
                            if (LogicPlayer != null)
                                LogicPlayer.Send("TSOMaze_Draw_Solution", solutionCoords.ToArray());
                        }
                        else // failed to solve
                        {
                            Controller.SendOBJEvent(new VMEODEvent((short)VMEODTwoPersonObjectMazePluginEvents.Failure));
                            BroadcastSharedEvent("TSOMaze_Show_Result", new byte[] { 0 }); // loss dialog as UIAlert
                        }
                        Tock = 0;
                        RoundTimeRemaining = ReactionSeconds;
                        GameState = state;
                        break;
                    }
            }
        }
        private void Reset()
        {
            MazeGenerated = false;
            MazeSolved = false;
            LogicPlayerHasInit = false;
            CharismaPlayerHasInit = false;

            // indeces: 0 for blue, 1 for green, 2 for red, 3 for yellow as seen in enum: VMEODTwoPersonJobObjectMazePluginCellColors
            ColorCells = new List<AbstractMazeCell<TSOMazeData>>[] { new List<AbstractMazeCell<TSOMazeData>>(MAX_COLORS),
                new List<AbstractMazeCell<TSOMazeData>>(MAX_COLORS), new List<AbstractMazeCell<TSOMazeData>>(MAX_COLORS + 1),
                new List<AbstractMazeCell<TSOMazeData>>(MAX_COLORS + 1)};

            // build color pools
            int blanksPerPool = (MAX_ROWS * MAX_COLUMNS - MAX_COLORS * 4) - 2;
            blanksPerPool /= 8;
            AllColorPools = new List<ColorPool>()
            {
                new ColorPool(VMEODTwoPersonJobObjectMazePluginCellColors.Blank, blanksPerPool),
                new ColorPool(VMEODTwoPersonJobObjectMazePluginCellColors.Blank, blanksPerPool),
                new ColorPool(VMEODTwoPersonJobObjectMazePluginCellColors.Blue, MAX_COLORS),
                new ColorPool(VMEODTwoPersonJobObjectMazePluginCellColors.Blank, blanksPerPool),
                new ColorPool(VMEODTwoPersonJobObjectMazePluginCellColors.Blank, blanksPerPool),
                new ColorPool(VMEODTwoPersonJobObjectMazePluginCellColors.Green, MAX_COLORS),
                new ColorPool(VMEODTwoPersonJobObjectMazePluginCellColors.Blank, blanksPerPool),
                new ColorPool(VMEODTwoPersonJobObjectMazePluginCellColors.Blank, blanksPerPool),
                new ColorPool(VMEODTwoPersonJobObjectMazePluginCellColors.Red, MAX_COLORS + 1),
                new ColorPool(VMEODTwoPersonJobObjectMazePluginCellColors.Blank, blanksPerPool),
                new ColorPool(VMEODTwoPersonJobObjectMazePluginCellColors.Blank, blanksPerPool),
                new ColorPool(VMEODTwoPersonJobObjectMazePluginCellColors.Yellow, MAX_COLORS + 1),
            };

            if (CurrentMaze == null) // first run
            {
                CurrentMaze = AbstractMazeGenerator<TSOMazeData>.GetEmptyMaze(MAX_ROWS, MAX_COLUMNS);
                CurrentMaze.OnMazeGenerated += MazeGeneratedHandler;
                CurrentMaze.OnFinalProcessingCell += OnProcessedCellHandler;
            }
            // random origin from the 5 choices of enum: BuildFromOrigins
            var origin = Rng.Next(0, (int)BuildFromOrigins.Dead_Center + 1);
            // also resets previous mazes
            CurrentMaze.BuildFromOrigin(origin); 
        }
        private void SendCharismaPlayerData(bool disableInput)
        {
            if (CharismaPlayer != null)
            {
                CharismaPlayerHasInit = true;
                if (disableInput)
                    SendCharismaPlayerCellData("TSOMaze_Init_Cell");
                else
                    SendCharismaPlayerCellData("TSOMaze_Update_Cell");
            }
        }
        private void SendCharismaPlayerCellData(string eventName)
        {
            byte wallConfig = (byte)CharismaPlayerOrigin.CellData.Wall_Config;
            byte color = (byte)CharismaPlayerOrigin.CellData.Color;
            CharismaPlayer.Send(eventName, new byte[] { wallConfig, color });
        }
        private void SendLogicPlayerData()
        {
            if (LogicPlayer != null)
            {
                LogicPlayerHasInit = true;
                
                // get the wall configs for half of the cells: every other one in each row, but staggered starting at 0 or 1 moving down rows
                var wallData = new List<byte>();
                for (int row = 0; row < MAX_ROWS; row++)
                {
                    for (int column = 0; column < MAX_COLUMNS; column++)
                    {
                        if (row % 2 == 0) // start at 0, skipping odd columns
                        {
                            if (column % 2 == 0)
                                wallData.Add((byte)MazeArray[row, column].CellData.Wall_Config);
                        }
                        else // start at 1, skipping even columns
                        {
                            if (column % 2 == 1)
                                wallData.Add((byte)MazeArray[row, column].CellData.Wall_Config);
                        }
                    }
                }
                LogicPlayer.Send("TSOMaze_Mark_Walls", wallData.ToArray());

                /*
                 * Send each colored cell to the LogicPlayer
                 */
                string eventHeaderText = "TSOMaze_";
                string eventFooterText = "Icon";
                var colorEnums = Enum.GetValues(typeof(VMEODTwoPersonJobObjectMazePluginCellColors)).Cast<VMEODTwoPersonJobObjectMazePluginCellColors>();
                foreach (var color in colorEnums)
                {
                    if (!color.Equals(VMEODTwoPersonJobObjectMazePluginCellColors.Blank))
                    {
                        int index = (int)color;
                        var thisColor = ColorCells[index];
                        var cellCoords = new List<byte>();
                        foreach (var cell in thisColor)
                        {
                            cellCoords.Add((byte)cell.Row);
                            cellCoords.Add((byte)cell.Column);
                        }
                        LogicPlayer.Send(eventHeaderText + color.ToString() + eventFooterText, cellCoords.ToArray());
                    }
                }
                // send the solution cell's coordinates
                LogicPlayer.Send("TSOMaze_ExitIcon", new byte[] { (byte)SolutionCell.Row, (byte)SolutionCell.Column });
            }
        }
        private void MazeGeneratedHandler(AbstractMazeCell<TSOMazeData>[,] maze)
        {
            MazeArray = maze;

            // random number from 0 to 3 inclusive for one of the 4 colors
            var colorIndex = Rng.Next((int)VMEODTwoPersonJobObjectMazePluginCellColors.Blue, (int)VMEODTwoPersonJobObjectMazePluginCellColors.Blank);
            ThisRoundOriginColor = colorIndex;
            var cellList = ColorCells[colorIndex];
            // randon number from 0 to list.count for any cell of that color
            colorIndex = Rng.Next(0, cellList.Count);
            CharismaPlayerOrigin = cellList[colorIndex];

            // get the solution
            var numberOfMoves = Rng.Next(MIN_MOVES_TO_EXIT, MAX_MOVES_TO_EXIT + 1);
            SolutionPath = CurrentMaze.GetPathFromOriginToExit(CharismaPlayerOrigin, null, numberOfMoves);
            if (SolutionPath != null)
                SolutionCell = SolutionPath[SolutionPath.Count - 1];
            else
                Reset();
            if (SolutionCell != null)
            {
                SolutionCell.CellData.Color = VMEODTwoPersonJobObjectMazePluginCellColors.Blue;
                SolutionCell.CellData.IsExit = true;
                MazeGenerated = true;
            }
            else
                Reset();
        }
        private void OnProcessedCellHandler(AbstractMazeCell<TSOMazeData> cell)
        {
            // get wall configuration
            cell.CellData.Wall_Config = AbstractMazeGenerator<TSOMazeData>.GetWallConfig(cell);

            // choose a color from available pools
            if (AllColorPools.Count > 1)
            {
                int index = Rng.Next(0, AllColorPools.Count);
                cell.CellData.Color = AllColorPools[index].Color;
                AllColorPools[index].Remaining--;

                // if the pool is dry, remove it as an option
                if (AllColorPools[index].Remaining == 0)
                    AllColorPools.RemoveAt(index);
            }
            else // only one color left
                cell.CellData.Color = AllColorPools[0].Color;

            // push the cell into the correct list to send to the UIEOD
            var colorIndex = (int)cell.CellData.Color;
            if (colorIndex < 4)
                ColorCells[colorIndex].Add(cell);
        }
    }
    internal class TSOMazeData
    {
        public VMEODTwoPersonJobObjectMazePluginCellColors Color;
        public MazeWallConfigCodes Wall_Config;
        public bool IsExit;
    }
    class ColorPool
    {
        public VMEODTwoPersonJobObjectMazePluginCellColors Color { get; set; }
        public int Remaining { get; set; }
        public ColorPool(VMEODTwoPersonJobObjectMazePluginCellColors color, int available)
        {
            Color = color;
            Remaining = available;
        }
    }
    public enum VMEODTwoPersonJobObjectMazePluginCellColors : byte
    {
        Blue = 0,
        Green = 1,
        Red = 2,
        Yellow = 3,
        Blank = 4
    }
    public enum VMEODTwoPersonJobObjectMazePluginStates
    {
        Invalid = -1,
        Waiting_For_Player = 0,
        Ready = 1,
        Solving = 2,
        Reacting = 3
    }
    public enum VMEODTwoPersonObjectMazePluginEvents : short
    {
        Failure = 1,    
        Success = 2,
        ForceDisconnect = 3
    }
}
