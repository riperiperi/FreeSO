using FSO.Common.Security;
using FSO.Server.Database.DA;
using FSO.Server.Database.DA.Lots;
using FSO.Server.Framework.Gluon;
using FSO.Server.Protocol.Electron.Model;
using FSO.Server.Protocol.Gluon.Packets;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace FSO.Server.Servers.City.Domain
{
    public class LotAllocations
    {
        private ConcurrentDictionary<uint, LotAllocation> _Locks = new ConcurrentDictionary<uint, LotAllocation>();
        private LotServerPicker PickingEngine;
        private IDAFactory DAFactory;
        private CityServerContext Context;

        public LotAllocations(LotServerPicker PickingEngine, IDAFactory daFactory, CityServerContext context)
        {
            this.PickingEngine = PickingEngine;
            this.DAFactory = daFactory;
            this.Context = context;
        }

        public Task<TryFindLotResult> TryFindOrOpen(uint lotId, ISecurityContext security)
        {
            return TryFind(lotId, true, security);
        }

        public Task<TryFindLotResult> TryFind(uint lotId, ISecurityContext security)
        {
            return TryFind(lotId, false, security);
        }

        public void OnTransferClaimResponse(TransferClaimResponse response)
        {
            if (response.Type != Protocol.Gluon.Model.ClaimType.LOT) { return; }

            var allocation = Get(response.EntityId);
            lock (allocation)
            {
                allocation.OnTransferClaimResponse(response);
                if (allocation.State == LotAllocationState.FAILED)
                {
                    //Failed, remove
                    LotAllocation removedAllocation;
                    _Locks.TryRemove(response.EntityId, out removedAllocation);
                }
            }

            //Touches the db so do this outside of the lock
            if (allocation.State == LotAllocationState.FAILED){
                allocation.Free();
            }
        }


        /// <summary>
        /// Tasks we handle are:
        ///   1) Asking a server to host the lot
        ///   2) Telling users which server is hosting the lot
        /// 
        /// It is then up to the client and server to connect to each other. This is a bit crappy because it means you
        /// must open a connection to the lot server before you can find out if there is room for you to join. But this removes
        /// a lot of complexity.
        /// 
        /// In the future we may move the person claiming logic to city if it causes problems. One problem I can see is there is a possible
        /// race condition in which the lot could fill up before the owner gets in. The lot would then automatically shut down
        /// 
        /// </summary>
        /// <param name="lotId"></param>
        /// <param name="openIfClosed"></param>
        /// <returns></returns>

        private Task<TryFindLotResult> TryFind(uint lotId, bool openIfClosed, ISecurityContext security)
        {
            var allocation = Get(lotId);
            lock (allocation)
            {
                switch (allocation.State)
                {
                    case LotAllocationState.NOT_ALLOCATED:
                        //We need to pick a server to run this lot
                        if (!openIfClosed){
                            //Sorry, cant do this
                            return Immediate(new TryFindLotResult
                            {
                                Status = FindLotResponseStatus.NOT_OPEN
                            });
                        }

                        DbLot lot = null;
                        using (var db = DAFactory.Get())
                        {
                            //Convert the lot location into a lot db id
                            lot = db.Lots.GetByLocation(Context.ShardId, lotId);
                            if (lot == null)
                            {
                                return Immediate(new TryFindLotResult
                                {
                                    Status = FindLotResponseStatus.NO_SUCH_LOT
                                });
                            }

                            //TODO: Roommates
                            try {
                                security.DemandAvatar(lot.owner_id, AvatarPermissions.WRITE);
                            }catch(Exception ex){
                                return Immediate(new TryFindLotResult {
                                    Status = FindLotResponseStatus.NOT_PERMITTED_TO_OPEN
                                });
                            }
                        }

                        if (!allocation.TryClaim(lot))
                        {
                            return Immediate(new TryFindLotResult
                            {
                                Status = FindLotResponseStatus.CLAIM_FAILED
                            });
                        }

                        var pick = PickingEngine.PickServer();
                        if(pick.Success == false)
                        {
                            //Release claim
                            allocation.TryUnclaim();

                            return Immediate(new TryFindLotResult
                            {
                                Status = FindLotResponseStatus.NO_CAPACITY
                            });
                        }else{
                            return allocation.BeginPick(pick).ContinueWith(x => {
                                if (x.IsFaulted || x.IsCanceled || x.Result.Accepted == false)
                                {
                                    return new TryFindLotResult
                                    {
                                        Status = FindLotResponseStatus.NO_CAPACITY
                                    };
                                }

                                return new TryFindLotResult {
                                    Status = FindLotResponseStatus.FOUND,
                                    Server = allocation.Server
                                };
                            });
                        }
                        break;

                    case LotAllocationState.ALLOCATING:
                        break;

                    case LotAllocationState.ALLOCATED:
                        return Immediate(new TryFindLotResult
                        {
                            Status = FindLotResponseStatus.FOUND,
                            Server = allocation.Server
                        });
                        
                    //Should never get here
                    case LotAllocationState.FAILED:
                        return Immediate(new TryFindLotResult {
                            Status = FindLotResponseStatus.UNKNOWN_ERROR
                        });
                }

                return null;
            }
        }

        private Task<T> Immediate<T>(T data)
        {
            var tcs = new TaskCompletionSource<T>();
            tcs.SetResult(data);
            return tcs.Task;
        }

        private LotAllocation Get(uint lotId)
        {
            return _Locks.GetOrAdd(lotId, x => {
                return new LotAllocation(DAFactory, Context);
            });
        }
    }

    public class TryFindLotResult
    {
        public FindLotResponseStatus Status;
        public IGluonSession Server;
    }

    public class LotAllocation
    {
        public LotAllocationState State { get; internal set; } = LotAllocationState.NOT_ALLOCATED;
        public LotPickerAttempt PickingAttempt { get; internal set; }

        private uint? ClaimId { get; set; }
        private IDAFactory DAFactory;
        private CityServerContext Context;

        private TaskCompletionSource<LotPickResult> PickingTask;
        private Task<LotPickResult> PickingTaskWithTimeout;
        private DbLot Lot;

        public LotAllocation(IDAFactory da, CityServerContext context)
        {
            Context = context;
            DAFactory = da;
        }

        public IGluonSession Server
        {
            get
            {
                return PickingAttempt.Session;
            }
        }

        public void OnTransferClaimResponse(TransferClaimResponse response)
        {
            PickingAttempt.Free();

            if(response.Status == TransferClaimResponseStatus.ACCEPTED)
            {
                //Not our claim anymore
                ClaimId = null;
                State = LotAllocationState.ALLOCATED;

                PickingTask.SetResult(new LotPickResult {
                    Accepted = true
                });
            }
            else
            {
                State = LotAllocationState.FAILED;
                PickingTask.SetResult(new LotPickResult
                {
                    Accepted = false
                });
            }
        }

        public void Free()
        {
            //Release the claim
            using (var da = DAFactory.Get())
            {
                da.LotClaims.Delete(ClaimId.Value, Context.Config.Call_Sign);
            }
        }

        //LotAllocation
        public bool TryClaim(DbLot lot)
        {
            Lot = lot;

            //Write a db record to claim the lot
            using (var db = DAFactory.Get())
            {
                var claim = db.LotClaims.TryCreate(new Database.DA.LotClaims.DbLotClaim
                {
                    shard_id = Context.ShardId,
                    lot_id = lot.lot_id,
                    owner = Context.Config.Call_Sign
                });

                ClaimId = claim;

                if (claim.HasValue)
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }
        }

        public void TryUnclaim()
        {
            if (ClaimId.HasValue)
            {
                using (var db = DAFactory.Get())
                {
                    db.LotClaims.Delete(ClaimId.Value, Context.Config.Call_Sign);
                }
                ClaimId = null;
            }
        }

        public Task<LotPickResult> BeginPick(LotPickerAttempt attempt)
        {
            State = LotAllocationState.ALLOCATING;
            PickingAttempt = attempt;
            PickingTask = new TaskCompletionSource<LotPickResult>();
            
            var cts = new CancellationTokenSource(TimeSpan.FromSeconds(20));
            PickingTaskWithTimeout = PickingTask.Task.ContinueWith(x => {
                return x.Result;
            }, cts.Token);

            try
            {
                attempt.Session.Write(new TransferClaim
                {
                    Type = Protocol.Gluon.Model.ClaimType.LOT,
                    //x,y used as id for lots
                    EntityId = Lot.location,
                    ClaimId = ClaimId.Value,
                    FromOwner = Context.Config.Call_Sign
                });
            }
            catch (Exception ex)
            {
                PickingTask.SetException(ex);
            }

            return PickingTaskWithTimeout;
        }
    }

    public class LotPickResult
    {
        public bool Accepted;
    }

    public enum LotAllocationState
    {
        NOT_ALLOCATED,
        ALLOCATING,
        ALLOCATED,
        FAILED
    }
}
