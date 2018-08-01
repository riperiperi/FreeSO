using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FSO.SimAntics.NetPlay.EODs.Utils
{
    public static class AbstractMazeGenerator<T>
    {
        public static AbstractMaze<T> GetEmptyMaze(int rows, int columns)
        {
            return new AbstractMaze<T>(rows, columns);
        }
        public static MazeWallConfigCodes GetWallConfig(AbstractMazeCell<T> cell)
        {
            if (cell.North_Neighbor != null)
            {
                if (cell.West_Neighbor != null)
                {
                    if (cell.East_Neighbor != null)
                    {
                        if (cell.South_Neighbor != null)
                            return MazeWallConfigCodes.None;
                        else // south is null
                            return MazeWallConfigCodes.South_Only;
                    }
                    else // east is null
                    {
                        if (cell.South_Neighbor != null)
                            return MazeWallConfigCodes.East_Only;
                        else // south is null
                            return MazeWallConfigCodes.East_South;
                    }
                }
                else // west is null
                {
                    if (cell.East_Neighbor != null)
                    {
                        if (cell.South_Neighbor != null)
                            return MazeWallConfigCodes.West_Only;
                        else // south is null
                            return MazeWallConfigCodes.West_South;
                    }
                    else // east is null
                    {
                        if (cell.South_Neighbor != null)
                            return MazeWallConfigCodes.West_East;
                        else // south is null
                            return MazeWallConfigCodes.West_East_South;
                    }
                }
            }
            else // north is null
            {
                if (cell.West_Neighbor != null)
                {
                    if (cell.East_Neighbor != null)
                    {
                        if (cell.South_Neighbor != null)
                            return MazeWallConfigCodes.North_Only;
                        else // south is null
                            return MazeWallConfigCodes.North_South;
                    }
                    else // east is null
                    {
                        if (cell.South_Neighbor != null)
                            return MazeWallConfigCodes.North_East;
                        else // south is null
                            return MazeWallConfigCodes.North_East_South;
                    }
                }
                else // west is null
                {
                    if (cell.East_Neighbor != null)
                    {
                        if (cell.South_Neighbor != null)
                            return MazeWallConfigCodes.North_West;
                        else // south is null
                            return MazeWallConfigCodes.North_West_South;
                    }
                    else // east is null
                    {
                        if (cell.South_Neighbor != null)
                            return MazeWallConfigCodes.North_West_East;
                        else // not possible without adding different maze genrating algorithms to AbstractMazeGenerator.cs
                            return MazeWallConfigCodes.All;
                    }
                }
            }
        }
    }

    public class AbstractMaze<T>
    {
        private Stack<AbstractMazeCell<T>> BuildStack;
        private AbstractMazeCell<T>[,] m_Maze;
        private int Rows;
        private int Columns;
        private AbstractMazeCell<T> BuildOrigin;
        private Random Random = new Random();
        private bool DeadEnd;

        public delegate void MazeEvent(AbstractMazeCell<T>[,] maze);
        public event MazeEvent OnMazeGenerated;
        public event MazeEvent OnSolutionGenerated;
        public delegate void CellEvent(AbstractMazeCell<T> cell);
        public event CellEvent OnFinalProcessingCell;
        public event CellEvent OnDeadEndCreation;
        public event CellEvent OnVisitingCell;


        public AbstractMaze(int rows, int columns)
        {
            Rows = rows;
            Columns = columns;
            m_Maze = new AbstractMazeCell<T>[Rows, Columns];
            BuildStack = new Stack<AbstractMazeCell<T>>();
            Random = new Random();
        }
        public void ResetMaze()
        {
            BuildOrigin = null;
            for (int rowIndex = 0; rowIndex < Rows; rowIndex++)
                for (int columnIndex = 0; columnIndex < Columns; columnIndex++)
                    m_Maze[rowIndex, columnIndex] = new AbstractMazeCell<T>(rowIndex, columnIndex);
        }
        public void BuildFromOrigin(int value)
        {
            switch (value)
            {
                case (int)BuildFromOrigins.North_West:
                    {
                        BuildFromOrigin(0, 0);
                        break;
                    }
                case (int)BuildFromOrigins.North_East:
                    {
                        BuildFromOrigin(0, Columns - 1);
                        break;
                    }
                case (int)BuildFromOrigins.South_West:
                    {
                        BuildFromOrigin(Rows - 1, 0);
                        break;
                    }
                case (int)BuildFromOrigins.South_East:
                    {
                        BuildFromOrigin(Rows - 1, Columns - 1);
                        break;
                    }
                default: // BuildFromOrigins.Dead_Center:
                    {
                        BuildFromOrigin(Rows / 2, Columns / 2);
                        break;
                    }
            }
        }
        // Currently only uses the recursive backtracking algorithm
        public void BuildFromOrigin(int rowOrigin, int columnOrigin)
        {
            ResetMaze();
            BuildOrigin = m_Maze[rowOrigin, columnOrigin];

            // recursive backtracking
            var currentCell = BuildOrigin;
            currentCell.Status = AbstractMazeCellStatuses.Ready;
            while (currentCell != null)
            {
                // neighbor options
                var neighborOptions = new List<AbstractMazeCell<T>>();

                // push north neighbor
                if (currentCell.Row - 1 > -1)
                {
                    if (!m_Maze[currentCell.Row - 1, currentCell.Column].Status.Equals(AbstractMazeCellStatuses.Ready))
                        neighborOptions.Add(m_Maze[currentCell.Row - 1, currentCell.Column]);
                }
                // push west neighbor
                if (currentCell.Column - 1 > -1)
                {
                    if (!m_Maze[currentCell.Row, currentCell.Column - 1].Status.Equals(AbstractMazeCellStatuses.Ready))
                        neighborOptions.Add(m_Maze[currentCell.Row, currentCell.Column - 1]);
                }
                // push east neighbor
                if (currentCell.Column + 1 < Columns)
                {
                    if (!m_Maze[currentCell.Row, currentCell.Column + 1].Status.Equals(AbstractMazeCellStatuses.Ready))
                        neighborOptions.Add(m_Maze[currentCell.Row, currentCell.Column + 1]);
                }
                // push south neighbor
                if (currentCell.Row + 1 < Rows)
                {
                    if (!m_Maze[currentCell.Row + 1, currentCell.Column].Status.Equals(AbstractMazeCellStatuses.Ready))
                        neighborOptions.Add(m_Maze[currentCell.Row + 1, currentCell.Column]);
                }

                // if there are options, choose one and push currentCell to stack
                if (neighborOptions.Count > 0)
                {
                    DeadEnd = false;
                    // choose a neighbor at random
                    var index = Random.Next(0, neighborOptions.Count);
                    var chosenCell = neighborOptions[index];

                    // push currentCell onto stack
                    BuildStack.Push(currentCell);

                    // whichever neighbor was chosen needs to have a path declared to and from currentCell
                    if (chosenCell.Row == currentCell.Row)
                    {
                        if (chosenCell.Column == currentCell.Column - 1) // west neighbor
                        {
                            currentCell.West_Neighbor = chosenCell;
                            chosenCell.East_Neighbor = currentCell;
                        }
                        else // east neighbor => chosenCell.Column == currentCell.Column + 1
                        {
                            currentCell.East_Neighbor = chosenCell;
                            chosenCell.West_Neighbor = currentCell;
                        }
                    }
                    else
                    {
                        if (chosenCell.Row == currentCell.Row - 1) // north neighbor
                        {
                            currentCell.North_Neighbor = chosenCell;
                            chosenCell.South_Neighbor = currentCell;
                        }
                        else // south neighbor => chosenCell.Row == currentCell.Row + 1
                        {
                            currentCell.South_Neighbor = chosenCell;
                            chosenCell.North_Neighbor = currentCell;
                        }
                    }

                    // make chosen into current after processing
                    chosenCell.Status = AbstractMazeCellStatuses.Ready;
                    currentCell = chosenCell;
                }
                // there are no neighbor options, pop a cell off the stack and make it current
                else
                {
                    OnFinalProcessingCell?.Invoke(currentCell);
                    if (!DeadEnd)
                    {
                        DeadEnd = true;
                        OnDeadEndCreation?.Invoke(currentCell);
                    }
                    if (BuildStack.Count > 0)
                        currentCell = BuildStack.Pop();
                    else
                        currentCell = null;
                }
            }
            // the origin is always a dead end, too
            OnDeadEndCreation?.Invoke(BuildOrigin);

            // the maze is generated
            OnMazeGenerated?.Invoke(m_Maze);
        }
        public AbstractMazeCell<T> GetExitFromOriginAndDepth(AbstractMazeCell<T> origin, int moves)
        {
            var solutionPath = GetPathFromOriginToExit(origin, null, moves);
            if (solutionPath != null)
                return solutionPath[solutionPath.Count - 1];
            return null;
        }
        public List<AbstractMazeCell<T>> GetPathFromOriginToExit(AbstractMazeCell<T> origin, AbstractMazeCell<T> exit, int killDepth)
        {
            var solutionPath = new List<AbstractMazeCell<T>>();
            var ProcessedCellsList = new List<AbstractMazeCell<T>>();
            var ProcessedCellsOriginsList = new List<AbstractMazeCell<T>>();
            var CellQueue = new Queue<AbstractMazeCell<T>>();
            
            // breadth-frist algorithm
            var currentCell = origin;
            ProcessedCellsOriginsList.Add(currentCell);
            while (currentCell != null)
            {
                solutionPath.Clear();
                currentCell.Status = AbstractMazeCellStatuses.Processed;
                if (currentCell.North_Neighbor != null && currentCell.North_Neighbor.Status.Equals(AbstractMazeCellStatuses.Ready))
                {
                    CellQueue.Enqueue(currentCell.North_Neighbor);
                    ProcessedCellsOriginsList.Add(currentCell);
                    currentCell.North_Neighbor.Status = AbstractMazeCellStatuses.Queued;
                }
                if (currentCell.West_Neighbor != null && currentCell.West_Neighbor.Status.Equals(AbstractMazeCellStatuses.Ready))
                {
                    CellQueue.Enqueue(currentCell.West_Neighbor);
                    ProcessedCellsOriginsList.Add(currentCell);
                    currentCell.West_Neighbor.Status = AbstractMazeCellStatuses.Queued;
                }
                if (currentCell.East_Neighbor != null && currentCell.East_Neighbor.Status.Equals(AbstractMazeCellStatuses.Ready))
                {
                    CellQueue.Enqueue(currentCell.East_Neighbor);
                    ProcessedCellsOriginsList.Add(currentCell);
                    currentCell.East_Neighbor.Status = AbstractMazeCellStatuses.Queued;
                }
                if (currentCell.South_Neighbor != null && currentCell.South_Neighbor.Status.Equals(AbstractMazeCellStatuses.Ready))
                {
                    CellQueue.Enqueue(currentCell.South_Neighbor);
                    ProcessedCellsOriginsList.Add(currentCell);
                    currentCell.South_Neighbor.Status = AbstractMazeCellStatuses.Queued;
                }
                ProcessedCellsList.Add(currentCell);

                // if the solution is found, force finish
                if (exit != null && currentCell.Equals(exit))
                    CellQueue.Clear();

                // process the solution, check its length for kill depth
                var parentCell = currentCell;
                int parentCellIndex = -1;
                while (!parentCell.Equals(origin))
                {
                    solutionPath.Insert(0, parentCell);
                    parentCellIndex = ProcessedCellsList.IndexOf(parentCell);
                    parentCell = ProcessedCellsOriginsList[parentCellIndex];
                }

                // check for kill depth, force finish if reached
                if (solutionPath.Count == killDepth)
                    CellQueue.Clear();

                // add the origin to the beginning
                solutionPath.Insert(0, origin);

                // finally, if there is another cell to process from the queue, do so
                if (CellQueue.Count > 0)
                    currentCell = CellQueue.Dequeue();
                else
                    currentCell = null;
            }
            // reset maze for next solution
            UnprocessCells();
            return solutionPath;
        }
        private void UnprocessCells()
        {
            for (int y = 0; y < Rows; y++)
                for (int x = 0; x < Columns; x++)
                    m_Maze[y, x].Status = AbstractMazeCellStatuses.Ready;
        }
    }
    public enum BuildFromOrigins : int
    {
        North_West = 0,
        North_East = 1,
        South_West = 2,
        South_East = 3,
        Dead_Center = 4
    }
    public class AbstractMazeCell<T>
    {
        public int Column { get; internal set; }
        public int Row { get; internal set; }

        public AbstractMazeCell<T> North_Neighbor { get; internal set; }
        public AbstractMazeCell<T> West_Neighbor { get; internal set; }
        public AbstractMazeCell<T> East_Neighbor { get; internal set; }
        public AbstractMazeCell<T> South_Neighbor { get; internal set; }
        public AbstractMazeCellStatuses Status { get; internal set; }

        public T CellData { get; set; }
        
        public AbstractMazeCell(int row, int column)
        {
            Row = row;
            Column = column;
            Status = AbstractMazeCellStatuses.Default;
            CellData = Activator.CreateInstance<T>();
        }
    }
    public enum AbstractMazeCellStatuses
    {
        Default = 0,
        Ready = 1,
        Queued = 2,
        Processed = 3
    }
    public enum AbstractMazeCellCardinalDirections : byte
    {
        North = 0,
        West = 1,
        East = 2,
        South = 3
    }
    public enum MazeWallConfigCodes : byte
    {
        None = 0,
        North_Only = 1,
        North_West = 2,
        North_East = 3,
        North_South = 4,
        North_West_East = 5,
        North_West_South = 6,
        North_East_South = 7,
        West_Only = 8,
        West_East = 9,
        West_South = 10,
        West_East_South = 11,
        East_Only = 12,
        East_South = 13,
        South_Only = 14,
        All = 15 // all walls not currently possible as only algorithm is recursive backtracking, which is a perfect maze
    }
}
