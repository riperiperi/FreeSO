using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FSO.Common.Serialization;
using Mina.Core.Buffer;

namespace FSO.Server.Protocol.Electron.Packets
{
    public class PurchaseLotRequest : AbstractElectronPacket
    {
        public ushort LotLocation_X;
        public ushort LotLocation_Y;
        public string Name;

        public override void Deserialize(IoBuffer input, ISerializationContext context)
        {
            LotLocation_X = input.GetUInt16();
            LotLocation_Y = input.GetUInt16();
            Name = input.GetPascalString();
        }

        public override ElectronPacketType GetPacketType()
        {
            return ElectronPacketType.PurchaseLotRequest;
        }

        public override void Serialize(IoBuffer output, ISerializationContext context)
        {
            output.PutUInt16(LotLocation_X);
            output.PutUInt16(LotLocation_Y);
            output.PutPascalString(Name);
        }
    }
}
