using FSO.Common.Serialization;
using Mina.Core.Buffer;
using FSO.Server.Protocol.Electron.Model;

namespace FSO.Server.Protocol.Electron.Packets
{
    public class FindLotResponse : AbstractElectronPacket
    {
        public FindLotResponseStatus Status;
        public uint LotId;
        public string LotServerTicket;
        public string Address;
        public string User;
        
        public override void Deserialize(IoBuffer input, ISerializationContext context)
        {
            Status = input.GetEnum<FindLotResponseStatus>();
            LotId = input.GetUInt32();
            LotServerTicket = input.GetPascalVLCString();
            Address = input.GetPascalVLCString();
            User = input.GetPascalVLCString();
        }

        public override ElectronPacketType GetPacketType()
        {
            return ElectronPacketType.FindLotResponse;
        }

        public override void Serialize(IoBuffer output, ISerializationContext context)
        {
            output.PutEnum(Status);
            output.PutUInt32(LotId);
            output.PutPascalVLCString(LotServerTicket);
            output.PutPascalVLCString(Address);
            output.PutPascalVLCString(User);
        }
    }
}
