using FSO.Common.Serialization;
using FSO.Server.Protocol.Electron.Model.CityEditCommands;
using Mina.Core.Buffer;

namespace FSO.Server.Protocol.Electron.Packets
{
    public class CityUpdateResponse : AbstractElectronPacket
    {
        public int StartIndex;
        public CityEditCommand[] Commands;

        public override void Deserialize(IoBuffer input, ISerializationContext context)
        {
            StartIndex = input.GetInt32();
            var commandCount = input.GetInt32();

            var commands = new List<CityEditCommand>();
            for (int i = 0; i < commandCount; i++)
            {
                commands.Add(new CityEditCommand(input, context));
            }

            Commands = [.. commands];
        }

        public override ElectronPacketType GetPacketType()
        {
            return ElectronPacketType.CityUpdateResponse;
        }

        public override void Serialize(IoBuffer output, ISerializationContext context)
        {
            output.PutInt32(StartIndex);
            output.PutInt32(Commands.Length);

            foreach (var command in Commands)
            {
                command.Serialize(output, context);
            }
        }
    }
}
