using FSO.Client.Model;
using FSO.Client.Regulators;
using FSO.Common;
using FSO.Common.DataService;
using FSO.Common.Domain.Shards;
using FSO.Server.Clients;
using FSO.Server.Protocol.CitySelector;
using System.Linq;

namespace FSO.Client.Network
{
    public class Network
    {
        private CityConnectionRegulator CityRegulator;
        private LotConnectionRegulator LotRegulator;
        private LoginRegulator LoginRegulator;
        private IShardsDomain Shards;

        public CityConnectionMode Mode => CityRegulator.Mode;
        public ArchiveConfigFlags ArchiveConfig => CityRegulator.ArchiveConfig;
        public bool SpectatorMode => CityRegulator.SpectatorMode;
        public ConnectArchiveRequest ArchiveHost => CityRegulator.ArchiveSettings;

        public Network(LoginRegulator loginReg, CityConnectionRegulator cityReg, LotConnectionRegulator lotReg, IShardsDomain shards)
        {
            this.Shards = shards;
            this.CityRegulator = cityReg;
            this.LoginRegulator = loginReg;
            this.LotRegulator = lotReg;
        }

        public AriesClient CityClient
        {
            get
            {
                return CityRegulator.Client;
            }
        }

        public AriesClient LotClient
        {
            get
            {
                return LotRegulator.Client;   
            }
        }

        public UserReference MyCharacterRef
        {
            get
            {
                return UserReference.Of(Common.Enum.UserReferenceType.AVATAR, MyCharacter);
            }
        }

        public uint MyCharacter
        {
            get
            {
                return uint.Parse(CityRegulator.CurrentShard.AvatarID);
            }
        }

        public ShardStatusItem MyShard
        {
            get
            {
                return Shards.All.First(x => x.Name == CityRegulator.CurrentShard.ShardName);
            }
        }

        public uint ModerationLevel
        {
            get
            {
                return CityRegulator.ModerationLevel;
            }
        }

        public string TryGetUsername(uint id)
        {
            if (Mode == CityConnectionMode.ARCHIVE && !ArchiveConfig.HasFlag(ArchiveConfigFlags.HideNames) && CityRegulator.UserList != null)
            {
                return CityRegulator.UserList.Clients.FirstOrDefault(x => x.AvatarId == id).DisplayName;
            }

            return null;
        }
    }
}
