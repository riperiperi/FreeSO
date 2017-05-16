using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FSO.Server.Protocol.Electron.Model
{
    public enum FindAvatarResponseStatus
    {
        FOUND,
        IGNORING_THEM,
        IGNORING_YOU,
        NOT_ON_LOT,
        PRIVACY_ENABLED,
    }
}
