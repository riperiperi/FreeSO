using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FSO.SimAntics.Utils
{
    /// <summary>
    /// Defines the context of a cheat
    /// </summary>
    public class VMCheatContext
    {
        public static VMCheatState CheatState = new VMCheatState();

        delegate void ExecuteHandler(VM vm, VMAvatar caller, VMCheatContext context, out bool result);
        private static Dictionary<VMCheatType, ExecuteHandler> predefinedBehavior = new Dictionary<VMCheatType, ExecuteHandler>()
            {
                { VMCheatType.Budget, new ExecuteHandler(ExecuteBudget) },
                { VMCheatType.MoveObjects, new ExecuteHandler(ExecuteMoveObjects) }
            };

        public enum VMCheatParameterType
        {
            NO_PARAMS = 0,
            INT = 1,
            BOOL = 2
        }
        public enum BudgetCheatPresetAmount : uint
        {
            /// <summary>
            /// Has no value and will not affect the budget of the family
            /// </summary>
            NO_CHEAT = 0,
            /// <summary>
            /// Has a value of $1000 (rosebud cheat in later versions)
            /// </summary>
            KLAPAUCIUS = 1000,
            /// <summary>
            /// Has a value of $1,000,000
            /// </summary>
            MOTHERLODE = 1000000
        }
        /// <summary>
        /// Defines the behavior of the cheat
        /// </summary>
        public enum VMCheatType
        {
            InvalidCheat = 0,
            /// <summary>
            /// For transaction cheats it adds the amount to the user's budget
            /// </summary>
            Budget = 1,
            MoveObjects = 2
        }
        /// <summary>
        /// The selected cheat to run
        /// </summary>
        public VMCheatType CheatBehavior;
        /// <summary>
        /// The numeric parameter submitted with the cheat
        /// </summary>
        public int Amount; // used by budget cheats
        public int Repetitions; // defined by how many ";!"s there are
        /// <summary>
        /// The boolean value submitted with the cheat
        /// </summary>
        public bool Modifier; // some cheats may require a "on" or "off" modifier
        public bool Executed
        {
            get;
            private set;
        } = false;
        public bool Execute(VM vm, VMAvatar caller)
        {
            if (CheatBehavior == VMCheatType.InvalidCheat)
                return false;
            predefinedBehavior[CheatBehavior].Invoke(vm, caller, this, out bool result); // careful! make sure all cheats are defined in predefinedBehaviors!
            Executed = true;
            return result;
        }
        private static void ExecuteBudget(VM vm, VMAvatar caller, VMCheatContext context, out bool result)
        {
            var amount = context.Amount * (context.Repetitions + 1);
            vm.GlobalLink.PerformTransaction(vm, false, uint.MaxValue, caller.PersistID, amount,
                (bool success, int transferAmount, uint uid1, uint budget1, uint uid2, uint budget2) =>
                {
                        //check if we got the money? why should I? i mean cheating is bad after all
                });
            result = true;
        }

        private static void ExecuteMoveObjects(VM vm, VMAvatar caller, VMCheatContext context, out bool result)
        {
            CheatState.TS1_MoveObjects = context.Modifier;
            result = true;
        }
    }

    public class VMCheatState
    {
        public bool TS1_MoveObjects;
    }
}
