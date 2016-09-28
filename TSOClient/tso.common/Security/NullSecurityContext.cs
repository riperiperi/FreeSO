using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace FSO.Common.Security
{
    public class NullSecurityContext : ISecurityContext
    {
        public static NullSecurityContext INSTANCE = new NullSecurityContext();


        public void DemandAvatar(uint id, AvatarPermissions permission)
        {
        }

        public void DemandAvatars(List<uint> id, AvatarPermissions permission)
        {
        }

        public void DemandInternalSystem()
        {
        }
    }
}
