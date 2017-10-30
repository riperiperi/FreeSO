﻿using System;
using Dapper;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FSO.Server.Database.DA.DbEvents
{
    public class SqlEvents : AbstractSqlDA, IEvents
    {
        public SqlEvents(ISqlContext context) : base(context){
        }

        public int Add(DbEvent evt)
        {
            var result = Context.Connection.Query<int>("INSERT INTO fso_inbox (title, description, start_day, " +
                         "end_day, type, value, value2, mail_subject, mail_message, mail_sender, mail_sender_name) " +
                         " VALUES (@title, @description, @start_day, @end_day, @type, @value, @value2, " +
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
    }
}
