using FSO.Server.Database.DA.Users;

namespace FSO.Server.Database.DA.ArchiveUsers
{
    public class ArchiveUser : User
    {
        public string display_name { get; set; }
        public bool is_verified { get; set; }
        public bool shared_user { get; set; }
    }
}
