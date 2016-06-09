using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
