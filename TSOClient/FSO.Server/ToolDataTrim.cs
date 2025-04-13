using FSO.Files.Formats.IFF.Chunks;
using FSO.Server.Database.DA;
using FSO.Server.Database.DA.Lots;
using FSO.Server.Servers.Lot;
using FSO.SimAntics;
using FSO.SimAntics.Marshals;
using Ninject;
using NLog;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.Caching;

namespace FSO.Server
{
    internal class ToolDataTrim : ITool
    {
        private static Logger LOG = LogManager.GetCurrentClassLogger();
        private IDAFactory DAFactory;
        private ServerConfiguration Config;
        private DataTrimOptions Options;
        private IKernel Kernel;

        public ToolDataTrim(DataTrimOptions options, IDAFactory factory, ServerConfiguration config, IKernel kernel)
        {
            this.Options = options;
            this.Config = config;
            this.DAFactory = factory;
            this.Kernel = kernel;
        }

        private string GetObjectDirectory(uint id)
        {
            return Path.Combine(Config.SimNFS, "Objects/", id.ToString("x8"));
        }

        private string GetLotDirectory(uint id)
        {
            return Path.Combine(Config.SimNFS, "Lots/", id.ToString("x8"));
        }

        private bool DeleteIfEmpty(string dir)
        {
            if (Directory.GetFileSystemEntries(dir).Length == 0)
            {
                Directory.Delete(dir);

                return true;
            }

            return false;
        }

        private bool DeleteInventoryState(uint id)
        {
            var dir = GetObjectDirectory(id);

            string invPath = Path.Combine(dir, "inventoryState.fsoo");

            if (File.Exists(invPath))
            {
                File.Delete(invPath);
            }

            return DeleteIfEmpty(dir);
        }

        public List<uint> GetObjectIDsNFS()
        {
            var basepath = Path.Combine(Config.SimNFS, "Objects/");
            var result = new List<uint>();

            foreach (var path in Directory.GetDirectories(basepath))
            {
                var idStr = Path.GetFileName(path);

                if (uint.TryParse(idStr, NumberStyles.HexNumber, null, out uint id))
                {
                    result.Add(id);
                }
            }

            return result;
        }

        public List<uint> GetLotIDsNFS()
        {
            var basepath = Path.Combine(Config.SimNFS, "Lots/");
            var result = new List<uint>();

            foreach (var path in Directory.GetDirectories(basepath))
            {
                var idStr = Path.GetFileName(path);

                if (uint.TryParse(idStr, NumberStyles.HexNumber, null, out uint id))
                {
                    result.Add(id);
                }
            }

            return result;
        }

        public List<uint> GetObjectIDsDB(SqlDA da, bool onLot)
        {
            return da.Objects.ListIDs(onLot);
        }

        public List<DbLot> GetLotsDB(SqlDA da)
        {
            // TODO: more than one shard...
            return da.Lots.All(1).ToList();
        }

        private VMMarshal LoadVM(string path)
        {
            using (var file = File.OpenRead(path))
            {
                using (var reader = new BinaryReader(file))
                {
                    var result = new VMMarshal();

                    result.Deserialize(reader);

                    return result;
                }
            }
        }

