using FSO.Client.Model;
using FSO.Client.Regulators;
using FSO.Common.Domain.Shards;
using FSO.Server.Clients;
using FSO.Server.Protocol.CitySelector;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace FSO.Client.Network
{
    public class Network
    {
        private CityConnectionRegulator CityRegulator;
        private LotConnectionRegulator LotRegulator;
        private LoginRegulator LoginRegulator;
        private IShardsDomain Shards;

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
    }
}
