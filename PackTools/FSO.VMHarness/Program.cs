using FSO.Common.Utils;
using Microsoft.Xna.Framework;
using FSO.Content;
using FSO.LotView.Model;
using FSO.SimAntics;
using FSO.SimAntics.Engine.TSOTransaction;
using FSO.SimAntics.Entities;
using FSO.SimAntics.Model;
using FSO.SimAntics.NetPlay.Drivers;
using FSO.SimAntics.NetPlay.Model.Commands;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace FSO.VMHarness
{
    /// <summary>
    /// Headless VM test harness: boots FSO.SimAntics without a graphics device or network,
    /// loads a compiled pack object, runs an interaction for N ticks, and dumps a trace.
    /// </summary>
    class Program
    {
        /// <summary>
        /// Loads a blueprint XML into a headless VM and reports what the architecture actually
        /// contains afterwards. Asserts on tile state rather than on the command returning
        /// without throwing — a blueprint that parses but places nothing looks identical to a
        /// working one from the outside.
        /// </summary>
        static void HouseMain(string[] args)
        {
            var housePath = Path.GetFullPath(args.Length > 1 ? args[1] : "PackTools/examples/house-one-room.xml");
            var gameLocation = args.Length > 2 ? args[2] :
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                    "Library/Application Support/The Sims Online/TSOClient");
            if (!gameLocation.EndsWith(Path.DirectorySeparatorChar.ToString()))
                gameLocation += Path.DirectorySeparatorChar;

            if (!File.Exists(housePath)) throw new Exception("Blueprint not found at " + housePath);
            if (!Directory.Exists(gameLocation)) throw new Exception("TSO game content not found at " + gameLocation);

            // FSO resolves its own content by relative path, so run from a scratch dir holding a
            // symlinked copy of the repo's Content tree (same approach as the object harness).
            var repoRoot = RepoRoot();
            var fsoContentDir = Path.Combine(repoRoot, "TSOClient", "FSO.Content.TSO", "Content");
            var work = Path.Combine(Path.GetTempPath(), "fso-househarness-work");
            if (Directory.Exists(work)) Directory.Delete(work, true);
            var workContent = Path.Combine(work, "Content");
            Directory.CreateDirectory(workContent);
            foreach (var entry in Directory.GetFileSystemEntries(fsoContentDir))
            {
                var link = Path.Combine(workContent, Path.GetFileName(entry));
                if (Directory.Exists(entry)) Directory.CreateSymbolicLink(link, entry);
                else File.CreateSymbolicLink(link, entry);
            }
            Directory.SetCurrentDirectory(work);

            Console.Error.WriteLine("Booting headless VM...");
            VM.UseWorld = false;
            VMContext.InitVMConfig(false);
            Content.Content.Init(gameLocation, ContentMode.SERVER);

            var vm = new VM(new VMContext(null), new VMServerDriver(new VMTSOGlobalLinkStub()), new VMNullHeadlineProvider());
            vm.Init();

            Console.Error.WriteLine("Restoring blueprint: " + housePath);
            vm.SendCommand(new VMBlueprintRestoreCmd
            {
                JobLevel = -1,
                XMLData = File.ReadAllBytes(housePath),
                FloorClipX = 0, FloorClipY = 0, FloorClipWidth = 0, FloorClipHeight = 0,
                OffsetX = 0, OffsetY = 0, TargetSize = 0
            });
            vm.Tick();

            var arch = vm.Context.Architecture;
            int floors = 0, wallTiles = 0, wallSegs = 0;
            for (short x = 0; x < arch.Width; x++)
            {
                for (short y = 0; y < arch.Height; y++)
                {
                    if (arch.GetFloor(x, y, 1).Pattern != 0) floors++;
                    var w = arch.GetWall(x, y, 1);
                    if (w.Segments != 0)
                    {
                        wallTiles++;
                        for (int b = 0; b < 4; b++) if (((int)w.Segments & (1 << b)) != 0) wallSegs++;
                    }
                }
            }

            var rooms = arch.RoomData?.Count ?? 0;
            int indoor = 0;
            if (arch.RoomData != null) foreach (var r in arch.RoomData) if (!r.IsOutside) indoor++;

            // Doors are objects, not wall attributes: an object flagged ArchitectualDoor calls
            // SetWallStyle on placement, which clears TopLeftSolid/TopRightSolid on its wall tile,
            // which stops VMRoomMap adding a pathing obstacle there. So the only honest check that
            // a door landed is to ask the architecture whether the wall it sits in is still solid.
            int doorCuts = 0;
            for (short x = 0; x < arch.Width; x++)
                for (short y = 0; y < arch.Height; y++)
                {
                    var w = arch.GetWall(x, y, 1);
                    if (w.TopLeftDoor) doorCuts++;
                    if (w.TopRightDoor) doorCuts++;
                }

            // An object whose blueprint level is 0 is never positioned by VMWorldActivator
            // (CreateObject only calls SetPosition when Level != 0) and sits out of world with no
            // error raised. Report it rather than letting a door silently not exist.
            int placed = 0, outOfWorld = 0;
            foreach (var ent in vm.Entities)
            {
                if (ent is FSO.SimAntics.VMAvatar) continue;
                if (ent.Position == LotTilePos.OUT_OF_WORLD) outOfWorld++; else placed++;
            }

            // The blueprint's own object list, re-read so a failed placement can be retried and
            // its error surfaced. Same parse the restore command does.
            var retryTargets = new List<FSO.LotView.Model.XmlHouseDataObject>();
            if (outOfWorld > 0)
            {
                using (var fs = File.OpenRead(housePath))
                {
                    var model = (FSO.LotView.Model.XmlHouseData)
                        new System.Xml.Serialization.XmlSerializer(typeof(FSO.LotView.Model.XmlHouseData)).Deserialize(fs);
                    if (model.Objects != null) retryTargets.AddRange(model.Objects);
                }
            }

            Console.WriteLine("floor tiles:  " + floors);
            Console.WriteLine("wall tiles:   " + wallTiles + " (" + wallSegs + " segments)");
            Console.WriteLine("rooms:        " + rooms + " (" + indoor + " indoor)");
            Console.WriteLine("objects:      " + placed + " placed, " + outOfWorld + " out of world");
            Console.WriteLine("door cuts:    " + doorCuts);

            // This harness runs with UseWorld = false, so it can never tell you whether something
            // DRAWS. The one rendering prerequisite it can check cheaply is the lot phone:
            // VMWorldActivator.LoadFromXML only sets VM.TSOState.Size (which
            // VMLotTerrainRestoreTools and VMContext both read) when the blueprint contains
            // 0x313D2F9A. Without it every check below still passes and the client shows an empty
            // grey screen — which is exactly what happened on 2026-08-10.
            var hasPhone = File.ReadAllText(housePath).IndexOf("313D2F9A", StringComparison.OrdinalIgnoreCase) >= 0;
            Console.WriteLine("lot phone:    " + (hasPhone
                ? "present (0x313D2F9A) — lot size will be set"
                : "MISSING — architecture is fine but the CLIENT WILL RENDER NOTHING. " +
                  "Generate with --base Content/Blueprints/empty_lot_fso.xml."));

            // An out-of-world object means SetPosition refused and VMWorldActivator dropped the
            // error on the floor. Say what the object wanted, so the next person does not have to
            // bisect it: WallPlacementFlags' low nibble is the wall configuration it requires,
            // compared against the tile's segments rotated into the object's frame.
            foreach (var ent in vm.Entities)
            {
                if (ent is FSO.SimAntics.VMAvatar) continue;
                if (ent.Position != LotTilePos.OUT_OF_WORLD) continue;
                var wpf = ent.ObjectData[(int)FSO.SimAntics.Model.VMStackObjectVariable.WallPlacementFlags];
                var f2 = (FSO.SimAntics.VMEntityFlags2)ent.ObjectData[(int)FSO.SimAntics.Model.VMStackObjectVariable.FlagField2];
                bool isDoor = (f2 & FSO.SimAntics.VMEntityFlags2.ArchitectualDoor) > 0;
                Console.WriteLine("  out of world: guid 0x" + ent.Object.OBJ.GUID.ToString("X8") +
                    " wallPlacementFlags=0x" + wpf.ToString("X") +
                    " subIndex=" + ent.Object.OBJ.SubIndex +
                    " groupSize=" + ent.MultitileGroup.Objects.Count +
                    (isDoor ? " [ArchitectualDoor]" : ""));

                // VMWorldActivator.CreateObject discards SetPosition's result, so the reason an
                // object refused placement is never reported anywhere. Retry it here purely to
                // read the error back out.
                // Matched by position, not GUID: the XML names a multitile master (a door is
                // 0x23941850) while the entities in the world are its parts (0x048B353D +
                // 0x79C2428F), so a GUID comparison never matches.
                foreach (var o in retryTargets)
                {
                    var res = ent.SetPosition(
                        LotTilePos.FromBigTile((short)o.X, (short)o.Y, (sbyte)o.Level),
                        o.Direction, vm.Context, VMPlaceRequestFlags.AcceptSlots);
                    Console.WriteLine("                retry at (" + o.X + "," + o.Y + ") level " + o.Level +
                        " dir " + o.Direction + " -> " + res.Status);
                    break;
                }
            }

            // Counting indoor rooms lot-wide is NOT a valid check: running this against
            // empty_lot_fso.xml (zero walls) still reports 1 indoor room, so "indoor > 0" passes
            // for a lot with no house on it. Assert on a specific tile instead — the one the
            // caller says is inside the room — and confirm the engine agrees it is not outside.
            if (floors == 0) throw new Exception("FAIL: blueprint loaded but no floor tiles landed.");
            if (wallTiles == 0) throw new Exception("FAIL: blueprint loaded but no walls landed.");

            var probe = args.Length > 3
                ? new Point(int.Parse(args[3].Split(',')[0]), int.Parse(args[3].Split(',')[1]))
                : new Point(33, 33); // interior of house-one-room.xml
            var roomAt = arch.Rooms[0].Map[probe.X + probe.Y * arch.Width] & 0xFFFF;
            var isOutside = roomAt >= arch.RoomData.Count || arch.RoomData[(int)roomAt].IsOutside;
            Console.WriteLine("probe tile:   (" + probe.X + "," + probe.Y + ") -> room " + roomAt +
                (isOutside ? " (OUTSIDE)" : " (indoors)"));
            if (isOutside)
                throw new Exception("FAIL: tile (" + probe.X + "," + probe.Y + ") is still outdoors — the walls do not enclose it.");
            Console.WriteLine("OK: the probe tile is inside an enclosed room.");
        }

        static string RepoRoot()
        {
            var dir = AppContext.BaseDirectory;
            var d = new DirectoryInfo(dir);
            while (d != null && !Directory.Exists(Path.Combine(d.FullName, "TSOClient")))
                d = d.Parent;
            if (d == null) throw new Exception("Could not locate repo root (no TSOClient/ found above " + dir + ")");
            return d.FullName;
        }

        static void Main(string[] args)
        {
            // --house <blueprint.xml> [gameLocation]: load a hand-authored (or generated) house
            // into a live VM and report what actually landed in the architecture. Separate entry
            // point because it needs no compiled object at all — it exercises the blueprint
            // delivery path on its own, which is what makes it useful as a first check.
            if (args.Length > 0 && args[0] == "--house")
            {
                HouseMain(args);
                return;
            }

            var gameLocation = args.Length > 0 ? args[0] :
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                    "Library/Application Support/The Sims Online/TSOClient");
            // Content.cs's file scanner (_ScanFiles) substrings paths by BasePath.Length; without a
            // trailing separator the result keeps a leading '/', which Path.Combine on Unix then
            // treats as absolute, silently discarding BasePath. Trailing slash sidesteps it.
            if (!gameLocation.EndsWith(Path.DirectorySeparatorChar.ToString()))
                gameLocation += Path.DirectorySeparatorChar;
            var objectIffPath = Path.GetFullPath(args.Length > 1 ? args[1] : "/tmp/pack-out/gossip_gnome.iff");
            var interactionName = args.Length > 2 ? args[2] : null; // null = first interaction (TTAB index 0)
            var maxTicks = args.Length > 3 ? int.Parse(args[3]) : 200;

            if (!Directory.Exists(gameLocation))
                throw new Exception("TSO game content not found at " + gameLocation);
            if (!File.Exists(objectIffPath))
                throw new Exception("Object iff not found at " + objectIffPath);

            var repoRoot = RepoRoot();
            var fsoContentDir = Path.Combine(repoRoot, "TSOClient", "FSO.Content.TSO", "Content");

            // Build a scratch working directory: FSO's own Content/ tree (symlinked, read-only source)
            // plus the test object symlinked into Content/Objects/, without touching the checked-in repo.
            var work = Path.Combine(Path.GetTempPath(), "fso-vmharness-work");
            if (Directory.Exists(work)) Directory.Delete(work, true);
            var workContent = Path.Combine(work, "Content");
            Directory.CreateDirectory(workContent);
            foreach (var entry in Directory.GetFileSystemEntries(fsoContentDir))
            {
                var name = Path.GetFileName(entry);
                // Objects/ needs one extra (test) file added, so it can't be a single symlink to the
                // real, checked-in directory - that would make writes into it land in the repo itself.
                if (name == "Objects") continue;
                var link = Path.Combine(workContent, name);
                if (Directory.Exists(entry)) Directory.CreateSymbolicLink(link, entry);
                else File.CreateSymbolicLink(link, entry);
            }
            var realObjectsDir = Path.Combine(fsoContentDir, "Objects");
            var objectsDir = Path.Combine(workContent, "Objects");
            Directory.CreateDirectory(objectsDir);
            foreach (var entry in Directory.GetFileSystemEntries(realObjectsDir))
            {
                var link = Path.Combine(objectsDir, Path.GetFileName(entry));
                if (Directory.Exists(entry)) Directory.CreateSymbolicLink(link, entry);
                else File.CreateSymbolicLink(link, entry);
            }
            File.CreateSymbolicLink(Path.Combine(objectsDir, Path.GetFileName(objectIffPath)), objectIffPath);

            Directory.SetCurrentDirectory(work);

            Console.Error.WriteLine("Booting headless VM (gameLocation=" + gameLocation + ")...");
            VM.UseWorld = false;
            VMContext.InitVMConfig(false);
            Content.Content.Init(gameLocation, ContentMode.SERVER);

            var globalLink = new VMTSOGlobalLinkStub();
            var driver = new VMServerDriver(globalLink);
            var vm = new VM(new VMContext(null), driver, new VMNullHeadlineProvider());
            vm.Init();

            Console.Error.WriteLine("Restoring empty lot blueprint...");
            var emptyLotPath = Path.Combine("Content", "Blueprints", "empty_lot_fso.xml");
            vm.SendCommand(new VMBlueprintRestoreCmd
            {
                JobLevel = -1,
                XMLData = File.ReadAllBytes(emptyLotPath),
                FloorClipX = 0,
                FloorClipY = 0,
                FloorClipWidth = 0,
                FloorClipHeight = 0,
                OffsetX = 0,
                OffsetY = 0,
                TargetSize = 0
            });
            vm.Tick();

            var objectFileName = Path.GetFileName(objectIffPath);
            var objRef = Content.Content.Get().WorldObjects.Entries.Values
                .FirstOrDefault(e => e.Source == GameObjectSource.Standalone &&
                    Path.GetFileName(e.FileName) == objectFileName);
            if (objRef == null)
                throw new Exception("Object at " + objectIffPath + " was not picked up by WorldObjectProvider (Content/Objects scan).");
            uint guid = (uint)objRef.ID;
            Console.Error.WriteLine("Loaded object '" + objRef.Name + "' GUID 0x" + guid.ToString("X"));

            var group = vm.Context.CreateObjectInstance(guid, new LotTilePos(32, 32, 1), Direction.NORTH);
            var target = group.Objects[0];

            // CreateObjectInstance places unconditionally (it's how the VM instantiates test/
            // network objects, not how Buy Mode validates a player's placement) — it does NOT
            // surface whether this object could actually be placed there by a player. Call the
            // same check Buy Mode does explicitly, so a "Must place on floor tile"-class bug
            // (AllowedHeightFlags never set on compiler-authored objects) is caught by running
            // the object through the real engine path, not by inspecting its bytecode.
            var placement = vm.Context.GetObjPlace(target, new LotTilePos(32, 32, 1), Direction.NORTH, VMPlaceRequestFlags.UserPlacement);
            Console.Error.WriteLine("GetObjPlace => " + placement.Status);

            var avatarGroup = vm.Context.CreateObjectInstance(VMAvatar.TEMPLATE_PERSON, new LotTilePos(30, 30, 1), Direction.NORTH);
            var avatar = (VMAvatar)avatarGroup.Objects[0];

            var ttab = target.Object.Resource.MainIff.List<FSO.Files.Formats.IFF.Chunks.TTAB>()?.FirstOrDefault();
            int ttaIndex = 0;
            string pushedName = "(index 0)";
            if (interactionName != null && ttab != null)
            {
                var ttas = target.Object.Resource.MainIff.List<FSO.Files.Formats.IFF.Chunks.TTAs>()?.FirstOrDefault();
                for (int i = 0; i < ttab.Interactions.Length; i++)
                {
                    var caption = ttas?.GetString((int)ttab.Interactions[i].TTAIndex);
                    if (string.Equals(caption, interactionName, StringComparison.OrdinalIgnoreCase))
                    {
                        ttaIndex = i;
                        pushedName = caption;
                        break;
                    }
                }
            }

            // An object with no interactions is legitimate — a purely decorative object has
            // nothing to push. Report that as a clean result rather than crashing: the caller
            // can only see the exit code, so an unhandled NRE here looks to an authoring agent
            // like "your object is broken", and it will burn its whole budget rewriting a
            // perfectly good object trying to appease a harness that simply cannot test it.
            if (ttab == null || ttab.Interactions == null || ttab.Interactions.Length == 0)
            {
                Console.Error.WriteLine("Object has no interactions — nothing to push.");
                Console.WriteLine(JsonConvert.SerializeObject(new
                {
                    pushed_interaction = (string)null,
                    placement_status = placement.Status.ToString(),
                    trace = new List<TraceEvent>(),
                    final_state = new Dictionary<string, object>
                    {
                        ["object_attribute_0"] = target.GetAttribute(0),
                        ["object_attribute_1"] = target.GetAttribute(1),
                        ["sim_motive_social"] = avatar.GetMotiveData(VMMotive.Social),
                        ["sim_motive_fun"] = avatar.GetMotiveData(VMMotive.Fun),
                        ["ticks_run"] = 0,
                        ["tick_limit_hit"] = false,
                        ["note"] = "object placed and instantiated successfully; it defines no interactions, so none was pushed",
                    },
                }, Formatting.Indented));
                return;
            }

            var pushedEntry = ttab.Interactions[ttaIndex];
            Console.Error.WriteLine("TTAB[" + ttaIndex + "] Flags=0x" + ((int)pushedEntry.Flags).ToString("X") + " (" + pushedEntry.Flags + ") Flags2=0x" +
                ((int)pushedEntry.Flags2).ToString("X") + " (" + pushedEntry.Flags2 + ") ActionFunction=" + pushedEntry.ActionFunction +
                " TestFunction=" + pushedEntry.TestFunction + " AutonomyThreshold=" + pushedEntry.AutonomyThreshold);
            Console.Error.WriteLine("avatar.AvatarState.Permissions=" + avatar.AvatarState.Permissions + " avatar.PersistID=" + avatar.PersistID);

            Console.Error.WriteLine("Pushing interaction " + pushedName + " (TTAB index " + ttaIndex + ")...");
            target.PushUserInteraction(ttaIndex, avatar, vm.Context, false);
            Console.Error.WriteLine("Post-push: avatar.Thread.Queue.Count=" + avatar.Thread.Queue.Count + " Stack.Count=" + avatar.Thread.Stack.Count);
            if (avatar.Thread.Queue.Count > 0)
            {
                var chk = avatar.Thread.CheckAction(avatar.Thread.Queue[0]);
                Console.Error.WriteLine("CheckAction(queued item) => " + (chk == null ? "NULL (rejected - this is why it never runs)" : "OK, " + chk.Count + " pie entries"));
            }

            var trace = new List<TraceEvent>();
            var prevFrame = new Dictionary<string, (string routine, int ip)>();

            int tick;
            for (tick = 0; tick < maxTicks; tick++)
            {
                vm.Update();

                LogThread(avatar, "sim", tick, trace, prevFrame);
                LogThread(target, "object", tick, trace, prevFrame);

                if (avatar.Thread != null && avatar.Thread.Stack.Count == 0 && avatar.Thread.Queue.Count == 0)
                {
                    trace.Add(new TraceEvent { Tick = tick, Event = "sim_idle", Detail = "avatar thread and queue empty" });
                    break;
                }
            }

            var finalState = new Dictionary<string, object>
            {
                ["object_attribute_0"] = target.GetAttribute(0),
                ["object_attribute_1"] = target.GetAttribute(1),
                ["sim_motive_social"] = avatar.GetMotiveData(VMMotive.Social),
                ["sim_motive_fun"] = avatar.GetMotiveData(VMMotive.Fun),
                ["ticks_run"] = tick,
                ["tick_limit_hit"] = tick >= maxTicks
            };

            var report = new { pushed_interaction = pushedName, placement_status = placement.Status.ToString(), trace, final_state = finalState };
            var json = JsonConvert.SerializeObject(report, Formatting.Indented);
            Console.WriteLine(json);
        }

        class TraceEvent
        {
            public int Tick;
            public string Event;
            public string Detail;
        }

        static void LogThread(VMEntity ent, string label, int tick, List<TraceEvent> trace, Dictionary<string, (string, int)> prev)
        {
            if (ent.Thread == null || ent.Thread.Stack.Count == 0)
            {
                if (prev.ContainsKey(label))
                {
                    trace.Add(new TraceEvent { Tick = tick, Event = "tree_exit", Detail = label });
                    prev.Remove(label);
                }
                return;
            }

            var frame = ent.Thread.Stack[ent.Thread.Stack.Count - 1];
            var routineName = frame.Routine?.Rti?.Name ?? ("id" + frame.Routine?.ID);
            var ip = frame.InstructionPointer;

            if (!prev.TryGetValue(label, out var last) || last.Item1 != routineName)
            {
                trace.Add(new TraceEvent { Tick = tick, Event = "tree_enter", Detail = label + ": " + routineName + " @ node " + ip });
            }
            else if (last.Item2 != ip)
            {
                trace.Add(new TraceEvent { Tick = tick, Event = "node_advance", Detail = label + ": " + routineName + " node " + last.Item2 + " -> " + ip });
            }
            prev[label] = (routineName, ip);
        }
    }
}
