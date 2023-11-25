using Mina.Filter.Codec;
using System;
using Mina.Core.Buffer;
using Mina.Core.Session;
using FSO.Server.Protocol.Voltron;
using FSO.Server.Protocol.Electron;
using FSO.Common.Serialization;
using FSO.Server.Protocol.Utils;
using FSO.Server.Protocol.Gluon;

namespace FSO.Server.Protocol.Aries
{
    public class AriesProtocolDecoder : CustomCumulativeProtocolDecoder
    {
        //private static Logger LOG = LogManager.GetCurrentClassLogger();
        private ISerializationContext Context;

        public AriesProtocolDecoder(ISerializationContext context)
        {
            this.Context = context;
        }

        protected override bool DoDecode(IoSession session, IoBuffer buffer, IProtocolDecoderOutput output)
        {
            if(buffer.Remaining < 12){
                return false;
            }

            /**
             * We expect aries, voltron or electron packets
             */
            var startPosition = buffer.Position;

            buffer.Order = ByteOrder.LittleEndian;
            uint packetType = buffer.GetUInt32();
            uint timestamp = buffer.GetUInt32();
            uint payloadSize = buffer.GetUInt32();

            if (buffer.Remaining < payloadSize)
            {
                /** Not all here yet **/
                buffer.Position = startPosition;
                return false;
            }

            //LOG.Info("[ARIES] " + packetType + " (" + payloadSize + ")");
            
            if(packetType == AriesPacketType.Voltron.GetPacketCode())
            {
                DecodeVoltronStylePackets(buffer, ref payloadSize, output, VoltronPackets.GetByPacketCode);
            }
            else if (packetType == AriesPacketType.Electron.GetPacketCode())
            {
                DecodeVoltronStylePackets(buffer, ref payloadSize, output, ElectronPackets.GetByPacketCode);
            }else if(packetType == AriesPacketType.Gluon.GetPacketCode())
            {
                DecodeVoltronStylePackets(buffer, ref payloadSize, output, GluonPackets.GetByPacketCode);
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
                    var io = IoBuffer.Wrap(data);
                    io.Order = ByteOrder.LittleEndian;
                    packet.Deserialize(io, Context);
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
                    packet.Deserialize(IoBuffer.Wrap(data), Context);
                    output.Write(packet);
                }

                payloadSize -= innerPayloadSize + 6;
            }
        }
    }
}
