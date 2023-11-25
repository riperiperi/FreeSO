using FSO.Client.UI.Panels.Neighborhoods;
using FSO.Common.DataService;
using FSO.Common.DataService.Model;
using FSO.Server.DataService.Model;
using System;

namespace FSO.Client.Controllers.Panels
{
    public class RatingListController : IDisposable
    {
        private Network.Network Network;
        private IClientDataService DataService;
        private UIRatingList View;

        public RatingListController(UIRatingList view, IClientDataService dataService, Network.Network network)
        {
            this.Network = network;
            this.DataService = dataService;
            this.View = view;
        }

        public void SetAvatar(uint avatarID)
        {
            DataService.Request(MaskedStruct.MayorInfo_Avatar, avatarID).ContinueWith(x =>
            {
                View.CurrentAvatar.Value = (x.Result as Avatar);
            });
        }

        public void Dispose()
        {
            View.CurrentAvatar.Dispose();
        }
    }
}
