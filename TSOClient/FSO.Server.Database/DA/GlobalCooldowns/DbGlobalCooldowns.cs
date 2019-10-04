using FSO.Common.Enum;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
