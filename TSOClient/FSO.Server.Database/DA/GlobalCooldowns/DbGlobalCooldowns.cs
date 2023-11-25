using System;

namespace FSO.Server.Database.DA.GlobalCooldowns
{
    public class DbGlobalCooldowns
    {
        public uint object_guid { get; set; }
        public uint avatar_id { get; set; }
        public uint user_id { get; set; }
        public uint category { get; set; }
        public DateTime expiry { get; set; }
    }
}
