using FSO.Common.Serialization;
using Mina.Core.Buffer;
using FSO.Server.Protocol.Electron.Model;

namespace FSO.Server.Protocol.Electron.Packets
{
    public class ModerationRequest : AbstractElectronPacket
    {
        public ModerationRequestType Type;
        public uint EntityId;

        public override void Deserialize(IoBuffer input, ISerializationContext context)
        {
            Type = input.GetEnum<ModerationRequestType>();
            EntityId = input.GetUInt32();
        }

        public override ElectronPacketType GetPacketType()
        {
            return ElectronPacketType.ModerationRequest;
        }

        public override void Serialize(IoBuffer output, ISerializationContext context)
        {
            output.PutEnum(Type);
            output.PutUInt32(EntityId);
        }
    }
}
