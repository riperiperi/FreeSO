using FSO.Common.Security;
using FSO.Server.Framework.Aries;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FSO.Server.Framework.Gluon
{
    public interface IGluonSession : IAriesSession, ISecurityContext
    {
        string CallSign { get; }
        string PublicHost { get; }
        string InternalHost { get; }
    }
}
