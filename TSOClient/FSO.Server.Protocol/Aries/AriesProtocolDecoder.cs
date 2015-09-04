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
using FSO.Server.Protocol.Aries.Packets;
using FSO.Server.Protocol.Electron;

namespace FSO.Server.Protocol.Aries
{
    public class AriesProtocolDecoder : CumulativeProtocolDecoder
    {
        private static Logger LOG = LogManager.GetCurrentClassLogger();

        protected override bool DoDecode(IoSession session, IoBuffer buffer, IProtocolDecoderOutput output)
        {
            if(buffer.Remaining < 12){
                return false;
            }

            /**
             * We expect aries, voltron or electron packets
             */
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

            if(packetType == AriesPacketType.Voltron.GetPacketCode())
            {
                DecodeVoltronStylePackets(buffer, ref payloadSize, output, VoltronPackets.GetByPacketCode);
            }
            else if (packetType == AriesPacketType.Electron.GetPacketCode())
            {
                DecodeVoltronStylePackets(buffer, ref payloadSize, output, ElectronPackets.GetByPacketCode);
            }
            else
            {
                //Aries
                var packetClass = AriesPackets.GetByPacketCode(packetType);
                if (packetClass != null)
                {
                    byte[] data = new byte[(int)payloadSize];
                    buffer.Get(data, 0, (int)payloadSize);

                    IAriesPacket packet = (IAriesPacket)Activator.CreateInstance(packetClass);
                    packet.Deserialize(IoBuffer.Wrap(data));
                    output.Write(packet);

                    payloadSize = 0;
                }
                else
                {
                    buffer.Skip((int)payloadSize);
                    payloadSize = 0;
                }
            }

            return true;
        }


        private void DecodeVoltronStylePackets(IoBuffer buffer, ref uint payloadSize, IProtocolDecoderOutput output, Func<ushort, Type> typeResolver)
        {
            while (payloadSize > 0)
            {
                /** Voltron packet **/
                buffer.Order = ByteOrder.BigEndian;
                ushort type = buffer.GetUInt16();
                uint innerPayloadSize = buffer.GetUInt32() - 6;

                byte[] data = new byte[(int)innerPayloadSize];
                buffer.Get(data, 0, (int)innerPayloadSize);

                var packetClass = typeResolver(type);
                if (packetClass != null)
                {
                    IoBufferDeserializable packet = (IoBufferDeserializable)Activator.CreateInstance(packetClass);
                    packet.Deserialize(IoBuffer.Wrap(data));
                    output.Write(packet);
                }

                payloadSize -= innerPayloadSize + 6;
            }
        }
    }
}
