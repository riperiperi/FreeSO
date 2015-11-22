using FSO.Common.Utils;
using FSO.Server.Database.DA;
using FSO.Server.Database.DA.Lots;
using FSO.Server.Framework.Aries;
using FSO.Server.Framework.Voltron;
using Ninject;
using Ninject.Extensions.ChildKernel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace FSO.Server.Servers.Lot.Domain
{
    public class LotHost
    {
        private Dictionary<int, LotHostEntry> Lots = new Dictionary<int, LotHostEntry>();
        private LotServerConfiguration Config;
        private IDAFactory DAFactory;
        private IKernel Kernel;

        public LotHost(LotServerConfiguration config, IDAFactory da, IKernel kernel)
        {
            this.Config = config;
            this.DAFactory = da;
            this.Kernel = kernel;
        }

        public void SessionClosed(IVoltronSession session)
        {
            var lot = GetLot(session);
            if(lot != null)
            {
                lot.Leave(session);
            }
        }

        public bool TryJoin(int lotId, IVoltronSession session)
        {
            var lot = GetLot(lotId);
            if (lot == null)
            {
                return false;
            }

            return lot.TryJoin(session);
        }

        private LotHostEntry GetLot(IVoltronSession session)
        {
            var lotId = (Int32)session.GetAttribute("currentLot");
            if(lotId == null)
            {
                return null;
            }
            return GetLot(lotId);
        }

        private LotHostEntry GetLot(int id)
        {
            if (Lots.ContainsKey(id))
            {
                return Lots[id];
            }
            return null;
        }

        public LotHostEntry TryHost(int id)
        {
            lock (Lots)
            {
                if(Lots.Values.Count >= Config.Max_Lots)
                {
                    //No room
                    return null;
                }

                if (Lots.ContainsKey(id))
                {
                    return null;
                }

                var ctnr = Kernel.Get<LotHostEntry>();
                Lots.Add(id, ctnr);
                return ctnr;
            }
        }

        public bool TryAcceptClaim(int lotId, uint claimId, string previousOwner)
        {
            using (var da = DAFactory.Get())
            {
                var didClaim = da.LotClaims.Claim(claimId, previousOwner, Config.Call_Sign);
                if (!didClaim)
                {
                    Lots.Remove(lotId);
                    return false;
                }
                else
                {
                    var claim = da.LotClaims.Get(claimId);
                    if(claim == null)
                    {
                        Lots.Remove(lotId);
                        return false;
                    }

                    var lot = da.Lots.Get(claim.lot_id);
                    if(lot == null)
                    {
                        Lots.Remove(lotId);
                        return false;
                    }

                    GetLot(claim.lot_id).Bootstrap(new LotContext {
                        DbId = lot.lot_id,
                        Id = lot.location,
                        ClaimId = claimId,
                        ShardId = lot.shard_id 
                    });
                    return true;
                }
            }
        }

    }


    public class LotHostEntry : ILotHost
    {
        public LotContainer Container { get; internal set; }
        private List<IVoltronSession> _Visitors = new List<IVoltronSession>();
        private IKernel ParentKernel;
        private IKernel Kernel;

        private Thread MainThread;
        private LotContext Context;

        private ManualResetEvent BackgroundNotify = new ManualResetEvent(false);
        private Thread BackgroundThread;
        private List<Callback> BackgroundTasks = new List<Callback>();


        public LotHostEntry(IKernel kernel)
        {
            ParentKernel = kernel;
        }

        public void Broadcast(params object[] messages)
        {
            //TODO: Make this more efficient
            foreach(var visitor in _Visitors)
            {
                try {
                    visitor.Write(messages);
                }catch(Exception ex){
                }
            }
        }

        public void InBackground(Callback cb)
        {
            lock (BackgroundTasks)
            {
                BackgroundTasks.Add(cb);
                BackgroundNotify.Set();
            }
        }

        public void Bootstrap(LotContext context)
        {
            this.Context = context;

            //Each lot gets itsi own set of bindings
            Kernel = new ChildKernel(
                ParentKernel
            );

            Kernel.Bind<LotContext>().ToConstant(context);
            Kernel.Bind<ILotHost>().ToConstant(this);

            Container = Kernel.Get<LotContainer>();

            BackgroundThread = new Thread(_DigestBackground);
            BackgroundThread.Start();

            MainThread = new Thread(Container.Run);
            MainThread.Start();
        }

        private void _DigestBackground()
        {
            while (BackgroundNotify.WaitOne())
            {
                List<Callback> tasks = new List<Callback>();
                lock (BackgroundTasks)
                {
                    tasks.AddRange(BackgroundTasks);
                    BackgroundTasks.Clear();
                }

                foreach (var task in tasks)
                {
                    try
                    {
                        task.Invoke();
                    }
                    catch (Exception ex)
                    {
                    }
                }
            }
        }

        public void Leave(IVoltronSession session)
        {
            lock (_Visitors)
            {
                _Visitors.Remove(session);
                session.SetAttribute("currentLot", null);
                InBackground(() => Container.AvatarLeave(session));
            }
        }

        public bool TryJoin(IVoltronSession session)
        {
            lock (_Visitors)
            {
                if(_Visitors.Count >= 24)
                {
                    //Full
                    return false;
                }

                session.SetAttribute("currentLot", Context.DbId);
                _Visitors.Add(session);
                InBackground(() => Container.AvatarJoin(session));
                return true;
            }
        }
    }

    public interface ILotHost
    {
        void Broadcast(params object[] messages);
    }
}
