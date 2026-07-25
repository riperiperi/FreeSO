using FSO.Client.Regulators;
using FSO.Client.UI.Controls;
using FSO.Client.UI.Framework;
using FSO.Client.UI.Panels;
using FSO.Client.UI.Screens;
using FSO.Common;
using FSO.Common.Utils;
using FSO.Server.Protocol.CitySelector;
using System;
using System.Diagnostics;

namespace FSO.Client.Controllers
{
    public class LoginController : IDisposable
    {
        private LoginScreen View;
        private LoginRegulator Regulator;
        private UIAlert _UpdaterAlert;

        public LoginController(LoginScreen view, LoginRegulator reg)
        {
            View = view;
            Regulator = reg;
            Regulator.OnTransition += Regulator_OnTransition;
        }

        private void Regulator_OnTransition(string transition, object data)
        {
            switch (transition)
            {
                case "UpdateRequired":
                    var info = (UserAuthorized)data;
                    View.LoginDialog.Visible = false;
                    View.LoginProgress.Visible = false;
                    var controller = new UpdateController(ContinueFromUpdate);
                    controller.DoUpdate(info.GetVersion());
                    break;
            }
        }

        private void ContinueFromUpdate(bool toSAS)
        {
            if (toSAS)
            {
                Regulator.AsyncTransition("AvatarData");
                View.LoginDialog.Visible = true;
                View.LoginProgress.Visible = true;
            }
            else
            {
                View.LoginDialog.Visible = true;
                View.LoginProgress.Visible = true;
                Regulator.AsyncReset();
            }
        }

        public void Dispose()
        {
            View.Dispose();
            Regulator.OnTransition -= Regulator_OnTransition;
        }
    }
}
