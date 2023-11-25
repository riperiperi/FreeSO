using FSO.Common.Serialization;
using FSO.Server.Protocol.Electron.Model;
using Mina.Core.Buffer;

namespace FSO.Server.Protocol.Electron.Packets
{
    public class ChangeRoommateResponse : AbstractElectronPacket
    {
        public ChangeRoommateResponseStatus Type;
        public uint Extra;

        public override void Deserialize(IoBuffer input, ISerializationContext context)
        {
            Type = input.GetEnum<ChangeRoommateResponseStatus>();
            Extra = input.GetUInt32();
        }

        public override ElectronPacketType GetPacketType()
        {
            return ElectronPacketType.ChangeRoommateResponse;
        }

        public override void Serialize(IoBuffer output, ISerializationContext context)
        {
            output.PutEnum(Type);
            output.PutUInt32(Extra);
        }
    }
}
