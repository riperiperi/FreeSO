using Dapper;
using FSO.Server.Database.DA.Utils;
using System;
using System.Collections.Generic;
using System.Linq;

namespace FSO.Server.Database.DA.Avatars
{
    public class SqlAvatars : AbstractSqlDA, IAvatars
    {
        public SqlAvatars(ISqlContext context) : base(context){
        }
        public PagedList<DbAvatar> AllByPage(int shard_id,int offset = 1, int limit = 100, string orderBy = "avatar_id")
        {
            var total = Context.Connection.Query<int>("SELECT COUNT(*) FROM fso_avatars WHERE shard_id = @shard_id",new { shard_id = shard_id }).FirstOrDefault();
            var results = Context.Connection.Query<DbAvatar>("SELECT * FROM fso_avatars WHERE shard_id = @shard_id ORDER BY @order DESC LIMIT @offset, @limit", new { shard_id = shard_id, order = orderBy, offset = offset, limit = limit });
            return new PagedList<DbAvatar>(results, offset, total);
        }

        public IEnumerable<DbAvatar> All()
        {
            return Context.Connection.Query<DbAvatar>("SELECT * FROM fso_avatars");
        }

        public IEnumerable<DbAvatar> All(int shard_id){
            return Context.Connection.Query<DbAvatar>("SELECT * FROM fso_avatars WHERE shard_id = @shard_id", new { shard_id = shard_id });
        }

        public List<uint> GetLivingInNhood(uint neigh_id)
        {
            return Context.Connection.Query<uint>("SELECT avatar_id FROM fso_roommates r JOIN fso_lots l ON r.lot_id = l.lot_id "
                + "WHERE neighborhood_id = @neigh_id", new { neigh_id = neigh_id }).ToList();
        }
        
        public List<AvatarRating> GetPossibleCandidatesNhood(uint neigh_id)
        {
            return Context.Connection.Query<AvatarRating>("SELECT r.avatar_id, a.name, AVG(CAST(v.rating as DECIMAL(10,6))) AS rating " +
                "FROM (fso_roommates r JOIN fso_lots l ON r.lot_id = l.lot_id) " +
                "LEFT JOIN fso_mayor_ratings v ON v.to_avatar_id = r.avatar_id " +
                "JOIN fso_avatars a ON r.avatar_id = a.avatar_id " +
                "WHERE l.neighborhood_id = @neigh_id " +
                "GROUP BY avatar_id", new { neigh_id = neigh_id }).ToList();
        }

        public DbAvatar Get(uint id){
            return Context.Connection.Query<DbAvatar>("SELECT * FROM fso_avatars WHERE avatar_id = @id", new { id = id }).FirstOrDefault();
        }

        public bool Delete(uint id)
        {
            return Context.Connection.Execute("DELETE FROM fso_avatars WHERE avatar_id = @id", new { id = id }) > 0;
        }

        public int GetPrivacyMode(uint id)
        {
            return Context.Connection.Query<int>("SELECT privacy_mode FROM fso_avatars WHERE avatar_id = @id", new { id = id }).FirstOrDefault();
        }

        public int GetModerationLevel(uint id)
        {
            return Context.Connection.Query<int>("SELECT moderation_level FROM fso_avatars WHERE avatar_id = @id", new { id = id }).FirstOrDefault();
        }

        public uint Create(DbAvatar avatar)
        {
            return (uint)Context.Connection.Query<int>("INSERT INTO fso_avatars (shard_id, user_id, name, " +
                                        "gender, date, skin_tone, head, body, description, budget, moderation_level, " +
                                        " body_swimwear, body_sleepwear) " +
                                        " VALUES (@shard_id, @user_id, @name, @gender, @date, " +
                                        " @skin_tone, @head, @body, @description, @budget, @moderation_level, "+
                                        " @body_swimwear, @body_sleepwear); SELECT LAST_INSERT_ID();", new
                                        {
                                            shard_id = avatar.shard_id,
                                            user_id = avatar.user_id,
                                            name = avatar.name,
                                            gender = avatar.gender.ToString(),
                                            date = avatar.date,
                                            skin_tone = avatar.skin_tone,
                                            head = avatar.head,
                                            body = avatar.body,
                                            description = avatar.description,
                                            budget = avatar.budget,
                                            moderation_level = avatar.moderation_level,
                                            body_swimwear = avatar.body_swimwear,
                                            body_sleepwear = avatar.body_sleepwear
                                        }).First();
            //for now, everything else assumes default values.
        }


