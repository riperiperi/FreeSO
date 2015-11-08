using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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
