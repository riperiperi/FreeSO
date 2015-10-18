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
        private uint AvatarId;

        public PersonPageController(UIPersonPage view, IClientDataService dataService)
        {
            this.View = view;
            this.DataService = dataService;
        }


        public void Show(uint avatarId){
            AvatarId = avatarId;

            DataService.Get<Avatar>(avatarId).ContinueWith(x =>
            {
                View.CurrentAvatar.Value = x.Result;
            });

            View.CurrentTab = UIPersonPageTab.Description;
            View.SetOpen(false);
            DataService.Request(MaskedStruct.SimPage_Main, avatarId);
            View.Visible = true;
        }

        public void RefreshData(UIPersonPageTab tab){
            switch (tab)
            {
                case UIPersonPageTab.Description:
                    DataService.Request(MaskedStruct.SimPage_DescriptionPanel, AvatarId);
                    break;
            }
        }
    }
}
