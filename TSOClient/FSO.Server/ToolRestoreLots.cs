using FSO.Common.Enum;
using FSO.Server.Database.DA;
using FSO.Server.Database.DA.Lots;
using FSO.Server.Database.DA.Objects;
using FSO.SimAntics;
using FSO.SimAntics.Marshals;
using FSO.SimAntics.Model.TSOPlatform;
using NLog;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace FSO.Server
{
    public class ToolRestoreLots : ITool
    {
        private static Logger LOG = LogManager.GetCurrentClassLogger();
        private IDAFactory DAFactory;
        private RestoreLotsOptions Options;
        private ServerConfiguration Config;

        // avatar ids that we have verified exist
        private Dictionary<uint, uint> AvatarIDs = new Dictionary<uint, uint>();
        private Dictionary<uint, uint> PersistRemap = new Dictionary<uint, uint>();
        private uint OwnerID;

        public ToolRestoreLots(ServerConfiguration config, RestoreLotsOptions options, IDAFactory factory)
        {
            this.Options = options;
            this.DAFactory = factory;
            this.Config = config;
        }

        private void Done()
        {
            if (!Options.Report) Console.WriteLine("Done!");
            else Console.WriteLine("(skipped)");
        }

        private void ReplaceOBJID(List<VMEntityMarshal> entities, uint oldPID, uint newPID)
        {
            foreach (var obj in entities)
            {
                if (obj.PersistID == oldPID) obj.PersistID = newPID;
            }
        }

        private uint RemapAvatarID(IDA da, uint avatarID)
        {
            if (avatarID == 0) return 0;
            uint remapped;
            if (!AvatarIDs.TryGetValue(avatarID, out remapped))
            {
                var ava = da.Avatars.Get(avatarID);
                if (ava == null)
                {
                    Console.WriteLine($"(could not find avatar {avatarID}, replacing with owner id {OwnerID})");
                    AvatarIDs[avatarID] = OwnerID;
                    remapped = OwnerID;
                }
                else
                {
                    AvatarIDs[avatarID] = avatarID;
                    remapped = avatarID;
                }
            }
            return remapped;
        }

        private void CreateDbObject(IDA da, VMEntityMarshal entity, DbLot lot)
        {
            var ownerID = ((VMTSOObjectState)entity.PlatformState).OwnerID;
            var obj = new DbObject()
            {
                budget = (int)((VMTSOEntityState)entity.PlatformState).Budget.Value,
                type = (entity.MasterGUID == 0) ? entity.GUID : entity.MasterGUID,
                lot_id = lot.lot_id,
                owner_id = (ownerID == 0) ? (uint?)null : ownerID,
                shard_id = lot.shard_id,
                dyn_obj_name = "", //get from multitile?
                value = 0 //get from multitile?
            };
            if (!Options.Report)
            {
                uint id = da.Objects.Create(obj);
                PersistRemap[entity.PersistID] = id;
                entity.PersistID = id;
            }
        }

        public int Run()
        {
            if (Options.RestoreFolder == null)
            {
                Console.WriteLine("Please pass: <shard id> <lot folder path>");
                return 1;
            }
            Console.WriteLine("Scanning content, please wait...");

            VMContext.InitVMConfig(false);
            Content.Content.Init(Config.GameLocation, Content.ContentMode.SERVER);

            Console.WriteLine($"Starting property restore - scanning { Options.RestoreFolder }...");

            if (!Directory.Exists(Options.RestoreFolder))
            {
                Console.WriteLine($"Could not find the given directory: { Options.RestoreFolder }");
                return 1;
            }

            var files = Directory.EnumerateFiles(Options.RestoreFolder).Where(x => x.ToLowerInvariant().EndsWith(".fsov")).ToList();

            if (files.Count == 0)
            {
                Console.WriteLine($"Specified folder did not contain any lot saves (*.fsov). Note that blueprint .xmls are not supported.");
                return 1;
            }

            using (var da = DAFactory.Get())
            {
                foreach (var file in files)
                {
                    Console.WriteLine($"===== { Path.GetFileName(file) } =====");

                    var data = File.ReadAllBytes(file);

                    var vm = new VMMarshal();
                    VMTSOLotState state;
                    try
                    {
                        using (var mem = new MemoryStream(data))
                        {
                            vm.Deserialize(new BinaryReader(mem));
                        }
                        state = (VMTSOLotState)vm.PlatformState;
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine($"Could not read FSOV. ({e.Message}) Continuing...");
                        continue;
                    }

                    var lot = new DbLot();
                    lot.name = state.Name;
                    lot.location = state.LotID;
                    lot.description = "Restored from FSOV";
                    if (state.PropertyCategory == 255) state.PropertyCategory = 11;
                    lot.category = (LotCategory)state.PropertyCategory;
                    lot.owner_id = RemapAvatarID(da, state.OwnerID);
                    lot.neighborhood_id = state.NhoodID;
                    lot.ring_backup_num = 0;
                    lot.shard_id = Options.ShardId;
                    lot.skill_mode = state.SkillMode;
                    var random = new Random();

                    OwnerID = lot.owner_id ?? 0;
                    if (lot.owner_id == 0) lot.owner_id = null;

                    Console.WriteLine($"Attempting to restore '{state.Name}', at location {lot.location}.");
                    var originalName = lot.name;
                    int addedOffset = 1;
                    var existingName = da.Lots.GetByName(lot.shard_id, lot.name);
                    while (existingName != null)
                    {
                        lot.name = originalName + " (" + (addedOffset++) + ")";
                        Console.WriteLine($"Lot already exists with name {originalName}. Trying with name {lot.name}.");
                        existingName = da.Lots.GetByName(lot.shard_id, lot.name);
                    }

                    var existingLocation = da.Lots.GetByLocation(Options.ShardId, lot.location);
                    while (existingLocation != null)
                    {
                        lot.location = (uint)(random.Next(512) | (random.Next(512) << 16));
                        Console.WriteLine($"Lot already exists at location {existingLocation.location}. Placing at random location {lot.location}.");
                        existingLocation = da.Lots.GetByLocation(Options.ShardId, lot.location);
                    }

                    var objectFromInventory = 0;
                    var objectFromLot = 0;
                    var objectCreate = 0;
                    var objectIgnore = 0;

                    string lotFolder = "./";
                    if (!Options.Report)
                    {
                        Console.WriteLine($"Creating database entry for lot...");
                        try
                        {
                            lot.lot_id = (int)da.Lots.Create(lot);
                            Console.WriteLine($"Database entry for lot created! (ID {lot.lot_id})");
                        }
                        catch (Exception e)
                        {
                            Console.WriteLine("FATAL! Could not create lot in database.");
                            Console.WriteLine(e.ToString());
                            continue;
                        }

                        Console.WriteLine($"Creating and populating data folder for lot...");
                        try
                        {
                            lotFolder = Path.Combine(Config.SimNFS, $"Lots/{lot.lot_id.ToString("x8")}/");
                            Directory.CreateDirectory(lotFolder);
                        }
                        catch (Exception e)
                        {
                            Console.WriteLine("FATAL! Could not create lot data in NFS.");
                            Console.WriteLine(e.ToString());
                            continue;
                        }
                    }

                    foreach (var obj in vm.Entities)
                    {
                        var estate = obj.PlatformState as VMTSOObjectState;
                        if (estate != null)
                        {
                            estate.OwnerID = RemapAvatarID(da, estate.OwnerID); //make sure the owners exist
                        }
                    }

                    //check the objects
                    var processed = new HashSet<uint>();
                    foreach (var obj in vm.Entities)
                    {
                        if (obj.PersistID == 0 || processed.Contains(obj.PersistID) || obj is VMAvatarMarshal)
                        {
                            if (PersistRemap.ContainsKey(obj.PersistID))
                            {
                                obj.PersistID = PersistRemap[obj.PersistID];
                            }
                            continue;
                        }
                        processed.Add(obj.PersistID);

                        try
                        {
                            //does this object exist in the database?
                            var dbObj = da.Objects.Get(obj.PersistID);
                            var guid = (obj.MasterGUID == 0) ? obj.GUID : obj.MasterGUID;
                            if (dbObj == null)
                            {
                                Console.Write("++");
                                Console.Write(guid);
                                Console.Write(": Does not exist in DB. Creating new entry...");
                                objectCreate++;
                                CreateDbObject(da, obj, lot);
                                Done();
                            }
                            else
                            {
                                if (dbObj.lot_id != null)
                                {
                                    Console.Write("!!");
                                    Console.Write(dbObj.dyn_obj_name ?? dbObj.type.ToString());
                                    Console.Write(": In another property! ");
                                    if (Options.Safe || Options.Objects)
                                    {
                                        if (Options.Objects)
                                        {
                                            Console.Write("Creating a new entry...");
                                            objectCreate++;
                                            CreateDbObject(da, obj, lot);
                                            Done();
                                        }
                                        else
                                        {
                                            Console.WriteLine("Object will be ignored.");
                                            objectIgnore++;
                                        }
                                    }
                                    else
                                    {
                                        Console.Write("Taking the object back...");
                                        objectFromLot++;
                                        if (!Options.Report) da.Objects.SetInLot(obj.PersistID, (uint)lot.lot_id);
                                        Done();
                                    }
                                }
                                else
                                {
                                    Console.Write("~~");
                                    Console.Write(dbObj.dyn_obj_name ?? dbObj.type.ToString());
                                    Console.Write(": In a user's inventory. ");
                                    if (dbObj.type != guid) Console.Write("(WRONG GUID - MAKING NEW OBJECT) ");
                                    if (Options.Objects || dbObj.type != guid)
                                    {
                                        Console.Write("Creating a new entry...");
                                        objectCreate++;
                                        CreateDbObject(da, obj, lot);
                                        Done();
                                    }
                                    else
                                    {
                                        Console.Write("Taking the object back...");
                                        objectFromInventory++;
                                        if (!Options.Report) da.Objects.SetInLot(obj.PersistID, (uint)lot.lot_id);
                                        Done();
                                    }
                                }
                            }
                        } catch (Exception e)
                        {
                            Console.WriteLine($"Failed - {e.Message}. Continuing...");
                        }
                    }

                    Console.WriteLine($"Objects created: {objectCreate}, Objects from inventory: {objectFromInventory}, Objects from other lot: {objectFromLot}, Objects ignored: {objectIgnore}");
                    Console.WriteLine($"Object/lot owner avatars missing (replaced with lot owner): {AvatarIDs.Count(x => x.Key != x.Value)}");
                    Console.WriteLine("Object scan complete! Serializing restored state...");
                    byte[] newData;
                    using (var mem = new MemoryStream())
                    {
                        using (var writer = new BinaryWriter(mem))
                        {
                            vm.SerializeInto(writer);
                            newData = mem.ToArray();
                        }
                    }
                    Console.WriteLine("New FSOV created. Finalizing restore...");

                    if (!Options.Report)
                    {
                        File.WriteAllBytes(Path.Combine(lotFolder, "state_0.fsov"), newData);
                        Console.WriteLine($"Restoring {Path.GetFileName(file)} complete!");
                        da.Lots.UpdateRingBackup(lot.lot_id, 0);
                    }
                    else
                    {
                        Console.WriteLine($"Report for {Path.GetFileName(file)} complete!");
                    }
                }
            }
            Console.WriteLine("All properties processed. Press any key to exit.");
            Console.ReadKey();
            return 0;
        }
    }
}
