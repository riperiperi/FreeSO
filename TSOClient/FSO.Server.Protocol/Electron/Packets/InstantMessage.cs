using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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

        public override void Deserialize(IoBuffer input, ISerializationContext context)
        {
            FromType = input.GetEnum<UserReferenceType>();
            From = input.GetUInt32();
            To = input.GetUInt32();
            Type = input.GetEnum<InstantMessageType>();
            Message = input.GetPascalVLCString();
            AckID = input.GetPascalVLCString();
            Reason = input.GetEnum<InstantMessageFailureReason>();
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
