using FSO.Common.Security;
using FSO.Server.Framework.Aries;

namespace FSO.Server.Framework.Gluon
{
    public interface IGluonSession : IAriesSession, ISecurityContext
    {
        string CallSign { get; }
        string PublicHost { get; }
        string InternalHost { get; }
    }
}
