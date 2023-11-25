using FSO.Common.Enum;
using System;

namespace FSO.Server.Database.DA.LotTop100
{
    public class DbLotTop100
    {
        public LotCategory category { get; set; }
        public byte rank { get; set; }
        public int shard_id { get; set; }
        public int? lot_id { get; set; }
        public int? minutes { get; set; }
        public DateTime date { get; set; }

        //Joins
        public string lot_name { get; set; }
        public uint? lot_location { get; set; }
    }
}
