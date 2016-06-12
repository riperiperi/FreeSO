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

        private IDAFactory DAFactory;
        private LotContext Context;
        private ILotHost Host;
        
        public LotContainer(IDAFactory da, LotContext context, ILotHost host)
        {
            DAFactory = da;
            Host = host;
            Context = context;
            
        }

        /// <summary>
        /// Load and initialize everything to start up the lot
        /// </summary>
        public void Run()
        {
            LOG.Info("Starting to host lot with dbid = " + Context.DbId);
            Host.SetOnline(true);

            while (true)
            {
                //TODO: Bootstrap + simulation
                Thread.Sleep(1000);
            }
        }

        //Run on the background thread
        public void AvatarJoin(IVoltronSession session)
        {
            using (var da = DAFactory.Get())
            {
                var avatar = da.Avatars.Get(session.AvatarId);
                LOG.Info("Avatar " + avatar.name + " has joined");
                //Load all the avatars data
            }
        }

        //Run on the background thread
        public void AvatarLeave(IVoltronSession session)
        {
            //Exit lot, Persist the avatars data, remove avatar lock
            LOG.Info("Avatar left");
            Host.ReleaseAvatarClaim(session);
        }


    }
}
