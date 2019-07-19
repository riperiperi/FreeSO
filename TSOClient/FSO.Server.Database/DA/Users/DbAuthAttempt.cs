using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FSO.Server.Database.DA.Users
{
    public class DbAuthAttempt
    {
        public uint attempt_id { get; set; }
        public string ip { get; set; }
        public uint user_id { get; set; }
        public uint expire_time { get; set; }
        public int count { get; set; }
        public bool active { get; set; }
        public bool invalidated { get; set; }
    }
}
