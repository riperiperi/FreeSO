using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Mina.Core.Buffer;

namespace FSO.Server.Protocol.Aries.Packets
{
    public class RequestClientSession : IAriesPacket
    {
        public void Deserialize(IoBuffer input)
        {
        }

        public AriesPacketType GetPacketType()
        {
            return AriesPacketType.RequestClientSession;
        }

        public IoBuffer Serialize()
        {
            return IoBuffer.Allocate(0);
        }
    }
}
