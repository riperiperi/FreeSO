using FSO.Server.Database.DA;
using FSO.Server.Database.DA.Lots;
using FSO.SimAntics.Marshals;
using FSO.SimAntics.Model.TSOPlatform;
using NLog;

namespace FSO.Server
{
    internal class ToolBackupSelection : ITool
    {
        private BackupSelectionOptions Options;
        private static Logger LOG = LogManager.GetCurrentClassLogger();
        private IDAFactory DAFactory;

        private ServerConfiguration Config;
        public ToolBackupSelection(BackupSelectionOptions options, ServerConfiguration config, IDAFactory daFactory)
        {
            Options = options;
            Config = config;
            DAFactory = daFactory;
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

        private Dictionary<uint, int> GetRoommateObjectCounts(VMMarshal vm)
        {
            var result = new Dictionary<uint, int>();
            var state = (VMTSOLotState)vm.PlatformState;

            if (state.OwnerID != 0)
            {
                result[state.OwnerID] = 0;
            }

            foreach (var roomie in state.Roommates)
            {
                result[roomie] = 0;
            }

            foreach (var ent in vm.Entities)
            {
                if (ent is VMGameObjectMarshal obj)
                {
                    var objState = (VMTSOObjectState)obj.PlatformState;

                    if (result.TryGetValue(objState.OwnerID, out int toUpdate))
                    {
                        result[objState.OwnerID] = toUpdate + 1;
                    }
                }
            }

            return result;
        }

        private string GetLotDirectory(uint id)
        {
            return Path.Combine(Config.SimNFS, "Lots/", id.ToString("x8"));
        }

        private const int MinObjectsMissing = 10;
        private const int MissingRoomieExtraScore = 10;

        private static int GetNewObjectCount(Dictionary<uint, int> future, Dictionary<uint, int> past)
        {
            var sharedOwners = new HashSet<uint>(future.Keys);
            sharedOwners.IntersectWith(past.Keys);

            int newObjectCount = 0;

            foreach (var owner in sharedOwners)
            {
                newObjectCount += future[owner] - past[owner];
            }

            return newObjectCount;
        }

        private static int GetScore(Dictionary<uint, int> refCounts, Dictionary<uint, int> compareCounts)
        {
            // Look for avatars who are no longer roommates
            int score = 0;

            var missing = new HashSet<uint>(compareCounts.Keys);
            missing.ExceptWith(refCounts.Keys);

            var newAvas = new HashSet<uint>(refCounts.Keys);
            newAvas.ExceptWith(compareCounts.Keys);

            foreach (var ex in missing)
            {
                var missingSimObjCount = compareCounts[ex];

                // If they had a notable number of objects, increase the score.
                if (missingSimObjCount > MinObjectsMissing)
                {
                    score += MissingRoomieExtraScore + missingSimObjCount;
                }
            }

            foreach (var newAva in newAvas)
            {
                var newSimObjCount = refCounts[newAva];

                // If they have a notable number of objects, decrease the score.
                if (newSimObjCount > MinObjectsMissing)
                {
                    score -= MissingRoomieExtraScore + newSimObjCount;
                }
            }

            return score;
        }

        private bool ProcessLot(IDA da, DbLot lot)
        {
            int bestBackup = -1;
            int bestScore = 0;
            int bestNewObjectsSince = 0;
            int baseObjectCount = 0;
            Dictionary<uint, int> refCounts = null;
            Dictionary<uint, int> bestCounts = null;
            VMMarshal bestVM = null;

            int backupCount = 10;
            var dir = GetLotDirectory((uint)lot.lot_id);

            if (!Directory.Exists(dir))
            {
                return false;
            }

            // Find the backup with the most roomies and objects

            int nextBackup = lot.ring_backup_num;
            bool modifiedForEnd = false;

            for (int i = 0; i < backupCount; i++)
            {
                try
                {
                    var backup = nextBackup;
                    var path = Path.Combine(dir, $"state_{nextBackup}.fsov");

                    if (File.Exists(path))
                    {
                        var fsov = LoadVM(path);

                        var objCounts = GetRoommateObjectCounts(fsov);

                        if (refCounts == null)
                        {
                            bestBackup = backup;
                            refCounts = objCounts;
                            baseObjectCount = refCounts.Values.Sum();

                            var modifiedTime = File.GetLastWriteTime(path);
                            modifiedForEnd = modifiedTime > new DateTime(2024, 12, 1) && modifiedTime < new DateTime(2024, 12, 12);
                        }
                        else
                        {
                            var backupScore = GetScore(refCounts, objCounts);

                            if (backupScore > bestScore && objCounts.Values.Sum() > baseObjectCount)
                            {
                                bestScore = backupScore;
                                bestNewObjectsSince = GetNewObjectCount(refCounts, objCounts);
                                bestBackup = backup;
                                bestCounts = objCounts;
                                bestVM = fsov;
                            }
                        }
                    }

                    nextBackup--;

                    if (nextBackup < 0)
                    {
                        nextBackup += backupCount;
                    }
                }
                catch (Exception e)
                {
                    if (!(e is FileNotFoundException))
                    {
                        LOG.Warn($" * Failed to load backup {i} for lot {lot.lot_id}: {e.Message}. Continuing until there's a working one.");
                    }

                    nextBackup--;

                    if (nextBackup < 0)
                    {
                        nextBackup += backupCount;
                    }
                }
            }

            // Try to avoid cases where someone added objects to the lot right before the shutdown.
            if (bestScore != 0 && (!modifiedForEnd || bestNewObjectsSince <= 0))
            {
                if (Options.DryRun)
                {
                    LOG.Info($"Lot {lot.name} ({lot.lot_id:x8}) would be switched to backup {bestBackup} from {lot.ring_backup_num} with score {bestScore}");
                }
                else
                {
                    var restoredRoomies = new HashSet<uint>(bestCounts.Keys);
                    restoredRoomies.ExceptWith(refCounts.Keys);
                    int objectsStolen = 0;
                    int totalObjects = 0;

                    foreach (uint simId in restoredRoomies)
                    {
                        var toRestore = bestVM.Entities.Where(x => (x is VMGameObjectMarshal obj) && (((VMTSOObjectState)obj.PlatformState).OwnerID) == simId).Select(x => x.PersistID).ToArray();

                        totalObjects += toRestore.Length;

                        foreach (uint objId in toRestore)
                        {
                            try
                            {
                                var obj = da.Objects.Get(objId);
                                if (obj != null && obj.lot_id == null)
                                {
                                    da.Objects.SetInLot(objId, (uint)lot.lot_id);
                                    objectsStolen++;
                                }
                            } catch { }
                        }
                    }

                    da.Lots.UpdateRingBackupSilent(lot.lot_id, (sbyte)bestBackup);
                    da.Lots.UpdateArchiveFlags(lot.lot_id, 1);

                    LOG.Info($"Lot {lot.name} ({lot.lot_id:x8}) switched to backup {bestBackup} from {lot.ring_backup_num} with score {bestScore}");
                    LOG.Info($"  - Stole {objectsStolen}/{totalObjects} from user inventories");
                }

                if (((VMTSOLotState)bestVM.PlatformState).Name != lot.name)
                {
                    LOG.Info($"  - Previously had name {((VMTSOLotState)bestVM.PlatformState).Name}");
                }

                if (modifiedForEnd)
                {
                    LOG.Info($"  - !!! This lot was modified before the end !!!");
                }

                if (bestNewObjectsSince != 0)
                {
                    LOG.Info($"  + ~~~ Has {bestNewObjectsSince} new objects ~~~");
                }

                return true;
            }

            return false;
        }

        public int Run()
        {
            int processedLots = 0;
            int totalLots = 0;

            LOG.Info("Processing lots for backup selection");

            if (Options.DryRun)
            {
                LOG.Info("-v argument provided, so no changes will be made.");
            }

            using (var da = (SqlDA)DAFactory.Get())
            {
                var shards = da.Shards.All();

                foreach (var shard in shards)
                {
                    int shardId = shard.shard_id;

                    var lots = da.Lots.All(shardId);

                    foreach (var lot in lots)
                    {
                        if (lot.admit_mode < 4 && lot.category != FSO.Common.Enum.LotCategory.community && ProcessLot(da, lot))
                        {
                            processedLots++;
                        }
                    }

                    totalLots += lots.Count();
                }
            }

            LOG.Info($"Selected better backup for {processedLots}/{totalLots} lots.");

            return 0;
        }
    }
}
