using FSO.Server.Database.DA.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FSO.Server.Database.DA.Updates
{
    public interface IUpdates
    {
        DbUpdate GetUpdate(int update_id);
        IEnumerable<DbUpdate> GetRecentUpdatesForBranchByID(int branch_id, int limit);
        IEnumerable<DbUpdate> GetRecentUpdatesForBranchByName(string branch_name, int limit);
        IEnumerable<DbUpdate> GetPublishableByBranchName(string branch_name);
        PagedList<DbUpdate> All(int offset = 0, int limit = 20, string orderBy = "date");
        int AddUpdate(DbUpdate update);
        bool MarkUpdatePublished(int update_id);

        DbUpdateAddon GetAddon(int addon_id);
        IEnumerable<DbUpdateAddon> GetAddons(int limit);
        bool AddAddon(DbUpdateAddon addon);

        DbUpdateBranch GetBranch(int branch_id);
        DbUpdateBranch GetBranch(string branch_name);
        IEnumerable<DbUpdateBranch> GetBranches();
        bool AddBranch(DbUpdateBranch branch);
        bool UpdateBranchLatest(int branch_id, int last_version_number, int minor_version_number);
        bool UpdateBranchLatestDeployed(int branch_id, int current_dist_id);
        bool UpdateBranchAddon(int branch_id, int addon_id);
        bool UpdateBranchInfo(DbUpdateBranch branch);
    }
}
