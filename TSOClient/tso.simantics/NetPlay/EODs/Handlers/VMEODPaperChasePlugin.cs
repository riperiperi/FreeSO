using FSO.SimAntics.NetPlay.EODs.Model;
using FSO.SimAntics.NetPlay.EODs.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FSO.SimAntics.NetPlay.EODs.Handlers
{
    public class VMEODPaperChasePlugin : VMEODHandler
    {
        private VMEODClient Controller;
        private EODLobby Lobby;
        
        public VMEODPaperChasePlugin(VMEODServer server) : base(server)
        {
            Lobby = new EODLobby(server, 3)
                    .BroadcastPlayersOnChange("paperchase_players")
                    .OnJoinSend("paperchase_show")
                    .OnFailedToJoinDisconnect();

            PlaintextHandlers["close"] = Lobby.Close;
            
            //SimanticsHandlers[(short)VMEODPizzaObjEvent.RespondPhone] = S_RespondPhone;
            //SimanticsHandlers[(short)VMEODPizzaObjEvent.AllContributed] = S_AllContributed;
            //SimanticsHandlers[(short)VMEODPizzaObjEvent.RespondBake] = S_RespondBake;
        }

        public override void OnConnection(VMEODClient client)
        {
            var param = client.Invoker.Thread.TempRegisters;
            if (client.Avatar != null)
            {
                var slot = param[0]; //1 Body, 2 Mechanical, 3 Logic
                if(Lobby.Join(client, (short)(slot - 1)))
                {
                    client.SendOBJEvent(new VMEODEvent((short)VMEODPizzaObjEvent.Contribute));
                }
            }
            else
            {
                Controller = client;
            }
        }

        public override void Tick()
        {
        }

        public void EnterState(VMEODPizzaState state)
        {

        }
    }


    public enum VMEODPaperChaseSlots : byte
    {
        BODY = 1,
        MECHANICAL = 2,
        LOGIC = 3
    }
}
