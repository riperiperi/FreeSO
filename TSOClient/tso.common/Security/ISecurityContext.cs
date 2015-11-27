using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace FSO.Common.Security
{
    public interface ISecurityContext
    {
        void DemandAvatar(uint id, AvatarPermissions permission);
        void DemandInternalSystem();
    }
}
