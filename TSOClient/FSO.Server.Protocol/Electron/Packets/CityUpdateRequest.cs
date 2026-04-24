using FSO.Common.Serialization;
using FSO.Server.Protocol.Electron.Model.CityEditCommands;
using Mina.Core.Buffer;

namespace FSO.Server.Protocol.Electron.Packets
{
    public class CityUpdateRequest : AbstractElectronPacket
    {
        public CityEditCommand Command;

        public override void Deserialize(IoBuffer input, ISerializationContext context)
        {
            Command = new CityEditCommand(input, context);
        }

        public override ElectronPacketType GetPacketType()
        {
            return ElectronPacketType.CityUpdateRequest;
        }

        public override void Serialize(IoBuffer output, ISerializationContext context)
        {
            Command.Serialize(output, context);
        }
    }
}
