using FSO.SimAntics.NetPlay.EODs.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FSO.SimAntics.NetPlay.EODs.Handlers
{
    public class VMEODDanceFloorPlugin : VMEODHandler
    {
        public VMEODClient ControllerClient;
        public VMEODDanceFloorPlugin(VMEODServer server) : base(server)
        {
            PlaintextHandlers["close"] = P_Close;
            PlaintextHandlers["press_button"] = P_DanceButton;
        }

        public void P_Close(string evt, string text, VMEODClient client)
        {
            Server.Disconnect(client);
        }

        public void P_DanceButton(string evt, string text, VMEODClient client)
        {
            byte num = 0;
            if (!byte.TryParse(text, out num)) return;
            if (ControllerClient != null) ControllerClient.SendOBJEvent(new VMEODEvent(num, client.Avatar.ObjectID));
        }

        public override void OnConnection(VMEODClient client)
        {
            if (client.Avatar != null)
            {
                client.Send("dance_show", "");
            }
            else
            {
                //we're the dance floor controller!
                ControllerClient = client;
            }
        }
    }
}
