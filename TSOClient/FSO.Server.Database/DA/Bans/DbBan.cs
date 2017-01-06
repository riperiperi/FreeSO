using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FSO.Server.Database.DA.Bans
{
    public class DbBan
    {
        public uint user_id { get; set; }
        public string ip_address { get; set; }
        public string ban_reason { get; set; }
        public int end_date { get; set; }
    }
}
