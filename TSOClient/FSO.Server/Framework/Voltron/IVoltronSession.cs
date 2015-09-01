using FSO.Server.Framework.Aries;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FSO.Server.Framework.Voltron
{
    public interface IVoltronSession : IAriesSession
    {
        bool IsAnonymous { get; }

        uint UserId { get; }
        uint AvatarId { get; }
    }
}
