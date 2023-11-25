using FSO.Common.Serialization;
using Mina.Core.Buffer;

// Task -> City notifications.

namespace FSO.Server.Protocol.Gluon.Packets
{
    public class CityNotify : AbstractGluonPacket
    {
        public CityNotifyType Mode;
        public uint Value;
        public string Message = "";

        public CityNotify() { }

        public CityNotify(CityNotifyType mode)
        {
            Mode = mode;
        }

        public override void Deserialize(IoBuffer input, ISerializationContext context)
        {
            Mode = input.GetEnum<CityNotifyType>();
            Value = input.GetUInt32();
            Message = input.GetPascalVLCString();
        }

        public override GluonPacketType GetPacketType()
        {
            return GluonPacketType.CityNotify;
        }

        public override void Serialize(IoBuffer output, ISerializationContext context)
        {
            output.PutEnum(Mode);
            output.PutUInt32(Value);
            output.PutPascalVLCString(Message);
        }
    }

    public enum CityNotifyType : byte
    {
        NhoodUpdate = 1
    }
}
