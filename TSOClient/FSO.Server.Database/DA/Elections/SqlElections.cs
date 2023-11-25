using System;
using System.Collections.Generic;
using System.Linq;
using Dapper;
using FSO.Server.Common;

namespace FSO.Server.Database.DA.Elections
{
    public class SqlElections : AbstractSqlDA, IElections
    {
        public SqlElections(ISqlContext context) : base(context)
        {
        }

        public DbElectionCandidate GetCandidate(uint avatar_id, uint cycle_id, DbCandidateState state)
        {
            return Context.Connection.Query<DbElectionCandidate>("SELECT * FROM fso_election_candidates WHERE candidate_avatar_id = @avatar_id " +
                "AND election_cycle_id = @cycle_id AND state = @state",
                new { avatar_id, cycle_id, state = state.ToString() }).FirstOrDefault();
        }

        public List<DbElectionCandidate> GetCandidates(uint cycle_id, DbCandidateState state)
        {
            return Context.Connection.Query<DbElectionCandidate>("SELECT * FROM fso_election_candidates WHERE election_cycle_id = @cycle_id AND state = @state", 
                new { cycle_id, state = state.ToString() }).ToList();
        }

        public List<DbElectionCandidate> GetCandidates(uint cycle_id)
        {
            return Context.Connection.Query<DbElectionCandidate>("SELECT * FROM fso_election_candidates WHERE election_cycle_id = @cycle_id",
                new { cycle_id }).ToList();
        }

        public List<DbElectionCycle> GetActiveCycles(int shard_id)
        {
            
            return Context.Connection.Query<DbElectionCycle>("SELECT *, n.neighborhood_id AS 'nhood_id' " +
                "FROM fso_election_cycles JOIN fso_neighborhoods n ON election_cycle_id = cycle_id " +
                "WHERE current_state != 'shutdown' " +
                "AND cycle_id IN (SELECT election_cycle_id FROM fso_neighborhoods WHERE shard_id = @shard_id)",
                new { shard_id }).ToList();
        }

        public DbElectionCycle GetCycle(uint cycle_id)
        {
            return Context.Connection.Query<DbElectionCycle>("SELECT * FROM fso_election_cycles WHERE cycle_id = @cycle_id",
                new { cycle_id = cycle_id }).FirstOrDefault();
        }

        public List<DbElectionVote> GetCycleVotes(uint cycle_id, DbElectionVoteType type)
        {
            return Context.Connection.Query<DbElectionVote>("SELECT * FROM fso_election_votes WHERE election_cycle_id = @cycle_id AND type = @type",
                new { cycle_id = cycle_id, type = type.ToString() }).ToList();
        }

        public List<DbElectionVote> GetCycleVotesForAvatar(uint avatar_id, uint cycle_id, DbElectionVoteType type)
        {
            return Context.Connection.Query<DbElectionVote>("SELECT * FROM fso_election_votes WHERE election_cycle_id = @cycle_id AND type = @type "
                + "AND target_avatar_id = @avatar_id", 
                new { avatar_id = avatar_id, cycle_id = cycle_id, type = type.ToString() }).ToList();
        }

        public DbElectionVote GetMyVote(uint avatar_id, uint cycle_id, DbElectionVoteType type)
        {
            //this isn't as straightforward as you might think.
            //we also need to include votes from:
            // - other sims on our user
            // - other sims on other users that share login ip
            //note that this criteria is duplicated in the database in the form of a BEFORE INSERT check.

            var query = "SELECT * from fso_election_votes v INNER JOIN fso_avatars va ON v.from_avatar_id = va.avatar_id " +
                "WHERE v.election_cycle_id = @cycle_id AND v.type = @type AND va.user_id IN " +
                "(SELECT user_id FROM fso_users WHERE last_ip = " +
                    "(SELECT last_ip FROM fso_avatars a JOIN fso_users u on a.user_id = u.user_id WHERE avatar_id = @avatar_id)" +
                ")";

            return Context.Connection.Query<DbElectionVote>(query,
                new { avatar_id = avatar_id, cycle_id = cycle_id, type = type.ToString() }).FirstOrDefault();
        }

