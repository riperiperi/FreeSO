using Mina.Core.Buffer;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FSO.Server.Protocol.Voltron
{
    public interface IVoltronPacket
    {
        /**
	     * Get packet type
	     * 
	     * @return
	     */
        VoltronPacketType GetPacketType();

        /**
	     * Serialize packet to byte stream
	     * @param buffer
	     */
        IoBuffer Serialize();

        /**
	     * Read data from packet buffer
	     * @param in
	     */
        void Deserialize(IoBuffer input);
    }
}
