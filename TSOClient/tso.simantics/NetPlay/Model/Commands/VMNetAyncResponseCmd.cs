using FSO.SimAntics.Engine.TSOTransaction;
using FSO.SimAntics.Model.TSOPlatform;
using FSO.SimAntics.NetPlay.Model;
using FSO.SimAntics.Primitives;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace FSO.SimAntics.NetPlay.Model.Commands
{
    /// <summary>
    /// For asynchronous events, such as transactions, inventory access and plugins. Updates the
    /// state for a thread waiting on an asynchronous response, usually waking it up.
    /// CANNOT be sent by clients. Must be sent to server.
    /// 
    /// The intention is for the async state updates being sent to the SimAntics VM are run at 
    /// the same time on both the client and server VMs.
    /// </summary>
    public class VMNetAsyncResponseCmd : VMNetCommandBodyAbstract
    {
        public short ID;
        public VMAsyncState State;

        public VMNetAsyncResponseCmd() { }

        public VMNetAsyncResponseCmd(short id, VMAsyncState state)
        {
            ID = id;
            State = state;
        }

        public override bool Execute(VM vm)
        {
            VMEntity obj = vm.GetObjectById(ID);
            var type = State.GetType();
            //we can only update an object's blocking state if it exists and is of the same type exactly.
            if (obj == null || obj.Thread.BlockingState == null || obj.Thread.BlockingState.GetType() != type) return false;
            obj.Thread.BlockingState = State;

            if (type == typeof(VMTransferFundsState))
            {
                //special handling. update visual budgets of involved elements.
                //note: if we are the server and the reference budget IS the visual, do not update.
                if (vm.GlobalLink == null || !(vm.GlobalLink is VMTSOGlobalLinkStub))
                {
                    var state = (VMTransferFundsState)State;
                    var obj1 = vm.GetObjectByPersist(state.UID1);
                    if (obj1 != null) obj1.TSOState.Budget.Value = state.Budget1;
                    var obj2 = vm.GetObjectByPersist(state.UID2);
                    if (obj2 != null) obj2.TSOState.Budget.Value = state.Budget2;
                }
            }

            return true;
        }

        #region VMSerializable Members
        public override void SerializeInto(BinaryWriter writer)
        {
            base.SerializeInto(writer);
            writer.Write(ID);
            VMAsyncState.SerializeGeneric(writer, State);
        }

        public override void Deserialize(BinaryReader reader)
        {
            base.Deserialize(reader);
            ID = reader.ReadInt16();
            State = VMAsyncState.DeserializeGeneric(reader);
        }
        #endregion
    }
}
