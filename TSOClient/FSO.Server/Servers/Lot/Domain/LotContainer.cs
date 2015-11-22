using FSO.Server.Database.DA;
using FSO.Server.Framework.Aries;
using FSO.Server.Framework.Voltron;
using Ninject;
using Ninject.Extensions.ChildKernel;
using NLog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace FSO.Server.Servers.Lot.Domain
{
    /// <summary>
    /// 
    /// </summary>
    public class LotContainer
    {
        private static Logger LOG = LogManager.GetCurrentClassLogger();

        private IKernel Kernel;
        private IDAFactory DAFactory;
        private LotContext Context;

        private AriesPacketRouter _Router;
        private Thread Thread;

        public LotContainer(IKernel kernel, IDAFactory da)
        {
            Kernel = new ChildKernel(
                kernel
            );
            DAFactory = da;
            _Router = new AriesPacketRouter();
        }

        /// <summary>
        /// Load and initialize everything to start up the lot
        /// </summary>
        public void Bootstrap(LotContext context)
        {
            LOG.Info("Starting to host lot with dbid = " + context.DbId);

            Context = context;
            Kernel.Bind<LotContext>().ToConstant(context);
            Thread = new Thread(_Bootstrap);
            Thread.Start();
        }

        private void _Bootstrap()
        {
            int y = 22;
        }

        public void AvatarJoin(IVoltronSession session)
        {
        }

        public void AvatarLeave(IVoltronSession session)
        {
        }

        public void LotClaimed(uint claimId)
        {

        }

        public IAriesPacketRouter Router
        {
            get { return this.Router; }
        }
    }
}
