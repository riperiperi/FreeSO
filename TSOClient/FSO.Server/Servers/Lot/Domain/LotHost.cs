using FSO.Common.DataService;
using FSO.Common.Serialization.Primitives;
using FSO.Common.Utils;
using FSO.Server.Database.DA;
using FSO.Server.Database.DA.Lots;
using FSO.Server.DataService;
using FSO.Server.Framework.Aries;
using FSO.Server.Framework.Gluon;
using FSO.Server.Framework.Voltron;
using FSO.Server.Protocol.Electron.Packets;
using FSO.Server.Protocol.Gluon.Packets;
using FSO.Server.Protocol.Voltron.Packets;
using FSO.Server.Servers.Lot.Lifecycle;
using Ninject;
using Ninject.Extensions.ChildKernel;
using NLog;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace FSO.Server.Servers.Lot.Domain
{
    public class LotHost
    {
        private static Logger LOG = LogManager.GetCurrentClassLogger();

        private Dictionary<int, LotHostEntry> Lots = new Dictionary<int, LotHostEntry>();
        private LotServerConfiguration Config;
        private IDAFactory DAFactory;
        private IKernel Kernel;
        private IDataServiceSync<FSO.Common.DataService.Model.Lot> LotStatusSync;
        private IDataServiceSync<FSO.Common.DataService.Model.Lot> LotRoomiesSync;
        private CityConnections CityConnections;

        private bool AwaitingShutdown;
        private TaskCompletionSource<bool> ShutdownWait = new TaskCompletionSource<bool>();

        public LotHost(LotServerConfiguration config, IDAFactory da, IKernel kernel, IDataServiceSyncFactory ds, CityConnections connections)
        {
            this.Config = config;
            this.DAFactory = da;
            this.Kernel = kernel;
            this.CityConnections = connections;

            LotStatusSync = ds.Get<FSO.Common.DataService.Model.Lot>("Lot_NumOccupants", "Lot_IsOnline", "Lot_SpotLightText");
            LotRoomiesSync = ds.Get<FSO.Common.DataService.Model.Lot>("Lot_RoommateVec");
        }

        public async Task<bool> Shutdown()
        {
            bool noWaiting = true;
            lock (Lots)
            {
                if (AwaitingShutdown) return false;
                
                LOG.Info("Lot server "+ Config.Call_Sign + " shutting down...");
                AwaitingShutdown = true;
                foreach (var lot in Lots)
                {
                    noWaiting = false;
                    lot.Value.ForceShutdown(false);
                }
            }

            if (noWaiting)
            {
                LOG.Info("Lot server " + Config.Call_Sign + " was hosting nothing!");
                CityConnections.Stop();
                return true;
            }
            return await ShutdownWait.Task;
        }

        public void ShutdownComplete(LotHostEntry entry)
        {
            lock (Lots)
            {
                if (AwaitingShutdown && Lots.Count == 0)
                {
                    //this lot server has completely shut down!
                    LOG.Info("Lot server "+ Config.Call_Sign + " successfully shut down!");
                    CityConnections.Stop();
                    ShutdownWait.SetResult(true);
                }
            }
        }

        public void Sync(LotContext context, FSO.Common.DataService.Model.Lot lot)
        {
            var city = CityConnections.GetByShardId(context.ShardId);
            if(city != null)
            {
                LotStatusSync.Sync(city, lot);
            }
        }

        public void SyncRoommates(LotContext context, FSO.Common.DataService.Model.Lot lot)
        {
            var city = CityConnections.GetByShardId(context.ShardId);
            if (city != null)
            {
                LotRoomiesSync.Sync(city, lot);
            }
        }

        public void RouteMessage(IVoltronSession session, object message)
        {
            var lot = GetLot(session);
            if (lot != null)
            {
                lot.Message(session, message);
            }
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

        public void RemoveLot(int id)
        {
            lock (Lots)
            {
                Lots.Remove(id);
            }
        }

        public bool TryDisconnectClient(int lot_id, uint avatar_id)
        {
            var lot = GetLot(lot_id);
            if (lot == null) return false;
            lot.DropClient(avatar_id);
            return true;
        }

        private LotHostEntry GetLot(IVoltronSession session)
        {
            var lotId = (int?)session.GetAttribute("currentLot");
            if (lotId == null)
            {
                return null;
            }
            lock (Lots)
            {
                if (Lots.ContainsKey(lotId.Value))
                {
                    return Lots[lotId.Value];
                }
            }
            return null;
        }

        private LotHostEntry GetLot(int id)
        {
            lock (Lots)
            {
                if (AwaitingShutdown) return null;
                if (Lots.ContainsKey(id))
                {
                    return Lots[id];
                }
            }
            return null;
        }

        public LotHostEntry TryHost(int id, IGluonSession cityConnection)
        {
            lock (Lots)
            {
                if (AwaitingShutdown) return null;
                if (Lots.Values.Count >= Config.Max_Lots)
                {
                    //No room
                    return null;
                }

                if (Lots.ContainsKey(id))
                {
                    return null;
                }

                var ctnr = Kernel.Get<LotHostEntry>();
                ctnr.CityConnection = cityConnection;
                Lots.Add(id, ctnr);
                return ctnr;
            }
        }

        public bool TryAcceptClaim(int lotId, uint claimId, uint specialId, string previousOwner)
        {
            if (claimId == 0)
            { //job lot
                GetLot(lotId).Bootstrap(new LotContext
                {
                    DbId = (int)specialId, //contains job type/grade
                    Id = (uint)lotId, //lotId contains a "job lot location", not a DbId.
                    ClaimId = claimId,
                    ShardId = 0
                });
                return true;
            }

            using (var da = DAFactory.Get())
            {
                var didClaim = da.LotClaims.Claim(claimId, previousOwner, Config.Call_Sign);
                if (!didClaim)
                {
                    lock (Lots) Lots.Remove(lotId);
                    return false;
                }
                else
                {
                    try
                    {
                        LOG.Info("Checking out db... " + lotId + "...");
                        var claim = da.LotClaims.Get(claimId);
                        if (claim == null)
                        {
                            lock (Lots) Lots.Remove(lotId);
                            return false;
                        }

                        var lot = da.Lots.Get(claim.lot_id);
                        if (lot == null)
                        {
                            lock (Lots) Lots.Remove(lotId);
                            return false;
                        }

                        LOG.Info("Starting claimed lot with dbid = " + lotId + "...");
                        GetLot(claim.lot_id).Bootstrap(new LotContext
                        {
                            DbId = lot.lot_id,
                            Id = lot.location,
                            ClaimId = claimId,
                            ShardId = lot.shard_id
                        });
                        LOG.Info("Bootstrapped lot with dbid = " + lotId + "!");
                        return true;
                    } catch (Exception e)
                    {
                        LOG.Info("Lot bootstrap error! EXCEPTION: " + e.ToString());
                        return false;
                    }
                }
            }
        }

    }


    public class LotHostEntry : ILotHost
    {
        private static Logger LOG = LogManager.GetCurrentClassLogger();

        //Partial model for syncing updates
        private FSO.Common.DataService.Model.Lot Model;
        private LotHost Host;

        public LotContainer Container { get; internal set; }
        private Dictionary<uint, IVoltronSession> _Visitors = new Dictionary<uint, IVoltronSession>();
        public IGluonSession CityConnection;
        private IKernel ParentKernel;
        private IKernel Kernel;

        private LotServerConfiguration Config;
        private IDAFactory DAFactory;

        private Thread MainThread;
        private LotContext Context;

        private AutoResetEvent BackgroundNotify = new AutoResetEvent(false);
        private Thread BackgroundThread;
        private List<Callback> BackgroundTasks = new List<Callback>();
        private bool ShuttingDown;

        public LotHostEntry(LotHost host, IKernel kernel, IDAFactory da, LotServerConfiguration config)
        {
            Host = host;
            DAFactory = da;
            Config = config;
            ParentKernel = kernel;

            Model = new FSO.Common.DataService.Model.Lot();
        }

        public void Send(uint avatarID, params object[] messages)
        {
            lock (_Visitors)
            {
                IVoltronSession visitor = null;
                if (_Visitors.TryGetValue(avatarID, out visitor))
                {
                    visitor.Write(messages);
                }
            }
        }

        public void Broadcast(HashSet<uint> ignoreIDs, params object[] messages)
        {
            //TODO: Make this more efficient
            lock (_Visitors)
            {
                foreach (var visitor in _Visitors.Values)
                {
                    if (ignoreIDs.Contains(visitor.AvatarId)) continue;
                    try
                    {
                        visitor.Write(messages);
                    }
                    catch (Exception ex)
                    {
                    }
                }
            }
        }

        public void DropClient(uint id)
        {
            DropClient(id, true);
        }

        public void DropClient(uint id, bool returnClaim)
        {
            lock (_Visitors)
            {
                IVoltronSession visitor = null;
                if (_Visitors.TryGetValue(id, out visitor))
                {
                    visitor.SetAttribute("returnClaim", returnClaim);
                    visitor.Close();
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
            Model.Id = context.Id;
            Model.DbId = context.DbId;

            LOG.Info("Bootstrapping lot with dbid = " + context.DbId + "...");

            //Each lot gets its own set of bindings
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
            try
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
            catch (ThreadAbortException) {
                //complete remaining tasks
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

        public void Message(IVoltronSession session, object message)
        {
            if (message is FSOVMCommand)
            {
                Container.Message(session, (FSOVMCommand)message);
            }
        }

        public void Leave(IVoltronSession session)
        {
            lock (_Visitors)
            {
                InBackground(() => Container.AvatarLeave(session));
            }
        }

        public bool TryJoin(IVoltronSession session)
        {
            lock (_Visitors)
            {
                if (_Visitors.Count >= 64 || ShuttingDown)//|| Container.IsAvatarOnLot(session.AvatarId))
                {
                    //cannot join
                    return false;
                }

                session.SetAttribute("currentLot", ((Context.Id & 0x40000000) > 0)?(int)Context.Id:Context.DbId);
                _Visitors.Add(session.AvatarId, session);

                SyncNumVisitors();

                InBackground(() => Container.AvatarJoin(session));
                return true;
            }
        }

        private void SyncNumVisitors()
        {
            Model.Lot_NumOccupants = (byte)_Visitors.Count;
            Host.Sync(Context, Model);
        }

        public void ReleaseAvatarClaim(uint id)
        {
            IVoltronSession session = null;
            lock (_Visitors)
            {
                _Visitors.TryGetValue(id, out session);
            }
            ReleaseAvatarClaim(session);
        }

        public void ReleaseAvatarClaim(IVoltronSession session)
        {
            if (session.GetAttribute("currentLot") == null) return;
            lock (_Visitors) _Visitors.Remove(session.AvatarId);
            session.SetAttribute("currentLot", null);
            SyncNumVisitors();

            using (var db = DAFactory.Get())
            {
                //return claim to the city we got it from.
                
                if ((bool)(session.GetAttribute("returnClaim") ?? true))
                    db.AvatarClaims.Claim(session.AvatarClaimId, Config.Call_Sign, (string)session.GetAttribute("cityCallSign"), 0);
                else
                    db.AvatarClaims.Delete(session.AvatarClaimId, Config.Call_Sign);
            }
        }

        public void Shutdown()
        {
            if (!ShuttingDown) ForceShutdown(true);
            Host.RemoveLot(((Context.Id & 0x40000000) > 0)?(int)Context.Id:Context.DbId);
            SetOnline(false);
            SetSpotlight(false);
            ReleaseLotClaim();
            Host.ShutdownComplete(this);
            BackgroundThread.Abort();
        }

        public void ForceShutdown(bool lotClosed)
        {
            //drop all clients. do not accept new ones.
            lock (_Visitors)
            {
                if (ShuttingDown) return;
                ShuttingDown = true;
                foreach (var visitor in _Visitors)
                {
                    if (!lotClosed) visitor.Value.SetAttribute("returnClaim", false);
                    visitor.Value.Close();
                }
            }
            if (!lotClosed)
            {
                //we still need to close the lot safely. Start a rapid simulation to end the lot asap.
                Container.ForceShutdown();
            }
        }

        public void ReleaseLotClaim()
        {
            //tell our city that we're no longer hosting this lot.
            if (CityConnection != null)
            {
                CityConnection.Write(new TransferClaim()
                {
                    Type = Protocol.Gluon.Model.ClaimType.LOT,
                    ClaimId = Context.ClaimId,
                    EntityId = ((Context.Id&0x40000000)>0)?(int)Context.Id:Context.DbId,
                    FromOwner = Config.Call_Sign
                });
            }
        }

        public void SetOnline(bool online)
        {
            Model.Lot_IsOnline = online;
            Host.Sync(Context, Model);
        }

        public void SyncRoommates()
        {
            using (var db = DAFactory.Get())
            {
                var roomies = db.Roommates.GetLotRoommates(Context.DbId);
                var modelVec = new List<uint>();
                foreach (var roomie in roomies)
                {
                    if (roomie.is_pending == 0) modelVec.Add(roomie.avatar_id);
                }
                Model.Lot_RoommateVec = ImmutableList.ToImmutableList(modelVec);
                Host.SyncRoommates(Context, Model);
            }
        }

        public void SetSpotlight(bool on)
        {
            Model.Lot_SpotLightText = on?"spot":"";
            Host.Sync(Context, Model);
        }
    }

    public interface ILotHost
    {
        void Send(uint avatarID, params object[] messages);
        void Broadcast(HashSet<uint> ignoreIDs, params object[] messages);
        void DropClient(uint avatarID);
        void InBackground(Callback cb);
        void ReleaseAvatarClaim(IVoltronSession session);
        void ReleaseAvatarClaim(uint avatarID);
        void Shutdown();
        void SetOnline(bool online);
        void SetSpotlight(bool on);
        void SyncRoommates();
    }
}
