using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TSO.Simantics.net.model;

namespace TSO.Simantics.net.drivers
{
    public class VMServerDriver : VMNetDriver
    {
        public void SendCommand(VMNetCommandBodyAbstract cmd)
        {
            throw new NotImplementedException();
        }

        public void Tick(VM vm)
        {
            throw new NotImplementedException();
        }
    }
}
