namespace FSO.Server.Database.DA.Users
{
    public class UserSummary
    {
        public uint user_id { get; set; }
        public string username { get; set; }
        public string email { get; set; }
        public UserState user_state { get; set; }
        public uint register_date { get; set; }
        public bool is_admin { get; set; }
        public bool is_moderator { get; set; }
        public bool is_banned { get; set; }
        public string register_ip { get; set; }
        public string last_ip { get; set; }
        public string client_id { get; set; }
        public uint last_login { get; set; }
        public int avatar_count { get; set; }
        public string display_name { get; set; } // Archive exclusive
        public bool is_verified { get; set; } // Archive exclusive
    }
}
