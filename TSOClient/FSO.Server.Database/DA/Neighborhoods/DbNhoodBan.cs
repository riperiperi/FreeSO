using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FSO.Server.Database.DA.Neighborhoods
{
    public class DbNhoodBan
    {
        public uint user_id { get; set; }
        public string ban_reason { get; set; }
        public uint end_date { get; set; }
    }
}
