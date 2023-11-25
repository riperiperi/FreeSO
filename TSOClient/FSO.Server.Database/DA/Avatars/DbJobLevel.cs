namespace FSO.Server.Database.DA.Avatars
{
    public class DbJobLevel
    {
        public uint avatar_id { get; set; }
        public ushort job_type { get; set; }
        public ushort job_experience { get; set; }
        public ushort job_level { get; set; }
        public ushort job_sickdays { get; set; }
        public ushort job_statusflags { get; set; }
    }
}
