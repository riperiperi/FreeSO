using FSO.Client.Regulators;
using FSO.Client.UI.Screens;
using FSO.Common.Utils;
using FSO.Server.Protocol.CitySelector;
using System;

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
                case "SelectCity":
                    //4  ^Starting engines^                 # City is Selected...
                    View.SetProgress((1.0f / 14.0f) * 100, 4);
                    break;
                case "ConnectToCitySelector":
                    //5  ^Talking with the mothership^      # Connecting to City Selector...
                    View.SetProgress((2.0f / 14.0f) * 100, 5);
                    break;
                case "CitySelected":
                    //6  ^1^ # Processing XML response...
                    View.SetProgress((3.0f / 14.0f) * 100, 6);
                    break;
                case "OpenSocket":
                    //7	  ^Sterilizing TCP/IP sockets^       # Connecting to City...
                    View.SetProgress((4.0f / 14.0f) * 100, 7);
                    break;
                case "PartiallyConnected":
                    //7	  ^Sterilizing TCP/IP sockets^       # Connecting to City...
                    View.SetProgress((5.0f / 14.0f) * 100, 8);
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