        public List<DbAvatar> GetByUserId(uint user_id)
        {
            return Context.Connection.Query<DbAvatar>(
                "SELECT * FROM fso_avatars WHERE user_id = @user_id", 
                new { user_id = user_id }
            ).ToList();
        }

        public List<DbAvatar> GetMultiple(uint[] id)
        {
            String inClause = "IN (";
            for (int i = 0; i < id.Length; i++)
            {
                inClause = inClause + "'" + id.ElementAt(i) + "'" + ",";
            }
            inClause = inClause.Substring(0, inClause.Length - 1);
            inClause = inClause + ")";

            return Context.Connection.Query<DbAvatar>(
                "Select * from fso_avatars Where avatar_id "+ inClause
            ).ToList();
        }

        public List<DbAvatar> SearchExact(int shard_id, string name, int limit)
        {
            return Context.Connection.Query<DbAvatar>(
                "SELECT avatar_id, name FROM fso_avatars WHERE shard_id = @shard_id AND name = @name LIMIT @limit",
                new { name = name, limit = limit, shard_id = shard_id }
            ).ToList();
        }

        public List<DbAvatar> SearchWildcard(int shard_id, string name, int limit)
        {
            name = name
                .Replace("!", "!!")
                .Replace("%", "!%")
                .Replace("_", "!_")
                .Replace("[", "!["); //must sanitize format...
            return Context.Connection.Query<DbAvatar>(
                "SELECT avatar_id, name FROM fso_avatars WHERE shard_id = @shard_id AND name LIKE @name LIMIT @limit",
                new { name = "%" + name + "%", limit = limit, shard_id = shard_id }
            ).ToList();
        }

        public void UpdateDescription(uint id, string description)
        {
            Context.Connection.Query("UPDATE fso_avatars SET description = @desc WHERE avatar_id = @id", new { id = id, desc = description });
        }

        public void UpdatePrivacyMode(uint id, byte mode)
        {
            Context.Connection.Query("UPDATE fso_avatars SET privacy_mode = @privacy_mode WHERE avatar_id = @id", new { id = id, privacy_mode = mode });
        }

        public void UpdateMoveDate(uint id, uint date)
        {
            Context.Connection.Query("UPDATE fso_avatars SET move_date = @date WHERE avatar_id = @id", new { id = id, date = date });
        }

        public void UpdateMayorNhood(uint id, uint? nhood)
        {
            Context.Connection.Query("UPDATE fso_avatars SET mayor_nhood = @nhood WHERE avatar_id = @id", new { id = id, nhood = nhood });
        }


        public void UpdateAvatarLotSave(uint id, DbAvatar avatar)
        {
            avatar.avatar_id = id;
            Context.Connection.Query("UPDATE fso_avatars SET "
                + "motive_data = @motive_data, "
                + "skilllock = @skilllock, "
                + "lock_mechanical = @lock_mechanical, "
                + "lock_cooking = @lock_cooking, "
                + "lock_charisma = @lock_charisma, "
                + "lock_logic = @lock_logic, "
                + "lock_body = @lock_body, "
                + "lock_creativity = @lock_creativity, "
                + "skill_mechanical = @skill_mechanical, "
                + "skill_cooking = @skill_cooking, "
                + "skill_charisma = @skill_charisma, "
                + "skill_logic = @skill_logic, "
                + "skill_body = @skill_body, "
                + "skill_creativity = @skill_creativity, "
                + "body = @body, "
                + "body_swimwear = @body_swimwear, "
                + "body_sleepwear = @body_sleepwear, "
                + "body_current = @body_current, "
                + "current_job = @current_job, "
                + "is_ghost = @is_ghost, "
                + "ticker_death = @ticker_death, "
                + "ticker_gardener = @ticker_gardener, "
                + "ticker_maid = @ticker_maid, "
                + "ticker_repairman = @ticker_repairman WHERE avatar_id = @avatar_id", avatar);
        }

        private static string[] LockNames = new string[]
        {
            "lock_mechanical",
            "lock_cooking",
            "lock_charisma",
            "lock_logic",
            "lock_body",
            "lock_creativity"
        };

