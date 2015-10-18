using FSO.Client.Regulators;
using FSO.Client.UI.Framework;
using FSO.Client.UI.Screens;
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

        public CoreGameScreenController(CoreGameScreen view, Network.Network network)
        {
            this.Screen = view;
            this.Network = network;
            this.Chat = new MessagingController(this);

            var shard = Network.MyShard;
            view.Initialize(shard.Name, int.Parse(shard.Map));
        }

        public void AddWindow(UIContainer window)
        {
            Screen.WindowContainer.Add(window);
        }

        public void OpenMessage(uint avatarId){
            Chat.OpenChat(avatarId);
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
