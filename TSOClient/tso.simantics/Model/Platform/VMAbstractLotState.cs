using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FSO.SimAntics.Model.Platform
{
    public abstract class VMAbstractLotState : VMPlatformState
    {
        public virtual bool LimitExceeded { get { return false; } set { } }

        public virtual bool CanPlaceNewUserObject(VM vm)
        {
            return true;
        }

        public VMAbstractLotState() { }
        public VMAbstractLotState(int version) : base(version) { }
    }
}
