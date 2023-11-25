using Mina.Core.Buffer;
using FSO.Common.Serialization;

namespace FSO.Server.Protocol.Voltron.Packets
{
    public class TransmitCreateAvatarNotificationPDU : AbstractVoltronPacket
    {
        public override void Deserialize(IoBuffer input, ISerializationContext context)
        {

        }

        public override VoltronPacketType GetPacketType()
        {
            return VoltronPacketType.TransmitCreateAvatarNotificationPDU;
        }

        public override void Serialize(IoBuffer output, ISerializationContext context)
        {
            output.Put(10);
        }
    }
}
