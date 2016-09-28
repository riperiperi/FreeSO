using FSO.Server.Framework.Aries;
using Mina.Core.Session;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FSO.Common.Security;

namespace FSO.Server.Framework.Gluon
{
    public class GluonSession : AriesSession, IGluonSession
    {
        public GluonSession(IoSession ioSession) : base(ioSession)
        {
        }

        public string CallSign { get; set; }

        public string InternalHost { get; set; }

        public string PublicHost { get; set; }



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
