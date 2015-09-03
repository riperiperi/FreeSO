using FSO.Client.Regulators;
using FSO.Client.UI.Screens;
using FSO.Common.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FSO.Client.Controllers
{
    public class DisconnectController : IDisposable
    {
        private TransitionScreen View;
        private CityConnectionRegulator CityConnectionRegulator;

        private Callback onDisconnected;

        public DisconnectController(TransitionScreen view, CityConnectionRegulator cityRegulator)
        {
            View = view;
            View.ShowProgress = false;

            CityConnectionRegulator = cityRegulator;
            CityConnectionRegulator.OnTransition += CityConnectionRegulator_OnTransition;
        }

        private void CityConnectionRegulator_OnTransition(string state, object data)
        {
            switch (state)
            {
                case "Disconnect":
                    break;
                case "Disconnected":
                    onDisconnected();
                    break;
            }
        }

        public void Disconnect(Callback onDisconnected)
        {
            this.onDisconnected = onDisconnected;
            CityConnectionRegulator.Disconnect();
        }

        public void Dispose()
        {
            CityConnectionRegulator.OnTransition -= CityConnectionRegulator_OnTransition;
        }
    }
}
