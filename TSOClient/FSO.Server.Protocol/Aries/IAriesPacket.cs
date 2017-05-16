using FSO.Common.Serialization;
using Mina.Core.Buffer;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FSO.Server.Protocol.Aries
{
    public interface IAriesPacket : IoBufferSerializable, IoBufferDeserializable
    {
        /**
	     * Get packet type
	     * 
	     * @return
	     */
        AriesPacketType GetPacketType();
    }
}
