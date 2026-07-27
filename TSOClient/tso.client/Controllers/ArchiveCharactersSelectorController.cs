using FSO.Client.Regulators;
using FSO.Client.UI.Archive;
using FSO.Common.Utils;
using FSO.Server.Protocol.Electron.Packets;
using System;

namespace FSO.Client.Controllers
{
    public interface IArchiveCharacterSelector
    {
        void SetData(ArchiveAvatarsResponse data);
    }

    internal class ArchiveCharactersSelectorController : IDisposable
    {
        private IArchiveCharacterSelector View;
        private GenericActionRegulator<ArchiveAvatarsRequest, ArchiveAvatarsResponse> ConnectionReg;
        public CityResourceController CityResource;

        public ArchiveCharactersSelectorController(IArchiveCharacterSelector view, Network.Network network, GenericActionRegulator<ArchiveAvatarsRequest, ArchiveAvatarsResponse> regulator)
        {
            View = view;
            CityResource = new CityResourceController(network);
            regulator.OnError += Regulator_OnError;
            regulator.OnTransition += Regulator_OnTransition;
            regulator.OnMessage += Regulator_OnMessage;

            ConnectionReg = regulator;
        }

        private void Regulator_OnMessage(object data)
        {
            if (data is VerificationNotification verification)
            {
                if (verification.IsVerified)
                {
                    Refresh();
                }
                else
                {
                    FSOFacade.Controller.FatalError(GameFacade.Strings.GetString("f128", "92"), GameFacade.Strings.GetString("f128", "93"), 1);
                }
            }
        }

        public void Dispose()
        {
            ConnectionReg.OnError -= Regulator_OnError;
            ConnectionReg.OnTransition -= Regulator_OnTransition;

            CityResource.Dispose();
        }

        public void Refresh()
        {
            ConnectionReg.MakeRequest(new ArchiveAvatarsRequest());
        }

        private void Regulator_OnError(object data)
        {
            // TODO: tell the view so it can try again? or handle weird errors like missing auth
        }

        private void Regulator_OnTransition(string state, object data)
        {
            var progress = 0;

            GameThread.InUpdate(() =>
            {
                switch (state)
                {
                    case "ActionSuccess":
                        var packet = (ArchiveAvatarsResponse)data;
                        View.SetData(packet);
                        break;
                }
            });
        }
    }
}