        public bool CreateCandidate(DbElectionCandidate candidate)
        {
            try
            {
                var result = Context.Connection.Execute("INSERT INTO fso_election_candidates (election_cycle_id, candidate_avatar_id, comment) "
                    + "VALUES (@election_cycle_id, @candidate_avatar_id, @comment)", candidate);
                return (result > 0);
            }
            catch (Exception)
            {
                return false;
            }
        }

        public bool SetCandidateState(DbElectionCandidate candidate)
        {
            try
            {
                var result = Context.Connection.Execute("UPDATE fso_election_candidates SET state = @state, comment = @comment " +
                    "WHERE election_cycle_id = @election_cycle_id AND candidate_avatar_id = @candidate_avatar_id"
                    , new { state = candidate.state.ToString(), candidate.comment,
                            candidate.election_cycle_id, candidate.candidate_avatar_id});
                return (result > 0);
            }
            catch (Exception)
            {
                return false;
            }
        }

        public bool DeleteCandidate(uint election_cycle_id, uint candidate_avatar_id)
        {
            try
            {
                var result = Context.Connection.Execute("DELETE FROM fso_election_candidates " +
                    "WHERE election_cycle_id = @election_cycle_id AND candidate_avatar_id = @candidate_avatar_id",
                    new { election_cycle_id = election_cycle_id, candidate_avatar_id = candidate_avatar_id });
                return (result > 0);
            }
            catch (Exception)
            {
                return false;
            }
        }

        public uint CreateCycle(DbElectionCycle cycle)
        {
            var result = Context.Connection.Query<uint>("INSERT INTO fso_election_cycles (start_date, end_date, current_state, election_type) "
                + "VALUES (@start_date, @end_date, @current_state, @election_type); SELECT LAST_INSERT_ID();", 
                new { cycle.start_date, cycle.end_date,
                    current_state = cycle.current_state.ToString(),
                    election_type = cycle.election_type.ToString() }).FirstOrDefault();
            return result;
        }

        public bool CreateVote(DbElectionVote vote)
        {
            try
            {
                var result = Context.Connection.Execute("INSERT INTO fso_election_votes (election_cycle_id, from_avatar_id, type, target_avatar_id, date) "
                    + "VALUES (@election_cycle_id, @from_avatar_id, @type, @target_avatar_id, @date)", new
                    {
                        vote.election_cycle_id,
                        vote.from_avatar_id,
                        type = vote.type.ToString(),
                        vote.target_avatar_id,
                        vote.date
                    });
                return (result > 0);
            }
            catch (Exception)
            {
                return false;
            }
        }

        public void UpdateCycleState(uint cycle_id, DbElectionCycleState state)
        {
            Context.Connection.Query<DbElectionVote>("UPDATE fso_election_cycles SET current_state = @state WHERE cycle_id = @cycle_id",
                new { cycle_id = cycle_id, state = state.ToString() }).ToList();
        }

        public DbMayorRating GetSpecificRating(uint from_user_id, uint to_avatar_id)
        {
            return Context.Connection.Query<DbMayorRating>("SELECT * FROM fso_mayor_ratings WHERE from_user_id = @from_user_id "
                + "AND to_avatar_id = @to_avatar_id",
                new { from_user_id = from_user_id, to_avatar_id = to_avatar_id }).FirstOrDefault();
        }

        public DbMayorRating GetRating(uint rating_id)
        {
            return Context.Connection.Query<DbMayorRating>("SELECT * FROM fso_mayor_ratings WHERE rating_id = @rating_id ",
                new { rating_id = rating_id }).FirstOrDefault();
        }


        public List<uint> GetRatings(uint to_avatar_id)
        {
            return Context.Connection.Query<uint>("SELECT rating_id FROM fso_mayor_ratings WHERE to_avatar_id = @to_avatar_id ORDER BY rating",
                new { to_avatar_id = to_avatar_id }).ToList();
        }

        public float? GetAvgRating(uint to_avatar_id)
        {
            return Context.Connection.Query<float?>("SELECT AVG(CAST(rating as DECIMAL(10,6))) FROM fso_mayor_ratings WHERE to_avatar_id = @to_avatar_id",
                new { to_avatar_id = to_avatar_id }).FirstOrDefault();
        }

