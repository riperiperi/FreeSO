using System;
using Mina.Core.Buffer;
using FSO.Common.Serialization;

namespace FSO.Server.Protocol.Voltron.Packets
{
    public class VariableVoltronPacket : AbstractVoltronPacket
    {
        public ushort Type;
        public byte[] Bytes;

        public VariableVoltronPacket(ushort type, byte[] bytes)
        {
            this.Type = type;
            this.Bytes = bytes;
        }


        public override void Deserialize(IoBuffer input, ISerializationContext context)
        {
            throw new NotImplementedException();
        }

        public override VoltronPacketType GetPacketType()
        {
            return VoltronPacketTypeUtils.FromPacketCode(Type);
        }

        public override void Serialize(IoBuffer output, ISerializationContext context)
        {
            output.Put(Bytes, 0, Bytes.Length);
        }
    }
}
