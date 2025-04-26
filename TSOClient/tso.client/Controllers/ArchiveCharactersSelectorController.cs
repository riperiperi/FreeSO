using FSO.Client.Regulators;
using FSO.Client.UI.Archive;
using FSO.Common.Utils;
using FSO.Server.Protocol.Electron.Packets;
using System;

namespace FSO.Client.Controllers
{
    internal class ArchiveCharactersSelectorController : IDisposable
    {
        private UIArchiveCharacterSelector View;
        private GenericActionRegulator<ArchiveAvatarsRequest, ArchiveAvatarsResponse> ConnectionReg;

        public ArchiveCharactersSelectorController(UIArchiveCharacterSelector view, GenericActionRegulator<ArchiveAvatarsRequest, ArchiveAvatarsResponse> regulator)
        {
            View = view;
            regulator.OnError += Regulator_OnError;
            regulator.OnTransition += Regulator_OnTransition;

            ConnectionReg = regulator;
        }

        public void Dispose()
        {
            ConnectionReg.OnError -= Regulator_OnError;
            ConnectionReg.OnTransition -= Regulator_OnTransition;
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
