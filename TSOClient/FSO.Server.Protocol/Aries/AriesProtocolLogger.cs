using Mina.Core.Session;
using Mina.Core.Buffer;
using Mina.Core.Filterchain;
using FSO.Server.Common;
using Mina.Core.Write;
using FSO.Server.Protocol.Voltron;
using FSO.Common.Serialization;

namespace FSO.Server.Protocol.Aries
{
    public class AriesProtocolLogger : IoFilterAdapter
    {
        //private static Logger LOG = LogManager.GetCurrentClassLogger();

        private IPacketLogger PacketLogger;
        private ISerializationContext Context;

        public AriesProtocolLogger(IPacketLogger packetLogger, ISerializationContext context)
        {
            this.PacketLogger = packetLogger;
            this.Context = context;
        }

        public override void MessageSent(INextFilter nextFilter, IoSession session, IWriteRequest writeRequest)
        {
            IVoltronPacket voltronPacket = writeRequest.OriginalRequest.Message as IVoltronPacket;
            if (voltronPacket != null)
            {
                var voltronBuffer = IoBuffer.Allocate(512);
                voltronBuffer.Order = ByteOrder.BigEndian;
                voltronBuffer.AutoExpand = true;
                voltronPacket.Serialize(voltronBuffer, Context);
                voltronBuffer.Flip();

                var byteArray = new byte[voltronBuffer.Remaining];
                voltronBuffer.Get(byteArray, 0, voltronBuffer.Remaining);

                PacketLogger.OnPacket(new Packet
                {
                    Data = byteArray,
                    Type = PacketType.VOLTRON,
                    SubType = voltronPacket.GetPacketType().GetPacketCode(),
                    Direction = PacketDirection.OUTPUT
                });
                nextFilter.MessageSent(session, writeRequest);
                return;
            }

            IAriesPacket ariesPacket = writeRequest.OriginalRequest.Message as IAriesPacket;
            if(ariesPacket != null)
            {
                IoBuffer ariesBuffer = IoBuffer.Allocate(128);
                ariesBuffer.AutoExpand = true;
                ariesBuffer.Order = ByteOrder.LittleEndian;
                ariesPacket.Serialize(ariesBuffer, Context);
                ariesBuffer.Flip();

                var byteArray = new byte[ariesBuffer.Remaining];
                ariesBuffer.Get(byteArray, 0, ariesBuffer.Remaining);

                PacketLogger.OnPacket(new Packet
                {
                    Data = byteArray,
                    Type = PacketType.ARIES,
                    SubType = ariesPacket.GetPacketType().GetPacketCode(),
                    Direction = PacketDirection.OUTPUT
                });
                nextFilter.MessageSent(session, writeRequest);
                return;
            }

            IoBuffer buffer = writeRequest.Message as IoBuffer;
            if (buffer == null)
            {
                nextFilter.MessageSent(session, writeRequest);
                return;
            }

            TryParseAriesFrame(buffer, PacketDirection.OUTPUT);
            nextFilter.MessageSent(session, writeRequest);
        }

        public override void MessageReceived(INextFilter nextFilter, IoSession session, object message)
        {
            IoBuffer buffer = message as IoBuffer;
            if (buffer == null)
            {
                nextFilter.MessageReceived(session, message);
                return;
            }

            TryParseAriesFrame(buffer, PacketDirection.INPUT);

            nextFilter.MessageReceived(session, message);
        }
        
        private void TryParseAriesFrame(IoBuffer buffer, PacketDirection direction)
        {
            buffer.Rewind();

            if(buffer.Remaining < 12)
            {
                return;
            }

            buffer.Order = ByteOrder.LittleEndian;
            uint packetType = buffer.GetUInt32();
            uint timestamp = buffer.GetUInt32();
            uint payloadSize = buffer.GetUInt32();

            if (buffer.Remaining < payloadSize)
            {
                buffer.Skip(-12);
                return;
            }

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

                    PacketLogger.OnPacket(new Packet
                    {
                        Data = data,
                        Type = PacketType.VOLTRON,
                        SubType = voltronType,
                        Direction = direction
                    });

                    payloadSize -= voltronPayloadSize + 6;
                }
                else
                {
                    byte[] data = new byte[(int)payloadSize];
                    buffer.Get(data, 0, (int)payloadSize);

                    PacketLogger.OnPacket(new Packet
                    {
                        Data = data,
                        Type = PacketType.ARIES,
                        SubType = packetType,
                        Direction = direction
                    });

                    payloadSize = 0;
                }
            }


            buffer.Rewind();
        }
    }
}
