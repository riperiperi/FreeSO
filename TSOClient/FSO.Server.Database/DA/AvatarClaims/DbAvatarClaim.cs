using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FSO.Server.Database.DA.AvatarClaims
{
    public class DbAvatarClaim
    {
        public int avatar_claim_id { get; set; }
        public uint avatar_id { get; set; }
        public string owner { get; set; }
        public uint location { get; set; }
    }
}
