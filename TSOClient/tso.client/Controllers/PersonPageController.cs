using FSO.Client.UI.Framework;
using FSO.Client.UI.Panels;
using FSO.Common.DataService;
using FSO.Common.DataService.Model;
using FSO.Server.DataService.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FSO.Client.Controllers
{
    public class PersonPageController
    {
        private UIPersonPage View;
        private IClientDataService DataService;

        public PersonPageController(UIPersonPage view, IClientDataService dataService)
        {
            this.View = view;
            this.DataService = dataService;
        }


        public void Show(uint avatarId){
            DataService.Get<Avatar>(avatarId).ContinueWith(x =>
            {
                View.CurrentAvatar.Value = x.Result;
            });

            View.CurrentTab = UIPersonPageTab.Description;
            View.SetOpen(false);
            DataService.Request(MaskedStruct.SimPage_Main, avatarId);
            View.Visible = true;
        }
    }
}