        public int GetOtherLocks(uint avatar_id, string except)
        {
            string columns = "(";
            foreach (var l in LockNames)
            {
                if (l == except) continue;
                columns += l;
                columns += " + ";
            }
            columns += "0) AS Sum";

            return Context.Connection.Query<int>("SELECT "+columns+" FROM fso_avatars WHERE avatar_id = @id", new { id = avatar_id }).FirstOrDefault();
        }

        //budget and transactions
        public int GetBudget(uint avatar_id)
        {
            return Context.Connection.Query<int>("SELECT budget FROM fso_avatars WHERE avatar_id = @id", new { id = avatar_id }).FirstOrDefault();
        }

        public DbTransactionResult Transaction(uint source_id, uint dest_id, int amount, short reason)
        {
            return Transaction(source_id, dest_id, amount, reason, null);
        }

        public DbTransactionResult Transaction(uint source_id, uint dest_id, int amount, short reason, Func<bool> transactionInject)
        {
            var t = Context.Connection.BeginTransaction();
            var srcObj = (source_id >= 16777216);
            var dstObj = (dest_id >= 16777216);
            var success = true;
            try {
                int srcRes, dstRes;
                if (source_id != uint.MaxValue)
                {
                    if (srcObj)
                    {
                        srcRes = Context.Connection.Execute("UPDATE fso_objects SET budget = budget - @amount WHERE object_id = @source_id;",
                            new { source_id = source_id, amount = amount });
                    }
                    else
                    {
                        srcRes = Context.Connection.Execute("UPDATE fso_avatars SET budget = budget - @amount WHERE avatar_id = @source_id;",
                            new { source_id = source_id, amount = amount });
                    }
                    if (srcRes == 0) throw new Exception("Source avatar/object does not exist!");
                }

                if (dest_id != uint.MaxValue)
                {
                    if (dstObj)
                    {
                        dstRes = Context.Connection.Execute("UPDATE fso_objects SET budget = budget + @amount WHERE object_id = @dest_id;",
                            new { dest_id = dest_id, amount = amount });
                    }
                    else
                    {
                        dstRes = Context.Connection.Execute("UPDATE fso_avatars SET budget = budget + @amount WHERE avatar_id = @dest_id;",
                            new { dest_id = dest_id, amount = amount });
                    }
                    if (dstRes == 0) throw new Exception("Dest avatar/object does not exist!");
                }

                if (transactionInject != null)
                {
                    if (!transactionInject()) throw new Exception("Transaction Cancelled");
                }

                t.Commit();
            } catch (Exception)
            {
                success = false;
                t.Rollback();
            }

            if (success && ((reason > 7 && reason != 9) || (source_id != uint.MaxValue && dest_id != uint.MaxValue))) {
                var days = (DateTime.UtcNow - new DateTime(1970, 1, 1)).TotalDays;
                Context.Connection.Execute("INSERT INTO fso_transactions (from_id, to_id, transaction_type, day, value, count) "+
                    "VALUES (@from_id, @to_id, @transaction_type, @day, @value, @count) " +
                    "ON DUPLICATE KEY UPDATE value = value + @value, count = count+1", new
                {
                    from_id = (amount>0)?source_id:dest_id,
                    to_id = (amount>0)?dest_id:source_id,
                    transaction_type = reason,
                    day = (int)days,
                    value = Math.Abs(amount),
                    count = 1
                });
            }

            var result = Context.Connection.Query<DbTransactionResult>("SELECT a1.budget AS source_budget, a2.budget AS dest_budget "
                + "FROM"
                + "(SELECT budget, count(budget) FROM " + (srcObj ? "fso_objects" : "fso_avatars") + " WHERE " + (srcObj ? "object_id" : "avatar_id") + " = @source_id) a1,"
                + "(SELECT budget, count(budget) FROM " + (dstObj ? "fso_objects" : "fso_avatars") + " WHERE " + (dstObj ? "object_id" : "avatar_id") + " = @avatar_id) a2; ",
                new { avatar_id = dest_id, source_id = source_id }).FirstOrDefault();
            if (result != null)
            {
                result.amount = amount;
                result.success = success;
            }
            return result;
        }

