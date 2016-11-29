using FSO.SimAntics.Engine.Debug;
using FSO.SimAntics.Marshals;
using FSO.SimAntics.Utils;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;

namespace FSO.SimAntics.NetPlay.Model.Commands
{
    public class VMStateSyncCmd : VMNetCommandBodyAbstract
    {
        public VMMarshal State;
        public VMSyncTraceTick Trace;

        public override bool Execute(VM vm)
        {
#if VM_DESYNC_DEBUG
            if (Trace != null) vm.Trace.CompareFirstError(Trace);
#endif
            vm.Load(State);
            if (VM.UseWorld && vm.Context.Blueprint.SubWorlds.Count == 0) VMLotTerrainRestoreTools.RestoreSurroundings(vm, vm.HollowAdj);
            return true;
        }

        public override bool Verify(VM vm, VMAvatar caller)
        {
            return !FromNet;
        }

        #region VMSerializable Members
        public override void Deserialize(BinaryReader reader)
        {
            State = new VMMarshal();
            State.Deserialize(reader);
            if (reader.ReadBoolean())
            {
                Trace = new VMSyncTraceTick();
                Trace.Deserialize(reader);
            }
        }

        public override void SerializeInto(BinaryWriter writer)
        {
            State.SerializeInto(writer);
            writer.Write(Trace != null);
            if (Trace != null) Trace.SerializeInto(writer);
        }
        #endregion
    }
}
