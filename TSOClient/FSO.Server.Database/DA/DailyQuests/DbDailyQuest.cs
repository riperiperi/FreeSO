namespace FSO.Server.Database.DA.DailyQuests
{
    public class DbDailyQuest
    {
        public uint avatar_id { get; set; }
        public uint day { get; set; }          // days-since-epoch UTC
        public byte slot { get; set; }         // 0..2
        public byte quest_type { get; set; }   // see QuestType constants
        public ulong target { get; set; }
        public ulong progress { get; set; }
        public uint reward { get; set; }       // simoleons paid on completion
        public uint? parameter { get; set; }   // reserved for future typed quests
        public uint? completed_ts { get; set; }
        public uint? paid_ts { get; set; }
    }
}