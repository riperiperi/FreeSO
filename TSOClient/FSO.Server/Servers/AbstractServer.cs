using FSO.Server.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FSO.Server.Servers
{
    public abstract class AbstractServer
    {
        public abstract void Start();
        public abstract void Shutdown();

        public abstract void AttachDebugger(IServerDebugger debugger);
    }
}
