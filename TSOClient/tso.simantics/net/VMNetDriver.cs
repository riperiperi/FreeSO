using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TSO.Simantics.net.model;

namespace TSO.Simantics.net
{
    public interface VMNetDriver
    {
        void SendCommand(VMNetCommandBodyAbstract cmd);
        void Tick(VM vm);
    }
}
