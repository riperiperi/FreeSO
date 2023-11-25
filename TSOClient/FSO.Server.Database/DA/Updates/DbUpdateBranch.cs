namespace FSO.Server.Database.DA.Updates
{
    public class DbUpdateBranch
    {
        public int branch_id { get; set; }
        public string branch_name { get; set; }
        public string version_format { get; set; }
        public int last_version_number { get; set; }
        public int minor_version_number { get; set; }
        public int? current_dist_id { get; set; }
        public int? addon_id { get; set; }
        public string base_build_url { get; set; }
        public string base_server_build_url { get; set; }
        public DbUpdateBuildMode build_mode { get; set; }
        public int flags { get; set; }
    }

    public enum DbUpdateBuildMode
    {
        zip,
        teamcity
    }
}
