using FSO.Common.Enum;
using FSO.Server.Database.DA.Lots;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FSO.Server.Database.DA.LotTop100
{≈
    public class DbAvatarTop100
    {
        public AvatarTop100Category category { get; set; }
        public byte rank { get; set; }
        public int shard_id { get; set; }
        public int? avatar_id { get; set; }
        public int? value { get; set; }
        public DateTime date { get; set; }

        //Joins
        public string avatar_name { get; set; }
    }
}
