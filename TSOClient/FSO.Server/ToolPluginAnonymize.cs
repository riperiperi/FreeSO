using FSO.LotView.Model;
using FSO.Server.Database.DA;
using FSO.Server.Database.DA.Lots;
using FSO.Server.Database.DA.Objects;
using FSO.SimAntics;
using FSO.SimAntics.Engine;
using FSO.SimAntics.Engine.TSOTransaction;
using FSO.SimAntics.Marshals;
using FSO.SimAntics.Model;
using FSO.SimAntics.NetPlay.Drivers;
using FSO.SimAntics.NetPlay.EODs.Handlers.Data;
using FSO.SimAntics.NetPlay.Model.Commands;
using FSO.SimAntics.Primitives;
using Newtonsoft.Json;
using NLog;

namespace FSO.Server
{
    internal class ToolPluginAnonymize : ITool
    {
        private PluginAnonymizeOptions Options;
        private static Logger LOG = LogManager.GetCurrentClassLogger();
        private IDAFactory DAFactory;

        private ServerConfiguration Config;
        public ToolPluginAnonymize(PluginAnonymizeOptions options, ServerConfiguration config, IDAFactory daFactory)
        {
            Options = options;
            Config = config;
            DAFactory = daFactory;
        }

        private struct ModifyCount
        {
            public int Attempt;
            public int Success;

            public void Add(bool success)
            {
                Attempt++;
                
                if (success)
                {
                    Success++;
                }
            }

            public override string ToString()
            {
                return $"{Success}/{Attempt}";
            }
        }

        private class PluginJson
        {
            [JsonProperty("objectID")]
            public uint ObjectID { get; set; }
            [JsonProperty("isReachable")]
            public bool IsReachable { get; set; } = false;
            [JsonProperty("delete")]
            public bool Delete { get; set; } = false;
            [JsonProperty("modified")]
            public bool Modified { get; set; } = false;
        }

        private class SignPluginJson : PluginJson
        {
            [JsonProperty("signFlags")]
            public uint SignFlags { get; set; }
            [JsonProperty("message")]
            public string Message { get; set; }
        }

        private class CardPluginJson : PluginJson
        {
            [JsonProperty("title")]
            public string Title { get; set; }
            [JsonProperty("description")]
            public string Description { get; set; }
            [JsonProperty("cardContents")]
            public string[] CardContents { get; set; }
        }

        private class DoorPluginJson : PluginJson
        {
            [JsonProperty("code")]
            public uint Code { get; set; }
        }

        private class HouseJson
        {
            [JsonProperty("houseName")]
            public string HouseName { get; set; }
            [JsonProperty("houseId")]
            public uint HouseId { get; set; }
            [JsonProperty("houseAdmitMode")]
            public int HouseAdmitMode { get; set; }

            [JsonProperty("signs")]
            public SignPluginJson[] Signs { get; set; }
            [JsonProperty("cards")]
            public CardPluginJson[] Cards { get; set; }
            [JsonProperty("doors")]
            public DoorPluginJson[] Doors { get; set; }
        }

        private class ReviewJson
        {
            [JsonProperty("publicHouses")]
            public HouseJson[] PublicHouses; // Admit all, ban list
            [JsonProperty("privateHouses")]
            public HouseJson[] PrivateHouses; // Admit list, ban all
        }

        private const uint SIGN_PLUGIN = 0x2a6356a0;
        private const uint DRAW_CARD_PLUGIN = 0x895C1CEB;
        private const uint PERMISSION_DOOR_PLUGIN = 0x0A69F29F;

