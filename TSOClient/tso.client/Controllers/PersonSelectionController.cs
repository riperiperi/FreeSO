using FSO.Client.Regulators;
using FSO.Client.UI.Screens;
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

        public PersonSelectionController(PersonSelection view, LoginRegulator regulator)
        {
            this.View = view;
            this.Regulator = regulator;
        }

        public void ConnectToAvatar(AvatarData avatar)
        {
            GameFacade.Controller.ConnectToCity(avatar.ShardName, avatar.ID);
        }

        public void CreateAvatar()
        {
            View.ShowCitySelector(Regulator.Shards, (ShardStatusItem selectedShard) =>
            {
                GameFacade.Controller.ConnectToCAS(selectedShard.Name);
            });
        }
    }
}
