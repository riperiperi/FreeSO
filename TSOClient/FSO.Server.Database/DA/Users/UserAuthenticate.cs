namespace FSO.Server.Database.DA.Users
{
    public class UserAuthenticate
    {
        public uint user_id { get; set; }
        public string scheme_class { get; set; }
        public byte[] data { get; set; }
    }
}