        // There's not really a brilliant way of getting all the objects that use the plugin type,
        // So here's all the ones we expect with base content.
        private static uint[] SignTypes = [
            0xA92EFE75, // Rustic
            0x99F6D314, // Sandwich board
            0xFFEEA490, // Sci-fi
            0xE86BB6D7, // Shop
            0xDCECE8AA, // Theater
            0x23295F48, // robotfactory
            0xD067F355, // Warning
            0xA996978A, // Conference
            0x70BD99F7, // Corkboard
            0xEB402C8A, // Holiday
            0x5700D1C5, // Landmark
            0xBFBB8152, // Neon
            0xA9B78F1D, // Chalkboard L (unused?)
            0xA9A6DF80, // Chalkboard R (unused?)

            // FSO CC
            0x7E055DC7, // Lucky Folding Write Board
            0x59896C56, // Leaf Note Sign
            0x4BA28DDD, // Postcards
            0x2CB89BF8, // Halloween Sign
            0x2C47F9F4, // Chalk it down
            0x584B4823, // Chalk it up
            ];

        private const uint DRAW_A_CARD_TYPE = 0x34E956FE;
        private const uint TELEPORTER_TYPE = 0x96A776CE;
        private const int TELEPORT_INTERACTION = 8;
        public static readonly int TICKRATE = 30;

        private const string DOOR_GLOBALS = "doorglobals";
        private const string TELEPORT_START_ANIM = "a20-teleporter-step-in";
        private const string TELEPORT_FAIL_ANIM = "a20-teleporter-check-self-insideout";

        // If the teleporter start animation plays, it is reachable.
        // If the interaction finishes after this and the fail animation plays, then the teleporter is obstructed
        // If the interaction fihishes after this and the fail animation doesn't play, then the teleporter works.

        private static bool AdmitModePublic(int admitMode)
        {
            return !(admitMode == 1 || admitMode == 3); // admit list, ban all
        }

        private HashSet<int> GetUniqueLots(List<DbObject> objects)
        {
            var result = new HashSet<int>();

            foreach (var obj in objects)
            {
                if (obj.lot_id.HasValue)
                {
                    result.Add(obj.lot_id.Value);
                }
            }

            return result;
        }

        private HashSet<uint> GetUniqueInventorySims(List<DbObject> objects)
        {
            var result = new HashSet<uint>();

            foreach (var obj in objects)
            {
                if (!obj.lot_id.HasValue && obj.owner_id.HasValue)
                {
                    result.Add(obj.owner_id.Value);
                }
            }

            return result;
        }

        private void CleanLot(VM Lot)
        {
            var avatars = new List<VMEntity>(Lot.Entities.Where(x => x is VMAvatar && x.PersistID != 0));
            //step 1, force everyone to leave.
            foreach (var avatar in avatars)
                Lot.ForwardCommand(new VMNetSimLeaveCmd()
                {
                    ActorUID = avatar.PersistID,
                    FromNet = false
                });

            //simulate for a bit to try get rid of the avatars on the lot
            try
            {
                for (int i = 0; i < 30 * TICKRATE && Lot.Entities.FirstOrDefault(x => x is VMAvatar && x.PersistID > 0) != null; i++)
                {
                    Lot.Tick();
                }
            }
            catch (Exception) { } //if something bad happens just immediately try to delete everyone

            avatars = new List<VMEntity>(Lot.Entities.Where(x => x is VMAvatar && (x.PersistID != 0 || (!(x as VMAvatar).IsPet))));
            foreach (var avatar in avatars) avatar.Delete(true, Lot.Context);
        }

