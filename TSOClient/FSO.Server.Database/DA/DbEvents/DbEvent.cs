using System;

namespace FSO.Server.Database.DA.DbEvents
{
    public class DbEvent
    {
        public int event_id { get; set; }
        public string title { get; set; }
        public string description { get; set; }
        public DateTime start_day { get; set; }
        public DateTime end_day { get; set; }
        public DbEventType type { get; set; }
        public int value { get; set; }
        public int value2 { get; set; }
        public string mail_subject { get; set; }
        public string mail_message { get; set; }
        public int? mail_sender { get; set; }
        public string mail_sender_name { get; set; }

        public string type_str {
            get {
                return type.ToString();
            }
        }
    }

    public class DbEventParticipation
    {
        public int event_id { get; set; }
        public uint user_id { get; set; }
    }

    public enum DbEventType
    {
        mail_only,
        free_object,
        free_money,
        free_green,
        obj_tuning
    }
}
