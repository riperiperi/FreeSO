using FSO.Common.Security;
using FSO.Server.Database.DA;
using FSO.Server.Database.DA.Lots;
using FSO.Server.Framework.Gluon;
using FSO.Server.Protocol.Electron.Model;
using FSO.Server.Protocol.Gluon.Model;
using FSO.Server.Protocol.Gluon.Packets;
using Ninject;
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
        private JobMatchmaker Matchmaker;

        public LotAllocations(LotServerPicker PickingEngine, IDAFactory daFactory, CityServerContext context, IKernel kernel)
        {
            this.PickingEngine = PickingEngine;
            this.DAFactory = daFactory;
            this.Context = context;
            this.Matchmaker = kernel.Get<JobMatchmaker>();
        }

        public Task<TryFindLotResult> TryFindOrOpen(uint lotId, uint avatarId, ISecurityContext security)
        {
            return TryFind(lotId, avatarId, true, security);
        }

        public Task<TryFindLotResult> TryFind(uint lotId, uint avatarId, ISecurityContext security)
        {
            return TryFind(lotId, avatarId, false, security);
        }

        public void OnTransferClaimResponse(TransferClaimResponse response)
        {
            if (response.Type != Protocol.Gluon.Model.ClaimType.LOT) { return; }

            uint? location;
            if (response.ClaimId != 0)
            {
                using (var da = DAFactory.Get())
                {
                    location = da.Lots.Get(response.EntityId)?.location;
                }
            } else
            {
                location = (uint)response.EntityId;
            }
            if (location == null) return;

            var allocation = Get(location.Value);
            lock (allocation)
            {
                allocation.OnTransferClaimResponse(response);
                if (allocation.State == LotAllocationState.FAILED)
                {
                    //Failed, remove
                    LotAllocation removedAllocation = Remove(location.Value);
                }
            }

            //Touches the db so do this outside of the lock
            if (allocation.State == LotAllocationState.FAILED){
                allocation.Free();
            }
        }

        /// <summary>
        /// Simply removes the existing allocation and frees the lot claim.
        /// </summary>
        /// <param name="lotId"></param>
        public void TryClose(int lotId, uint claimId)
        {
            uint? location = null;
            if (claimId != 0)
            {
                using (var da = DAFactory.Get())
                {
                    var lot = da.Lots.Get(lotId);
                    if (lot != null)
                    {
                        location = lot.location;
                        if (lot.owner_id == null) da.Lots.Delete(lotId); //this lot should no longer exist.
                    }
                }
            }
            else
            {
                //job lot. there is no claim, no db lot. Id is the location.
                location = (uint)lotId;
            }

            if (location == null)
            {
                return;
            }

            var allocation = Remove(location ?? 0);
            if (allocation != null)
            {
                lock (allocation)
                {
                    allocation.State = LotAllocationState.FAILED;
                    //kill this allocation
                    //TODO: is this safe? should correctly interrupt in-progress allocations, but shouldn't get here in that case anyways
                }
                allocation.TryUnclaim();
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
        /// <param name="avatarId">The id of the avatar opening this lot. If 0, we're opening for a scheduled cleanup. (lot start fresh)</param>
        /// <param name="openIfClosed"></param>
        /// <returns></returns>

        private Task<TryFindLotResult> TryFind(uint lotId, uint avatarId, bool openIfClosed, ISecurityContext security)
        {
            bool jobLot = false;
            var originalId = lotId;
            if (lotId > 0x200 && lotId < 0x300)
            {
                //special: join available job lot instance
                lotId = Matchmaker.TryGetJobLot(lotId, avatarId) ?? 0;
                jobLot = true;
                if (lotId == 0) return Immediate(new TryFindLotResult
                {
                    Status = FindLotResponseStatus.NO_CAPACITY
                });
            }
            if (lotId > 0x200 && lotId < 0x10000)
            { //job lot range
                lotId |= 0x40000000;
                jobLot = true;
            }

            var allocation = Get(lotId);
            lock (allocation)
            {
                switch (allocation.State)
                {
                    case LotAllocationState.NOT_ALLOCATED:
                        //We need to pick a server to run this lot
                        if (!openIfClosed){
                            //Sorry, cant do this
                            Remove(lotId);
                            return Immediate(new TryFindLotResult
                            {
                                Status = FindLotResponseStatus.NOT_OPEN
                            });
                        }

                        if (!jobLot)
                        {
                            DbLot lot = null;
                            using (var db = DAFactory.Get())
                            {
                                //Convert the lot location into a lot db id
                                lot = db.Lots.GetByLocation(Context.ShardId, lotId);
                                if (lot == null)
                                {
                                    Remove(lotId);
                                    return Immediate(new TryFindLotResult
                                    {
                                        Status = FindLotResponseStatus.NO_SUCH_LOT
                                    });
                                }

                                if (avatarId != 0)
                                {
                                    var roomies = db.Roommates.GetLotRoommates(lot.lot_id);
                                    var modState = db.Avatars.GetModerationLevel(avatarId);
                                    var avatars = new List<uint>();
                                    foreach (var roomie in roomies)
                                    {
                                        if (roomie.is_pending == 0) avatars.Add(roomie.avatar_id);
                                    }

                                    try
                                    {
                                        if (lot.admit_mode < 4 && modState == 0) security.DemandAvatars(avatars, AvatarPermissions.WRITE);
                                    }
                                    catch (Exception ex)
                                    {
                                        Remove(lotId);
                                        return Immediate(new TryFindLotResult
                                        {
                                            Status = FindLotResponseStatus.NOT_PERMITTED_TO_OPEN
                                        });
                                    }
                                }
                            }

                            if (!allocation.TryClaim(lot))
                            {

                                Remove(lotId);
                                return Immediate(new TryFindLotResult
                                {
                                    Status = FindLotResponseStatus.CLAIM_FAILED
                                });
                            }
                            allocation.SetLot(lot, 0,
                                (avatarId == 0) ? ClaimAction.LOT_CLEANUP : ClaimAction.LOT_HOST);
                        }
                        else { 
                            allocation.SetLot(new DbLot() { lot_id = (int)lotId }, originalId,
                                (avatarId == 0)? ClaimAction.LOT_CLEANUP : ClaimAction.LOT_HOST);
                        }

                        var pick = PickingEngine.PickServer();
                        if(pick.Success == false)
                        {
                            //Release claim
                            allocation.TryUnclaim();
                            Remove(lotId);
                            return Immediate(new TryFindLotResult
                            {
                                Status = FindLotResponseStatus.NO_CAPACITY
                            });
                        }else{
                            return allocation.BeginPick(pick).ContinueWith(x => {
                                if (x.IsFaulted || x.IsCanceled || x.Result.Accepted == false)
                                {
                                    Remove(lotId);
                                    return new TryFindLotResult
                                    {
                                        Status = FindLotResponseStatus.NO_CAPACITY
                                    };
                                }

                                return new TryFindLotResult {
                                    Status = FindLotResponseStatus.FOUND,
                                    Server = allocation.Server,
                                    LotDbId = allocation.LotDbId,
                                    LotId = lotId,
                                };
                            });
                        }
                        break;

                    case LotAllocationState.ALLOCATING:
                        break;

                    case LotAllocationState.ALLOCATED:
                        if (!jobLot && avatarId != 0)
                        {
                            //check admit type (might be expensive?)
                            using (var db = DAFactory.Get())
                            {
                                var lot = db.Lots.GetByLocation(Context.ShardId, lotId);
                                if (lot != null)
                                {
                                    if (lot.admit_mode > 0 && lot.admit_mode < 4)
                                    {
                                        //special admit mode

                                        var roomies = db.Roommates.GetLotRoommates(lot.lot_id);
                                        var modState = db.Avatars.GetModerationLevel(avatarId);
                                        var avatars = new List<uint>();
                                        foreach (var roomie in roomies) avatars.Add(roomie.avatar_id);

                                        try
                                        {
                                            if (modState == 0)
                                                security.DemandAvatars(avatars, AvatarPermissions.WRITE);
                                        }
                                        catch (Exception ex)
                                        {

                                            //if we're not a roommate, check admit rules
                                            if ((lot.admit_mode == 1 && !db.LotAdmit.GetLotAdmitDeny(lot.lot_id, 0).Contains(avatarId)) //admit list
                                                || (lot.admit_mode == 2 && db.LotAdmit.GetLotAdmitDeny(lot.lot_id, 1).Contains(avatarId)) //ban list 
                                                || (lot.admit_mode == 3)) //ban all
                                            {
                                                return Immediate(new TryFindLotResult
                                                {
                                                    Status = FindLotResponseStatus.NO_ADMIT
                                                });
                                            }
                                        }
                                    }
                                }
                            }
                        }

                        return Immediate(new TryFindLotResult
                        {
                            Status = FindLotResponseStatus.FOUND,
                            Server = allocation.Server,
                            LotDbId = allocation.LotDbId,
                            LotId = lotId
                        });
                        
                    //Should never get here..
                    case LotAllocationState.FAILED:
                        Remove(lotId);
                        return Immediate(new TryFindLotResult {
                            Status = FindLotResponseStatus.UNKNOWN_ERROR
                        });
                }

                return Immediate(new TryFindLotResult
                {
                    Status = FindLotResponseStatus.UNKNOWN_ERROR
                });
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

        private LotAllocation Remove(uint lotId)
        {
            LotAllocation removed = null;
            if ((lotId & 0x40000000) > 0) Matchmaker.RemoveJobLot(lotId & 0x3FFFFFFF);
            _Locks.TryRemove(lotId, out removed);
            return removed;
        }
    }

    public class TryFindLotResult
    {
        public FindLotResponseStatus Status;
        public IGluonSession Server;
        public int LotDbId;
        public uint LotId;
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
        private uint SpecialId;
        private ClaimAction OpenAction;

        public LotAllocation(IDAFactory da, CityServerContext context)
        {
            Context = context;
            DAFactory = da;
        }

        public int LotDbId
        {
            get { return Lot.lot_id; }
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
            if (State == LotAllocationState.FAILED) return;
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
            if (ClaimId == null) return;
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
                    var oldClaim = db.LotClaims.GetByLotID(lot.lot_id);
                    if (oldClaim == null) return false; //what?
                    else if (oldClaim.owner == Context.Config.Call_Sign)
                    {
                        //something went wrong and this lot claim did not get freed... but the city does own it.
                        //if we got here, then there was no allocation in the city before now. 
                        //therefore the only way the city could own a lot claim and not an allocation is if it got stuck somehow
                        db.LotClaims.Delete((uint)oldClaim.claim_id, oldClaim.owner);
                        claim = db.LotClaims.TryCreate(new Database.DA.LotClaims.DbLotClaim
                        {
                            shard_id = Context.ShardId,
                            lot_id = lot.lot_id,
                            owner = Context.Config.Call_Sign
                        });
                        ClaimId = claim;

                        if (claim.HasValue) return true;
                        else return false;
                    }
                    else return false;
                }
            }
        }

        public void SetLot(DbLot lot, uint specialId, ClaimAction openAction)
        {
            Lot = lot;
            SpecialId = specialId;
            OpenAction = openAction;
        }

        public void TryUnclaim()
        {
            if (ClaimId.HasValue)
            {
                using (var db = DAFactory.Get())
                {
                    try {
                        db.LotClaims.Delete(ClaimId.Value, Context.Config.Call_Sign);
                    }
                    catch (Exception)
                    {
                        //we weren't allowed to remove this anyways - probably not ours.
                    }
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
                    Type = ClaimType.LOT,
                    Action = OpenAction,
                    //x,y used as id for lots
                    EntityId = Lot.lot_id,
                    SpecialId = SpecialId,
                    ClaimId = ClaimId ?? 0,
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