        public (VM, DbLot)? AttemptLoad(int lotId)
        {
            DbLot LotPersist;

            using (var da = (SqlDA)DAFactory.Get())
            {
                var lot = da.Lots.Get(lotId);

                if (lot == null) return null;

                LotPersist = lot;
            }

            VM.UseWorld = false;
            var link = new VMTSOGlobalLinkStub();
            link.Database = new SimAntics.Engine.TSOGlobalLink.VMTSOStandaloneDatabase();
            var Lot = new VM(new VMContext(null), new VMServerDriver(link), new VMNullHeadlineProvider());
            Lot.Init();

            //first let's try load our adjacent lots.
            int attempts = 0;
            var lotStr = lotId.ToString("x8");
            var ringSize = Config.Services.Lots.First().RingBufferSize;

            while (++attempts < ringSize)
            {
                LOG.Info("Checking ring " + attempts + " for lot with dbid = " + lotId);
                try
                {
                    var path = Path.Combine(Config.SimNFS, "Lots/" + lotStr + "/state_" + LotPersist.ring_backup_num.ToString() + ".fsov");
                    using (var file = new BinaryReader(File.OpenRead(path)))
                    {
                        var marshal = new VMMarshal();
                        marshal.Deserialize(file);

                        // Don't bother using move flags to rotate.

                        Lot.Load(marshal);
                        CleanLot(Lot);
                        Lot.Reset();
                    }

                    return (Lot, LotPersist);
                }
                catch (Exception e)
                {
                    LOG.Info("Ring load failed with exception: " + e.ToString() + " for lot with dbid = " + lotId);
                    LotPersist.ring_backup_num--;
                    if (LotPersist.ring_backup_num < 0) LotPersist.ring_backup_num += (sbyte)ringSize;
                }
            }

            LOG.Error("FAILED to load all backups for lot with dbid = " + lotId + "! Forcing lot close");
            var backupPath = Path.Combine(Config.SimNFS, "Lots/" + lotStr + "/failedRestore" + (DateTime.Now.ToBinary().ToString()) + "/");
            Directory.CreateDirectory(backupPath);
            foreach (var file in Directory.EnumerateFiles(Path.Combine(Config.SimNFS, "Lots/" + lotStr + "/")))
            {
                File.Copy(file, backupPath + Path.GetFileName(file));
            }

            return null;
        }

        private List<SignPluginJson> GetSignPluginData(List<DbObject> objs)
        {
            return objs.Select(obj => ReadSign(obj.object_id)).Where(x => x != null).ToList();
        }

        private List<CardPluginJson> GetDrawCardPluginData(List<DbObject> objs)
        {
            return objs.Select(obj => ReadCards(obj.object_id)).Where(x => x != null).ToList();
        }

        private List<DoorPluginJson> GetDoorPluginData(List<DbObject> objs)
        {
            return objs.Select(obj => ReadDoor(obj.object_id)).Where(x => x != null).ToList();
        }

        private VMAvatar CreateAvatar(VM vm)
        {
            return (VMAvatar)vm.Context.CreateObjectInstance(VMAvatar.TEMPLATE_PERSON, LotTilePos.OUT_OF_WORLD, Direction.NORTH).Objects[0];
        }

        private void ResetMotives(VMAvatar sim)
        {
            sim.SetMotiveData(VMMotive.Hunger, 100);
            sim.SetMotiveData(VMMotive.Comfort, 100);
            sim.SetMotiveData(VMMotive.Energy, 100);
            sim.SetMotiveData(VMMotive.Bladder, 100);
            sim.SetMotiveData(VMMotive.Hygiene, 100);
            sim.SetMotiveData(VMMotive.Fun, 100);
            sim.SetMotiveData(VMMotive.Social, 100);
        }

        private void ResetPosition(VM vm, VMAvatar sim)
        {
            var mailbox = vm.Entities.FirstOrDefault(x => (x.Object.OBJ.GUID == 0xEF121974 || x.Object.OBJ.GUID == 0x1D95C9B0));
            if (mailbox != null) VMFindLocationFor.FindLocationFor(sim, mailbox, vm.Context, VMPlaceRequestFlags.Default);
            else sim.SetPosition(LotTilePos.FromBigTile(3, 3, 1), Direction.NORTH, vm.Context);
        }

        private const int MAX_INTERACTION_ATTEMPT_COUNT = 30;
        private const int MAX_ROUTING_TICKS = 30 * 180; // 3 minutes

