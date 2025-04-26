using FSO.Common.Serialization;
using FSO.Server.Protocol.Electron.Model;
using Mina.Core.Buffer;

namespace FSO.Server.Protocol.Electron.Packets
{
    public enum ArchiveAvatarSelectCode
    {
        Success = 0,
        NotFound,
        NoPermission,
        InUseSelf,
        InUse,
        UnknownError
    }

    public class ArchiveAvatarSelectResponse : AbstractElectronPacket
    {
        public ArchiveAvatarSelectCode Code;

        public override void Deserialize(IoBuffer input, ISerializationContext context)
        {
            Code = input.GetEnum<ArchiveAvatarSelectCode>();
        }

        public override ElectronPacketType GetPacketType()
        {
            return ElectronPacketType.ArchiveAvatarSelectResponse;
        }

        public override void Serialize(IoBuffer output, ISerializationContext context)
        {
            output.PutEnum(Code);
        }
    }
}
