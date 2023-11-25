using System;

namespace FSO.Server.Database.DA.Updates
{
    public class DbUpdateAddon
    {
        public int addon_id { get; set; }
        public string name { get; set; }
        public string description { get; set; }
        public string addon_zip_url { get; set; }
        public string server_zip_url { get; set; }
        public DateTime date { get; set; }
    }
}
