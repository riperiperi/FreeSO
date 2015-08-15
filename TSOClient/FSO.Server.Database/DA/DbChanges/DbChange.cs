using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FSO.Server.Database.DA.DbChanges
{
    public class DbChange
    {
        public string id { get; set; }
        public string filename { get; set; }
        public uint date { get; set; }
        public string hash { get; set; }
    }
}
