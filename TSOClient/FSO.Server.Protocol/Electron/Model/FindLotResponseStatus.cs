using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FSO.Server.Protocol.Electron.Model
{
    public enum FindLotResponseStatus
    {
        FOUND,
        NO_SUCH_LOT,
        NOT_OPEN,
        NOT_PERMITTED_TO_OPEN,
        CLAIM_FAILED,
        NO_CAPACITY,
        UNKNOWN_ERROR
    }
}
