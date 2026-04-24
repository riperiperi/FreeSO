using FSO.Common.Serialization;
using Mina.Core.Buffer;

namespace FSO.Server.Protocol.Electron.Packets
{
    public enum CityUpdateCommandMode : byte
    {
        ClearTemp,
        Undo
    }

    public class CityUpdateCommand : AbstractElectronPacket
    {
        public CityUpdateCommandMode Mode;
        public uint AvatarID;
        public int TargetUID;

        public override void Deserialize(IoBuffer input, ISerializationContext context)
        {
            Mode = (CityUpdateCommandMode)input.Get();
            AvatarID = input.GetUInt32();
            TargetUID = input.GetInt32();
        }

        public override ElectronPacketType GetPacketType()
        {
            return ElectronPacketType.CityUpdateCommand;
        }

        public override void Serialize(IoBuffer output, ISerializationContext context)
        {
            output.Put((byte)Mode);
            output.PutUInt32(AvatarID);
            output.PutInt32(TargetUID);
        }
    }
}
