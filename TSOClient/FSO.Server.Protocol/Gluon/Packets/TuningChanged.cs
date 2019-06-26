using FSO.Common.Serialization;
using Mina.Core.Buffer;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FSO.Server.Protocol.Gluon.Packets
{
    public class TuningChanged : AbstractGluonCallPacket
    {
        public bool UpdateInstantly;

        public override void Deserialize(IoBuffer input, ISerializationContext context)
        {
            base.Deserialize(input, context);
            UpdateInstantly = input.GetBool();
        }

        public override void Serialize(IoBuffer output, ISerializationContext context)
        {
            base.Serialize(output, context);
            output.PutBool(UpdateInstantly);
        }

        public override GluonPacketType GetPacketType()
        {
            return GluonPacketType.TuningChanged;
        }
    }
}
