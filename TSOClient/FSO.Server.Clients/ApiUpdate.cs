using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FSO.Server.Clients
{
    public class ApiUpdate
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

    public class UpdatePath
    {
        public List<ApiUpdate> Path;
        public bool FullZipStart;
        public bool MissingInfo;

        public UpdatePath(List<ApiUpdate> path, bool fullZip)
        {
            Path = path;
            FullZipStart = fullZip;
        }

        public static UpdatePath FindPath(List<ApiUpdate> updates, string current, string target)
        {
            var to = updates.FirstOrDefault(x => x.version_name == target);
            if (to == null) return null; //cannot find path, fallback on what the server told us before we looked for the delta
            var from = updates.FirstOrDefault(x => x.version_name == current);
            if (from != null)
            {
                //search for route from "to" to "from". recursive search - we then return the updates in order of application
                var follow = to;
                var result = new List<ApiUpdate>();
                while (follow != null)
                {
                    if (follow == from) return new UpdatePath(result, false); //we got here with incremental updates.
                    result.Insert(0, follow);
                    if (follow.incremental_zip == null) break;
                    follow = (follow.last_update_id == null) ? null : updates.FirstOrDefault(x => x.update_id == follow.last_update_id.Value);
                }
            }
            //we couldn't find a path to our current version. find a path to any update that has a full zip.
            {
                var follow = to;
                var result = new List<ApiUpdate>();
                while (follow != null)
                {
                    result.Insert(0, follow);
                    if (follow.full_zip != null) return new UpdatePath(result, true); //we found a full zip
                    follow = (follow.last_update_id == null) ? null : updates.FirstOrDefault(x => x.update_id == follow.last_update_id.Value);
                }
            }

            return null; //no clue what to do here, we could not find a full zip to build from. this is fatal.
        }
    }
}
