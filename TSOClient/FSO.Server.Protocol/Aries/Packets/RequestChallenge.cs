using FSO.Common.Serialization;
using Mina.Core.Buffer;

namespace FSO.Server.Protocol.Aries.Packets
{
    public class RequestChallenge : IAriesPacket
    {
        public string CallSign;
        public string PublicHost;
        public string InternalHost;

        public void Deserialize(IoBuffer input, ISerializationContext context)
        {
            CallSign = input.GetPascalString();
            PublicHost = input.GetPascalString();
            InternalHost = input.GetPascalString();
        }

        public AriesPacketType GetPacketType()
        {
            return AriesPacketType.RequestChallenge;
        }

        public void Serialize(IoBuffer output, ISerializationContext context)
        {
            output.PutPascalString(CallSign);
            output.PutPascalString(PublicHost);
            output.PutPascalString(InternalHost);
        }
    }
}
