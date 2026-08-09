using FSO.Common.Utils;
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