        private bool VerifyAndTrimBackups(DbLot lot)
        {
            int backupCount = 10;
            var dir = GetLotDirectory((uint)lot.lot_id);

            if (!Directory.Exists(dir))
            {
                return true;
            }

            // Find the newest backup that still works.

            int newestBackup = lot.ring_backup_num;

            for (int i = 0; i < backupCount; i++)
            {
                try
                {
                    var path = Path.Combine(dir, $"state_{newestBackup}.fsov");

                    if (!File.Exists(path))
                    {
                        newestBackup--;

                        if (newestBackup < 0)
                        {
                            newestBackup += backupCount;
                        }

                        continue;
                    }

                    var fsov = LoadVM(path);

                    break;
                }
                catch (Exception e)
                {
                    if (!(e is FileNotFoundException))
                    {
                        LOG.Warn($" * Failed to load backup {i} for lot {lot.lot_id}: {e.Message}. Continuing until there's a working one.");
                    }

                    newestBackup--;

                    if (newestBackup < 0)
                    {
                        newestBackup += backupCount;
                    }
                }

                if (i == 9)
                {
                    LOG.Error($" * Failed to load ALL backups for lot {lot.lot_id}. Leaving it as-is.");
                    return false;
                }
            }

            int oldestBackup = (lot.ring_backup_num + 1) % backupCount;

            for (int i = 0; i < backupCount; i++)
            {
                try
                {
                    var path = Path.Combine(dir, $"state_{oldestBackup}.fsov");

                    if (!File.Exists(path))
                    {
                        oldestBackup = (oldestBackup + 1) % backupCount;

                        continue;
                    }

                    var fsov = LoadVM(path);

                    break;
                }
                catch (Exception e)
                {
                    if (!(e is FileNotFoundException))
                    {
                        LOG.Warn($" * Failed to load oldest backup {i} for lot {lot.lot_id}: {e.Message}. Continuing until there's a working one.");
                    }

                    oldestBackup = (oldestBackup + 1) % backupCount;
                }

                if (i == 9)
                {
                    LOG.Error($" * Failed to load ALL backups for lot {lot.lot_id}. Leaving it as-is.");
                    return false;
                }
            }

            // Delete everything that isn't the oldest and newest backup.

            for (int i = 0; i < backupCount; i++)
            {
                if (i != newestBackup && i != oldestBackup)
                {
                    var path = Path.Combine(dir, $"state_{i}.fsov");
                    if (File.Exists(path))
                    {
                        File.Delete(path);
                    }
                }
            }

            // Delete any contained directories
            foreach (var delDir in Directory.GetDirectories(dir))
            {
                Directory.Delete(delDir, true);
            }

            return true;
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

        public void DeleteAllRows(SqlDA da, string tableName)
        {
            RunCommand(da, $"DELETE FROM `{tableName}`");
        }

        public int Run()
        {
            //TODO: Some content preloading
            LOG.Info("Scanning content");
            VMContext.InitVMConfig(false);
            Content.Content.Init(Config.GameLocation, Content.ContentMode.SERVER);
            Kernel.Bind<Content.Content>().ToConstant(Content.Content.Get());
            Kernel.Bind<MemoryCache>().ToConstant(new MemoryCache("fso_server"));

            using (var da = (SqlDA)DAFactory.Get())
            {
                LOG.Info("Trimming relationships (invalid from)");

                RunCommand(da, "DELETE FROM fso_relationships WHERE NOT EXISTS (SELECT 1 FROM fso_avatars a where from_id = a.avatar_id)");

                LOG.Info("Trimming relationships (low value, person to person)");

                RunCommand(da, "DELETE FROM fso_relationships WHERE value < 5 AND value > -5");

                LOG.Info("Scanning objects for NFS trimming");

                var nfsIds = GetObjectIDsNFS();
                var ids = GetObjectIDsDB(da, false);
                var onLot = GetObjectIDsDB(da, true);

                LOG.Info("Trimming NFS objects (deleted objects)");

                var toDelete = new HashSet<uint>(nfsIds);
                var existsInNfs = new HashSet<uint>();

                foreach (uint id in ids)
                {
                    if (toDelete.Remove(id))
                    {
                        existsInNfs.Add(id);
                    }
                }

                LOG.Info($" - Cleaning state for {toDelete.Count} deleted objects.");

                foreach (uint del in toDelete)
                {
                    var dir = GetObjectDirectory(del);
                    Directory.Delete(dir, true);
                }

                LOG.Info("Trimming NFS objects (deleting state when object is on lot)");

                var deleteNfsOnLot = new List<uint>();

                foreach (uint id in onLot)
                {
                    if (existsInNfs.Contains(id))
                    {
                        deleteNfsOnLot.Add(id);
                    }
                }

                LOG.Info($" - Removing saved inventory state for {deleteNfsOnLot.Count} on-lot objects. (plugin state is kept)");

                int directoryDeleteCount = 0;
                foreach (uint id in deleteNfsOnLot)
                {
                    if (DeleteInventoryState(id))
                    {
                        directoryDeleteCount++;
                    }
                }

                LOG.Info($" - Deleted {directoryDeleteCount} directories (no remaining object state).");

                LOG.Info("Trimming NFS lots (keeping only oldest and newest backup, deleting invalid lot saves)");

                var nfsLotIds = GetLotIDsNFS();
                var lots = GetLotsDB(da);

                var dbLotHash = new HashSet<uint>(lots.Select(lot => (uint)lot.lot_id));

                int deletedLeftoverLots = 0;
                foreach (var lot in nfsLotIds)
                {
                    if (!dbLotHash.Contains(lot))
                    {
                        // This lot doesn't exist on the database... delete it.
                        var dir = GetLotDirectory(lot);

                        Directory.Delete(dir, true);

                        deletedLeftoverLots++;
                    }
                }

                LOG.Info($" - Deleted {deletedLeftoverLots} NFS lots without database entries.");
                LOG.Info($" - Verifying lot data. This could take a while.");

                foreach (var lot in lots)
                {
                    VerifyAndTrimBackups(lot);
                }

                LOG.Info("Clearing fso_auth_attempts");
                DeleteAllRows(da, "fso_auth_attempts");
                LOG.Info("Clearing fso_auth_tickets");
                DeleteAllRows(da, "fso_auth_tickets");
                LOG.Info("Clearing fso_lot_server_tickets");
                DeleteAllRows(da, "fso_lot_server_tickets");
                LOG.Info("Clearing fso_shard_tickets");
                DeleteAllRows(da, "fso_shard_tickets");
                LOG.Info("Clearing fso_tasks");
                DeleteAllRows(da, "fso_tasks");
                LOG.Info("Clearing fso_transactions");
                DeleteAllRows(da, "fso_transactions");

                if (Options.Anon)
                {
                    LOG.Info("Anonymize: Clearing fso_inbox");
                    DeleteAllRows(da, "fso_inbox");
                    LOG.Info("Anonymize: Clearing fso_bookmarks"); // This is also the ignore list.
                    DeleteAllRows(da, "fso_bookmarks");
                    LOG.Info("Anonymize: Removing deleted fso_bulletin_posts"); // This used soft delete for moderation purposes.
                    RunCommand(da, $"DELETE FROM `fso_bulletin_posts` WHERE deleted=1");
                    LOG.Info("Anonymize: Clearing fso_election_votes");
                    DeleteAllRows(da, "fso_election_votes");
                    LOG.Info("Anonymize: Clearing fso_election_freevotes");
                    DeleteAllRows(da, "fso_election_freevotes");
                    LOG.Info("Anonymize: Clearing fso_election_candidates");
                    DeleteAllRows(da, "fso_election_candidates");
                    LOG.Info("Anonymize: Clearing fso_ip_ban");
                    DeleteAllRows(da, "fso_ip_ban");
                    LOG.Info("Anonymize: Clearing fso_lot_visits");
                    DeleteAllRows(da, "fso_lot_visits");
                    LOG.Info("Anonymize: Removing from_user_id from fso_mayor_ratings");
                    //TODO
                    LOG.Info("Anonymize: Clearing fso_nhood_ban");
                    DeleteAllRows(da, "fso_nhood_ban");
                }

                RunCommand(da, "PRAGMA wal_checkpoint(TRUNCATE)");
                RunCommand(da, "vacuum");
                RunCommand(da, "PRAGMA wal_checkpoint(TRUNCATE)");

                //LOG.Info("Anonymize: Clearing fso_lot_admit");
                //LOG.Info("Anonymize: Clearing fso_update_addons (except currently used)");

                // TODO: user + user authenticate anonymization. archive mode conversion actually changes the table for this, and detaches avatars from users.

                // event participation?

                return 1;
            }
        }
    }
}
