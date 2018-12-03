using FSO.Common.DataService;
using FSO.Common.DataService.Model;
using FSO.Common.Domain.Realestate;
using FSO.Common.Domain.RealestateDomain;
using FSO.Files.Formats.tsodata;
using FSO.Server.Common;
using FSO.Server.Database.DA;
using FSO.Server.Database.DA.Avatars;
using FSO.Server.Database.DA.Elections;
using FSO.Server.Database.DA.Neighborhoods;
using FSO.Server.Framework.Gluon;
using FSO.Server.Framework.Voltron;
using FSO.Server.Protocol.Electron.Packets;
using FSO.Server.Protocol.Gluon.Packets;
using FSO.Server.Servers.City.Domain;
using Ninject;
using NLog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FSO.Server.Servers.City.Handlers
{
    public class NhoodHandler
    {
        private IRealestateDomain GlobalRealestate;
        private IShardRealestateDomain Realestate;
        private IDAFactory DA;
        private IDataService DataService;
        private CityServerContext Context;
        private IKernel Kernel;
        private Neighborhoods Nhoods;
        private static Logger LOG = LogManager.GetCurrentClassLogger();

        public NhoodHandler(CityServerContext context, IRealestateDomain realestate, IDAFactory da, IDataService dataService, IKernel kernel, Neighborhoods nhoods)
        {
            Context = context;
            GlobalRealestate = realestate;
            Realestate = realestate.GetByShard(context.ShardId);
            DA = da;
            DataService = dataService;
            Kernel = kernel;
            Nhoods = nhoods;
        }
        
        private NhoodResponse Code(NhoodResponseCode code)
        {
            return new NhoodResponse()
            {
                Code = code
            };
        }

        public async void Handle(IGluonSession session, CityNotify packet)
        {
            try
            {
                await Nhoods.TickNeighborhoods();
            } catch (Exception e)
            {
                LOG.Error("Neighborhood Task Failed: " + e.ToString());
            }
        }

        public async void Handle(IVoltronSession session, NhoodRequest packet)
        {
            if (session.IsAnonymous) //CAS users can't do this.
                return;

            var moveTime = 0; //24 * 60 * 60 * 30; //must live in an nhood 30 days before participating in an election
            var mail = Kernel.Get<MailHandler>();

            try
            {
                using (var da = DA.Get())
                {
                    var myAva = da.Avatars.Get(session.AvatarId);
                    if (myAva == null) Code(NhoodResponseCode.UNKNOWN_ERROR);

                    if (packet.Type >= NhoodRequestType.DELETE_RATE)
                    {
                        if (myAva.moderation_level == 0)
                        {
                            session.Write(Code(NhoodResponseCode.NOT_MODERATOR));
                            return;
                        }
                    }

                    //are we nhood gameplay banned?

                    var ban = da.Neighborhoods.GetNhoodBan(myAva.user_id);
                    if (ban != null)
                    {
                        session.Write(new NhoodResponse
                        {
                            Code = NhoodResponseCode.NHOOD_GAMEPLAY_BAN,
                            Message = ban.ban_reason,
                            BanEndDate = ban.end_date
                        });
                        return;
                    }

                    //common info used by most requests
                    var myLotID = da.Roommates.GetAvatarsLots(session.AvatarId).FirstOrDefault();
                    var myLot = (myLotID == null) ? null : da.Lots.Get(myLotID.lot_id);

                    switch (packet.Type)
                    {
                        //user requests
                        case NhoodRequestType.CAN_RATE:
                        case NhoodRequestType.RATE:
                            {
                                if (packet.Value > 10 || packet.Value < 0 || packet.Message == null || packet.Message.Length > 200)
                                {
                                    session.Write(Code(NhoodResponseCode.INVALID_RATING));
                                    return;
                                }
                                if (myLot == null || myLot.neighborhood_id != packet.TargetNHood)
                                {
                                    session.Write(Code(NhoodResponseCode.NOT_IN_NHOOD));
                                    return;
                                }
                                if (session.AvatarId == packet.TargetAvatar)
                                {
                                    session.Write(Code(NhoodResponseCode.CANT_RATE_AVATAR)); //you can't rate yourself...
                                    return;
                                }
                                //verify the target avatar is the current mayor
                                var rateNhood = da.Neighborhoods.Get(packet.TargetNHood);
                                //user check: ratings are technically towards individual avatars, but are shared between the same user.
                                var avaFrom = da.Avatars.Get(session.AvatarId);
                                var avaTo = da.Avatars.Get(packet.TargetAvatar);
                                if (rateNhood == null || avaFrom == null || avaTo == null)
                                {
                                    session.Write(Code(NhoodResponseCode.UNKNOWN_ERROR)); //shouldn't happen, but just in case
                                    return;
                                }
                                if (rateNhood.mayor_id != packet.TargetAvatar)
                                {
                                    session.Write(Code(NhoodResponseCode.NOT_YOUR_MAYOR)); //you can't rate yourself...
                                    return;
                                }

                                if (packet.Type == NhoodRequestType.RATE)
                                {
                                    //insert or replace rating
                                    var id = da.Elections.SetRating(new DbMayorRating()
                                    {
                                        from_avatar_id = avaFrom.avatar_id,
                                        to_avatar_id = avaTo.avatar_id,
                                        from_user_id = avaFrom.user_id,
                                        to_user_id = avaTo.user_id,
                                        comment = packet.Message,
                                        rating = packet.Value,
                                        neighborhood = packet.TargetNHood,
                                        date = Epoch.Now,
                                        anonymous = 1,
                                    });

                                    if (id != 0)
                                    {
                                        var ds = Kernel.Get<IDataService>();
                                        ds.Invalidate<MayorRating>(id); //update this rating in data service
                                        ds.Invalidate<Avatar>(packet.TargetAvatar);
                                    }
                                }

                                session.Write(Code(NhoodResponseCode.SUCCESS)); return;
                            }

                        case NhoodRequestType.CAN_NOMINATE:
                        case NhoodRequestType.NOMINATE:
                        case NhoodRequestType.CAN_VOTE:
                        case NhoodRequestType.VOTE:
                            {

                                if (myLot == null || myLot.neighborhood_id != packet.TargetNHood)
                                {
                                    session.Write(Code(NhoodResponseCode.NOT_IN_NHOOD)); return;
                                }

                                var now = Epoch.Now;
                                if (now - myAva.move_date < moveTime)
                                {
                                    session.Write(Code(NhoodResponseCode.YOU_MOVED_RECENTLY)); return;
                                }
                                
                                //check if voting cycle in correct state
                                var nhood = da.Neighborhoods.Get(packet.TargetNHood);
                                if (nhood == null)
                                {
                                    session.Write(Code(NhoodResponseCode.MISSING_ENTITY)); return;
                                }
                                var cycle = (nhood.election_cycle_id == null) ? null : da.Elections.GetCycle(nhood.election_cycle_id.Value);
                                if (cycle == null)
                                {
                                    session.Write(Code(NhoodResponseCode.ELECTION_OVER)); return;
                                }

                                var nominations = packet.Type == NhoodRequestType.CAN_NOMINATE || packet.Type == NhoodRequestType.NOMINATE;
                                if ((nominations && cycle.current_state != DbElectionCycleState.nomination)
                                    || (!nominations && cycle.current_state != DbElectionCycleState.election)) {
                                    session.Write(Code(NhoodResponseCode.BAD_STATE)); return;
                                }

                                DbAvatar targetAva = null;
                                if (packet.Type != NhoodRequestType.CAN_NOMINATE && packet.Type != NhoodRequestType.CAN_VOTE)
                                {
                                    //when we are actually nominating or voting, we have a target avatar in mind.

                                    targetAva = da.Avatars.Get(packet.TargetAvatar);
                                    if (targetAva == null)
                                    {
                                        session.Write(Code(NhoodResponseCode.INVALID_AVATAR)); return;
                                    }
                                    var targLotID = da.Roommates.GetAvatarsLots(session.AvatarId).FirstOrDefault();
                                    var targLot = (targLotID == null) ? null : da.Lots.Get(targLotID.lot_id);

                                    if (targLot == null || targLot.neighborhood_id != packet.TargetNHood)
                                    {
                                        session.Write(Code(NhoodResponseCode.CANDIDATE_NOT_IN_NHOOD)); return;
                                    }

                                    var targBan = da.Neighborhoods.GetNhoodBan(targetAva.user_id);
                                    if (targBan != null)
                                    {
                                        session.Write(Code(NhoodResponseCode.CANDIDATE_NHOOD_GAMEPLAY_BAN)); return;
                                    }

                                    if (now - targetAva.move_date < moveTime)
                                    {
                                        session.Write(Code(NhoodResponseCode.CANDIDATE_MOVED_RECENTLY)); return;
                                    }
                                }

                                //ok, we're all good.

                                if (!nominations)
                                {
                                    //have we already voted?
                                    //note: this should be double checked with a BEFORE INSERT on the table.
                                    //just in case there's some kind of race condition due to this request being duplicated.
                                    //this check is therefore just to get a better voting error message.
                                    DbElectionVote existing = da.Elections.GetMyVote(session.AvatarId, cycle.cycle_id, DbElectionVoteType.vote);

                                    if (existing != null)
                                    {
                                        if (existing.from_avatar_id != session.AvatarId)
                                            session.Write(Code(NhoodResponseCode.ALREADY_VOTED_SAME_IP)); //couldn't vote due to relation
                                        else
                                            session.Write(Code(NhoodResponseCode.ALREADY_VOTED)); 
                                    }

                                    if (packet.Type == NhoodRequestType.CAN_VOTE)
                                    {
                                        //we are allowed to vote

                                        //send back the candidate list

                                        
                                        var sims = da.Elections.GetCandidates(cycle.cycle_id);
                                        var result = new List<NhoodCandidate>();

                                        foreach (var x in sims)
                                        {
                                            if (x.state != DbCandidateState.running) continue;
                                            var ava = (await DataService.Get<Avatar>(x.candidate_avatar_id));
                                            if (ava == null) continue;

                                            var win = da.Elections.FindLastWin(x.candidate_avatar_id);
                                            result.Add(new NhoodCandidate()
                                            {
                                                ID = x.candidate_avatar_id,
                                                Name = ava.Avatar_Name,
                                                Rating = ava.Avatar_MayorRatingHundredth,
                                                Message = x.comment,
                                                LastNhoodName = win?.nhood_name ?? "",
                                                LastNhoodID = win?.nhood_id ?? 0,
                                                TermNumber = (uint)((nhood.mayor_id == x.candidate_avatar_id) ? GetTermsSince(nhood.mayor_elected_date) : 0)
                                            });
                                        }

                                        session.Write(new NhoodCandidateList()
                                        {
                                            NominationMode = false,
                                            Candidates = result.ToList()
                                        });

                                        session.Write(Code(NhoodResponseCode.SUCCESS)); return;
                                    }

                                    //extra checks. has the target user been nominated?
                                    if (!da.Elections.GetCandidates(cycle.cycle_id, DbCandidateState.running).Any(x => x.candidate_avatar_id == packet.TargetAvatar))
                                    {
                                        session.Write(Code(NhoodResponseCode.CANDIDATE_NOT_NOMINATED)); return;
                                    }

                                    //put our vote through! democracy is great.
                                    var success = da.Elections.CreateVote(new DbElectionVote()
                                    {
                                        date = Epoch.Now,
                                        election_cycle_id = cycle.cycle_id,
                                        from_avatar_id = session.AvatarId,
                                        target_avatar_id = packet.TargetAvatar,
                                        type = DbElectionVoteType.vote
                                    });
                                    if (!success)
                                    {
                                        session.Write(Code(NhoodResponseCode.ALREADY_VOTED));
                                        return;
                                    }

                                    mail.SendSystemEmail("f116", (int)NeighMailStrings.VoteCountedSubject, (int)NeighMailStrings.VoteCounted,
                                        1, MessageSpecialType.Normal, cycle.end_date, session.AvatarId, targetAva.name, nhood.name);
                                    session.Write(Code(NhoodResponseCode.SUCCESS));
                                }
                                else
                                {
                                    //have we already nominated?
                                    DbElectionVote existing = da.Elections.GetMyVote(session.AvatarId, cycle.cycle_id, DbElectionVoteType.nomination);

                                    if (existing != null)
                                    {
                                        if (existing.from_avatar_id != session.AvatarId)
                                            session.Write(Code(NhoodResponseCode.ALREADY_VOTED_SAME_IP)); //couldn't nominate due to relation
                                        else
                                            session.Write(Code(NhoodResponseCode.ALREADY_VOTED));
                                    }

                                    if (packet.Type == NhoodRequestType.CAN_NOMINATE)
                                    {
                                        //we are allowed to nominate.
                                        
                                        //send back the list of sims in nhood
                                        var sims = da.Avatars.GetPossibleCandidatesNhood((uint)packet.TargetNHood);
                                        var result = sims.Select(x => new NhoodCandidate()
                                        {
                                            ID = x.avatar_id,
                                            Name = x.name,
                                            Rating = (x.rating == null) ? uint.MaxValue: (uint)((x.rating / 2) * 100)
                                        });

                                        session.Write(new NhoodCandidateList()
                                        {
                                            NominationMode = true,
                                            Candidates = result.ToList()
                                        });

                                        session.Write(Code(NhoodResponseCode.SUCCESS)); return;
                                    }

                                    //do the nomination.
                                    var success = da.Elections.CreateVote(new DbElectionVote()
                                    {
                                        date = Epoch.Now,
                                        election_cycle_id = cycle.cycle_id,
                                        from_avatar_id = session.AvatarId,
                                        target_avatar_id = packet.TargetAvatar,
                                        type = DbElectionVoteType.nomination
                                    });
                                    if (!success)
                                    {
                                        session.Write(Code(NhoodResponseCode.ALREADY_VOTED));
                                        return;
                                    }

                                    //if >= 3 nominations, allow the player to run for election.
                                    var noms = da.Elections.GetCycleVotesForAvatar(packet.TargetAvatar, cycle.cycle_id, DbElectionVoteType.nomination);
                                    if (noms.Count() >= Nhoods.MinNominations)
                                    {
                                        var created = da.Elections.CreateCandidate(new DbElectionCandidate() {
                                            candidate_avatar_id = packet.TargetAvatar,
                                            election_cycle_id = cycle.cycle_id,
                                            comment = packet.Message,
                                            state = DbCandidateState.informed
                                        });
                                        //only send the mail if they have not accepted.
                                        if (created)
                                        {
                                            mail.SendSystemEmail("f116", (int)NeighMailStrings.NominationQuerySubject, (int)NeighMailStrings.NominationQuery,
                                                1, MessageSpecialType.AcceptNomination, cycle.end_date, packet.TargetAvatar, nhood.name, cycle.end_date.ToString());
                                        }
                                    }

                                    mail.SendSystemEmail("f116", (int)NeighMailStrings.NominationCountedSubject, (int)NeighMailStrings.NominationCounted,
                                        1, MessageSpecialType.Normal, cycle.end_date, session.AvatarId, targetAva.name, nhood.name);

                                    session.Write(Code(NhoodResponseCode.SUCCESS));
                                }

                                break;
                            }

                        case NhoodRequestType.NOMINATION_RUN:
                        case NhoodRequestType.CAN_RUN:
                            {
                                //if a user has been nominated 3 or more times, they will be asked if they would like to run for mayor by email.
                                //the email will contain a button that lets the user accept the nomination.
                                //this doesn't immediately get them running for mayor, it means that they are a valid selection from
                                //the top nominated candidates when the nominations are finalized.

                                //first check if we have the three required nominations
                                if (myLot == null || myLot.neighborhood_id != packet.TargetNHood)
                                {
                                    session.Write(Code(NhoodResponseCode.NOT_IN_NHOOD)); return;
                                }

                                var nhood = da.Neighborhoods.Get(packet.TargetNHood);
                                if (nhood == null)
                                {
                                    session.Write(Code(NhoodResponseCode.MISSING_ENTITY)); return;
                                }
                                var cycle = (nhood.election_cycle_id == null) ? null : da.Elections.GetCycle(nhood.election_cycle_id.Value);
                                if (cycle == null)
                                {
                                    session.Write(Code(NhoodResponseCode.ELECTION_OVER)); return;
                                }

                                if (cycle.current_state != DbElectionCycleState.nomination)
                                {
                                    session.Write(Code(NhoodResponseCode.BAD_STATE)); return;
                                }

                                //have we been nominated the minimum number of times? (3)
                                var noms = da.Elections.GetCycleVotesForAvatar(session.AvatarId, cycle.cycle_id, DbElectionVoteType.nomination);
                                if (noms.Count < Nhoods.MinNominations)
                                {
                                    session.Write(Code(NhoodResponseCode.NOBODY_NOMINATED_YOU_IDIOT)); return;
                                }

                                if (packet.Type == NhoodRequestType.CAN_RUN)
                                {
                                    if (da.Elections.GetCandidate(session.AvatarId, cycle.cycle_id, DbCandidateState.informed) == null)
                                        session.Write(Code(NhoodResponseCode.ALREADY_RUNNING));
                                    else
                                        session.Write(Code(NhoodResponseCode.SUCCESS));
                                }
                                else
                                {
                                    var success = da.Elections.SetCandidateState(new DbElectionCandidate()
                                    {
                                        candidate_avatar_id = session.AvatarId,
                                        election_cycle_id = cycle.cycle_id,
                                        comment = packet.Message,
                                        state = DbCandidateState.running
                                    });

                                    if (success)
                                    {
                                        mail.SendSystemEmail("f116", (int)NeighMailStrings.NominationAcceptedSubject, (int)NeighMailStrings.NominationAccepted,
                                            1, MessageSpecialType.Normal, cycle.end_date, session.AvatarId, nhood.name, cycle.end_date.ToString());

                                        session.Write(Code(NhoodResponseCode.SUCCESS));
                                    }
                                    else session.Write(Code(NhoodResponseCode.ALREADY_RUNNING));
                                }
                                return;
                            }
                        //management
                        case NhoodRequestType.DELETE_RATE:
                            if (da.Elections.DeleteRating(packet.Value))
                                session.Write(Code(NhoodResponseCode.SUCCESS));
                            else
                                session.Write(Code(NhoodResponseCode.MISSING_ENTITY));
                            return;
                        case NhoodRequestType.FORCE_MAYOR:
                            //set the mayor.
                            await Nhoods.SetMayor(da, packet.TargetAvatar, packet.TargetNHood);
                            session.Write(Code(NhoodResponseCode.SUCCESS));
                            return;
                        case NhoodRequestType.ADD_CANDIDATE:
                            //check if voting cycle in correct state
                            {
                                var nhood = da.Neighborhoods.Get(packet.TargetNHood);
                                if (nhood == null)
                                {
                                    session.Write(Code(NhoodResponseCode.MISSING_ENTITY)); return;
                                }
                                var cycle = (nhood.election_cycle_id == null) ? null : da.Elections.GetCycle(nhood.election_cycle_id.Value);
                                if (cycle == null)
                                {
                                    session.Write(Code(NhoodResponseCode.ELECTION_OVER)); return;
                                }
                                //while candidates DO have a use in nominations, adding them is largely pointless before the elections have begun
                                //(because they will likely be removed when choosing the top 5 anyways)
                                if (cycle.current_state != DbElectionCycleState.election)
                                { 
                                    session.Write(Code(NhoodResponseCode.BAD_STATE)); return;
                                }
                            }
                            break;
                        case NhoodRequestType.REMOVE_CANDIDATE:
                            //get current cycle
                            {
                                var nhood = da.Neighborhoods.Get(packet.TargetNHood);
                                if (nhood == null)
                                {
                                    session.Write(Code(NhoodResponseCode.MISSING_ENTITY)); return;
                                }
                                var cycle = (nhood.election_cycle_id == null) ? null : da.Elections.GetCycle(nhood.election_cycle_id.Value);
                                if (cycle == null)
                                {
                                    session.Write(Code(NhoodResponseCode.ELECTION_OVER)); return;
                                }

                                if (da.Elections.DeleteCandidate(cycle.cycle_id, packet.TargetAvatar))
                                    session.Write(Code(NhoodResponseCode.SUCCESS));
                                else
                                    session.Write(Code(NhoodResponseCode.INVALID_AVATAR));
                            }
                            break;
                        case NhoodRequestType.TEST_ELECTION:
                            //create an election cycle
                            {
                                var nhood = da.Neighborhoods.Get(packet.TargetNHood);
                                if (nhood == null)
                                {
                                    session.Write(Code(NhoodResponseCode.MISSING_ENTITY)); return;
                                }

                                var cycle = new DbElectionCycle()
                                {
                                    current_state = DbElectionCycleState.nomination,
                                    election_type = DbElectionCycleType.election,
                                    start_date = Epoch.Now,
                                    end_date = packet.Value
                                };
                                var cycleID = da.Elections.CreateCycle(cycle);
                                if (cycleID == 0)
                                {
                                    session.Write(Code(NhoodResponseCode.UNKNOWN_ERROR));
                                    return;
                                }
                                cycle.cycle_id = cycleID;
                                nhood.election_cycle_id = cycleID;
                                da.Neighborhoods.UpdateCycle((uint)nhood.neighborhood_id, cycleID);

                                await Nhoods.ChangeElectionState(da, nhood, cycle, (DbElectionCycleState)packet.TargetAvatar);
                                session.Write(Code(NhoodResponseCode.SUCCESS));
                                return;
                            }
                        case NhoodRequestType.PRETEND_DATE:
                            try
                            {
                                uint day = 60 * 60 * 24;
                                Nhoods.SaveCheatOffset((((packet.Value - Epoch.Now) + day-1)/day) * day);
                                await Nhoods.TickNeighborhoods(Epoch.ToDate(packet.Value));
                                session.Write(Code(NhoodResponseCode.SUCCESS));
                            }
                            catch (Exception e)
                            {
                                session.Write(new NhoodResponse()
                                {
                                    Code = NhoodResponseCode.UNKNOWN_ERROR,
                                    Message = e.ToString()
                                });
                            }
                            return;
                        case NhoodRequestType.NHOOD_GAMEPLAY_BAN:
                            {
                                var targetAva = da.Avatars.Get(packet.TargetAvatar);
                                if (targetAva == null)
                                {
                                    session.Write(Code(NhoodResponseCode.INVALID_AVATAR));
                                }
                                var success = da.Neighborhoods.AddNhoodBan(new DbNhoodBan()
                                {
                                    user_id = targetAva.user_id,
                                    ban_reason = packet.Message,
                                    end_date = packet.Value
                                });
                                if (success)
                                {
                                    mail.SendSystemEmail("f116", (int)NeighMailStrings.NeighGameplayBanSubject, (int)NeighMailStrings.NeighGameplayBan,
                                        1, MessageSpecialType.Normal, packet.Value, packet.TargetAvatar, packet.Message, packet.Value.ToString());
                                }
                                session.Write(Code(NhoodResponseCode.SUCCESS));
                                return;
                            }
                        default:
                            session.Write(Code(NhoodResponseCode.UNKNOWN_ERROR)); return;
                    }
                }
            } catch (Exception e)
            {
                LOG.Error(e.ToString());
                session?.Write(Code(NhoodResponseCode.UNKNOWN_ERROR));
            }
        }

        private int GetTermsSince(uint time)
        {
            var startDate = Epoch.ToDate(time);
            var now = DateTime.Now;

            var months = 0;
            if (now.Year != startDate.Year)
            {
                months += (((now.Year - startDate.Year) - 1) * 12 + 13 - startDate.Month);
                months += now.Month - 1;
            }
            else
            {
                months = (now.Month - startDate.Month) + 1;
            }
            return months + 1;
        }
    }
}
