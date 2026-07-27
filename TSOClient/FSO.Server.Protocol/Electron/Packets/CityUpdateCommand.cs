using FSO.Common.Serialization;
using Mina.Core.Buffer;

namespace FSO.Server.Protocol.Electron.Packets
{
    public enum CityUpdateCommandMode : byte
    {
        ClearTemp,
        Undo,
        SetCityName,
        SetThumbnail,
        CommandError,
        UndoError
    }

    public class CityUpdateCommand : AbstractElectronPacket
    {
        private const int MaxThumbnailSizeBytes = 180 * 135 * 4 + 4096; // Raw image plus some allowance.
        private const int MaxCityNameWidth = 24;

        public CityUpdateCommandMode Mode;
        public uint AvatarID;
        public int TargetUID;

        public string CityName;

        public byte[] Thumbnail;

        public override void Deserialize(IoBuffer input, ISerializationContext context)
        {
            Mode = (CityUpdateCommandMode)input.Get();
            switch (Mode)
            {
                case CityUpdateCommandMode.SetCityName:
                    CityName = input.GetPascalString();
                    if (CityName.Length > MaxCityNameWidth)
                    {
                        throw new InvalidDataException("City name size is out of range");
                    }
                    break;
                case CityUpdateCommandMode.SetThumbnail:
                    var length = input.GetInt32();
                    if (length > MaxThumbnailSizeBytes)
                    {
                        throw new InvalidDataException("City thumbnail is too large");
                    }
                    Thumbnail = input.GetSlice(length).GetBytes();
                    break;
                default:
                    AvatarID = input.GetUInt32();
                    TargetUID = input.GetInt32();
                    break;
            }
        }

        public override ElectronPacketType GetPacketType()
        {
            return ElectronPacketType.CityUpdateCommand;
        }

        public override void Serialize(IoBuffer output, ISerializationContext context)
        {
            output.Put((byte)Mode);

            switch (Mode)
            {
                case CityUpdateCommandMode.SetCityName:
                    output.PutPascalString(CityName);
                    break;
                case CityUpdateCommandMode.SetThumbnail:
                    output.PutInt32(Thumbnail.Length);
                    output.Put(Thumbnail);
                    break;
                default:
                    output.PutUInt32(AvatarID);
                    output.PutInt32(TargetUID);
                    break;
            }
        }
    }
}
