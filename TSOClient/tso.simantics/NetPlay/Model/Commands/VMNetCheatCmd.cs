using FSO.SimAntics.Utils;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FSO.SimAntics.NetPlay.Model.Commands
{
    /// <summary>
    /// A command submitting a cheat with a typed parameter T
    /// </summary>
    public class VMNetCheatCmd : VMNetCommandBodyAbstract
    {                
        public override bool AcceptFromClient => true; //if a client wants to cheat that should be ignored.

        public VMCheatContext Context;

        public override bool Execute(VM vm, VMAvatar caller)
        {
            if (!vm.TS1 || caller == null || Context == null) return false; //if we aren't in TS1 that would be bad -- invalid if we have no Context
            if (Context.Executed) // the cheat has already been run
                return false;
            return Context.Execute(vm, caller);            
        }

        public override bool Verify(VM vm, VMAvatar caller)
        {
            if (FromNet) return false; //disable for network play
            //TODO: add Verify() to cheat context to allow cheats to verify themself based off their intended behavior
            return true;
        }

        #region VMSerializeable Members
        public override void SerializeInto(BinaryWriter writer)
        {
            base.SerializeInto(writer);            
            Context.SerializeInto(writer);
        }

        public override void Deserialize(BinaryReader reader)
        {
            base.Deserialize(reader);
            Context = new VMCheatContext();
            Context.Deserialize(reader);
        }
        #endregion
    }
}
