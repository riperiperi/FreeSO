using FSO.Common.Serialization;
using Mina.Core.Buffer;

namespace FSO.Server.Protocol.Aries.Packets
{
    public class AnswerAccepted : IAriesPacket
    {
        public void Deserialize(IoBuffer input, ISerializationContext context)
        {
        }

        public AriesPacketType GetPacketType()
        {
            return AriesPacketType.AnswerAccepted;
        }

        public void Serialize(IoBuffer output, ISerializationContext context)
        {
        }
    }
}
