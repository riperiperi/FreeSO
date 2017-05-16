using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FSO.Server.Database.DA.Users
{
    public class UserAuthenticate
    {
        public uint user_id { get; set; }
        public string scheme_class { get; set; }
        public byte[] data { get; set; }
    }
}
