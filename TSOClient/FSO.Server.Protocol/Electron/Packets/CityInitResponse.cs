using FSO.Common.Serialization;
using FSO.Server.Protocol.Electron.Model;
using FSO.Server.Protocol.Electron.Model.CityEditCommands;
using Mina.Core.Buffer;

namespace FSO.Server.Protocol.Electron.Packets
{
    public class CityInitResponse : AbstractElectronPacket, IActionResponse
    {
        public bool Success => true;

        public object OCode => 0;

        public byte[] CityData;
        public CityEditCommand[] Commands;

        public override void Deserialize(IoBuffer input, ISerializationContext context)
        {
            int cityDataLength = input.GetInt32();

            CityData = input.GetSlice(cityDataLength).GetBytes();

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
            return ElectronPacketType.CityInitResponse;
        }

        public override void Serialize(IoBuffer output, ISerializationContext context)
        {
            output.PutInt32(CityData.Length);
            output.Put(CityData);

            output.PutInt32(Commands.Length);

            foreach (var command in Commands)
            {
                command.Serialize(output, context);
            }
        }
    }
}
