using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Mina.Core.Buffer;

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


        public override void Deserialize(IoBuffer input)
        {
            throw new NotImplementedException();
        }

        public override VoltronPacketType GetPacketType()
        {
            return VoltronPacketTypeUtils.FromPacketCode(Type);
        }

        public override IoBuffer Serialize()
        {
            var result = Allocate(Bytes.Length);
            result.Put(Bytes, 0, Bytes.Length);
            return result;
        }
    }
}
