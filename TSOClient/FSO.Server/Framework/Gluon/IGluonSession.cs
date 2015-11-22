using FSO.Server.Framework.Aries;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FSO.Server.Framework.Gluon
{
    public interface IGluonSession : IAriesSession
    {
        string CallSign { get; }
    }
}
