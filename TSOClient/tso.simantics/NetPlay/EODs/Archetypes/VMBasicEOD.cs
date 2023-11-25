using FSO.SimAntics.NetPlay.EODs.Handlers;
using FSO.SimAntics.NetPlay.EODs.Utils;

namespace FSO.SimAntics.NetPlay.EODs.Archetypes
{
    public class VMBasicEOD <T> : VMEODHandler
    {
        protected EODLobby<T> Lobby;
        protected string EODName;

        public VMBasicEOD(VMEODServer server, string name) : base(server)
        {
            EODName = name;

            Lobby = new EODLobby<T>(server, 1)
                    .OnJoinSend(EODName + "_show")
                    .OnFailedToJoinDisconnect();

            PlaintextHandlers["close"] = Lobby.Close;
        }

        protected virtual void OnConnected(VMEODClient client)
        {
        }

        public override void OnConnection(VMEODClient client)
        {
            var param = client.Invoker.Thread.TempRegisters;
            if (client.Avatar != null)
            {
                var slot = param[0];
                if (Lobby.Join(client, 0))
                {
                    OnConnected(client);
                }
            }
        }

        public override void OnDisconnection(VMEODClient client)
        {
            Lobby.Leave(client);
        }
    }
}
