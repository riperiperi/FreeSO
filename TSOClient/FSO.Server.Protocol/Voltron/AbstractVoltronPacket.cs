using FSO.Common.Serialization;
using FSO.Server.Protocol.Voltron.Model;
using Mina.Core.Buffer;

namespace FSO.Server.Protocol.Voltron
{
    public abstract class AbstractVoltronPacket : IVoltronPacket
    {
        public static Sender GetSender(IoBuffer buffer)
        {
            var ariesID = buffer.GetPascalString();
            var masterID = buffer.GetPascalString();
            return new Sender { AriesID = ariesID, MasterAccountID = masterID };
        }

        public static void PutSender(IoBuffer buffer, Sender sender)
        {
            buffer.PutPascalString(sender.AriesID);
            buffer.PutPascalString(sender.MasterAccountID);
        }

        public static IoBuffer Allocate(int size)
        {
            IoBuffer buffer = IoBuffer.Allocate(size);
            buffer.Order = ByteOrder.BigEndian;
            return buffer;
        }

        public abstract VoltronPacketType GetPacketType();
        public abstract void Serialize(IoBuffer output, ISerializationContext context);
        public abstract void Deserialize(IoBuffer input, ISerializationContext context);
    }
}
