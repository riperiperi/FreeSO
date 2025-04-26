using FSO.Common.Serialization;
using FSO.Server.Protocol.Electron.Model;
using Mina.Core.Buffer;

namespace FSO.Server.Protocol.Electron.Packets
{
    public class ArchiveAvatarsRequest : AbstractElectronPacket, IActionRequest
    {
        public object OType => 0;

        public bool NeedsValidation => false;

        public override void Deserialize(IoBuffer input, ISerializationContext context)
        {
            input.GetUInt32();
        }

        public override ElectronPacketType GetPacketType()
        {
            return ElectronPacketType.ArchiveAvatarsRequest;
        }

        public override void Serialize(IoBuffer output, ISerializationContext context)
        {
            output.PutUInt32(0);
        }
    }
}
