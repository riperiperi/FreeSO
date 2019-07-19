using FSO.Common.DataService;
using FSO.Common.DataService.Model;
using FSO.Common.Security;
using FSO.Files.Formats.tsodata;
using FSO.Server.Common;
using FSO.Server.Database.DA;
using FSO.Server.Database.DA.Bulletin;
using FSO.Server.Database.DA.Elections;
using FSO.Server.Database.DA.LotVisits;
using FSO.Server.Database.DA.Neighborhoods;
using FSO.Server.Framework.Aries;
using FSO.Server.Framework.Voltron;
using FSO.Server.Protocol.Gluon.Packets;
using FSO.Server.Servers.City.Handlers;
using Ninject;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FSO.Server.Servers.City.Domain
{
    public class Neighborhoods
    {
        private IDAFactory DAFactory;
        private CityServerContext Context;
        private IKernel Kernel;
        private ISessions Sessions;

        public Neighborhoods(IDAFactory daFactory, CityServerContext context, IKernel kernel, ISessions sessions)
        {
            Kernel = kernel;
            DAFactory = daFactory;
            Context = context;
            Sessions = sessions;
        }

        public void SendBulletinPost(IDA da, int nhoodID, string cst, int subjectIndex, int msgIndex, uint expireDate, params string[] args)
        {
            for (int i = 0; i < args.Length; i++)
            {
                args[i] = args[i].Replace(';', ':');
            }

            var item = new DbBulletinPost()
            {
                neighborhood_id = nhoodID,
                title = ";" + cst + ";" + subjectIndex,
                body = ";" + expireDate + ";" + cst + ";" + msgIndex + ';' + string.Join(";", args),
                date = Epoch.Now,
                flags = 0,
                type = DbBulletinType.system
            };

            da.BulletinPosts.Create(item);
        }

        public void UserJoined(IVoltronSession session)
        {
            using (var da = DAFactory.Get())
            {
                var myLotID = da.Roommates.GetAvatarsLots(session.AvatarId).FirstOrDefault();
                var myLot = (myLotID == null) ? null : da.Lots.Get(myLotID.lot_id);
                if (myLot != null)
                {
                    var free = da.Elections.GetFreeVote(session.AvatarId);
                    var nhoodID = (int)(myLot?.neighborhood_id ?? 0);
                    if (free != null)
                    {
                        nhoodID = free.neighborhood_id; //enrolled to a free vote. receive vote mail for that neighborhood
                    }

                    var myNeigh = da.Neighborhoods.Get((uint)nhoodID);
                    if (myNeigh != null && myNeigh.election_cycle_id != null)
                    {
                        var curCycle = da.Elections.GetCycle(myNeigh.election_cycle_id.Value);
                        if (curCycle != null)
                        {
                            var date = Epoch.Now;
                            if (StateHasEmail(curCycle.current_state) && date < curCycle.end_date + 3*24*60*60)
                            {
                                var mail = Kernel.Get<MailHandler>();
                                SendStateEmail(da, mail, myNeigh, curCycle, session.AvatarId);
                            }
                        }
                    }
                }
            }
        }

        public void BroadcastNhoodState(IDA da, MailHandler mail, DbNeighborhood nhood, DbElectionCycle cycle)
        {
            var all = Sessions.Clone();
            foreach (var session in all.OfType<IVoltronSession>())
            {
                if (session.IsAnonymous) continue;

                var myLotID = da.Roommates.GetAvatarsLots(session.AvatarId).FirstOrDefault();
                var myLot = (myLotID == null) ? null : da.Lots.Get(myLotID.lot_id);

                var free = da.Elections.GetFreeVote(session.AvatarId);
                var nhoodID = (int)(myLot?.neighborhood_id ?? 0);
                if (free != null)
                {
                    nhoodID = free.neighborhood_id; //enrolled to a free vote. receive vote mail for that neighborhood
                }

                if (myLot != null && nhoodID == nhood.neighborhood_id)
                    SendStateEmail(da, mail, nhood, cycle, session.AvatarId);
            }
        }

        public void SendStateEmail(IDA da, MailHandler mail, DbNeighborhood nhood, DbElectionCycle cycle, uint avatarID)
        {
            if (!da.Elections.TryRegisterMail(new DbElectionCycleMail()
            {
                avatar_id = avatarID,
                cycle_id = cycle.cycle_id,
                cycle_state = cycle.current_state
            }))
            {
                return;
            }

            var endDate = cycle.end_date;
            switch (cycle.current_state)
            {
                case DbElectionCycleState.nomination:
                    endDate -= 60 * 60 * 24 * 3; //nomination ends 3 days before end of cycle
                    mail.SendSystemEmail("f116", (int)NeighMailStrings.NominateSubject, (int)NeighMailStrings.Nominate,
                        1, MessageSpecialType.Nominate, endDate, avatarID, nhood.name, endDate.ToString());
                    break;
                case DbElectionCycleState.election:
                    mail.SendSystemEmail("f116", (int)NeighMailStrings.VoteSubject, (int)NeighMailStrings.Vote,
                        1, MessageSpecialType.Vote, endDate, avatarID, nhood.name, endDate.ToString());
                    break;
                case DbElectionCycleState.failsafe:
                    mail.SendSystemEmail("f116", (int)NeighMailStrings.FailsafeSubject, (int)NeighMailStrings.Failsafe,
                        1, MessageSpecialType.Normal, endDate, avatarID, nhood.name);
                    break;

                case DbElectionCycleState.shutdown:
                    if (Context.Config.Neighborhoods.Election_Free_Vote)
                    {
                        mail.SendSystemEmail("f116", (int)NeighMailStrings.FreeVoteSubject, (int)NeighMailStrings.FreeVote,
                        1, MessageSpecialType.FreeVote, endDate, avatarID, nhood.name, endDate.ToString());
                    }
                    break;

                case DbElectionCycleState.ended:
                    var winner = da.Avatars.Get(nhood.mayor_id ?? 0)?.name;
                    if (winner == null) return;

                    mail.SendSystemEmail("f116", (int)NeighMailStrings.ElectionOverSubject, (int)NeighMailStrings.ElectionOver,
                        1, MessageSpecialType.Normal, endDate, avatarID, winner, nhood.name, "someone else idk");
                    break;
            }
        }

        public bool StateHasEmail(DbElectionCycleState state)
        {
            return state == DbElectionCycleState.nomination || state == DbElectionCycleState.election
                || state == DbElectionCycleState.ended || state == DbElectionCycleState.failsafe 
                || (state == DbElectionCycleState.shutdown && Context.Config.Neighborhoods.Election_Free_Vote);
        }

        public Task TickNeighborhoods()
        {
            return TickNeighborhoods(DateTime.UtcNow.AddSeconds(LoadCheatOffset()));
        }

        public void SaveCheatOffset(uint offset)
        {
            try
            {
                using (var str = File.Open("nhoodCheat.txt", FileMode.Create, FileAccess.Write, FileShare.None))
                {
                    using (var writer = new StreamWriter(str))
                        writer.Write(offset.ToString());
                }
            }
            catch
            {

            }
        }

        public uint LoadCheatOffset()
        {
            uint offset = 0;
            try
            {
                if (File.Exists("nhoodCheat.txt"))
                {
                    uint.TryParse(File.ReadAllText("nhoodCheat.txt"), out offset);
                }
            }
            catch
            {

            }
            return offset;
        }

        /**
         * Given the first day in the next month, find the monday after the final week of the last month.
         */
        private DateTime FindLastWeek(DateTime date)
        {
            var lastday = date.AddDays(-1); //move back into this month
            var weekStart = lastday.AddDays(-(((int)lastday.DayOfWeek + 6) % 7)); //move back to monday
            return weekStart.AddDays(7); //monday next week. (the date that we end the election for good)
        }

        public async Task TickNeighborhoods(DateTime now)
        {
            var config = Context.Config.Neighborhoods;
            //process the neighbourhoods for this city
            var endDate = new DateTime(now.Year, now.Month, 1).AddMonths(1);
            if (config.Election_Week_Align)
            {
                endDate = FindLastWeek(endDate);
            }
            var timeToNextMonth = (endDate - now);
            var ds = Kernel.Get<IDataService>();

            var epochNow = Epoch.FromDate(now);

            using (var da = DAFactory.Get())
            {
                var midnight = LotVisitUtils.Midnight(); //gets this morning's midnight (basically current date, with day reset)
                var activityBeginning = midnight - new TimeSpan(7, 0, 0, 0);

                var range = da.LotVisits.StreamBetweenPlusNhood(Context.ShardId, activityBeginning, midnight);
                var enumerator = range.GetEnumerator();

                var nhoodHours = new Dictionary<uint, double>();

                while (enumerator.MoveNext())
                {
                    var visit = enumerator.Current;
                    var span = LotVisitUtils.CalculateDateOverlap(activityBeginning, midnight, visit.time_created, visit.time_closed.Value);
                    if (nhoodHours.ContainsKey(visit.neighborhood_id))
                    {
                        nhoodHours[visit.neighborhood_id] += span.TotalMinutes;
                    }
                    else
                    {
                        nhoodHours.Add(visit.neighborhood_id, span.TotalMinutes);
                    }
                }

                //
                var nhoodOrder = nhoodHours.OrderByDescending(x => x.Value);

                var nhoods = da.Neighborhoods.All(Context.ShardId);

                foreach (var nhood in nhoods)
                {
                    var nhoodDS = await ds.Get<Neighborhood>((uint)nhood.neighborhood_id);
                    if (nhoodDS == null) continue;
                    //placement within the top neighbourhoods for activity
                    var placement = nhoodOrder.ToList().FindIndex(x => x.Key == nhood.neighborhood_id);
                    if (placement == -1) placement = nhoods.Count;

                    nhoodDS.Neighborhood_ActivityRating = (uint)placement + 1;

                    //is there an active cycle for this neighbourhood?
                    var stillActive = false;
                    if (nhood.election_cycle_id != null)
                    {
                        var cycle = da.Elections.GetCycle(nhood.election_cycle_id.Value);
                        if (cycle != null)
                        {
                            if (cycle.current_state == DbElectionCycleState.shutdown 
                                || cycle.current_state == DbElectionCycleState.failsafe)
                            {
                                long timeToEnd = (long)cycle.end_date - epochNow;
                                if (timeToEnd <= 0 && nhood.mayor_id != null)
                                {
                                    await SetMayor(da, 0, (uint)nhood.neighborhood_id);
                                }
                                stillActive = epochNow < cycle.end_date; //can't switch eligibility til we end
                            }
                            else if (cycle.current_state < DbElectionCycleState.ended)
                            { 
                                var active = (epochNow >= cycle.start_date && epochNow < cycle.end_date);

                                long timeToEnd = (long)cycle.end_date - epochNow;

                                DbElectionCycleState targetState;
                                if (timeToEnd <= 0)
                                    targetState = DbElectionCycleState.ended;
                                else if (timeToEnd <= 60 * 60 * 24 * 3) //last 3 days are the full election
                                    targetState = DbElectionCycleState.election;
                                else //all other time is the nomination
                                    targetState = DbElectionCycleState.nomination;

                                if (targetState != cycle.current_state)
                                {
                                    await ChangeElectionState(da, nhood, cycle, targetState);
                                    nhoodDS.Neighborhood_ElectionCycle.ElectionCycle_CurrentState = (byte)targetState;
                                }
                                //important: if we are in failsafe mode we can't switch eligibility or start a new cycle.
                                if (cycle.current_state != DbElectionCycleState.ended) stillActive = true;
                            }
                        }
                    }

                    //do we need to start a new cycle?
                    if (!stillActive && timeToNextMonth.TotalDays < 7 && (nhood.flag & 1) == 0)
                    {
                        //update eligibility
                        if ((nhood.flag & 2) > 0)
                        {
                            //not eligibile for elections (right now, at least)
                            //is our placement within bounds?
                            if (placement != -1 && placement < config.Mayor_Elegibility_Limit)
                            {
                                //make us eligible.
                                nhood.flag &= ~(uint)2;
                                nhoodDS.Neighborhood_Flag = nhood.flag;
                                da.Neighborhoods.UpdateFlag((uint)nhood.neighborhood_id, nhood.flag);

                                SendBulletinPost(da, nhood.neighborhood_id, "f123", (int)NeighBulletinStrings.ElectionBeginSubject, (int)NeighBulletinStrings.ElectionBegin,
                                    0, nhood.name, config.Mayor_Elegibility_Limit.ToString());
                            }
                            else if (Context.Config.Neighborhoods.Election_Free_Vote)
                            {
                                //still ineligible for elections, but we need to tell resdients they are eligible for a free vote
                                var cycle = da.Elections.GetCycle(nhood.election_cycle_id ?? 0);
                                if (cycle == null || cycle.end_date < epochNow)
                                {
                                    //free vote needs to start another shutdown cycle, so we can keep track of reminder emails sent to residents
                                    //this will set stillActive to true for the rest of the election cycle (time to next month < 7), 
                                    //so we won't get back here til next cycle
                                    var dbCycle = new DbElectionCycle
                                    {
                                        current_state = DbElectionCycleState.shutdown,
                                        election_type = DbElectionCycleType.shutdown,
                                        start_date = Epoch.FromDate(midnight),
                                        end_date = Epoch.FromDate(endDate)
                                    };

                                    var cycleID = da.Elections.CreateCycle(dbCycle);
                                    nhoodDS.Neighborhood_ElectionCycle = new ElectionCycle()
                                    {
                                        ElectionCycle_CurrentState = (byte)dbCycle.current_state,
                                        ElectionCycle_ElectionType = (byte)dbCycle.election_type,
                                        ElectionCycle_StartDate = dbCycle.start_date,
                                        ElectionCycle_EndDate = dbCycle.end_date
                                    };
                                    da.Neighborhoods.UpdateCycle((uint)nhood.neighborhood_id, cycleID);
                                }
                            }
                        }
                        else
                        {
                            //is our placement outwith bounds?
                            if (placement == -1 || placement >= config.Mayor_Elegilility_Falloff)
                            {
                                //make us ineligible.
                                nhood.flag |= 2;
                                nhoodDS.Neighborhood_Flag = nhood.flag;
                                da.Neighborhoods.UpdateFlag((uint)nhood.neighborhood_id, nhood.flag);

                                //start a shutdown cycle
                                var dbCycle = new DbElectionCycle
                                {
                                    current_state = DbElectionCycleState.shutdown,
                                    election_type = DbElectionCycleType.shutdown,
                                    start_date = Epoch.FromDate(midnight),
                                    end_date = Epoch.FromDate(endDate)
                                };
                                var cycleID = da.Elections.CreateCycle(dbCycle);
                                nhoodDS.Neighborhood_ElectionCycle = new ElectionCycle()
                                {
                                    ElectionCycle_CurrentState = (byte)dbCycle.current_state,
                                    ElectionCycle_ElectionType = (byte)dbCycle.election_type,
                                    ElectionCycle_StartDate = dbCycle.start_date,
                                    ElectionCycle_EndDate = dbCycle.end_date
                                };
                                da.Neighborhoods.UpdateCycle((uint)nhood.neighborhood_id, cycleID);

                                SendBulletinPost(da, nhood.neighborhood_id, "f123", (int)NeighBulletinStrings.ElectionCancelledSubject, (int)NeighBulletinStrings.ElectionCancelled,
                                    0, nhood.name);
                            }
                        }

                        var eligible = (nhood.flag & 2) == 0;

                        if (eligible || nhood.election_cycle_id == null)
                        {
                            //yes
                            var dbCycle = new DbElectionCycle
                            {
                                current_state = (eligible) ? DbElectionCycleState.nomination : DbElectionCycleState.shutdown,
                                election_type = (eligible) ? DbElectionCycleType.election : DbElectionCycleType.shutdown,
                                start_date = Epoch.FromDate(midnight),
                                end_date = Epoch.FromDate(endDate)
                            };
                            var cycleID = da.Elections.CreateCycle(dbCycle);

                            nhoodDS.Neighborhood_ElectionCycle = new ElectionCycle()
                            {
                                ElectionCycle_CurrentState = (byte)dbCycle.current_state,
                                ElectionCycle_ElectionType = (byte)dbCycle.election_type,
                                ElectionCycle_StartDate = dbCycle.start_date,
                                ElectionCycle_EndDate = dbCycle.end_date
                            };
                            da.Neighborhoods.UpdateCycle((uint)nhood.neighborhood_id, cycleID);

                            if (eligible)
                            {
                                //notify current mayor
                                if (nhood.mayor_id != null)
                                {
                                    var mail = Kernel.Get<MailHandler>();
                                    mail.SendSystemEmail("f116", (int)NeighMailStrings.TermLengthSubject, (int)NeighMailStrings.TermLength,
                                        1, MessageSpecialType.Nominate, dbCycle.end_date, nhood.mayor_id.Value, nhood.name, dbCycle.end_date.ToString());
                                }

                                //post to bulletin
                                var nomEndDate = dbCycle.end_date - 60 * 60 * 24 * 3; //nomination ends 3 days before end of cycle
                                SendBulletinPost(da, nhood.neighborhood_id, "f123", (int)NeighBulletinStrings.NominateSubject, (int)NeighBulletinStrings.Nominate, nomEndDate,
                                    nhood.name, nomEndDate.ToString());
                            }
                        }
                    }
                }
            }
        }

        private bool VerifyCanBeMayor(IDA da, uint avatar_id, uint nhood_id, uint now, ref string name)
        {
            //avatar must exist, their lot must be in the neighbourhood. time since moving is checked when sims try to nominate in the first place

            var ava = da.Avatars.Get(avatar_id);

            name = ava.name;
            if (ava == null) return false;
            var lotid = da.Roommates.GetAvatarsLots(avatar_id).FirstOrDefault();
            if (lotid == null) return false;
            var lot = da.Lots.Get(lotid.lot_id);
            if (lot == null || lot.neighborhood_id != nhood_id) return false;

            var nhoodBan = da.Neighborhoods.GetNhoodBan(ava.user_id);
            if (nhoodBan != null) return false; //neighborhood banned user cannot be mayor.

            return true;
        }

        public async Task ChangeElectionState(IDA da, DbNeighborhood nhood, DbElectionCycle cycle, DbElectionCycleState state)
        {
            var now = Epoch.Now;
            var mail = Kernel.Get<MailHandler>();
            switch (state)
            {
                case DbElectionCycleState.nomination:
                    //start nominations for this cycle.
                    var endDate = cycle.end_date - 60 * 60 * 24 * 3; //nomination ends 3 days before end of cycle
                    SendBulletinPost(da, nhood.neighborhood_id, "f123", (int)NeighBulletinStrings.NominateSubject, (int)NeighBulletinStrings.Nominate, endDate,
                        nhood.name, endDate.ToString());

                    break;
                case DbElectionCycleState.election:
                    //end nominations. choose the candidate sims.
                    var cycleNoms = da.Elections.GetCycleVotes(cycle.cycle_id, DbElectionVoteType.nomination);
                    var toRemove = da.Elections.GetCandidates(cycle.cycle_id).ToDictionary(x => x.candidate_avatar_id);
                    if (cycleNoms.Count > 0)
                    {
                        var grouped = cycleNoms.GroupBy(x => x.target_avatar_id).OrderByDescending(x => x.Sum(y => y.value));
                        var selected = 0;

                        var candidates = new List<IGrouping<uint, DbElectionVote>>();

                        foreach (var winner in grouped)
                        {
                            //verify the winners are still alive and still in this neighborhood
                            string name = null;
                            DbElectionCandidate cand;
                            if (toRemove.TryGetValue(winner.Key, out cand) && VerifyCanBeMayor(da, winner.Key, (uint)nhood.neighborhood_id, now, ref name)) {
                                if (cand.state != DbCandidateState.running) continue; //user must have accepted their nomination
                                toRemove.Remove(winner.Key);

                                mail.SendSystemEmail("f116", (int)NeighMailStrings.RunningForMayorSubject, (int)NeighMailStrings.RunningForMayor,
                                        1, MessageSpecialType.Normal, cycle.end_date, winner.Key, nhood.name, cand.comment, cycle.end_date.ToString());

                                candidates.Add(winner);

                                if (++selected >= 5) break;
                            }
                        }

                        foreach (var remove in toRemove)
                        {
                            da.Elections.DeleteCandidate(remove.Value.election_cycle_id, remove.Value.candidate_avatar_id);

                            if (remove.Value.state == DbCandidateState.running)
                            {
                                mail.SendSystemEmail("f116", (int)NeighMailStrings.TooFewNominationsSubject, (int)NeighMailStrings.TooFewNominations,
                                    1, MessageSpecialType.Normal, cycle.end_date, remove.Key, nhood.name, cycle.end_date.ToString());
                            }
                            else
                            {
                                mail.SendSystemEmail("f116", (int)NeighMailStrings.NominationNotAcceptedSubject, (int)NeighMailStrings.NominationNotAccepted,
                                    1, MessageSpecialType.Normal, cycle.end_date, remove.Key, nhood.name, cycle.end_date.ToString());
                            }
                        }

                        if (selected == 0)
                        {
                            state = DbElectionCycleState.failsafe;
                            break;
                        }

                        //email will be sent by event system. make a bulletin post too
                        SendBulletinPost(da, nhood.neighborhood_id, "f123", (int)NeighBulletinStrings.VoteSubject, (int)NeighBulletinStrings.Vote, cycle.end_date,
                            nhood.name, string.Join("\n", candidates.Select(x => "- " + (da.Avatars.Get(x.Key)?.name ?? "(unknown)"))), cycle.end_date.ToString()); 
                    }
                    else
                    {
                        state = DbElectionCycleState.failsafe;
                    }
                    break;
                case DbElectionCycleState.ended:
                    //end election. if there are any candidates, set the mayor to the one that recieved the most votes.
                    var cycleVotes = da.Elections.GetCycleVotes(cycle.cycle_id, DbElectionVoteType.vote);
                    if (cycleVotes.Count > 0)
                    {
                        var grouped = cycleVotes.GroupBy(x => x.target_avatar_id).OrderByDescending(x => x.Sum(y => y.value)).ToList();

                        //verify the winner is still alive and still in this neighborhood
                        string name = "";
                        while (grouped.Count > 0 && !VerifyCanBeMayor(da, grouped[0].Key, (uint)nhood.neighborhood_id, now, ref name))
                        {
                            grouped.RemoveAt(0);
                        }

                        if (grouped.Count == 0)
                        {
                            state = DbElectionCycleState.failsafe;
                            if (nhood.mayor_id != null) await SetMayor(da, 0, (uint)nhood.neighborhood_id);
                            break;
                        }

                        var winner = grouped[0];
                        //we have a winner
                        da.Elections.SetCandidateState(new DbElectionCandidate()
                        {
                            election_cycle_id = cycle.cycle_id,
                            candidate_avatar_id = winner.Key,
                            state = DbCandidateState.won
                        });
                        await SetMayor(da, winner.Key, (uint)nhood.neighborhood_id);

                        mail.SendSystemEmail("f116", (int)NeighMailStrings.YouWinSubject, (int)NeighMailStrings.YouWin,
                            1, MessageSpecialType.Normal, 0, winner.Key, nhood.name, winner.Count().ToString());

                        //tell the losers they lost
                        grouped.RemoveAt(0);
                        var placement = 2;
                        foreach (var loser in grouped)
                        {
                            da.Elections.SetCandidateState(new DbElectionCandidate()
                            {
                                election_cycle_id = cycle.cycle_id,
                                candidate_avatar_id = loser.Key,
                                state = DbCandidateState.lost
                            });
                            placement++;
                        }

                        int runnerI = 2;
                        SendBulletinPost(da, nhood.neighborhood_id, "f123", (int)NeighBulletinStrings.ElectionOverSubject, (int)NeighBulletinStrings.ElectionOver, cycle.end_date,
                            (da.Avatars.Get(winner.Key)?.name ?? "(unknown)"),
                            nhood.name, 
                            string.Join("\n", grouped.Select(x => (runnerI++).ToString() + ". " + (da.Avatars.Get(x.Key)?.name ?? "(unknown)"))));
                    } else
                    {
                        state = DbElectionCycleState.failsafe;
                        if (nhood.mayor_id != null) await SetMayor(da, 0, (uint)nhood.neighborhood_id);
                    }
                    break;
            }

            da.Elections.UpdateCycleState(cycle.cycle_id, state);
            if (state == DbElectionCycleState.failsafe && state != cycle.current_state)
            {
                SendBulletinPost(da, nhood.neighborhood_id, "f123", (int)NeighBulletinStrings.FailsafeSubject, (int)NeighBulletinStrings.Failsafe, 
                    0, nhood.name);
            }
            cycle.current_state = state;

            if (StateHasEmail(state))
            {
                BroadcastNhoodState(da, mail, nhood, cycle);
            }
        }

        public async Task SetMayor(IDA da, uint avatarID, uint nhoodID)
        {
            //what we need to do:

            // set the mayor in the database
            // set the mayor in the data service

            // if there is a town hall, set us as its owner
            //   if it's open, inform it of the owner change
            //   if it isn't, open the lot to vacate the old mayor's objects

            // send an award email to the winner
            // (optionally) send emails to the voters telling them the result
            var mail = Kernel.Get<MailHandler>();

            var ds = Kernel.Get<IDataService>();
            var nhood = await ds.Get<Neighborhood>(nhoodID);
            if (nhood == null) return;

            var oldMayorID = nhood.Neighborhood_MayorID;
            var oldMayor = (oldMayorID != 0) ? da.Avatars.Get(oldMayorID) : null;

            nhood.Neighborhood_MayorID = avatarID;
            nhood.Neighborhood_ElectedDate = Epoch.Now;
            uint? nAvatarID = (avatarID == 0) ? null : (uint?)avatarID;
            da.Neighborhoods.UpdateMayor(nhoodID, nAvatarID);
            if (avatarID != 0) da.Avatars.UpdateMayorNhood(avatarID, nhoodID);

            var dbNhood = da.Neighborhoods.Get(nhoodID);
            if (dbNhood == null) return;
            if (dbNhood.town_hall_id != null) {
                var lot = da.Lots.Get(dbNhood.town_hall_id.Value);
                if (lot != null)
                {
                    var lots = Kernel.Get<LotAllocations>();
                    var lotServers = Kernel.Get<LotServerPicker>();
                    da.Lots.UpdateOwner(lot.lot_id, nAvatarID);

                    //this invalidation handles a few things... any changes to the roommates vector, and the possibility of the lot being deleted
                    //(when owner id is null, the lot disappears from the city until it is deleted)
                    ds.Invalidate<FSO.Common.DataService.Model.Lot>(lot.location);

                    //if online, notify the lot
                    var lotOwned = da.LotClaims.GetByLotID(lot.lot_id);
                    if (lotOwned != null)
                    {
                        var lotServer = lotServers.GetLotServerSession(lotOwned.owner);
                        if (lotServer != null)
                        {
                            //immediately notify lot of new roommate
                            lotServer.Write(new NotifyLotRoommateChange()
                            {
                                AvatarId = avatarID,
                                LotId = lot.lot_id,
                                Change = Protocol.Gluon.Model.ChangeType.BECOME_OWNER
                            });
                        }
                    }
                    else
                    {
                        //try force the lot open
                        var result = await lots.TryFindOrOpen(lot.location, 0, NullSecurityContext.INSTANCE);
                    }
                }
            }

            if (oldMayor != null)
            {
                da.Avatars.UpdateMayorNhood(oldMayor.avatar_id, null);
                if (avatarID != 0)
                {
                    var replaced = da.Avatars.Get(avatarID);
                    if (replaced != null)
                    {
                        mail.SendSystemEmail("f116", (int)NeighMailStrings.NoLongerMayorSubject, (int)NeighMailStrings.NoLongerMayor,
                            1, MessageSpecialType.Normal, 0, oldMayorID, replaced.name, dbNhood.name);
                    }
                }
                else
                {
                    //replaced with nobody. This happens at the end of an election cycle where this neighborhood is no longer eligible.
                    mail.SendSystemEmail("f116", (int)NeighMailStrings.NoLongerMayorModSubject, (int)NeighMailStrings.NoLongerMayorMod,
                        1, MessageSpecialType.Normal, 0, oldMayorID, dbNhood.name);
                }
            }
        }
    }

    public enum NeighBulletinStrings : int
    {
        NominateSubject = 1,
        Nominate = 2,

        VoteSubject = 3,
        Vote = 4,

        ElectionOverSubject = 5,
        ElectionOver = 6,

        ElectionCancelledSubject = 7,
        ElectionCancelled = 8,

        FailsafeSubject = 9,
        Failsafe = 10,

        ElectionBeginSubject = 9,
        ElectionBegin = 10
    }
}
