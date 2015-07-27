using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using TSO.Simantics.model;

namespace TSO.Simantics.net.model.commands
{
    public class VMNetArchitectureCmd : VMNetCommandBodyAbstract
    {
        List<VMArchitectureCommand> Commands;

        public override bool Execute(VM vm)
        {
            vm.Context.Architecture.RunCommands(Commands);
            return true;
        }

        #region VMSerializable Members
        public override void SerializeInto(BinaryWriter writer)
        {
            if (Commands == null) writer.Write(0);
            else
            {
                writer.Write(Commands.Count);
                for (int i=0; i<Commands.Count; i++)
                {
                    Commands[i].SerializeInto(writer);
                }
            }
        }

        public override void Deserialize(BinaryReader reader)
        {
            Commands = new List<VMArchitectureCommand>();
            int length = reader.ReadInt32();
            for (int i=0; i<length; i++)
            {
                var cmd = new VMArchitectureCommand();
                cmd.Deserialize(reader);
                Commands.Add(cmd);
            }
        }
        #endregion

    }
}
