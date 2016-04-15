using FSO.SimAntics.NetPlay.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using FSO.SimAntics.Primitives;

namespace FSO.SimAntics.Model.TSOPlatform
{
    public class VMAsyncState : VMSerializable
    {
        public static Dictionary<VMAsyncStateType, Type> TypeResolve = new Dictionary<VMAsyncStateType, System.Type>()
        {
            { VMAsyncStateType.TransferFunds, typeof(VMTransferFundsState) },
            { VMAsyncStateType.DialogResult, typeof(VMDialogResult) }
        };
        public static Dictionary<Type, VMAsyncStateType> TypeMarshal = TypeResolve.ToDictionary(x => x.Value, x => x.Key);

        public static VMAsyncState DeserializeGeneric(BinaryReader reader)
        {
            var type = (VMAsyncStateType)reader.ReadByte();
            Type cmdType = TypeResolve[type];
            var state = (VMAsyncState)Activator.CreateInstance(cmdType);
            state.Deserialize(reader);
            return state;
        }

        public static void SerializeGeneric(BinaryWriter writer, VMAsyncState state)
        {
            writer.Write((byte)TypeMarshal[state.GetType()]);
            state.SerializeInto(writer);
        }

        public bool Responded;
        public virtual void Deserialize(BinaryReader reader)
        {
            Responded = reader.ReadBoolean();
        }

        public virtual void SerializeInto(BinaryWriter writer)
        {
            writer.Write(Responded);
        }
    }

    public enum VMAsyncStateType : byte
    {
        TransferFunds = 0,
        DialogResult = 1
    }
}
