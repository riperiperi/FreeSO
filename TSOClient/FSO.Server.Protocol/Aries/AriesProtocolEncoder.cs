using FSO.Server.Protocol.Utils;
using FSO.Server.Protocol.Voltron;
using Mina.Core.Buffer;
using Mina.Core.Session;
using Mina.Filter.Codec;
using NLog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FSO.Server.Protocol.Aries
{
    public class AriesProtocolEncoder : IProtocolEncoder
    {
        private static Logger LOG = LogManager.GetCurrentClassLogger();

        public void Dispose(IoSession session)
        {
        }

        public void Encode(IoSession session, object message, IProtocolEncoderOutput output)
        {
            if (message is IVoltronPacket)
            {
                EncodeVoltron(session, message, output);
            }else if(message is IAriesPacket)
            {
                EncodeAries(session, message, output);
            }
        }

        private void EncodeAries(IoSession session, object message, IProtocolEncoderOutput output)
        {
            IAriesPacket ariesPacket = (IAriesPacket)message;
            AriesPacketType ariesPacketType = ariesPacket.GetPacketType();

            LOG.Info("[ARIES-OUT] " + ariesPacketType.ToString() + " (" + ariesPacket.ToString() + ")");

            IoBuffer payload = ariesPacket.Serialize();
            payload.Flip();

            int payloadSize = payload.Remaining;
            IoBuffer headers = IoBuffer.Allocate(12);
            headers.Order = ByteOrder.LittleEndian;

            /** 
		     * Aries header
		     * 	uint32	type
		     *  uint32	timestamp
		     *  uint32	payloadSize
		     */
            uint timestamp = (uint)TimeSpan.FromTicks(DateTime.Now.Ticks - session.CreationTime.Ticks).TotalMilliseconds;
            headers.PutUInt32(ariesPacketType.GetPacketCode());
            headers.PutUInt32(timestamp);
            headers.PutUInt32((uint)payloadSize);
            headers.Flip();

            output.Write(headers);
            if (payloadSize > 0)
            {
                output.Write(payload);
            }
            output.Flush();
        }

        private void EncodeVoltron(IoSession session, object message, IProtocolEncoderOutput output)
        {
            IVoltronPacket voltronPacket = (IVoltronPacket)message;
            VoltronPacketType voltronPacketType = voltronPacket.GetPacketType();

            LOG.Info("[VOLTRON-OUT] " + voltronPacketType.ToString() + " (" + voltronPacket.ToString() + ")");

            IoBuffer payload = voltronPacket.Serialize();
            payload.Flip();

            int payloadSize = payload.Remaining;
            IoBuffer headers = IoBuffer.Allocate(18);
            headers.Order = ByteOrder.LittleEndian;

            /** 
		     * Aries header
		     * 	uint32	type
		     *  uint32	timestamp
		     *  uint32	payloadSize
		     */
            uint timestamp = (uint)TimeSpan.FromTicks(DateTime.Now.Ticks - session.CreationTime.Ticks).TotalMilliseconds;
            headers.PutUInt32(0);
            headers.PutUInt32(timestamp);
            headers.PutUInt32((uint)payloadSize + 6);

            /** 
		     * Voltron header
		     *  uint16	type
		     *  uint32	payloadSize
		     */
            headers.Order = ByteOrder.BigEndian;
            headers.PutUInt16(voltronPacketType.GetPacketCode());
            headers.PutUInt32((uint)payloadSize + 6);
            headers.Flip();

            output.Write(headers);
            output.Write(payload);
            output.Flush();
        }
    }
}
