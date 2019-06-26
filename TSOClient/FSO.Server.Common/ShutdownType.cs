using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FSO.Server.Common
{
    public enum ShutdownType : byte
    {
        SHUTDOWN = 0,
        RESTART = 1,
        UPDATE = 2 //restart but runs an update task
    }
}
