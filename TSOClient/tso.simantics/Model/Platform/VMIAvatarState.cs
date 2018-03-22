using FSO.SimAntics.Model.TSOPlatform;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FSO.SimAntics.Model.Platform
{
    public interface VMIAvatarState
    {
        VMTSOAvatarPermissions Permissions { get; set; }
    }
}
