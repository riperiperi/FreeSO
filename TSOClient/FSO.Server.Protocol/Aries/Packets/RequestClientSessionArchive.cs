using Mina.Core.Buffer;
using FSO.Common.Serialization;

namespace FSO.Server.Protocol.Aries.Packets
{
    public class RequestClientSessionArchive : IAriesPacket
    {
        public string ServerKey;
        public uint ShardId;
        public string ShardName;
        public string ShardMap;

        public void Deserialize(IoBuffer input, ISerializationContext context)
        {
            ServerKey = input.GetPascalVLCString();
            ShardId = input.GetUInt32();
            ShardName = input.GetPascalVLCString();
            ShardMap = input.GetPascalVLCString();
        }

        public AriesPacketType GetPacketType()
        {
            return AriesPacketType.RequestClientSessionArchive;
        }

        public void Serialize(IoBuffer output, ISerializationContext context)
        {
            output.PutPascalVLCString(ServerKey);
            output.PutUInt32(ShardId);
            output.PutPascalVLCString(ShardName);
            output.PutPascalVLCString(ShardMap);
        }
    }
}
