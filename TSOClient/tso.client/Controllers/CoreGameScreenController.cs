using FSO.Client.Regulators;
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
        private Network.Network Network;

        public CoreGameScreenController(CoreGameScreen screen, Network.Network network)
        {
            this.Screen = screen;
            this.Network = network;

            var shard = Network.MyShard;
            screen.Initialize(shard.Name, int.Parse(shard.Map));
        }

        public void ShowPersonPage(uint avatarId){

        }

        public void ShowMyPersonPage(){
            ShowPersonPage(Network.MyCharacter);
        }



        public void Dispose()
        {
        }
    }
}
