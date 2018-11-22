using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FSO.Common.Serialization;
using Mina.Core.Buffer;

namespace FSO.Server.Protocol.Electron.Packets
{
    public class NhoodResponse : AbstractElectronPacket
    {
        public NhoodResponseCode Code;
        public uint BanEndDate;
        public string Message = "";

        public override void Deserialize(IoBuffer input, ISerializationContext context)
        {
            Code = input.GetEnum<NhoodResponseCode>();
            BanEndDate = input.GetUInt32();
            Message = input.GetPascalVLCString();
        }

        public override ElectronPacketType GetPacketType()
        {
            return ElectronPacketType.NhoodResponse;
        }

        public override void Serialize(IoBuffer output, ISerializationContext context)
        {
            output.PutEnum(Code);
            output.PutUInt32(BanEndDate);
            output.PutPascalVLCString(Message);
        }
    }

    public enum NhoodResponseCode : byte
    {
        SUCCESS = 0x00,

        //nominate/vote
        NOT_IN_NHOOD = 0x01,
        ELECTION_OVER = 0x02,
        CANDIDATE_NOT_IN_NHOOD = 0x03,
        CANDIDATE_NOT_NOMINATED = 0x04,
        ALREADY_VOTED = 0x05,
        ALREADY_VOTED_SAME_IP = 0x06,
        BAD_STATE = 0x07,

        //rate
        NOT_YOUR_MAYOR = 0x08,
        INVALID_RATING = 0x09,
        CANT_RATE_AVATAR = 0x0A,

        //accept or decline a nomination
        NOBODY_NOMINATED_YOU_IDIOT = 0x0B,
        ALREADY_RUNNING = 0x0C,
        BAD_COMMENT = 0x0D,

        //moderator actions
        NOT_MODERATOR = 0x0E,
        INVALID_AVATAR = 0x0F,
        INVALID_NHOOD = 0x10,
        
        //shared
        NHOOD_GAMEPLAY_BAN = 0x11,
        CANDIDATE_MOVED_RECENTLY = 0x12,
        YOU_MOVED_RECENTLY = 0x13,
        CANDIDATE_NHOOD_GAMEPLAY_BAN = 0x14,
        MISSING_ENTITY = 0x15, //missing someone
        
        UNKNOWN_ERROR = 0xFF
    };
}
