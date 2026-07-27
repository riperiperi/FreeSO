using Mina.Core.Buffer;
using FSO.Common.Serialization;
using FSO.Common;

namespace FSO.Server.Protocol.Aries.Packets
{
    public class RequestClientSessionArchive : IAriesPacket
    {
        public string Name;
        public int PlayerCount;
        public string VersionInfo;

        public string ServerKey;
        public string Nonce;
        public ArchiveConfigFlags ArchiveConfig;
        public uint ShardId;
        public string ShardName;
        public string ShardMap;

        public void Deserialize(IoBuffer input, ISerializationContext context)
        {
            Name = input.GetPascalVLCString();
            PlayerCount = input.GetInt32();
            VersionInfo = input.GetPascalVLCString();

            ServerKey = input.GetPascalVLCString();
            Nonce = input.GetPascalVLCString();
            ArchiveConfig = input.GetEnum<ArchiveConfigFlags>();
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
            output.PutPascalVLCString(Name);
            output.PutInt32(PlayerCount);
            output.PutPascalVLCString(VersionInfo);

            output.PutPascalVLCString(ServerKey);
            output.PutPascalVLCString(Nonce);
            output.PutEnum(ArchiveConfig);
            output.PutUInt32(ShardId);
            output.PutPascalVLCString(ShardName);
            output.PutPascalVLCString(ShardMap);
        }
    }
}
