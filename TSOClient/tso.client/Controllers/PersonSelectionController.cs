using FSO.Client.Regulators;
using FSO.Client.UI.Screens;
using FSO.Common.Domain.Shards;
using FSO.Server.Protocol.CitySelector;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FSO.Client.Controllers
{
    public class PersonSelectionController
    {
        private PersonSelection View;
        private LoginRegulator Regulator;
        private IShardsDomain Shards;

        public PersonSelectionController(PersonSelection view, LoginRegulator regulator, IShardsDomain shards)
        {
            this.Shards = shards;
            this.View = view;
            this.Regulator = regulator;
        }

        public void ConnectToAvatar(AvatarData avatar)
        {
            GameFacade.Controller.ConnectToCity(avatar.ShardName, avatar.ID);
        }

        public void CreateAvatar()
        {
            View.ShowCitySelector(Shards.All, (ShardStatusItem selectedShard) =>
            {
                GameFacade.Controller.ConnectToCAS(selectedShard.Name);
            });
        }
    }
}
