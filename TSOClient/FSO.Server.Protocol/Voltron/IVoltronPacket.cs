using FSO.Server.Protocol.Utils;
using Mina.Core.Buffer;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FSO.Server.Protocol.Voltron
{
    public interface IVoltronPacket : IoBufferDeserializable, IoBufferSerializable
    {
        /**
	     * Get packet type
	     * 
	     * @return
	     */
        VoltronPacketType GetPacketType();
        
    }
}
