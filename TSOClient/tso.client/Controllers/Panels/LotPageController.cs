using FSO.Client.UI.Panels;
using FSO.Common.DataService;
using FSO.Common.DataService.Model;
using FSO.Server.DataService.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FSO.Client.Controllers.Panels
{
    public class LotPageController
    {
        private UILotPage View;
        private IClientDataService DataService;

        public LotPageController(UILotPage view, IClientDataService dataService)
        {
            this.View = view;
            this.DataService = dataService;
        }

        public void Show(uint lotId)
        {
            DataService.Get<Lot>(lotId).ContinueWith(x =>
            {
                View.CurrentLot.Value = x.Result;
            });

            DataService.Request(MaskedStruct.PropertyPage_LotInfo, lotId);
            View.Visible = true;
        }
    }
}
