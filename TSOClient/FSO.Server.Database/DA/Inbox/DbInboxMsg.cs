using System;

namespace FSO.Server.Database.DA.Inbox
{
    public class DbInboxMsg
    {
        public int message_id { get; set; }
        public uint sender_id { get; set; }
        public uint target_id { get; set; }
        public string subject { get; set; }
        public string body { get; set; }
        public string sender_name { get; set; }
        public DateTime time { get; set; }
        public int msg_type { get; set; }
        public int msg_subtype { get; set; }
        public int read_state { get; set; }
        public int? reply_id { get; set; }
    }
}
