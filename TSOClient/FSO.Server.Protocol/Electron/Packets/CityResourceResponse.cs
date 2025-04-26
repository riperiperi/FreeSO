using FSO.Common.Serialization;
using Mina.Core.Buffer;

namespace FSO.Server.Protocol.Electron.Packets
{
    public class CityResourceResponse : AbstractElectronPacket
    {
        public CityResourceRequestType Type;
        public uint ResourceID;
        public uint RequestID; // Needed for the client to know exactly what response is for what request.
        public byte[] Data;

        public override void Deserialize(IoBuffer input, ISerializationContext context)
        {
            Type = input.GetEnum<CityResourceRequestType>();
            ResourceID = input.GetUInt32();
            RequestID = input.GetUInt32();
            int length = input.GetInt32();
            Data = input.GetSlice(length).GetBytes();
        }

        public override ElectronPacketType GetPacketType()
        {
            return ElectronPacketType.CityResourceResponse;
        }

        public override void Serialize(IoBuffer output, ISerializationContext context)
        {
            output.PutEnum(Type);
            output.PutUInt32(ResourceID);
            output.PutUInt32(RequestID);
            output.PutInt32(Data.Length);
            output.Put(Data);
        }
    }
}