        private void EndInteraction(VM vm, VMAvatar ava, VMQueuedAction action)
        {
            vm.SendCommand(new VMNetInteractionCancelCmd()
            {
                ActorUID = ava.PersistID,
                ActionUID = action.UID,
            });

            for (int i = 0; i < MAX_INTERACTION_ATTEMPT_COUNT; i++)
            {
                // Wait for the interaction to end

                var newAction = ava.Thread.ActiveAction;
                if (newAction != action)
                {
                    return;
                }

                vm.Tick();
            }

            ava.Reset(vm.Context);
        }

        private bool TestSignRouteWithTeleporters(VM vm, VMAvatar ava, VMEntity sign, ref List<VMEntity> teleporterStarts)
        {
            if (TestSignRoute(vm, ava, sign))
            {
                return true;
            }

            if (teleporterStarts == null)
            {
                // Evaluate what teleporters are reachable from the mailbox

                var teleporters = vm.Context.ObjectQueries.GetObjectsByGUID(TELEPORTER_TYPE);

                teleporterStarts = new List<VMEntity>();

                if (teleporters != null)
                {
                    foreach (var teleporter in teleporters)
                    {

                    }
                }
            }

            // Can we get there from any of the teleporters?
            foreach (var start in teleporterStarts)
            {

            }

            return false;
        }

        private bool TestSignRoute(VM vm, VMAvatar ava, VMEntity sign)
        {
            // Place the avatar at the mailbox
            ResetMotives(ava);
            ResetPosition(vm, ava);

            // Interaction 2 is read.
            // for the card thing, interaction 2 is deck info

            vm.SendCommand(new VMNetInteractionCmd()
            {
                Interaction = 2,
                ActorUID = ava.PersistID,
                CalleeID = sign.ObjectID,
                Param0 = 0,
                Global = false
            });

            VMQueuedAction spyAction = null;

            for (int i = 0; i < MAX_INTERACTION_ATTEMPT_COUNT; i++)
            {
                // Wait for the interaction to show up.

                var action = ava.Thread.ActiveAction;
                if (action?.Callee == sign)
                {
                    spyAction = action;
                    break;
                }

                vm.Tick();

                if (i == MAX_INTERACTION_ATTEMPT_COUNT - 1)
                {
                    // Failed?
                    ava.Reset(vm.Context);
                    return false;
                }
            }

            // Wait for the action to either end (return false) or for the plugin to start (return true, forcibly end the interaction and wait)

            for (int i = 0; i < MAX_ROUTING_TICKS; i++)
            {
                // Has the plugin started?

                if (ava.Thread.EODConnection != null)
                {
                    EndInteraction(vm, ava, spyAction);
                    return true;
                }

                var action = ava.Thread.ActiveAction;
                if (action != spyAction)
                {
                    // The interaction ended
                    return false;
                }

                vm.Tick();
            }

            EndInteraction(vm, ava, spyAction);

            return false;
        }

