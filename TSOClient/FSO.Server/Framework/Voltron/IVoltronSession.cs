using FSO.Common.Security;
using FSO.Server.Framework.Aries;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
