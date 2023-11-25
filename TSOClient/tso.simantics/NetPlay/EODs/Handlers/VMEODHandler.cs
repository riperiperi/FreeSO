using System.Collections.Generic;

namespace FSO.SimAntics.NetPlay.EODs.Handlers
{
    public abstract class VMEODHandler
    {
        public Dictionary<string, EODPlaintextEventHandler> PlaintextHandlers;
        public Dictionary<string, EODBinaryEventHandler> BinaryHandlers;
        public Dictionary<short, EODSimanticsEventHandler> SimanticsHandlers;

        public VMEODServer Server;

        public VMEODHandler(VMEODServer server)
        {
            PlaintextHandlers = new Dictionary<string, EODPlaintextEventHandler>();
            BinaryHandlers = new Dictionary<string, EODBinaryEventHandler>();
            SimanticsHandlers = new Dictionary<short, EODSimanticsEventHandler>();
            Server = server;
        }

        public virtual void OnConnection(VMEODClient client)
        {

        }

        public virtual void OnDisconnection(VMEODClient client)
        {

        }

        public virtual void SelfResync()
        {
            //in some cases, the server might want to save and instantly reload its lot state to fix
            //lingering issues with "unsavable" state causing desyncs.

            //in this case, we need to make sure all eodclients are pointing to the new object.
            //when this is called, they should be pointing to the old objects we already overwrote
            //but their object ids should be the same in the new state, so we should just be able
            //to get them again from the VM.

            if (Server.Object != null) Server.Object = Server.vm.GetObjectById(Server.Object.ObjectID);
            foreach (var client in Server.Clients)
            {
                if (client.Invoker != null) client.Invoker = Server.vm.GetObjectById(client.Invoker.ObjectID);
                if (client.Avatar != null) client.Avatar = (VMAvatar)Server.vm.GetObjectById(client.Avatar.ObjectID);
            }
        }

        public virtual void Tick()
        {

        }
    }


    public delegate void EODPlaintextEventHandler(string evt, string body, VMEODClient client);
    public delegate void EODBinaryEventHandler(string evt, byte[] body, VMEODClient client);
    public delegate void EODSimanticsEventHandler(short evt, VMEODClient client);

    public delegate void EODDirectPlaintextEventHandler(string evt, string body);
    public delegate void EODDirectBinaryEventHandler(string evt, byte[] body);
}