        private HouseJson ProcessLot(int lotId, List<DbObject> allSigns, List<DbObject> allCards, List<DbObject> allDoors)
        {
            // Could be a bit faster by building a dictionary for this before each iteration, but not too important
            var mySigns = allSigns.Where(x => x.lot_id == lotId).ToList();
            var myCards = allCards.Where(x => x.lot_id == lotId).ToList();
            var myDoors = allDoors.Where(x => x.lot_id == lotId).ToList();

            var signData = GetSignPluginData(mySigns);
            var cardData = GetDrawCardPluginData(myCards);
            var doorData = GetDoorPluginData(myDoors);

            if (signData.Count > 0 || cardData.Count > 0)
            {
                var lot = AttemptLoad(lotId);

                if (lot != null)
                {
                    var vm = lot.Value.Item1;
                    var dbLot = lot.Value.Item2;

                    // Create a dummy avatar to route to the destination, with visitor permissions.
                    var dummy = CreateAvatar(vm);
                    dummy.PersistID = 1;
                    vm.Context.ObjectQueries.RegisterAvatarPersist(dummy, dummy.PersistID);
                    vm.MyUID = 1;

                    bool admitGuests = AdmitModePublic(dbLot.admit_mode);

                    // Constructed if any route attempts fail. See ConstructTeleportStarts for more info.
                    List<VMEntity> teleporterStarts = null;

                    foreach (var sign in signData)
                    {
                        // Try to look up the sign on the lot.
                        var realSign = vm.GetObjectByPersist(sign.ObjectID);

                        if (realSign == null) continue;

                        // Can we route to it from the mailbox?
                        sign.IsReachable = TestSignRouteWithTeleporters(vm, dummy, realSign, ref teleporterStarts);
                        sign.Delete = !sign.IsReachable || !admitGuests;
                    }

                    foreach (var card in cardData)
                    {
                        // Try to look up the sign on the lot.
                        var realCard = vm.GetObjectByPersist(card.ObjectID);

                        if (realCard == null) continue;

                        // Can we route to it from the mailbox?
                        card.IsReachable = TestSignRouteWithTeleporters(vm, dummy, realCard, ref teleporterStarts);
                        card.Delete = !card.IsReachable || !admitGuests;
                    }

                    return new HouseJson()
                    {
                        HouseId = (uint)lotId,
                        HouseAdmitMode = dbLot.admit_mode,
                        HouseName = dbLot.name,
                        Signs = [.. signData],
                        Cards = [.. cardData],
                        Doors = [.. doorData]
                    };
                }

                return new HouseJson()
                {
                    HouseId = (uint)lotId,
                    HouseAdmitMode = 0,
                    HouseName = "(invalid)",
                    Signs = [.. signData],
                    Cards = [.. cardData]
                };
            }

            return null;
        }

        private static uint[] GetDoorTypes()
        {
            var builder = new List<uint>();
            var worldObj = Content.Content.Get().WorldObjects;

            var entries = worldObj.Entries.ToList();

            foreach (var obj in entries)
            {
                var objRes = worldObj.Get(obj.Key);

                if (objRes.Resource.SemiGlobal?.Iff?.Filename == DOOR_GLOBALS+".iff")
                {
                    builder.Add(objRes.OBJ.GUID);
                }
            }

            return [.. builder];
        }

        private bool DeletePlugin(uint id, uint pluginID)
        {
            try
            {
                var path = PluginPersistPath(id, pluginID);

                if (Path.Exists(path))
                {
                    File.Delete(path);

                    var pluginDir = Path.GetDirectoryName(path);

                    if (Directory.GetFiles(pluginDir).Length == 0)
                    {
                        Directory.Delete(pluginDir);

                        var objectDir = Path.GetDirectoryName(pluginDir);

                        if (Directory.GetFiles(objectDir).Length == 0)
                        {
                            Directory.Delete(objectDir);
                        }
                    }

                    return true;
                }
            }
            catch
            {
                // ...
            }

            return false;
        }

        private bool ModifySign(uint id, ushort flags, string message)
        {
            try
            {
                var path = PluginPersistPath(id, SIGN_PLUGIN);

                if (Path.Exists(path))
                {
                    var data = new VMEODSignsData()
                    {
                        Flags = flags,
                        Text = message
                    };

                    using var file = File.OpenWrite(path);
                    using var writer = new BinaryWriter(file);

                    data.SerializeInto(writer);

                    return true;
                }
            }
            catch
            {
                // ...
            }

            return false;
        }

