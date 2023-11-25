using System;
using Mina.Core.Buffer;
using FSO.Common.Serialization;
using FSO.Server.Protocol.Gluon.Packets;

namespace FSO.Server.Protocol.Gluon
{
    public abstract class AbstractGluonPacket : IGluonPacket
    {
        public static IoBuffer Allocate(int size)
        {
            IoBuffer buffer = IoBuffer.Allocate(size);
            buffer.Order = ByteOrder.BigEndian;
            return buffer;
        }

        public abstract GluonPacketType GetPacketType();
        public abstract void Deserialize(IoBuffer input, ISerializationContext context);
        public abstract void Serialize(IoBuffer output, ISerializationContext context);
    }

    public abstract class AbstractGluonCallPacket : AbstractGluonPacket, IGluonCall
    {
        public Guid CallId { get; set; }

        public override void Serialize(IoBuffer output, ISerializationContext context)
        {
            output.PutPascalString(CallId.ToString());
        }

        public override void Deserialize(IoBuffer input, ISerializationContext context)
        {
            CallId = Guid.Parse(input.GetPascalString());
        }
    }
}
