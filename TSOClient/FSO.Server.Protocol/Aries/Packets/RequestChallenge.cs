using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FSO.Common.Serialization;
using Mina.Core.Buffer;

namespace FSO.Server.Protocol.Aries.Packets
{
    public class RequestChallenge : IAriesPacket
    {
        public string CallSign;

        public void Deserialize(IoBuffer input, ISerializationContext context)
        {
            CallSign = input.GetPascalString();
        }

        public AriesPacketType GetPacketType()
        {
            return AriesPacketType.RequestChallenge;
        }

        public void Serialize(IoBuffer output, ISerializationContext context)
        {
            output.PutPascalString(CallSign);
        }
    }
}
