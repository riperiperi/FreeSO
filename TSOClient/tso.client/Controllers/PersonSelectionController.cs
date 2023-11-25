using FSO.Client.Regulators;
using FSO.Client.UI.Screens;
using FSO.Common.Domain.Shards;
using FSO.Common.Utils.Cache;
using FSO.Server.Protocol.CitySelector;

namespace FSO.Client.Controllers
{
    public class PersonSelectionController
    {
        private PersonSelection View;
        private LoginRegulator Regulator;
        private IShardsDomain Shards;
        private ICache Cache;

        public PersonSelectionController(PersonSelection view, LoginRegulator regulator, IShardsDomain shards, ICache cache)
        {
            this.Shards = shards;
            this.View = view;
            this.Regulator = regulator;
            this.Cache = cache;
        }

        public void ConnectToAvatar(AvatarData avatar, bool autoJoinLot)
        {
            FSOFacade.Controller.ConnectToCity(avatar.ShardName, avatar.ID, autoJoinLot ? avatar.LotLocation : null);
        }

        public void CreateAvatar()
        {
            View.ShowCitySelector(Shards.All, (ShardStatusItem selectedShard) =>
            {
                FSOFacade.Controller.ConnectToCAS(selectedShard.Name);
            });
        }
    }
}
