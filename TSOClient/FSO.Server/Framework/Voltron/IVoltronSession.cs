using FSO.Common.Security;
using FSO.Server.Framework.Aries;

namespace FSO.Server.Framework.Voltron
{
    public interface IVoltronSession : IAriesSession, ISecurityContext
    {
        bool IsAnonymous { get; }

        uint UserId { get; }
        uint AvatarId { get; }
        int AvatarClaimId { get; }

        string IpAddress { get; }
    }
}
