using FSO.Common.DataService;
using FSO.Common.Serialization.Primitives;
using FSO.Common.Utils;
using FSO.Server.Common;
using FSO.Server.Database.DA;
using FSO.Server.Database.DA.Lots;
using FSO.Server.Database.DA.LotVisitors;
using FSO.Server.DataService;
using FSO.Server.Framework.Aries;
using FSO.Server.Framework.Gluon;
using FSO.Server.Framework.Voltron;
using FSO.Server.Protocol.Electron.Packets;
using FSO.Server.Protocol.Gluon.Model;
using FSO.Server.Protocol.Gluon.Packets;
using FSO.Server.Protocol.Voltron.Packets;
using FSO.Server.Servers.Lot.Lifecycle;
using Ninject;
using Ninject.Extensions.ChildKernel;
using NLog;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
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

        public void ShutdownByShard(int shardId)
        {
            var lots = new Dictionary<int, LotHostEntry>(Lots).Where(x => x.Value.ShardId == shardId);
            foreach(var lot in lots)
            {
                LOG.Info("Forcing shutdown of lot: " + lot.Key);
                lot.Value.ForceShutdown(false);
            }
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

        public void CheckLiveness()
        {
            lock (Lots)
            {
                var now = Epoch.Now;
                foreach (var lot in Lots.Values)
                {
                    if (now - lot.LastActivity > 90)
                    {
                        lot.Abort();
                    }
                }
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
                LotHostEntry entry;
                if (Lots.TryGetValue(id, out entry))
                {
                    entry.Dispose();
                    Kernel.Release(entry);
                }
                Lots.Remove(id);
                CityConnections.LotCount = (short)Lots.Count;
            }
        }

        public bool TryDisconnectClient(int lot_id, uint avatar_id)
        {
            var lot = GetLot(lot_id);
            if (lot == null) return false;
            lot.DropClient(avatar_id);
            return true;
        }

        public bool NotifyRoommateChange(int lot_id, uint avatar_id, ChangeType change)
        {
            var lot = GetLot(lot_id);
            if (lot == null) return false;
            lot.Container.NotifyRoommateChange(avatar_id, change);
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
                var bind = Kernel.GetBindings(typeof(LotHostEntry));
                ctnr.CityConnection = cityConnection;
                Lots.Add(id, ctnr);
                CityConnections.LotCount = (short)Lots.Count;
                return ctnr;
            }
        }

        public bool TryAcceptClaim(int lotId, uint claimId, uint specialId, string previousOwner, ClaimAction openAction)
        {
            if (claimId == 0)
            { //job lot
                GetLot(lotId).Bootstrap(new LotContext
                {
                    DbId = (int)specialId, //contains job type/grade
                    Id = (uint)lotId, //lotId contains a "job lot location", not a DbId.
                    ClaimId = claimId,
                    ShardId = 0,
                    Action = openAction
                });
                return true;
            }

            using (var da = DAFactory.Get())
            {
                var didClaim = da.LotClaims.Claim(claimId, previousOwner, Config.Call_Sign);
                if (!didClaim)
                {
                    RemoveLot(lotId);
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
                            RemoveLot(lotId);
                            return false;
                        }

                        var lot = da.Lots.Get(claim.lot_id);
                        if (lot == null)
                        {
                            RemoveLot(lotId);
                            return false;
                        }

                        LOG.Info("Starting claimed lot with dbid = " + lotId + "...");
                        GetLot(claim.lot_id).Bootstrap(new LotContext
                        {
                            DbId = lot.lot_id,
                            Id = lot.location,
                            ClaimId = claimId,
                            ShardId = lot.shard_id,
                            Action = openAction,
                            HighMax = lot.admit_mode == 5
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

        public int? ShardId
        {
            get
            {
                if(Context == null)
                {
                    return null;
                }
                return Context.ShardId;
            }
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

        public void Dispose()
        {
            Kernel?.Dispose();
        }

        /// <summary>
        /// Something went really wrong and we should just roll with it.
        /// </summary>
        public void Abort()
        {
            BgKilled = true;
            BackgroundThread?.Abort();
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

        //timeout for the background thread recieving more tasks.
        private static readonly int BACKGROUND_NOTIFY_TIMEOUT = 2000;
        //the number of times recieving no background tasks after which we assume the main thread is stuck in an infinite loop.
        private static readonly int BACKGROUND_TIMEOUT_ABANDON_COUNT = 4;
        private static readonly int BACKGROUND_TIMEOUT_SECONDS = 15;
        private uint LastTaskRecv = 0;
        private int BgTimeoutExpiredCount = 0;
        public uint LastActivity = Epoch.Now;
        private bool BgKilled;
        private void _DigestBackground()
        {
            try
            {
                while (true)
                {
                    if (LastTaskRecv == 0) LastTaskRecv = Epoch.Now;
                    var notified = BackgroundNotify.WaitOne(BACKGROUND_NOTIFY_TIMEOUT);
                    List<Callback> tasks = new List<Callback>();
                    lock (BackgroundTasks)
                    {
                        tasks.AddRange(BackgroundTasks);
                        BackgroundTasks.Clear();
                    }

                    if (tasks.Count > 1000) LOG.Error("Surprising number of background tasks for lot with dbid = " + Context.DbId + ": "+tasks.Count);

                    if (tasks.Count > 0) LastTaskRecv = Epoch.Now; //BgTimeoutExpiredCount = 0;
                    else if (Epoch.Now - LastTaskRecv > BACKGROUND_TIMEOUT_SECONDS) //++BgTimeoutExpiredCount > BACKGROUND_TIMEOUT_ABANDON_COUNT)
                    {
                        BgTimeoutExpiredCount = int.MinValue;

                        //Background tasks stop when we shut down
                        if (!ShuttingDown)
                        {
                            LOG.Error("Main thread for lot with dbid = " + Context.DbId + " entered an infinite loop and had to be terminated!");

                            bool IsRunningOnMono = (Type.GetType("Mono.Runtime") != null);

                            //suspend and resume are deprecated, but we need to use them to analyse the stack of stuck main threads
                            //sorry microsoft
                            if (!IsRunningOnMono)
                            {
                                MainThread.Suspend();
                                var trace = new StackTrace(MainThread, false);
                                MainThread.Resume();

                                LOG.Error("Trace (immediately when aborting): " + trace.ToString());
                            }
                            else
                            {
                                LOG.Error("on mono, so can't obtain immediate trace.");
                            }

                            MainThread.Abort(); //this will jolt the thread out of its infinite loop... into immediate lot shutdown
                            return;
                        }
                    }

                    foreach (var task in tasks)
                    {
                        try
                        {
                            task?.Invoke();
                            if (task == null) LastActivity = Epoch.Now;
                        }
                        catch (ThreadAbortException ex)
                        {
                            if (BgKilled) { 
                                LOG.Error("Background thread locked for lot with dbid = " + Context.DbId + "! TERMINATING! " + ex.ToString());
                                MainThread.Abort(); //this will jolt the thread out of its infinite loop... into immediate lot shutdown
                                return;
                            }
                        }
                        catch (Exception ex)
                        {
                            LOG.Info("Background task failed on lot with dbid = " + Context.DbId + "! (continuing)" + ex.ToString());
                        }
                        if (Epoch.Now - LastTaskRecv > 10)
                        {
                            LOG.Info("WARNING: Unusually long background task for dbid = " + Context.DbId + "! " + (Epoch.Now - LastTaskRecv) + "seconds");
                        }
                        LastTaskRecv = Epoch.Now;
                    }
                }
            }
            catch (ThreadAbortException ex2) {
                if (BgKilled)
                {
                    LOG.Error("Background thread locked for lot with dbid = " + Context.DbId + "! TERMINATING! " + ex2.ToString());
                    MainThread.Abort(); //this will jolt the thread out of its infinite loop... into immediate lot shutdown
                    return;
                }
                //complete remaining tasks
                LastActivity = Epoch.Now;
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
                        LOG.Info("Background task failed on lot " + Context.DbId + "! (when ending)" + ex.ToString());
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
            if (Container.IsAvatarOnLot(session.AvatarId)) return false; //already on the lot.
            lock (_Visitors)
            {
                if (ShuttingDown || (_Visitors.Count >= ((Context.HighMax)?128:24)))
                {
                    if (ShuttingDown) return false; //cannot join

                    //check if this user has special permissions. should only happen when a lot is full
                    //let them in anyways if they do
                    var avatar = DAFactory.Get().Avatars.Get(session.AvatarId);
                    if (avatar.moderation_level == 0 && !Context.JobLot)
                    {
                        if (DAFactory.Get().Roommates.Get(session.AvatarId, Context.DbId) == null)
                            return false; //not a roommate
                    }
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
            lock (_Visitors) Model.Lot_NumOccupants = (byte)_Visitors.Count;
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

            //We need to keep currentLot because we need to process the Leave action
            //session.SetAttribute("currentLot", null);
            SyncNumVisitors();

            InBackground(() =>
            {
                using (var db = DAFactory.Get())
                {
                    //return claim to the city we got it from.

                    if ((bool)(session.GetAttribute("returnClaim") ?? true))
                        db.AvatarClaims.Claim(session.AvatarClaimId, Config.Call_Sign, (string)session.GetAttribute("cityCallSign"), 0);
                    else
                        db.AvatarClaims.Delete(session.AvatarClaimId, Config.Call_Sign);
                }

                if (session.GetAttribute("visitId") != null)
                {
                    var id = (int)session.GetAttribute("visitId");
                    using (var da = DAFactory.Get())
                    {
                        da.LotVisits.Leave(id);
                    }
                }
            });
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
                var copy = new Dictionary<uint, IVoltronSession>(_Visitors);
                foreach (var visitor in copy)
                {
                    try {
                        if (!lotClosed) visitor.Value.SetAttribute("returnClaim", false);
                        ReleaseAvatarClaim(visitor.Value);
                        visitor.Value.Close();
                    }catch(Exception ex)
                    {
                        LOG.Error(ex, "Error releasing avatar");
                    }
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
                try {
                    CityConnection.Write(new TransferClaim()
                    {
                        Type = Protocol.Gluon.Model.ClaimType.LOT,
                        ClaimId = Context.ClaimId,
                        EntityId = ((Context.Id & 0x40000000) > 0) ? (int)Context.Id : Context.DbId,
                        FromOwner = Config.Call_Sign
                    });
                }catch(Exception ex)
                {
                    LOG.Error(ex, "Unable to inform city of lot " + Context.DbId + " claim release");
                }
            }
            //if this lot still has any avatar claims, kill them at least so the user's access to the game isn't limited until server restart.
            using (var da = DAFactory.Get())
            {
                da.AvatarClaims.RemoveRemaining(Config.Call_Sign, Context.Id);
                da.LotClaims.Delete(Context.ClaimId, Config.Call_Sign);
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

        public void RecordStartVisit(IVoltronSession session, DbLotVisitorType visitorType)
        {
            if (Context.JobLot) return;
            using (var da = DAFactory.Get())
            {
                var id = da.LotVisits.Visit(session.AvatarId, visitorType, Context.DbId);
                if (id != null && id.HasValue){
                    session.SetAttribute("visitId", id.Value);
                }
            }
        }

        public void UpdateActiveVisitRecords()
        {
            if (Context.JobLot) return;

            var visitIds = new List<int>();
            lock (_Visitors)
            {
                foreach (var visitor in _Visitors.Values)
                {
                    var id = visitor.GetAttribute("visitId");
                    if (id != null)
                    {
                        visitIds.Add((int)id);
                    }
                }
            }

            InBackground(() =>
            {
                //Update the timestamp on visit records, this helps us count
                //active sessions in top 100 + visitor bonus calculations
                using (var db = DAFactory.Get())
                {
                    db.LotVisits.Renew(visitIds.ToArray());
                }
            });
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

        //Visitor audits
        void RecordStartVisit(IVoltronSession session, DbLotVisitorType visitorType);
        void UpdateActiveVisitRecords();
    }
}
