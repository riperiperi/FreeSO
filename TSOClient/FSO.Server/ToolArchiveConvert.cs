using FSO.Server.Database.DA;
using Ninject;
using NLog;
using System;

namespace FSO.Server
{
    internal class ToolArchiveConvert : ITool
    {
        private static Logger LOG = LogManager.GetCurrentClassLogger();
        private IDAFactory DAFactory;

        private string ArchiveUsersCreate = @"ALTER TABLE `fso_users`
ADD COLUMN `display_name` varchar(100) NOT NULL DEFAULT '0';
ALTER TABLE `fso_users`
ADD COLUMN `is_verified` tinyint(3) NOT NULL DEFAULT 1;
ALTER TABLE `fso_users`
ADD COLUMN `shared_user` tinyint(3) NOT NULL DEFAULT 1;
CREATE INDEX `fso_users_display_name` ON `fso_users`(`display_name`);";

        private string User1Update = "UPDATE `fso_users` SET username='archive', register_date=0, email='unused', register_ip='0', last_ip='0', client_id='0', last_login=0 WHERE user_id=1;";

        public ToolArchiveConvert(IDAFactory factory)
        {
            this.DAFactory = factory;
        }

        private void RunCommand(SqlDA da, string sql)
        {
            var context = da.Context;

            var command = context.Connection.CreateCommand();

            command.CommandText = sql;

            try
            {
                var result = command.ExecuteNonQuery();
            }
            catch (Exception e)
            {
                throw e;
            }
        }

        public int Run()
        {
            using (var da = (SqlDA)DAFactory.Get())
            {
                LOG.Info("Adding archive columns to fso_users");

                RunCommand(da, ArchiveUsersCreate);

                LOG.Info("Removing avatar limit triggers");

                RunCommand(da, "DROP TRIGGER `fso_avatars_BEFORE_INSERT`;");

                LOG.Info("Repurposing user 1 as the archive shared user");

                // TODO: If user 1 doesn't exist, create it
                // TODO: avoid username collision on "archive"?

                RunCommand(da, User1Update);

                LOG.Info("Repointing every reference to user 1");

                // already cleared by anon trim: fso_auth_attempts, fso_ip_ban, fso_nhood_ban, fso_auth_tickets, fso_lot_server_tickets, fso_shard_tickets

                // fso_avatars

                RunCommand(da, "UPDATE `fso_avatars` SET user_id=1;");

                // fso_event_participation

                RunCommand(da, "DELETE FROM `fso_event_participation`"); // I don't think there's any reason to keep this around.

                // fso_global_cooldowns

                RunCommand(da, "DELETE FROM `fso_global_cooldowns`"); // I don't think there's any reason to keep this around.

                // fso_mayor_ratings (from/to, from avatar is lost)

                // TODO: how to get past the (from user + to avatar) unique constraint
                //RunCommand(da, "UPDATE `fso_mayor_ratings` SET from_user_id=1, to_user_id=1, from_avatar_id=NULL;");

                LOG.Info("Deleting all other users and auth");

                RunCommand(da, "DELETE FROM `fso_users` WHERE user_id != 1;");
                RunCommand(da, "DELETE FROM `fso_user_authenticate`;");

                // Cleanup
                RunCommand(da, "PRAGMA wal_checkpoint(TRUNCATE)");
                RunCommand(da, "vacuum");
                RunCommand(da, "PRAGMA wal_checkpoint(TRUNCATE)");
            }

            return 0;
        }
    }
}
