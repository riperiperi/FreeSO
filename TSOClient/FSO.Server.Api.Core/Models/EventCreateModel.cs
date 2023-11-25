using System;
using System.Collections.Generic;

namespace FSO.Server.Api.Core.Models
{
    public class EventCreateModel
    {
        public string title;
        public string description;
        public DateTime start_day;
        public DateTime end_day;
        public string type;
        public int value;
        public int value2;
        public string mail_subject;
        public string mail_message;
        public int mail_sender;
        public string mail_sender_name;
    }

    public class PresetCreateModel
    {
        public string name;
        public string description;
        public int flags;
        public List<PresetItemModel> items;
    }

    public class PresetItemModel
    {
        public string tuning_type;
        public int tuning_table;
        public int tuning_index;
        public float value;
    }
}
