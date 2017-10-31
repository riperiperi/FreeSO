using FSO.Server.Database.DA;
using FSO.Server.Database.DA.DbEvents;
using FSO.Server.Framework.Aries;
using FSO.Server.Framework.Voltron;
using FSO.Server.Servers.City.Handlers;
using Ninject;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FSO.Server.Servers.City.Domain
{
    public class EventSystem
    {
        private IDAFactory DA;
        private MailHandler Mail;
        private CityServerContext Context;
        private ISessions Sessions;
        private IKernel Kernel;

        public List<DbEvent> ActiveEvents = new List<DbEvent>();
        public DateTime Next = new DateTime(0);

        public EventSystem(IDAFactory da, CityServerContext ctx, ISessions sessions, IKernel kernel)
        {
            DA = da;
            Kernel = kernel;
            Context = ctx;
            Sessions = sessions;
        }

        public void Init()
        {
            Mail = Kernel.Get<MailHandler>();
            TickEvents();
        }

        public void TickEvents()
        {
            var time = DateTime.UtcNow;
            if (time > Next) //check events every hour
            {
                Next = NextHour(time);

                //activate the new event
                using (var da = DA.Get())
                {
                    var active = da.Events.GetActive(time);
                    List<DbEvent> newEvts, oldEvts;
                    lock (ActiveEvents) {
                        newEvts = active.Where(x => !ActiveEvents.Any(y => y.event_id == x.event_id)).ToList();
                        oldEvts = ActiveEvents.Where(x => !active.Any(y => y.event_id == x.event_id)).ToList();

                        ActiveEvents.Clear();
                        ActiveEvents.AddRange(active);
                    }
                    //TODO: deactivation event
                    ActivateEvents(newEvts);
                }
            }
        }

        private DateTime NextHour(DateTime now)
        {
            return Trim(now.AddHours(1), TimeSpan.TicksPerHour);
        }
        private DateTime Trim(DateTime date, long roundTicks)
        {
            return new DateTime(date.Ticks - date.Ticks % roundTicks);
        }


        public void ActivateEvents(List<DbEvent> evts)
        {
            var all = Sessions.Clone();
            foreach (var session in all.OfType<IVoltronSession>())
            {
                foreach (var evt in evts)
                {
                    if (session.IsAnonymous) continue;
                    UserJoinedEvent(session, evt, true);
                }
            }
        }

        public void UserJoined(IVoltronSession session) {

            List<DbEvent> evts;
            lock (ActiveEvents) evts = new List<DbEvent>(ActiveEvents);
            foreach (var evt in evts)
            {
                UserJoinedEvent(session, evt, false);
            }
        }

        public void UserJoinedEvent(IVoltronSession session, DbEvent evt, bool alreadyOnline)
        {
            try
            {
                var needsParticipation = evt.mail_message != null || ParticipationType(evt.type);
                if (!needsParticipation) return;
                using (var db = DA.Get())
                {
                    var user = session.UserId;
                    var participation = db.Events.TryParticipate(new DbEventParticipation() { event_id = evt.event_id, user_id = user });
                    if (participation)
                    {
                        if (evt.mail_message != null)
                        {
                            Mail.SendEmail(new Files.Formats.tsodata.MessageItem()
                            {
                                Subject = evt.mail_subject ?? "Event",
                                Body = evt.mail_message,
                                SenderID = (uint)(evt.mail_sender ?? (int.MinValue)),
                                SenderName = evt.mail_sender_name ?? "The Sims Online",
                                TargetID = session.AvatarId,
                                Type = 4,
                                Subtype = 0
                            }, alreadyOnline);
                        }
                        switch (evt.type)
                        {
                            case DbEventType.free_object:
                                for (int i = 0; i < Math.Max(1, evt.value2); i++)
                                {
                                    db.Objects.Create(new Database.DA.Objects.DbObject()
                                    {
                                        type = (uint)evt.value,
                                        shard_id = Context.ShardId,
                                        owner_id = session.AvatarId,
                                        lot_id = null, //to inventory
                                        dyn_obj_name = ""
                                    });
                                }
                                break;
                            case DbEventType.free_money:
                                db.Avatars.Transaction(uint.MaxValue, session.AvatarId, evt.value, 0);
                                break;
                        }
                    }
                }
            } catch
            {
                
            }
        }

        private bool ParticipationType(DbEventType type)
        {
            return type == DbEventType.free_money || type == DbEventType.free_object || type == DbEventType.mail_only;
        }
    }
}
