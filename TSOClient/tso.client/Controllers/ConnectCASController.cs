using FSO.Client.Regulators;
using FSO.Client.UI.Screens;
using FSO.Common.Utils;
using FSO.Server.Protocol.CitySelector;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FSO.Client.Controllers
{
    public class ConnectCASController : IDisposable
    {
        private TransitionScreen View;
        private CityConnectionRegulator CityConnectionRegulator;
        private Callback onConnect;
        private Callback onError;

        public ConnectCASController(TransitionScreen view, 
                                     CityConnectionRegulator cityConnectionRegulator)
        {
            this.View = view;
            this.CityConnectionRegulator = cityConnectionRegulator;
            this.CityConnectionRegulator.OnTransition += CityConnectionRegulator_OnTransition;
            this.CityConnectionRegulator.OnError += CityConnectionRegulator_OnError;

            View.ShowProgress = true;
            View.SetProgress(0, 4);
        }
        
        public void Connect(string shardName, Callback onConnect, Callback onError)
        {
            this.onConnect = onConnect;
            this.onError = onError;

            CityConnectionRegulator.Connect(CityConnectionMode.CAS, new ShardSelectorServletRequest {
                AvatarID = "0", //0 is used for cas
                ShardName = shardName
            });
        }

        private void CityConnectionRegulator_OnError(object data)
        {
            onError();
        }

        private void CityConnectionRegulator_OnTransition(string state, object data)
        {
            switch (state)
            {
                case "Disconnected":
                    break;
                case "ShardSelect":
                    View.SetProgress(25, 5);
                    break;
                case "OpenSocket":
                    View.SetProgress(50, 7);
                    break;
                case "Connected":
                    View.SetProgress(100, 8);
                    onConnect();
                    break;
            }
        }

        public void Dispose(){
            CityConnectionRegulator.OnTransition -= CityConnectionRegulator_OnTransition;
            CityConnectionRegulator.OnError -= CityConnectionRegulator_OnError;
        }
    }
}
