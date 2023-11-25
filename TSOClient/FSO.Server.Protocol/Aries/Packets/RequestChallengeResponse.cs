using FSO.Common.Serialization;
using Mina.Core.Buffer;

namespace FSO.Server.Protocol.Aries.Packets
{
    public class RequestChallengeResponse : IAriesPacket
    {
        public string Challenge;

        public void Deserialize(IoBuffer input, ISerializationContext context)
        {
            Challenge = input.GetPascalVLCString();
        }

        public AriesPacketType GetPacketType()
        {
            return AriesPacketType.RequestChallengeResponse;
        }

        public void Serialize(IoBuffer output, ISerializationContext context)
        {
            output.PutPascalVLCString(Challenge);
        }
    }
}
