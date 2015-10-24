using FSO.Client.Model;
using FSO.Client.Regulators;
using FSO.Client.UI.Framework;
using FSO.Client.UI.Screens;
using FSO.Common.DataService;
using FSO.Common.DataService.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FSO.Client.Controllers
{
    public class CoreGameScreenController : IDisposable
    {
        private CoreGameScreen Screen;
        private MessagingController Chat;
        private Network.Network Network;
        private IClientDataService DataService;

        public CoreGameScreenController(CoreGameScreen view, Network.Network network, IClientDataService dataService)
        {
            this.Screen = view;
            this.Network = network;
            this.DataService = dataService;
            this.Chat = new MessagingController(this, view.MessageTray);

            var shard = Network.MyShard;
            view.Initialize(shard.Name, int.Parse(shard.Map));
        }

        public void AddWindow(UIContainer window)
        {
            Screen.WindowContainer.Add(window);
        }

        public void CallAvatar(uint avatarId){
            DataService.Get<Avatar>(avatarId).ContinueWith(x =>
            {
                Chat.Call(UserReference.Wrap(x.Result));
            });
        }

        public void ShowPersonPage(UserReference user)
        {
            if(user.Type == UserReferenceType.AVATAR)
            {
                ShowPersonPage(user.Id);
            }
        }

        public void ShowPersonPage(uint avatarId){
            ((PersonPageController)Screen.PersonPage.Controller).Show(avatarId);
        }

        public void ShowMyPersonPage(){
            ShowPersonPage(Network.MyCharacter);
        }

        public void Dispose()
        {
        }
    }
}
