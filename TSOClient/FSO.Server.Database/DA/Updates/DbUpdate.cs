using System;

namespace FSO.Server.Database.DA.Updates
{
    public class DbUpdate
    {
        public int update_id { get; set; }
        public string version_name { get; set; }
        public int? addon_id { get; set; }
        public int branch_id { get; set; }
        public string full_zip { get; set; }
        public string incremental_zip { get; set; }
        public string manifest_url { get; set; }
        public string server_zip { get; set; }
        public int? last_update_id { get; set; }
        public int flags { get; set; }
        public DateTime date { get; set; }
        public DateTime? publish_date { get; set; }
        public DateTime? deploy_after { get; set; }
    }
}
