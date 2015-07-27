using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using TSO.Simantics.net.model.commands;

namespace TSO.Simantics.net.model
{
    public class VMNetCommand : VMSerializable
    {
        public static Dictionary<VMCommandType, Type> CmdMap = new Dictionary<VMCommandType, Type> {
            { VMCommandType.SimJoin, typeof(VMNetSimJoinCmd) },
            { VMCommandType.Interaction, typeof(VMNetInteractionCmd) },
            { VMCommandType.Architecture, typeof(VMNetArchitectureCmd) },
            { VMCommandType.BuyObject, typeof(VMNetBuyObjectCmd) },
            { VMCommandType.Chat, typeof(VMNetChatCmd) }
        };
        public static Dictionary<Type, VMCommandType> ReverseMap = CmdMap.ToDictionary(x => x.Value, x => x.Key);

        public VMCommandType Type;
        public VMNetCommandBodyAbstract Command;

        public void SetCommand(VMNetCommandBodyAbstract cmd)
        {
            Type = ReverseMap[cmd.GetType()];
            Command = cmd;
        }

        #region VMSerializable Members

        public void SerializeInto(BinaryWriter writer)
        {
            writer.Write((byte)Type);
            Command.SerializeInto(writer);
        }

        public void Deserialize(BinaryReader reader)
        {
            Type = (VMCommandType)reader.ReadByte();
            Type cmdType = CmdMap[Type];
            Command = (VMNetCommandBodyAbstract)Activator.CreateInstance(cmdType);
            Command.Deserialize(reader);
        }

        #endregion

    }

    public enum VMCommandType : byte
    {
        SimJoin = 0,
        Interaction = 1,
        Architecture = 2,
        BuyObject = 3,
        Chat = 4
    }
}
