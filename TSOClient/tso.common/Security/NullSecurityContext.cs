using System.Collections.Generic;

namespace FSO.Common.Security
{
    public class NullSecurityContext : ISecurityContext
    {
        public static NullSecurityContext INSTANCE = new NullSecurityContext();


        public void DemandAvatar(uint id, AvatarPermissions permission)
        {
        }

        public void DemandAvatars(IEnumerable<uint> id, AvatarPermissions permission)
        {
        }

        public void DemandInternalSystem()
        {
        }
    }
}