        public DbTransactionResult TestTransaction(uint source_id, uint dest_id, int amount, short reason)
        {
            var success = true;
            var srcObj = (source_id >= 16777216);
            var dstObj = (dest_id >= 16777216);
            try
            {
                int? srcVal, dstVal;
                if (srcObj)
                {
                    srcVal = Context.Connection.Query<int?>("SELECT budget FROM fso_objects WHERE object_id = @source_id;",
                        new { source_id = source_id }).FirstOrDefault();
                }
                else
                {
                    srcVal = Context.Connection.Query<int?>("SELECT budget FROM fso_avatars WHERE avatar_id = @source_id;",
                        new { source_id = source_id }).FirstOrDefault();
                }
                if (source_id != uint.MaxValue)
                {
                    if (srcVal == null) throw new Exception("Source avatar/object does not exist!");
                    if (srcVal.Value - amount < 0) throw new Exception("Source does not have enough money!");
                }
                if (dstObj)
                {
                    dstVal = Context.Connection.Query<int?>("SELECT budget FROM fso_objects WHERE object_id = @dest_id;",
                        new { dest_id = dest_id }).FirstOrDefault();
                }
                else
                {
                    dstVal = Context.Connection.Query<int?>("SELECT budget FROM fso_avatars WHERE avatar_id = @dest_id;",
                        new { dest_id = dest_id }).FirstOrDefault();
                }
                if (dest_id != uint.MaxValue)
                {
                    if (dstVal == null) throw new Exception("Dest avatar/object does not exist!");
                    if (dstVal.Value + amount < 0) throw new Exception("Destination does not have enough money! (transaction accidentally debits)");
                }
            }
            catch (Exception)
            {
                success = false;
            }
            var result = Context.Connection.Query<DbTransactionResult>("SELECT a1.budget AS source_budget, a2.budget AS dest_budget "
                + "FROM"
                + "(SELECT budget, count(budget) FROM "+(srcObj?"fso_objects":"fso_avatars")+" WHERE "+(srcObj?"object_id":"avatar_id")+" = @source_id) a1,"
                + "(SELECT budget, count(budget) FROM " + (dstObj ? "fso_objects" : "fso_avatars") + " WHERE " + (dstObj ? "object_id" : "avatar_id") + " = @avatar_id) a2; ", 
                new { avatar_id = dest_id, source_id = source_id }).FirstOrDefault();
            if (result != null)
            {
                result.amount = amount;
                result.success = success;
            }
            return result;
        }

        //JOB LEVELS

        public DbJobLevel GetCurrentJobLevel(uint avatar_id)
        {
            return Context.Connection.Query<DbJobLevel>("SELECT * FROM fso_avatars a JOIN fso_joblevels b "
                + "ON a.avatar_id = b.avatar_id AND a.current_job = b.job_type WHERE a.avatar_id = @id", 
                new { id = avatar_id }).FirstOrDefault();
        }

        public List<DbJobLevel> GetJobLevels(uint avatar_id)
        {
            return Context.Connection.Query<DbJobLevel>("SELECT * FROM fso_avatars a JOIN fso_joblevels b ON a.avatar_id = b.avatar_id WHERE a.avatar_id = @id", new { id = avatar_id }).ToList();
        }

        public void UpdateAvatarJobLevel(DbJobLevel jobLevel)
        {
            Context.Connection.Query<DbJobLevel>("INSERT INTO fso_joblevels (avatar_id, job_type, job_experience, job_level, job_sickdays, job_statusflags) "
                + "VALUES (@avatar_id, @job_type, @job_experience, @job_level, @job_sickdays, @job_statusflags) "
                + "ON DUPLICATE KEY UPDATE job_experience=VALUES(`job_experience`), job_level=VALUES(`job_level`), "
                +" job_sickdays=VALUES(`job_sickdays`), job_statusflags=VALUES(`job_statusflags`); ", jobLevel);
            return;
        }

        public List<DbAvatarSummary> GetSummaryByUserId(uint user_id)
        {
            return Context.Connection.Query<DbAvatarSummary>(
                @"SELECT a.avatar_id, 
		                 a.shard_id,
		                 a.user_id,
		                 a.name,
		                 a.gender,
		                 a.date,
		                 a.skin_tone,
		                 a.head,
		                 a.body,
		                 a.description,
		                 r.lot_id, 
		                 l.name as lot_name,
                         l.location as lot_location
                FROM fso_avatars a
                    LEFT OUTER JOIN fso_roommates r on r.avatar_id = a.avatar_id
                    LEFT OUTER JOIN fso_lots l on l.lot_id = r.lot_id AND r.is_pending = 0
                WHERE a.user_id = @user_id
                ORDER BY a.date ASC", new { user_id = user_id }).ToList();
        }
    }
}
