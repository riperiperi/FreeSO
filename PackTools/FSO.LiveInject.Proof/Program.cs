using FSO.LiveInject;
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

namespace FSO.LiveInject.Proof
{
    /// <summary>
    /// Proves the "modify your live running game" claim end to end: boots a VM and ticks it
    /// for real (proving it's genuinely already running, not just freshly initialized), THEN
    /// authors/compiles/injects a pack object that did not exist at boot time via
    /// FSO.LiveInject.LiveObjectInjector, spawns it, and pushes an interaction on it —
    /// all inside the one already-ticking VM/Content session. This is a one-shot proof
    /// harness, not a test suite; it's a sibling to FSO.VMHarness (which only ever loads
    /// objects that exist before boot) rather than a change to it.
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
            if (!gameLocation.EndsWith(Path.DirectorySeparatorChar.ToString()))
                gameLocation += Path.DirectorySeparatorChar;
            var repoRoot = RepoRoot();
            var packJsonPath = args.Length > 1 ? args[1] : Path.Combine(repoRoot, "PackTools", "examples", "gossip-gnome.json");
            var pushInteraction = args.Length > 2 ? args[2] : "Gossip";
            var maxTicks = args.Length > 3 ? int.Parse(args[3]) : 200;
            var preInjectTicks = args.Length > 4 ? int.Parse(args[4]) : 10;

            if (!Directory.Exists(gameLocation))
                throw new Exception("TSO game content not found at " + gameLocation);
            if (!File.Exists(packJsonPath))
                throw new Exception("Pack json not found at " + packJsonPath);

            // Scratch working directory holding a symlinked copy of FSO's Content/ tree — same
            // approach as FSO.VMHarness, but deliberately WITHOUT pre-placing the pack object:
            // the whole point of this proof is that it doesn't exist until injected post-boot.
            var fsoContentDir = Path.Combine(repoRoot, "TSOClient", "FSO.Content.TSO", "Content");
            var work = Path.Combine(Path.GetTempPath(), "fso-liveinject-proof-work");
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

            var report = new ProofReport();

            Console.Error.WriteLine("Booting headless VM (gameLocation=" + gameLocation + ")...");
            VM.UseWorld = false;
            VMContext.InitVMConfig(false);
            FSO.Content.Content.Init(gameLocation, FSO.Content.ContentMode.SERVER);

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

            // Prove the VM is genuinely "already running" before the object exists — tick it for
            // real, not just "initialized and immediately used."
            Console.Error.WriteLine("Ticking " + preInjectTicks + " times BEFORE the object exists, to prove the VM is already live...");
            for (int i = 0; i < preInjectTicks; i++) vm.Update();
            report.PreInjectTicksRun = preInjectTicks;
            report.ObjectExistedBeforeInject = FSO.Content.Content.Get().WorldObjects.Entries.Values
                .Any(e => e.Source == FSO.Content.GameObjectSource.Standalone && e.Name != null && e.Name.Contains("Gossip"));

            Console.Error.WriteLine("Injecting pack " + packJsonPath + " into the LIVE Content singleton...");
            var injectOutDir = Path.Combine(Path.GetTempPath(), "fso-liveinject-proof-out");
            var injectResult = LiveObjectInjector.InjectPack(packJsonPath, injectOutDir);
            report.Inject = new InjectSummary
            {
                Ok = injectResult.Ok,
                Errors = injectResult.Errors,
                Warnings = injectResult.Warnings,
                Objects = injectResult.Objects.Select(o => new { o.Id, Guid = "0x" + o.Guid.ToString("X8"), o.IffPath }).ToList<object>(),
            };
            if (!injectResult.Ok)
            {
                Console.WriteLine(JsonConvert.SerializeObject(report, Formatting.Indented));
                Console.Error.WriteLine("InjectPack failed, aborting.");
                Environment.Exit(1);
            }

            var injected = injectResult.Objects.First();
            Console.Error.WriteLine("Injected " + injected.Id + " GUID 0x" + injected.Guid.ToString("X8") + " — confirming it's now resolvable via Content.Get()...");
            var resolvedAfterInject = FSO.Content.Content.Get().WorldObjects.Get(injected.Guid);
            report.ResolvableAfterInject = resolvedAfterInject != null;
            if (resolvedAfterInject == null)
                throw new Exception("Injected object GUID did not resolve via Content.Get().WorldObjects.Get() — injection did not actually register it.");

