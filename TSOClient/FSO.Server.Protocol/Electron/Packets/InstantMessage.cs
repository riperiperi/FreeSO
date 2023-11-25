using FSO.Common.Serialization;
using Mina.Core.Buffer;
using FSO.Common.Enum;

namespace FSO.Server.Protocol.Electron.Packets
{
    public class InstantMessage : AbstractElectronPacket
    {
        public UserReferenceType FromType;
        public uint From;
        public uint To;
        public InstantMessageType Type;
        public string Message;
        public string AckID;
        public InstantMessageFailureReason Reason = InstantMessageFailureReason.NONE;
        public uint Color;

        public override void Deserialize(IoBuffer input, ISerializationContext context)
        {
            FromType = input.GetEnum<UserReferenceType>();
            From = input.GetUInt32();
            To = input.GetUInt32();
            Type = input.GetEnum<InstantMessageType>();
            Message = input.GetPascalVLCString();
            AckID = input.GetPascalVLCString();
            Reason = input.GetEnum<InstantMessageFailureReason>();
            Color = input.GetUInt32();
        }

        public override ElectronPacketType GetPacketType()
        {
            return ElectronPacketType.InstantMessage;
        }

        public override void Serialize(IoBuffer output, ISerializationContext context)
        {
            output.PutEnum(FromType);
            output.PutUInt32(From);
            output.PutUInt32(To);
            output.PutEnum(Type);
            output.PutPascalVLCString(Message);
            output.PutPascalVLCString(AckID);
            output.PutEnum(Reason);
            output.PutUInt32(Color);
        }
    }
    
    public enum InstantMessageType
    {
        MESSAGE,
        SUCCESS_ACK,
        FAILURE_ACK
    }

    public enum InstantMessageFailureReason
    {
        NONE,
        THEY_ARE_OFFLINE,
        THEY_ARE_IGNORING_YOU,
        YOU_ARE_IGNORING_THEM,
        MESSAGE_QUEUE_FULL
    }
}
