using FSO.Client.UI.Panels;
using FSO.Client.UI.Panels.Neighborhoods;
using FSO.Common.DataService;
using FSO.Common.DataService.Model;
using FSO.Common.Utils;
using FSO.Server.DataService.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FSO.Client.Controllers.Panels
{
    public interface IUIAbstractRating
    {
        Binding<MayorRating> CurrentRating { get; set; }
        uint HalfStars { get; set; }
    }

    public class RatingSummaryController : IDisposable
    {
        private Network.Network Network;
        private IClientDataService DataService;
        private IUIAbstractRating View;
        private BookmarkType CurrentType = BookmarkType.AVATAR;

        public RatingSummaryController(IUIAbstractRating view, IClientDataService dataService, Network.Network network)
        {
            this.Network = network;
            this.DataService = dataService;
            this.View = view;
        }

        public void SetRating(uint ratingID)
        {
            if (ratingID == 0)
            {
                View.HalfStars = uint.MaxValue;
            }
            else
            {
                DataService.Request(MaskedStruct.Rating_User, ratingID).ContinueWith(x =>
                {
                    View.CurrentRating.Value = (x.Result as MayorRating);
                });
            }
        }

        public void Dispose()
        {
            View.CurrentRating.Dispose();
        }
    }
}