        private void ApplyReview(HouseJson house, ref ModifyCount signDeleteCount, ref ModifyCount signUpdateCount, ref ModifyCount cardsDeleteCount, ref ModifyCount doorDeleteCount)
        {
            foreach (var sign in house.Signs)
            {
                if (sign.Delete)
                {
                    signDeleteCount.Add(DeletePlugin(sign.ObjectID, SIGN_PLUGIN));
                }
                else if (sign.Modified)
                {
                    signUpdateCount.Add(ModifySign(sign.ObjectID, (ushort)sign.SignFlags, sign.Message));
                }
            }

            foreach (var card in house.Cards)
            {
                if (card.Delete)
                {
                    cardsDeleteCount.Add(DeletePlugin(card.ObjectID, DRAW_CARD_PLUGIN));
                }
            }

            foreach (var door in house.Doors)
            {
                if (door.Delete)
                {
                    doorDeleteCount.Add(DeletePlugin(door.ObjectID, PERMISSION_DOOR_PLUGIN));
                }
            }
        }

        private void DeleteAll<T>(List<T> list) where T : PluginJson
        {
            foreach (var item in list)
            {
                item.Delete = true;
            }
        }

        private HouseJson ProcessInventory(IDA da, uint sim, List<DbObject> allSigns, List<DbObject> allCards, List<DbObject> allDoors)
        {
            // Could be a bit faster by building a dictionary for this before each iteration, but not too important
            var mySigns = allSigns.Where(x => !x.lot_id.HasValue && x.owner_id == sim).ToList();
            var myCards = allCards.Where(x => !x.lot_id.HasValue && x.owner_id == sim).ToList();
            var myDoors = allDoors.Where(x => !x.lot_id.HasValue && x.owner_id == sim).ToList();

            var signData = GetSignPluginData(mySigns);
            var cardData = GetDrawCardPluginData(myCards);
            var doorData = GetDoorPluginData(myDoors);

            if (signData.Count > 0 || cardData.Count > 0 || doorData.Count > 0)
            {
                DeleteAll(signData);
                DeleteAll(cardData);
                DeleteAll(doorData);

                return new HouseJson()
                {
                    HouseId = sim,
                    HouseName = da.Avatars.Get(sim)?.name ?? "unknown avatar",
                    Signs = [.. signData],
                    Cards = [.. cardData],
                    Doors = [.. doorData]
                };
            }

            return null;
        }

