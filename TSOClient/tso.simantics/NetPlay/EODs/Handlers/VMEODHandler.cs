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

        public VMEODServer Server;

        public VMEODHandler(VMEODServer server)
        {
            PlaintextHandlers = new Dictionary<string, EODPlaintextEventHandler>();
            BinaryHandlers = new Dictionary<string, EODBinaryEventHandler>();
            Server = server;
        }

    }

    public delegate void EODPlaintextEventHandler(string evt, string body);
    public delegate void EODBinaryEventHandler(string evt, byte[] body);
}
