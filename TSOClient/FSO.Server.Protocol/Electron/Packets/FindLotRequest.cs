using FSO.Common.Serialization;
using Mina.Core.Buffer;

namespace FSO.Server.Protocol.Electron.Packets
{
    public class FindLotRequest : AbstractElectronPacket
    {
        public uint LotId;
        public bool OpenIfClosed;

        public override void Deserialize(IoBuffer input, ISerializationContext context)
        {
            LotId = input.GetUInt32();
            OpenIfClosed = input.GetBool();
        }

        public override ElectronPacketType GetPacketType()
        {
            return ElectronPacketType.FindLotRequest;
        }

        public override void Serialize(IoBuffer output, ISerializationContext context)
        {
            output.PutUInt32(LotId);
            output.PutBool(OpenIfClosed);
        }
    }
}