        public int Run()
        {
            LOG.Info("Scanning content");
            VMContext.InitVMConfig(false);
            Content.Content.Init(Config.GameLocation, Content.ContentMode.SERVER);

            var publicHouses = new List<HouseJson>();
            var privateHouses = new List<HouseJson>();

            var doorTypes = GetDoorTypes();

            LOG.Info("Scanning for objects... this might take a while");

            using (var da = (SqlDA)DAFactory.Get())
            {
                var allSigns = new List<DbObject>();
                foreach (uint guid in SignTypes)
                {
                    allSigns.AddRange(da.Objects.GetByType(guid));
                }

                // This might be slightly insane, but I already wrote the code this way before I made it check doors.
                var allDoors = new List<DbObject>();
                foreach (uint guid in doorTypes)
                {
                    allDoors.AddRange(da.Objects.GetByType(guid));
                }

                var allDrawACard = da.Objects.GetByType(DRAW_A_CARD_TYPE);

                var allPluginObjects = new List<DbObject>(allSigns);
                allPluginObjects.AddRange(allDrawACard);
                allPluginObjects.AddRange(allDoors);

                if (Options.InputFile != null)
                {
                    LOG.Info($"Loading input file {Options.InputFile}...");

                    ReviewJson review;
                    try
                    {
                        string json = File.ReadAllText(Options.InputFile);
                        review = JsonConvert.DeserializeObject<ReviewJson>(json);
                    }
                    catch (Exception e)
                    {
                        LOG.Info($"Failed to load JSON: {e.Message}");
                        return 1;
                    }

                    LOG.Info($"Applying public differences in review...");

                    ModifyCount signDeleteCount = default, signUpdateCount = default, cardsDeleteCount = default, doorDeleteCount = default;

                    foreach (var lot in review.PublicHouses)
                    {
                        ApplyReview(lot, ref signDeleteCount, ref signUpdateCount, ref cardsDeleteCount, ref doorDeleteCount);
                    }

                    LOG.Info($" - Signs deleted: {signDeleteCount.ToString()}, Signs updated: {signUpdateCount.ToString()}, Cards deleted: {cardsDeleteCount.ToString()}, Doors deleted: {doorDeleteCount.ToString()}");

                    LOG.Info($"Applying private differences in review...");

                    signDeleteCount = default; signUpdateCount = default; cardsDeleteCount = default; doorDeleteCount = default;

                    foreach (var lot in review.PrivateHouses)
                    {
                        ApplyReview(lot, ref signDeleteCount, ref signUpdateCount, ref cardsDeleteCount, ref doorDeleteCount);
                    }

                    LOG.Info($" - Signs deleted: {signDeleteCount.ToString()}, Signs updated: {signUpdateCount.ToString()}, Cards deleted: {cardsDeleteCount.ToString()}, Doors deleted: {doorDeleteCount.ToString()}");

                    LOG.Info($"Building inventory deletion records...");

                    var objectsByUser = new List<HouseJson>();
                    var sims = GetUniqueInventorySims(allPluginObjects);

                    foreach (var sim in sims)
                    {
                        var simData = ProcessInventory(da, sim, allSigns, allDrawACard, allDoors);

                        if (simData != null)
                        {
                            objectsByUser.Add(simData);
                        }
                    }

                    // The review didn't have lots with only door codes, so add those too
                    var unreviewedDoors = new Dictionary<uint, DbObject>(allDoors.Where(x => x.lot_id.HasValue).Select(x => new KeyValuePair<uint, DbObject>(x.object_id, x)));

                    foreach (var lot in review.PublicHouses)
                    {
                        foreach (var door in lot.Doors)
                        {
                            unreviewedDoors.Remove(door.ObjectID);
                        }
                    }

                    foreach (var lot in review.PrivateHouses)
                    {
                        foreach (var door in lot.Doors)
                        {
                            unreviewedDoors.Remove(door.ObjectID);
                        }
                    }

                    var doorData = GetDoorPluginData([.. unreviewedDoors.Values]);

                    DeleteAll(doorData);

                    objectsByUser.Add(new HouseJson()
                    {
                        HouseId = 0,
                        HouseName = "=== Door codes without signs or cards ===",
                        Doors = doorData.ToArray(),
                        Cards = [],
                        Signs = [],
                        HouseAdmitMode = 0,
                    });

                    var deletionJson = JsonConvert.SerializeObject(objectsByUser.ToArray(), Formatting.Indented);

                    File.WriteAllText("inventoryPluginDeletion.json", deletionJson);

                    LOG.Info($"Succesfully output at inventoryPluginDeletion.json - deleting the plugin data for real");

                    LOG.Info($"Deleting inventory data, and leftover doors");

                    foreach (var lot in objectsByUser)
                    {
                        ApplyReview(lot, ref signDeleteCount, ref signUpdateCount, ref cardsDeleteCount, ref doorDeleteCount);
                    }

                    LOG.Info($" - Signs deleted: {signDeleteCount.ToString()}, Signs updated: {signUpdateCount.ToString()}, Cards deleted: {cardsDeleteCount.ToString()}, Doors deleted: {doorDeleteCount.ToString()}");
                }
                else
                {
                    HashSet<int> lots = GetUniqueLots(allPluginObjects);

                    LOG.Info("Scanning lots for plugin objects and building a report");

                    foreach (var lot in lots)
                    {
                        var lotData = ProcessLot(lot, allSigns, allDrawACard, allDoors);

                        if (lotData != null)
                        {
                            if (AdmitModePublic(lotData.HouseAdmitMode))
                                publicHouses.Add(lotData);
                            else
                                privateHouses.Add(lotData);
                        }
                    }

                    LOG.Info("Done - outputting pluginReview.json");

                    var result = new ReviewJson()
                    {
                        PublicHouses = [.. publicHouses],
                        PrivateHouses = [.. privateHouses]
                    };

                    var json = JsonConvert.SerializeObject(result, Formatting.Indented);

                    File.WriteAllText("pluginReview.json", json);
                }

            }

            // - Find all objects with the interesting plugins, and their owner lots.
            //  - Without a lot, the plugin data should be lost.
            // - Load a lot from the list. Determine a spawn location for a sim at the mailbox.
            //  - Find the plugin object on the lot and load the plugin data.
            //  - If there's plugin data, try see if the object is reachable from the start position.
            //    - Signs can be read from a distance, cards must be accessed from the front.
            //    - Consider the use of teleporters (reachable tps should have their destinations as possible new starting locations), and some special doors that can be passed through (escape room)
            //  - Permission doors are special in that their data should always be cleared (but their effect on routing calculations remains)

            // The user then hand validates the list of plugins that were accepted/rejected automatically, then can modify the json to specifically allow/deny entries based on opinion.
            // This should be with respect to if the content should remain private or not. You can always feed back in the JSON without review.

            return 0;
        }

