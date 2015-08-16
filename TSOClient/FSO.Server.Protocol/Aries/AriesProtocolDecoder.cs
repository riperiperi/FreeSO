using Mina.Filter.Codec;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Mina.Core.Buffer;
using Mina.Core.Session;
using NLog;
using FSO.Server.Protocol.Utils;
using FSO.Server.Protocol.Voltron;

namespace FSO.Server.Protocol.Aries
{
    public class AriesProtocolDecoder : CumulativeProtocolDecoder
    {
        private static Logger LOG = LogManager.GetCurrentClassLogger();

        protected override bool DoDecode(IoSession session, IoBuffer buffer, IProtocolDecoderOutput output)
        {
            /**
             * We expect aries, voltron or electron packets
             */
            buffer.Rewind();

            buffer.Order = ByteOrder.LittleEndian;
            uint packetType = buffer.GetUInt32();
            uint timestamp = buffer.GetUInt32();
            uint payloadSize = buffer.GetUInt32();

            if (buffer.Remaining < payloadSize)
            {
                buffer.Skip(-12);
                /** Not all here yet **/
                return false;
            }

            LOG.Info("[ARIES] " + packetType + " (" + payloadSize + ")");

            while (payloadSize > 0)
            {
                if (packetType == 0)
                {
                    /** Voltron packet **/
                    buffer.Order = ByteOrder.BigEndian;
                    ushort voltronType = buffer.GetUInt16();
                    uint voltronPayloadSize = buffer.GetUInt32() - 6;

                    byte[] data = new byte[(int)voltronPayloadSize];
                    buffer.Get(data, 0, (int)voltronPayloadSize);

                    var packetClass = VoltronPackets.GetByPacketCode(voltronType);
                    if (packetClass != null)
                    {
                        IVoltronPacket packet = (IVoltronPacket)Activator.CreateInstance(packetClass);
                        LOG.Info("[VOLTRON-IN] " + packet.GetPacketType().ToString() + " (" + packet.ToString() + ")");
                        packet.Deserialize(IoBuffer.Wrap(data));
                        output.Write(packet);
                    }
                    else
                    {
                        LOG.Info("[VOLTRON-IN] " + voltronType + " (" + voltronPayloadSize + ")");
                    }

                    payloadSize -= voltronPayloadSize + 6;
                }
                else
                {
                    payloadSize = 0;
                    buffer.Skip(((int)payloadSize) - 12);
                }
            }

            return true;
        }
    }
}
