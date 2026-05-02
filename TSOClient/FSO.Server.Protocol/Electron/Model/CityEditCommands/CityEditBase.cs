using FSO.Common.Serialization;
using Mina.Core.Buffer;

namespace FSO.Server.Protocol.Electron.Model.CityEditCommands
{
    public abstract class CityEditBase
    {
        public const int MaxReservedLocations = 512 * 256;

        public uint AvatarId;
        public int UserModId;
        public HashSet<uint> ReservedLocations;
        public bool IsTemp;

        public virtual void Deserialize(IoBuffer input, ISerializationContext context)
        {
            AvatarId = input.GetUInt32();
            UserModId = input.GetInt32();
            var reservedCount = input.GetInt32();

            if (reservedCount > MaxReservedLocations)
            {
                throw new Exception("Invalid number of reserved locations for city edit");
            }

            ReservedLocations = [];

            for (int i = 0; i < reservedCount; i++)
            {
                ReservedLocations.Add(input.GetUInt32());
            }

            IsTemp = input.GetBool();
        }

        public virtual void Serialize(IoBuffer output, ISerializationContext context)
        {
            output.PutUInt32(AvatarId);
            output.PutInt32(UserModId);

            if (ReservedLocations != null)
            {
                output.PutInt32(ReservedLocations.Count);

                foreach (var location in ReservedLocations)
                {
                    output.PutUInt32(location);
                }
            }
            else
            {
                output.PutInt32(0);
            }

            output.PutBool(IsTemp);
        }
    }
}
