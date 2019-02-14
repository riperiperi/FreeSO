using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FSO.Server.Database.DA.Bulletin
{
    public class DbBulletinPost
    {
        public uint bulletin_id { get; set; }
        public int neighborhood_id { get; set; }
        public uint? avatar_id { get; set; }
        public string title { get; set; }
        public string body { get; set; }
        public uint date { get; set; }
        public uint flags { get; set; }
        public int? lot_id { get; set; }
        public DbBulletinType type { get; set; }

        public string string_type { get
            {
                return type.ToString();
            }
        }
    }

    public enum DbBulletinType
    {
        mayor,
        system,
        community
    }
}