        public uint SetRating(DbMayorRating rating)
        {
            //first let's try insert our rating.
            try
            {
                var result = Context.Connection.Execute("INSERT INTO fso_mayor_ratings (from_user_id, to_user_id, rating, comment, "
                    + "date, from_avatar_id, to_avatar_id, anonymous, neighborhood) "
                    + "VALUES (@from_user_id, @to_user_id, @rating, @comment, "
                    + "@date, @from_avatar_id, @to_avatar_id, @anonymous, @neighborhood)", rating);
                return 0;
            }
            catch (Exception)
            {
                //didn't work? probably because the rating is already there. try updating instead.

                Context.Connection.Execute("UPDATE fso_mayor_ratings SET rating = @rating, comment = @comment, date = @date "
                   + "WHERE from_user_id = @from_user_id AND to_user_id = @to_user_id", rating);

                var id = Context.Connection.Query<uint>("SELECT rating_id FROM fso_mayor_ratings WHERE from_user_id = @from_user_id "
                + "AND to_user_id = @to_user_id", rating).FirstOrDefault();

                return id;
            }
        }

        public bool DeleteRating(uint id)
        {
            var result = Context.Connection.Execute("DELETE FROM fso_mayor_ratings " +
                "WHERE rating_id = @id", new { id = id });
            return (result > 0);
        }

        public bool EmailRegistered(DbElectionCycleMail p)
        {
            return Context.Connection.Query<int>("SELECT count(*) FROM fso_election_cyclemail WHERE cycle_id = @cycle_id AND avatar_id = @avatar_id AND cycle_state = @cycle_state", 
                new { p.avatar_id, p.cycle_id, cycle_state = p.cycle_state.ToString() }).First() > 0;
        }

        public bool TryRegisterMail(DbElectionCycleMail p)
        {
            try
            {
                return (Context.Connection.Execute("INSERT INTO fso_election_cyclemail (cycle_id, avatar_id, cycle_state) VALUES (@cycle_id, @avatar_id, @cycle_state)",
                    new { p.avatar_id, p.cycle_id, cycle_state = p.cycle_state.ToString() }) > 0);
            }
            catch
            {
                //already exists, or foreign key fails
                return false;
            }
        }

        public bool EnrollFreeVote(DbElectionFreeVote entry)
        {
            try
            {
                return (Context.Connection.Execute("INSERT INTO fso_election_freevotes (avatar_id, neighborhood_id, cycle_id, date, expire_date) " +
                    "VALUES (@avatar_id, @neighborhood_id, @cycle_id, @date, @expire_date)",
                    entry) > 0);
            }
            catch
            {
                //already exists, or foreign key fails
                return false;
            }
        }

        public DbElectionFreeVote GetFreeVote(uint avatar_id)
        {
            var result = Context.Connection.Query<DbElectionFreeVote>("SELECT * FROM fso_election_freevotes WHERE avatar_id = @avatar_id",
                new { avatar_id }).FirstOrDefault();
            if (result != null && result.expire_date < Epoch.Now)
            {
                //outdated. delete and set null.
                try
                {
                    Context.Connection.Execute("DELETE FROM fso_election_freevotes WHERE avatar_id = @avatar_id", new { avatar_id });
                }
                catch (Exception)
                {
                }
                result = null;
            }
            return result;
        }

        public DbElectionWin FindLastWin(uint avatar_id)
        {
            return Context.Connection.Query<DbElectionWin>("SELECT n.neighborhood_id AS nhood_id, n.name AS nhood_name " +
                "FROM (fso_election_candidates e JOIN fso_election_cycles c ON e.election_cycle_id = c.cycle_id) " +
                "JOIN fso_neighborhoods n ON c.neighborhood_id = n.neighborhood_id " +
                "WHERE e.candidate_avatar_id = @avatar_id AND state = 'won' " +
                "ORDER BY e.election_cycle_id DESC LIMIT 1",
                new { avatar_id = avatar_id }).FirstOrDefault();
        }
    }
}
