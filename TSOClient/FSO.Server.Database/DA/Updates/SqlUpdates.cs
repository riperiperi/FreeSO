using System;
using System.Collections.Generic;
using System.Linq;
using FSO.Server.Database.DA.Utils;
using Dapper;

namespace FSO.Server.Database.DA.Updates
{
    public class SqlUpdates : AbstractSqlDA, IUpdates
    {
        public SqlUpdates(ISqlContext context) : base(context)
        {
        }

        public bool AddAddon(DbUpdateAddon addon)
        {
            try
            {
                var result = Context.Connection.Execute("INSERT INTO fso_update_addons (name, description, addon_zip_url, server_zip_url) "
                    + "VALUES (@name, @description, @addon_zip_url, @server_zip_url)", addon);
                return (result > 0);
            }
            catch (Exception)
            {
                return false;
            }
        }

        public bool AddBranch(DbUpdateBranch branch)
        {
            try
            {
                var result = Context.Connection.Execute("INSERT INTO fso_update_branch " +
                    "(branch_name, version_format, last_version_number, current_dist_id, addon_id, base_build_url, base_server_build_url, build_mode, flags) "
                    + "VALUES (@branch_name, @version_format, @last_version_number, @current_dist_id, @addon_id, @base_build_url, @base_server_build_url, @build_mode, @flags)", 
                    branch);
                return (result > 0);
            }
            catch (Exception)
            {
                return false;
            }
        }

        public int AddUpdate(DbUpdate update)
        {
            var result = Context.Connection.Query<int>("INSERT INTO fso_updates " +
                "(version_name, addon_id, branch_id, full_zip, incremental_zip, manifest_url, server_zip, last_update_id, flags, publish_date, deploy_after) "
                + "VALUES (@version_name, @addon_id, @branch_id, @full_zip, @incremental_zip, @manifest_url, @server_zip, @last_update_id, @flags, @publish_date, @deploy_after); " +
                "SELECT LAST_INSERT_ID();",
                update).FirstOrDefault();
            return result;
        }

        public PagedList<DbUpdate> All(int offset = 1, int limit = 20, string orderBy = "date")
        {
            var connection = Context.Connection;
            var total = connection.Query<int>("SELECT COUNT(*) FROM fso_updates").FirstOrDefault();
            var results = connection.Query<DbUpdate>("SELECT * FROM fso_updates ORDER BY @order DESC LIMIT @offset, @limit", new { order = orderBy, offset = offset, limit = limit });
            return new PagedList<DbUpdate>(results, offset, total);
        }

        public DbUpdateAddon GetAddon(int addon_id)
        {
            return Context.Connection.Query<DbUpdateAddon>("SELECT * FROM fso_update_addons WHERE addon_id = @addon_id", new { addon_id }).FirstOrDefault();
        }

        public IEnumerable<DbUpdateAddon> GetAddons(int limit)
        {
            return Context.Connection.Query<DbUpdateAddon>("SELECT * FROM fso_update_addons ORDER BY date DESC");
        }

        public DbUpdateBranch GetBranch(int branch_id)
        {
            return Context.Connection.Query<DbUpdateBranch>("SELECT * FROM fso_update_branch WHERE branch_id = @branch_id", new { branch_id }).FirstOrDefault();
        }

        public DbUpdateBranch GetBranch(string branch_name)
        {
            return Context.Connection.Query<DbUpdateBranch>("SELECT * FROM fso_update_branch WHERE branch_name = @branch_name", new { branch_name }).FirstOrDefault();
        }

        public IEnumerable<DbUpdateBranch> GetBranches()
        {
            return Context.Connection.Query<DbUpdateBranch>("SELECT * FROM fso_update_branch");
        }

        public IEnumerable<DbUpdate> GetRecentUpdatesForBranchByID(int branch_id, int limit)
        {
            return Context.Connection.Query<DbUpdate>("SELECT * FROM fso_updates WHERE branch_id = @branch_id AND publish_date IS NOT NULL " +
                "ORDER BY date DESC " +
                "LIMIT @limit", new { branch_id, limit });
        }

        public IEnumerable<DbUpdate> GetRecentUpdatesForBranchByName(string branch_name, int limit)
        {
            return Context.Connection.Query<DbUpdate>("SELECT * FROM fso_updates u JOIN fso_update_branch b ON u.branch_id = b.branch_id " +
                "WHERE b.branch_name = @branch_name AND publish_date IS NOT NULL AND deploy_after IS NOT NULL AND deploy_after < NOW() " +
                "ORDER BY publish_date DESC " +
                "LIMIT @limit", new { branch_name, limit });
        }

        public IEnumerable<DbUpdate> GetPublishableByBranchName(string branch_name)
        {
            return Context.Connection.Query<DbUpdate>("SELECT * FROM fso_updates u JOIN fso_update_branch b ON u.branch_id = b.branch_id " +
                "WHERE b.branch_name = @branch_name AND publish_date IS NULL " +
                "ORDER BY date DESC", new { branch_name });
        }

        public DbUpdate GetUpdate(int update_id)
        {
            return Context.Connection.Query<DbUpdate>("SELECT * FROM fso_updates WHERE update_id = @update_id", new { update_id }).FirstOrDefault();
        }

        public bool UpdateBranchAddon(int branch_id, int addon_id)
        {
            try
            {
                var result = Context.Connection.Execute("UPDATE fso_update_branch SET addon_id = @addon_id " +
                    "WHERE branch_id = @branch_id"
                    , new { branch_id, addon_id });
                return (result > 0);
            }
            catch (Exception)
            {
                return false;
            }
        }

        public bool UpdateBranchInfo(DbUpdateBranch branch)
        {
            try
            {
                var result = Context.Connection.Execute("UPDATE fso_update_branch SET " +
                    "branch_name = @branch_name, version_format = @version_format, last_version_number = @last_version_number, " +
                    "minor_version_number = @minor_version_number, addon_id = @addon_id, base_build_url = @base_build_url, " +
                    "base_server_build_url = @base_server_build_url, build_mode = @build_mode, flags = @flags " +
                    "WHERE branch_id = @branch_id"
                    , branch);
                return (result > 0);
            }
            catch (Exception)
            {
                return false;
            }
        }

        public bool UpdateBranchLatest(int branch_id, int last_version_number, int minor_version_number)
        {
            try
            {
                var result = Context.Connection.Execute("UPDATE fso_update_branch SET last_version_number = @last_version_number, " +
                    "minor_version_number = @minor_version_number WHERE branch_id = @branch_id"
                    , new { branch_id, last_version_number, minor_version_number });
                return (result > 0);
            }
            catch (Exception)
            {
                return false;
            }
        }

        public bool UpdateBranchLatestDeployed(int branch_id, int current_dist_id)
        {
            try
            {
                var result = Context.Connection.Execute("UPDATE fso_update_branch SET current_dist_id = @current_dist_id WHERE branch_id = @branch_id"
                    , new { branch_id, current_dist_id });
                return (result > 0);
            }
            catch (Exception)
            {
                return false;
            }
        }

        public bool MarkUpdatePublished(int update_id)
        {
            try
            {
                var result = Context.Connection.Execute("UPDATE fso_updates SET publish_date = CURRENT_TIMESTAMP " +
                    "WHERE update_id = @update_id"
                    , new { update_id });
                return (result > 0);
            }
            catch (Exception)
            {
                return false;
            }
        }
    }
}
