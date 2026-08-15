using FSO.SimAntics.NetPlay.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using FSO.SimAntics.Primitives;
using FSO.SimAntics.NetPlay.EODs.Model;

namespace FSO.SimAntics.Model.TSOPlatform
{
    public class VMAsyncState : VMSerializable
    {
        public static Dictionary<VMAsyncStateType, Type> TypeResolve = new Dictionary<VMAsyncStateType, System.Type>()
        {
            { VMAsyncStateType.TransferFunds, typeof(VMTransferFundsState) },
            { VMAsyncStateType.DialogResult, typeof(VMDialogResult) },
            { VMAsyncStateType.PluginState, typeof(VMEODPluginThreadState) },
            { VMAsyncStateType.InventoryOp, typeof(VMInventoryOpState) }

        };
        public static Dictionary<Type, VMAsyncStateType> TypeMarshal = TypeResolve.ToDictionary(x => x.Value, x => x.Key);

        // Compile-time constructors: Mono on WASM fails Activator.CreateInstance
        // (Arg_NoDefCTor) for types never directly constructed in the compiled app.
        public static Dictionary<VMAsyncStateType, Func<VMAsyncState>> TypeFactories = new Dictionary<VMAsyncStateType, Func<VMAsyncState>>()
        {
            { VMAsyncStateType.TransferFunds, () => new VMTransferFundsState() },
            { VMAsyncStateType.DialogResult, () => new VMDialogResult() },
            { VMAsyncStateType.PluginState, () => new VMEODPluginThreadState() },
            { VMAsyncStateType.InventoryOp, () => new VMInventoryOpState() }
        };

        public static VMAsyncState DeserializeGeneric(BinaryReader reader, int version)
        {
            var type = (VMAsyncStateType)reader.ReadByte();
            var state = TypeFactories.TryGetValue(type, out var factory)
                ? factory()
                : (VMAsyncState)Activator.CreateInstance(TypeResolve[type]);
            state.Version = version;
            state.Deserialize(reader);
            return state;
        }

        public static void SerializeGeneric(BinaryWriter writer, VMAsyncState state)
        {
            writer.Write((byte)TypeMarshal[state.GetType()]);
            state.SerializeInto(writer);
        }

        public bool Responded;
        public int WaitTime;
        public int Version;

        public virtual void Deserialize(BinaryReader reader)
        {
            Responded = reader.ReadBoolean();
            if (Version > 1) WaitTime = reader.ReadInt32();
        }

        public virtual void SerializeInto(BinaryWriter writer)
        {
            writer.Write(Responded);
            writer.Write(WaitTime);
        }
    }

    public enum VMAsyncStateType : byte
    {
        TransferFunds = 0,
        DialogResult = 1,
        PluginState = 2,
        InventoryOp = 3
    }
}
