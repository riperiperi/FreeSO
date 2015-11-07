using FSO.Server.Framework.Aries;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FSO.Server.Servers.Lot
{
    public class LotServer : AbstractAriesServer
    {
        public LotServer(Ninject.IKernel kernel) : base(null, kernel)
        {

        }

        public override Type[] GetHandlers()
        {
            return new Type[] { };
        }
    }
}