            Console.Error.WriteLine("Spawning injected object into the live, still-ticking VM...");
            var group = LiveObjectInjector.Spawn(vm.Context, injected.Guid, new LotTilePos(32, 32, 1), Direction.NORTH);
            var target = group.Objects[0];

            var avatarGroup = vm.Context.CreateObjectInstance(VMAvatar.TEMPLATE_PERSON, new LotTilePos(30, 30, 1), Direction.NORTH);
            var avatar = (VMAvatar)avatarGroup.Objects[0];

            var ttab = target.Object.Resource.MainIff.List<FSO.Files.Formats.IFF.Chunks.TTAB>()?.FirstOrDefault();
            var ttas = target.Object.Resource.MainIff.List<FSO.Files.Formats.IFF.Chunks.TTAs>()?.FirstOrDefault();
            int ttaIndex = 0;
            string pushedName = "(index 0)";
            if (ttab != null)
            {
                for (int i = 0; i < ttab.Interactions.Length; i++)
                {
                    var caption = ttas?.GetString((int)ttab.Interactions[i].TTAIndex);
                    if (string.Equals(caption, pushInteraction, StringComparison.OrdinalIgnoreCase))
                    {
                        ttaIndex = i;
                        pushedName = caption;
                        break;
                    }
                }
            }

            var pushedEntry = ttab.Interactions[ttaIndex];
            Console.Error.WriteLine("TTAB[" + ttaIndex + "] Flags=0x" + ((int)pushedEntry.Flags).ToString("X") + " (" + pushedEntry.Flags + ") Flags2=0x" +
                ((int)pushedEntry.Flags2).ToString("X") + " (" + pushedEntry.Flags2 + ") ActionFunction=" + pushedEntry.ActionFunction +
                " TestFunction=" + pushedEntry.TestFunction + " AutonomyThreshold=" + pushedEntry.AutonomyThreshold);
            Console.Error.WriteLine("avatar.AvatarState.Permissions=" + avatar.AvatarState.Permissions + " avatar.PersistID=" + avatar.PersistID);

            Console.Error.WriteLine("Pushing interaction " + pushedName + " via VMNetInteractionCmd (the real live-play path)...");
            vm.SendCommand(new VMNetInteractionCmd
            {
                Interaction = (ushort)ttaIndex,
                CalleeID = target.ObjectID,
                CallerID = avatar.ObjectID,
                Param0 = 0,
                Global = false,
            });
            vm.Update();
            Console.Error.WriteLine("Post-push: avatar.Thread.Queue.Count=" + avatar.Thread.Queue.Count + " Stack.Count=" + avatar.Thread.Stack.Count);
            if (avatar.Thread.Queue.Count > 0)
            {
                var chk = avatar.Thread.CheckAction(avatar.Thread.Queue[0]);
                Console.Error.WriteLine("CheckAction(queued item) => " + (chk == null ? "NULL (rejected)" : "OK, " + chk.Count + " pie entries"));
            }
            report.PushedInteraction = pushedName;

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
            report.PostInjectTicksRun = tick;
            report.Trace = trace;
            report.FinalState = new Dictionary<string, object>
            {
                ["object_attribute_0"] = target.GetAttribute(0),
                ["sim_motive_social"] = avatar.GetMotiveData(VMMotive.Social),
            };

            Console.WriteLine(JsonConvert.SerializeObject(report, Formatting.Indented));
            Console.Error.WriteLine("Done — object authored, compiled, injected into a live VM, spawned, and used, all in one already-running session.");
        }

        class ProofReport
        {
            public int PreInjectTicksRun;
            public bool ObjectExistedBeforeInject;
            public InjectSummary Inject;
            public bool ResolvableAfterInject;
            public string PushedInteraction;
            public int PostInjectTicksRun;
            public List<TraceEvent> Trace;
            public Dictionary<string, object> FinalState;
        }

        class InjectSummary
        {
            public bool Ok;
            public List<string> Errors;
            public List<string> Warnings;
            public List<object> Objects;
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
                trace.Add(new TraceEvent { Tick = tick, Event = "tree_enter", Detail = label + ": " + routineName + " @ node " + ip });
            else if (last.Item2 != ip)
                trace.Add(new TraceEvent { Tick = tick, Event = "node_advance", Detail = label + ": " + routineName + " node " + last.Item2 + " -> " + ip });
            prev[label] = (routineName, ip);
        }
    }
}
