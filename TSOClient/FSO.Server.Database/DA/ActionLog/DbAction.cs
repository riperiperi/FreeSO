namespace FSO.Server.Database.DA.ActionLog
{
    public class DbAction
    {
        public long id { get; set; }
        public uint avatar_id { get; set; }
        public uint day { get; set; }          // days-since-epoch UTC
        public byte action_type { get; set; }  // see ActionType constants
        public ulong value { get; set; }       // amount/count semantic depends on action_type
        public uint? parameter { get; set; }   // lot_id, object guid, skill type — optional context
        public uint ts { get; set; }           // unix epoch
    }
}