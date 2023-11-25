using FSO.Common.Serialization;
using FSO.Server.Protocol.Electron;
using FSO.Server.Protocol.Gluon;
using FSO.Server.Protocol.Voltron;
using Mina.Core.Buffer;
using Mina.Core.Session;
using Mina.Filter.Codec;
using System;

namespace FSO.Server.Protocol.Aries
{
    public class AriesProtocolEncoder : IProtocolEncoder
    {
        //private static Logger LOG = LogManager.GetCurrentClassLogger();
        private ISerializationContext Context;

        public AriesProtocolEncoder(ISerializationContext context)
        {
            this.Context = context;
        }

        public void Dispose(IoSession session)
        {
        }

        public void Encode(IoSession session, object message, IProtocolEncoderOutput output)
        {
            if (message is IVoltronPacket)
            {
                EncodeVoltron(session, message, output);
            }
            else if (message is IElectronPacket)
            {
                EncodeElectron(session, message, output);
            }else if(message is IGluonPacket)
            {
                EncodeGluon(session, message, output);
            }
            else if (message is IAriesPacket)
            {
                EncodeAries(session, message, output);
            }
            else if (message.GetType().IsArray)
            {
                object[] arr = (object[])message;
                bool allVoltron = true;

                for (var i = 0; i < arr.Length; i++)
                {
                    if (!(arr[i] is IVoltronPacket))
                    {
                        allVoltron = false;
                        break;
                    }
                }

                //TODO: Chunk these into fewer packets
                for (var i = 0; i < arr.Length; i++)
                {
                    Encode(session, arr[i], output);
                }
            }
        }

        private void EncodeAries(IoSession session, object message, IProtocolEncoderOutput output)
        {
            IAriesPacket ariesPacket = (IAriesPacket)message;
            AriesPacketType ariesPacketType = ariesPacket.GetPacketType();

            var payload = IoBuffer.Allocate(128);
            payload.Order = ByteOrder.LittleEndian;
            payload.AutoExpand = true;
            ariesPacket.Serialize(payload, Context);
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

            if (payloadSize > 0)
            {
                headers.AutoExpand = true;
                headers.Put(payload);
            }

            headers.Flip();
            output.Write(headers);
            //output.Flush();
        }
        
        private void EncodeVoltron(IoSession session, object message, IProtocolEncoderOutput output)
        {
            IVoltronPacket voltronPacket = (IVoltronPacket)message;
            VoltronPacketType voltronPacketType = voltronPacket.GetPacketType();
            EncodeVoltronStylePackets(session, output, AriesPacketType.Voltron, voltronPacketType.GetPacketCode(), voltronPacket);
        }

        private void EncodeElectron(IoSession session, object message, IProtocolEncoderOutput output)
        {
            IElectronPacket packet = (IElectronPacket)message;
            ElectronPacketType packetType = packet.GetPacketType();
            EncodeVoltronStylePackets(session, output, AriesPacketType.Electron, packetType.GetPacketCode(), packet);
        }

        private void EncodeGluon(IoSession session, object message, IProtocolEncoderOutput output)
        {
            IGluonPacket packet = (IGluonPacket)message;
            GluonPacketType packetType = packet.GetPacketType();
            EncodeVoltronStylePackets(session, output, AriesPacketType.Gluon, packetType.GetPacketCode(), packet);
        }

        private void EncodeVoltronStylePackets(IoSession session, IProtocolEncoderOutput output, AriesPacketType ariesType, ushort packetType, IoBufferSerializable message)
        {
            var payload = IoBuffer.Allocate(512);
            payload.Order = ByteOrder.BigEndian;
            payload.AutoExpand = true;
            message.Serialize(payload, Context);
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
            headers.PutUInt32(ariesType.GetPacketCode());
            headers.PutUInt32(timestamp);
            headers.PutUInt32((uint)payloadSize + 6);

            /** 
		     * Voltron header
		     *  uint16	type
		     *  uint32	payloadSize
		     */
            headers.Order = ByteOrder.BigEndian;
            headers.PutUInt16(packetType);
            headers.PutUInt32((uint)payloadSize + 6);
            
            if (payloadSize > 0)
            {
                headers.AutoExpand = true;
                headers.Put(payload);
            }

            headers.Flip();
            output.Write(headers);
            //output.Flush();
        }
    }
}
