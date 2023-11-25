using Mina.Core.Buffer;
using FSO.Common.Serialization;

namespace FSO.Server.Protocol.Aries.Packets
{
    public class RequestClientSession : IAriesPacket
    {
        public void Deserialize(IoBuffer input, ISerializationContext context)
        {
        }

        public AriesPacketType GetPacketType()
        {
            return AriesPacketType.RequestClientSession;
        }

        public void Serialize(IoBuffer output, ISerializationContext context)
        {
        }
    }
}
