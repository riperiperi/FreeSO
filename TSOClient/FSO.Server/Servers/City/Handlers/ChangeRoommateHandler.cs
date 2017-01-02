using FSO.Common.DataService;
using FSO.Server.Database.DA;
using FSO.Server.Database.DA.Lots;
using FSO.Server.Database.DA.Roommates;
using FSO.Server.Framework.Aries;
using FSO.Server.Framework.Voltron;
using FSO.Server.Protocol.Electron.Model;
using FSO.Server.Protocol.Electron.Packets;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FSO.Server.Servers.City.Handlers
{
    public class ChangeRoommateHandler
    {
        private IDAFactory DAFactory;
        private CityServerContext Context;
        private ISessions Sessions;
        private IDataService DataService;
        public ChangeRoommateHandler(ISessions sessions, IDAFactory da, CityServerContext context, IDataService dataService)
        {
            this.Sessions = sessions;
            this.DAFactory = da;
            this.Context = context;
            this.DataService = dataService;
        }

        private void Status(IVoltronSession session, ChangeRoommateResponseStatus status)
        {
            session.Write(new ChangeRoommateResponse { Type = status });
        }

        public async void Handle(IVoltronSession session, ChangeRoommateRequest packet)
        {
            try
            {
                HandleInternal(session, packet);
            }
            catch (Exception e) { }
        }

        private void HandleInternal(IVoltronSession session, ChangeRoommateRequest packet)
        {
            if (session.IsAnonymous) return;
            using (var da = DAFactory.Get())
            {
                if (packet.Type == ChangeRoommateType.POLL)
                {
                    var lots = da.Roommates.GetAvatarsLots(packet.AvatarId);
                    foreach (var lot in lots)
                    {
                        if (lot.is_pending == 1)
                        {
                            var lotdb = da.Lots.Get(lot.lot_id);
                            if (lotdb == null) return;
                            session.Write(new ChangeRoommateRequest
                            {
                                Type = ChangeRoommateType.INVITE,
                                AvatarId = lotdb.owner_id,
                                LotLocation = lotdb.location
                            });
                        }
                    }
                }
                else if (packet.Type == ChangeRoommateType.ACCEPT)
                {
                    var lot = da.Lots.GetByLocation(Context.ShardId, packet.LotLocation);
                    if (lot == null) { Status(session, ChangeRoommateResponseStatus.LOT_DOESNT_EXIST); return; }
                    if (da.Roommates.AcceptRoommateRequest(session.AvatarId, lot.lot_id))
                    {
                        //todo: notify open lot
                        //todo: update data service for avatar and lot
                        Status(session, ChangeRoommateResponseStatus.ACCEPT_SUCCESS); return;
                    }
                    else
                    {
                        Status(session, ChangeRoommateResponseStatus.NO_INVITE_PENDING); return;
                    }
                }
                else if (packet.Type == ChangeRoommateType.DECLINE)
                {
                    var lot = da.Lots.GetByLocation(Context.ShardId, packet.LotLocation);
                    if (lot == null) { Status(session, ChangeRoommateResponseStatus.LOT_DOESNT_EXIST); return; }
                    if (da.Roommates.DeclineRoommateRequest(session.AvatarId, lot.lot_id))
                    {
                        Status(session, ChangeRoommateResponseStatus.DECLINE_SUCCESS); return;
                    }
                    else
                    {
                        Status(session, ChangeRoommateResponseStatus.NO_INVITE_PENDING); return;
                    }
                }
                else
                {
                    //verify that requester is definitely a roommate in the target lot

                    var ownedLot = da.Lots.GetByOwner(session.AvatarId);
                    var myLots = da.Roommates.GetAvatarsLots(session.AvatarId);
                    
                    if (packet.Type == ChangeRoommateType.INVITE)
                    {
                        //is invitee roommate somewhere else? count lot roommates and check for max
                        var targ = da.Avatars.Get(packet.AvatarId);
                        if (targ == null)
                        {
                            Status(session, ChangeRoommateResponseStatus.UNKNOWN);
                        }
                        var targLots = da.Roommates.GetAvatarsLots(packet.AvatarId);
                        if (targLots.Count > 0)
                        {
                            Status(session, ChangeRoommateResponseStatus.ROOMIE_ELSEWHERE); //request already pending or otherwise
                            return;
                        }
                        var lotr = myLots.FirstOrDefault();
                        DbLot lot = null;
                        if (lotr != null) lot = da.Lots.Get(lotr.lot_id);
                        if (lotr == null || lot == null)
                        {
                            Status(session, ChangeRoommateResponseStatus.LOT_DOESNT_EXIST); //what??
                            return;
                        }
                        if (lot.owner_id != session.AvatarId) //only an owner can add roommates
                        {
                            Status(session, ChangeRoommateResponseStatus.YOU_ARE_NOT_OWNER);
                            return;
                        }
                        var myLotRoomies = da.Roommates.GetLotRoommates(lotr.lot_id);
                        if (myLotRoomies.Count >= 8)
                        {
                            //if pending roommates put us over, cancel some of them.
                            //assume first is oldest request
                            var pending = myLotRoomies.FirstOrDefault(x => x.is_pending == 1);
                            if (pending == null)
                            {
                                Status(session, ChangeRoommateResponseStatus.TOO_MANY_ROOMMATES);
                                return;
                            }
                            else
                            {
                                da.Roommates.DeclineRoommateRequest(pending.avatar_id, pending.lot_id);
                            }
                        }
                        //create roommate request in database

                        if (!da.Roommates.Create(new DbRoommate
                        {
                            avatar_id = packet.AvatarId,
                            lot_id = lotr.lot_id,
                            is_pending = 1,
                            permissions_level = 0
                        }))
                        {
                            Status(session, ChangeRoommateResponseStatus.UNKNOWN);
                            return;
                        }

                        //if online, notify roommate of pending request.
                        var targetSession = Sessions.GetByAvatarId(packet.AvatarId);
                        if (targetSession != null)
                        {
                            targetSession.Write(new ChangeRoommateRequest()
                            {
                                Type = ChangeRoommateType.INVITE,
                                AvatarId = session.AvatarId,
                                LotLocation = lot.location
                            });
                        }

                        Status(session, ChangeRoommateResponseStatus.INVITE_SUCCESS);
                        return;
                        //if not, we'll catch them when they log in later.
                    }
                    else if (packet.Type == ChangeRoommateType.KICK)
                    {
                        //if target avatar is our avatar, we are moving out

                        //if we are owner of the lot, set the new owner to the first (earliest) roommate entry in the database.
                        //if we are the last person in the lot, the lot must be closed before doing this.
                        //make sure all references are set to new owner!

                        //remove roommate entry for target avatar.
                        //update lot data service and avatar data service for targets.

                        //if lot open, notify lot server of change (roommate add/remove AND new/same owner)
                        //the lot will remove objects as necessary

                        //future: if lot closed, special request to a lot server to quickly open an unjoinable instance of the lot to remove our objects.
                    }
                }
            }
        }
    }
}
