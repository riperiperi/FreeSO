using System;
using Dapper;
using System.Collections.Generic;
using System.Linq;
using FSO.Server.Database.DA.Utils;

namespace FSO.Server.Database.DA.DbEvents
{
    public class SqlEvents : AbstractSqlDA, IEvents
    {
        public SqlEvents(ISqlContext context) : base(context){
        }

        public PagedList<DbEvent> All(int offset = 1, int limit = 20, string orderBy = "start_day")
        {
            var connection = Context.Connection;
            var total = connection.Query<int>("SELECT COUNT(*) FROM fso_events").FirstOrDefault();
            var results = connection.Query<DbEvent>("SELECT * FROM fso_events ORDER BY @order DESC LIMIT @offset, @limit", new { order = orderBy, offset = offset, limit = limit });
            return new PagedList<DbEvent>(results, offset, total);
        }

        public int Add(DbEvent evt)
        {
            var result = Context.Connection.Query<int>("INSERT INTO fso_events (title, description, start_day, " +
                         "end_day, type, value, value2, mail_subject, mail_message, mail_sender, mail_sender_name) " +
                         " VALUES (@title, @description, @start_day, @end_day, @type_str, @value, @value2, " +
                         " @mail_subject, @mail_message, @mail_sender, @mail_sender_name); SELECT LAST_INSERT_ID();", evt).First();
            return result;
        }

        public bool Delete(int event_id)
        {
            return Context.Connection.Execute("DELETE FROM fso_events WHERE event_id = @event_id", new { event_id = event_id }) > 0;
        }

        public List<DbEvent> GetActive(DateTime time)
        {
            return Context.Connection.Query<DbEvent>("SELECT * FROM fso_events WHERE start_day <= @time AND end_day >= @time", new { time = time }).ToList();
        }

        public List<DbEvent> GetLatestNameDesc(int limit)
        {
            return Context.Connection.Query<DbEvent>("SELECT * FROM fso_events WHERE title IS NOT NULL AND description IS NOT NULL ORDER BY start_day DESC LIMIT "+limit).ToList();
        }

        public List<uint> GetParticipatingUsers(int event_id)
        {
            return Context.Connection.Query<uint>("SELECT user_id FROM fso_event_participation WHERE event_id = @event_id", new { event_id = event_id }).ToList();
        }

        public bool Participated(DbEventParticipation p)
        {
            return Context.Connection.Query<int>("SELECT count(*) FROM fso_event_participation WHERE event_id = @event_id AND user_id = @user_id", p).First() > 0;
        }

        public bool TryParticipate(DbEventParticipation p)
        {
            try
            {
                return (Context.Connection.Execute("INSERT INTO fso_event_participation (event_id, user_id) VALUES (@event_id, @user_id)", p) > 0);
            }
            catch
            {
                //already exists, or foreign key fails
                return false;
            }
        }

        public bool GenericAvaTryParticipate(DbGenericAvatarParticipation p)
        {
            try
            {
                return (Context.Connection.Execute("INSERT INTO fso_generic_avatar_participation (participation_name, participation_avatar) " +
                    "VALUES (@participation_name, @participation_avatar)", p) > 0);
            }
            catch
            {
                //already exists, or foreign key fails
                return false;
            }
        }

        public bool GenericAvaParticipated(DbGenericAvatarParticipation p)
        {
            return Context.Connection.Query<int>("SELECT count(*) FROM fso_generic_avatar_participation " +
                "WHERE participation_name = @participation_name AND participation_avatar = @participation_avatar", p).First() > 0;
        }

        public List<uint> GetGenericParticipatingAvatars(string genericName)
        {
            return Context.Connection.Query<uint>("SELECT participation_avatar FROM fso_generic_avatar_participation " +
                "WHERE participation_name = @participation_name", new { participation_name = genericName }).ToList();
        }
    }
}
