using FSO.Server.Framework.Aries;
using FSO.Server.Framework.Voltron;
using Ninject;
using Ninject.Extensions.ChildKernel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FSO.Server.Servers.Lot.Hosting
{
    /// <summary>
    /// 
    /// </summary>
    public class LotContainer
    {
        private IKernel Kernel;
        private AriesPacketRouter _Router;

        public LotContainer(IKernel kernel)
        {
            Kernel = new ChildKernel(
                kernel
            );

            _Router = new AriesPacketRouter();
        }

        /// <summary>
        /// Load and initialize everything to start up the lot
        /// </summary>
        public void Bootstrap(LotContext context)
        {
            Kernel.Bind<LotContext>().ToConstant(context);
        }

        public void AvatarJoin(IVoltronSession session)
        {
        }

        public void AvatarLeave(IVoltronSession session)
        {
        }


        public IAriesPacketRouter Router
        {
            get { return this.Router; }
        }
    }
}