        private SignPluginJson ReadSign(uint objectPID)
        {
            var data = LoadPluginPersist(objectPID, SIGN_PLUGIN);

            if (data != null)
            {
                try
                {
                    var parsed = new VMEODSignsData(data);

                    return new SignPluginJson()
                    {
                        ObjectID = objectPID,
                        SignFlags = parsed.Flags,
                        Message = parsed.Text
                    };
                }
                catch (Exception ex)
                {
                    return null;
                }
            }

            return null;
        }

        private CardPluginJson ReadCards(uint objectPID)
        {
            var data = LoadPluginPersist(objectPID, DRAW_CARD_PLUGIN);

            if (data != null)
            {
                try
                {
                    var parsed = new VMEODGameCompDrawACardData(data);

                    return new CardPluginJson()
                    {
                        ObjectID = objectPID,
                        Title = parsed.GameTitle,
                        Description = parsed.GameDescription,
                        CardContents = [.. parsed.CardText],
                    };
                }
                catch (Exception ex)
                {
                    return null;
                }
            }

            return null;
        }

        private DoorPluginJson ReadDoor(uint objectPID)
        {
            var data = LoadPluginPersist(objectPID, PERMISSION_DOOR_PLUGIN);

            if (data != null)
            {
                uint result = 0;
                if (uint.TryParse(System.Text.Encoding.UTF8.GetString(data), out result))
                {
                    return new DoorPluginJson()
                    {
                        ObjectID = objectPID,
                        Code = result,
                        Delete = true,
                    };
                }
            }

            return null;
        }

        private string PluginPersistPath(uint objectPID, uint pluginID)
        {
            var objStr = objectPID.ToString("x8");
            return Path.Combine(Config.SimNFS, "Objects/" + objStr + "/Plugin/" + pluginID.ToString("x8") + ".dat");
        }

        private byte[] LoadPluginPersist(uint objectPID, uint pluginID)
        {
            if (objectPID == 0) return null;
            try
            {
                var path = PluginPersistPath(objectPID, pluginID);

                if (!File.Exists(path))
                {
                    return null;
                }

                //if path does not exist, will throw FileNotFoundException
                using (var file = File.Open(path, FileMode.Open))
                {
                    var dat = new byte[file.Length];
                    file.ReadExactly(dat);
                    return dat;
                }
            }
            catch (Exception e)
            {
                //todo: specific types of exception that can be thrown here? instead of just catching em all
                /*
                if (!(e is FileNotFoundException))
                    //LOG.Error(e, 
                    Console.WriteLine("Failed to load plugin persist for object " + objectPID.ToString("x8") + " plugin " + pluginID.ToString("x8") + "!");
                */
                return null;
            }
        }
    }
}
