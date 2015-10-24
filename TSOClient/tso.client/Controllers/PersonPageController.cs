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
using System.Timers;

namespace FSO.Client.Controllers
{
    public class PersonPageController
    {
        private UIPersonPage View;
        private IClientDataService DataService;
        private uint AvatarId;
        private Timer ProgressTimer;

        public PersonPageController(UIPersonPage view, IClientDataService dataService)
        {
            this.View = view;
            this.DataService = dataService;


            ProgressTimer = new Timer();
            ProgressTimer.Interval = 5000;
            ProgressTimer.Elapsed += (x, y) =>
            {
                Refresh();
            };
            ProgressTimer.Start();
        }

        private void Refresh()
        {
            if (AvatarId == 0 && !View.Visible) { return; }
            DataService.Request(MaskedStruct.SimPage_DescriptionPanel, AvatarId);
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
