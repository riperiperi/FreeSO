using System.Collections.Generic;

namespace FSO.Server.Clients
{
    // Wire types matching DailyQuestsController on the server side. See
    // edenso_server_data/design_daily_quests_v1.md for the schema.

    public class ApiDailyQuestList
    {
        public List<ApiDailyQuest> quests { get; set; }
    }

    public class ApiDailyQuest
    {
        public byte slot { get; set; }
        public string type { get; set; }          // "EARN" | "SKILL" | "VISIT" | "BUY"
        public string description { get; set; }   // human-readable, localized server-side
        public ulong target { get; set; }
        public ulong progress { get; set; }
        public uint reward { get; set; }
        public bool completed { get; set; }
        public bool claimed { get; set; }
    }

    public class ApiDailyQuestClaimResult
    {
        public uint reward { get; set; }
        public int new_balance { get; set; }
    }
}