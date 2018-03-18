using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FSO.SimAntics.Model.Platform
{
    public abstract class VMAbstractEntityState : VMPlatformState
    {
        public VMAbstractEntityState() { }
        public VMAbstractEntityState(int version) : base(version) { }
    }
}
