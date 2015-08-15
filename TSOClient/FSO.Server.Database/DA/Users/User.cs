using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FSO.Server.Database.DA.Users
{
    public class User
    {
        public int user_id { get; set; }
        public string username { get; set; }
        public string email { get; set; }
        public UserState user_state { get; set; }
        public uint register_date { get; set; }
        public bool is_admin { get; set; }
        public bool is_moderator { get; set; }
        public bool is_banned { get; set; }
    }

    public enum UserState
    {
        valid,
        email_confirm,
        moderated
    }
}
