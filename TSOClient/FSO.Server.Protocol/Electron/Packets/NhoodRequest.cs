using FSO.Common.Serialization;
using FSO.Server.Protocol.Electron.Model;
using Mina.Core.Buffer;

namespace FSO.Server.Protocol.Electron.Packets
{
    public class NhoodRequest : AbstractElectronPacket, IActionRequest
    {
        public NhoodRequestType Type;
        public uint TargetAvatar; //vote, nominate, rate, delete rate, force mayor, gameplay ban, add candidate, remove candidate
        public uint TargetNHood; //vote, nominate, add candidate, remove candidate

        public object OType => Type;
        public bool NeedsValidation => 
            Type == NhoodRequestType.CAN_NOMINATE || Type == NhoodRequestType.CAN_RATE 
            || Type == NhoodRequestType.CAN_RUN || Type == NhoodRequestType.CAN_VOTE || Type == NhoodRequestType.CAN_FREE_VOTE;

        public string Message = ""; //bulletin, rate
        public uint Value; //rate (stars), nomination_run (accept if >0)

        public override void Deserialize(IoBuffer input, ISerializationContext context)
        {
            Type = input.GetEnum<NhoodRequestType>();
            TargetAvatar = input.GetUInt32();
            TargetNHood = input.GetUInt32();

            Message = input.GetPascalString();
            Value = input.GetUInt32();
        }

        public override ElectronPacketType GetPacketType()
        {
            return ElectronPacketType.NhoodRequest;
        }

        public override void Serialize(IoBuffer output, ISerializationContext context)
        {
            output.PutEnum(Type);
            output.PutUInt32(TargetAvatar);
            output.PutUInt32(TargetNHood);

            output.PutPascalString(Message);
            output.PutUInt32(Value);
        }
    }

    public enum NhoodRequestType : byte
    {
        VOTE = 0,
        CAN_VOTE,
        NOMINATE,
        CAN_NOMINATE,

        RATE,
        CAN_RATE,
        NOMINATION_RUN,
        CAN_RUN,

        CAN_FREE_VOTE,
        FREE_VOTE,

        //moderator commands
        DELETE_RATE,
        FORCE_MAYOR,
        REMOVE_CANDIDATE,
        ADD_CANDIDATE,
        TEST_ELECTION, //nhood id, avatar id = state (over/nomination/election), value = end date in x days
        PRETEND_DATE, //run the daily nhood task as if it's (value) date. (Epoch value)
        NHOOD_GAMEPLAY_BAN
    }
}
