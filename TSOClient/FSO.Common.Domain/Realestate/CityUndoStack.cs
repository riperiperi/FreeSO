using FSO.Server.Protocol.Electron.Model.CityEditCommands;

namespace FSO.Common.Domain.Realestate
{
    public class CityUndoStack
    {
        private uint AvatarID = uint.MaxValue;
        private readonly List<CityEditBase> UndoStack = [];
        private readonly Stack<CityEditBase> RedoStack = [];
        private readonly HashSet<int> ExpectedRedo = [];

        public event Action UndoChanged;

        public void WatchAvatar(uint avatarID, List<CityEditBase> history)
        {
            AvatarID = avatarID;

            ExpectedRedo.Clear();
            UndoStack.Clear();
            RedoStack.Clear();

            foreach (var item in history)
            {
                AddCommand(item);
            }
        }

        public bool CanUndo()
        {
            return UndoStack.Count > 0;
        }

        public bool CanRedo()
        {
            return RedoStack.Count > 0;
        }

        public int? Undo()
        {
            if (!CanUndo())
            {
                return null;
            }

            // Tell the city we want to undo the last command.
            return UndoStack.Last().UserModId;
        }

        public CityEditBase Redo()
        {
            if (!CanRedo())
            {
                return null;
            }

            // Resubmit the command.
            var redo = RedoStack.Pop();

            ExpectedRedo.Add(redo.UserModId);

            return redo;
        }

        public void AddCommand(CityEditBase command)
        {
            if (command.AvatarId == AvatarID)
            {
                if (!ExpectedRedo.Contains(command.UserModId))
                {
                    RedoStack.Clear();
                }
                UndoStack.Add(command);
                UndoChanged?.Invoke();
            }
        }

        public void HandleUndo(CityEditBase command)
        {
            // If this undo is ours, put the undo command on the redo stack, and remove it from the undo stack.

            if (command.AvatarId == AvatarID)
            {
                UndoStack.RemoveAt(UndoStack.Count - 1);
                RedoStack.Push(command);
                UndoChanged?.Invoke();
            }
        }
    }
}
