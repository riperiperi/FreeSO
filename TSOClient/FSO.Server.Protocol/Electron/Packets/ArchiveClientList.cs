using FSO.Common.Serialization;
using Mina.Core.Buffer;

namespace FSO.Server.Protocol.Electron.Packets
{
    public struct ArchiveClient
    {
        public uint UserId;
        public string DisplayName;
        public uint AvatarId;
        public uint ModerationLevel;

        public static ArchiveClient Deserialize(IoBuffer input)
        {
            return new ArchiveClient()
            {
                UserId = input.GetUInt32(),
                DisplayName = input.GetPascalVLCString(),
                AvatarId = input.GetUInt32(),
                ModerationLevel = input.GetUInt32(),
            };
        }

        public void Serialize(IoBuffer output)
        {
            output.PutUInt32(UserId);
            output.PutPascalVLCString(DisplayName);
            output.PutUInt32(AvatarId);
            output.PutUInt32(ModerationLevel);
        }
    }

    public struct ArchivePendingVerification
    {
        public uint UserId;
        public string DisplayName;

        public static ArchivePendingVerification Deserialize(IoBuffer input)
        {
            return new ArchivePendingVerification()
            {
                UserId = input.GetUInt32(),
                DisplayName = input.GetPascalVLCString(),
            };
        }

        public void Serialize(IoBuffer output)
        {
            output.PutUInt32(UserId);
            output.PutPascalVLCString(DisplayName);
        }
    }

    public class ArchiveClientList : AbstractElectronPacket
    {
        public ArchiveClient[] Clients;
        public ArchivePendingVerification[] Pending;

        public override void Deserialize(IoBuffer input, ISerializationContext context)
        {
            int clientCount = input.GetInt32();

            if (clientCount > 8192)
            {
                throw new System.Exception($"Too many clients: {clientCount}");
            }

            Clients = new ArchiveClient[clientCount];
            for (int i = 0; i < clientCount; i++)
            {
                Clients[i] = ArchiveClient.Deserialize(input);
            }

            int verificationCount = input.GetInt32();

            if (verificationCount > 8192)
            {
                throw new System.Exception($"Too many pending verifications: {verificationCount}");
            }

            Pending = new ArchivePendingVerification[clientCount];
            for (int i = 0; i < verificationCount; i++)
            {
                Pending[i] = ArchivePendingVerification.Deserialize(input);
            }
        }

        public override ElectronPacketType GetPacketType()
        {
            return ElectronPacketType.ArchiveClientList;
        }

        public override void Serialize(IoBuffer output, ISerializationContext context)
        {
            output.PutInt32(Clients.Length);
            
            foreach (var client in Clients)
            {
                client.Serialize(output);
            }

            output.PutInt32(Pending.Length);

            foreach (var pending in Pending)
            {
                pending.Serialize(output);
            }
        }
    }
}
