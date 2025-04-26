using FSO.Common.Serialization;
using FSO.Server.Protocol.CitySelector;
using FSO.Server.Protocol.Electron.Model;
using Mina.Core.Buffer;

namespace FSO.Server.Protocol.Electron.Packets
{
    public struct ArchiveAvatar
    {
        public uint UserId;
        public uint AvatarId;
        public string Name;
        public AvatarAppearanceType Type;
        public ulong Head;
        public ulong Body;

        public static ArchiveAvatar Deserialize(IoBuffer input)
        {
            return new ArchiveAvatar()
            {
                UserId = input.GetUInt32(),
                AvatarId = input.GetUInt32(),
                Name = input.GetPascalVLCString(),
                Head = input.GetUInt64(),
                Body = input.GetUInt64(),
            };
        }

        public void Serialize(IoBuffer output)
        {
            output.PutUInt32(UserId);
            output.PutUInt32(AvatarId);
            output.PutPascalVLCString(Name);
            output.PutUInt64(Head);
            output.PutUInt64(Body);
        }
    }

    public class ArchiveAvatarsResponse : AbstractElectronPacket, IActionResponse
    {
        public bool Success => true;

        public object OCode => 0;

        public ArchiveAvatar[] UserAvatars;
        public ArchiveAvatar[] SharedAvatars;

        public override void Deserialize(IoBuffer input, ISerializationContext context)
        {
            int userCount = input.GetInt32();

            if (userCount > 8192)
            {
                throw new System.Exception($"Too many user avatars: {userCount}");
            }

            UserAvatars = new ArchiveAvatar[userCount];
            for (int i = 0; i < userCount; i++)
            {
                UserAvatars[i] = ArchiveAvatar.Deserialize(input);
            }

            // TODO: compression?

            int sharedCount = input.GetInt32();

            if (sharedCount > 500000)
            {
                throw new System.Exception($"Too many shared avatars: {sharedCount}");
            }

            SharedAvatars = new ArchiveAvatar[sharedCount];
            for (int i = 0; i < sharedCount; i++)
            {
                SharedAvatars[i] = ArchiveAvatar.Deserialize(input);
            }
        }

        public override ElectronPacketType GetPacketType()
        {
            return ElectronPacketType.ArchiveAvatarsResponse;
        }

        public override void Serialize(IoBuffer output, ISerializationContext context)
        {
            output.PutInt32(UserAvatars.Length);

            foreach (var user in UserAvatars)
            {
                user.Serialize(output);
            }

            output.PutInt32(SharedAvatars.Length);

            foreach (var shared in SharedAvatars)
            {
                shared.Serialize(output);
            }
        }
    }
}
