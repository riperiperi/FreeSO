using FSO.Common.Serialization;
using FSO.Server.Protocol.Electron.Model;
using Mina.Core.Buffer;

namespace FSO.Server.Protocol.Electron.Packets
{
    public class CityResourceRequest : AbstractElectronPacket, IActionRequest
    {
        public CityResourceRequestType Type;
        public uint ResourceID;
        public uint RequestID; // Needed for the client to know exactly what response is for what request.

        public object OType => Type;
        public bool NeedsValidation => false; //the CAN POST items are one off requests, rather than a state machine.

        public override void Deserialize(IoBuffer input, ISerializationContext context)
        {
            Type = input.GetEnum<CityResourceRequestType>();
            ResourceID = input.GetUInt32();
            RequestID = input.GetUInt32();
        }

        public override ElectronPacketType GetPacketType()
        {
            return ElectronPacketType.CityResourceRequest;
        }

        public override void Serialize(IoBuffer output, ISerializationContext context)
        {
            output.PutEnum(Type);
            output.PutUInt32(ResourceID);
            output.PutUInt32(RequestID);
        }
    }

    public enum CityResourceRequestType : byte
    {
        LOT_THUMBNAIL = 0,
        LOT_FACADE = 1
    }
}
