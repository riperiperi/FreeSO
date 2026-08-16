using FSO.Content;
using FSO.Common.Utils;
using FSO.LotHostLite.Sandbox;
using FSO.SimAntics;
using FSO.SimAntics.Engine.TSOGlobalLink;
using FSO.SimAntics.Engine.TSOTransaction;
using FSO.SimAntics.Entities;
using FSO.SimAntics.Model;
using FSO.SimAntics.Model.TSOPlatform;
using FSO.SimAntics.NetPlay.Drivers;
using FSO.SimAntics.NetPlay.Model;
using FSO.SimAntics.NetPlay.Model.Commands;

namespace FSO.LotHostLite
{
    /// <summary>
    /// Headless lockstep lot host over FreeSO's sandbox protocol — the same
    /// VMServerDriver/FSOSandboxServer pairing desktop Sandbox Mode wires up
    /// (SandboxGameScreen), minus the client/UI. Boots the VM the way
    /// FSO.VMHarness does, restores a (furnished) blueprint, and ticks at 30Hz.
    ///
    ///   host:  FSO.LotHostLite --house h.xml --tso-dir DIR [--port 37564] [--packs DIR]
    ///   smoke: FSO.LotHostLite smoke --connect 127.0.0.1:37564 --tso-dir DIR
    ///          [--name NAME] [--chat MSG] [--interact OBJGUID] [--ticks N]
    /// </summary>
    public static class Program
    {
        public static int Main(string[] args)
        {
            if (args.Length > 0 && args[0] == "smoke") return SmokeClient.Run(args.Skip(1).ToArray());
            return HostMain(args);
        }

        static string Arg(string[] args, string name, string def = null)
        {
            for (int i = 0; i < args.Length - 1; i++)
                if (args[i] == name) return args[i + 1];
            return def;
        }

        static int HostMain(string[] args)
        {
            var housePath = Path.GetFullPath(Arg(args, "--house") ?? throw new ArgumentException("--house required"));
            var tsoDir = Arg(args, "--tso-dir") ?? throw new ArgumentException("--tso-dir required");
            var port = ushort.Parse(Arg(args, "--port", "37564"));
            var packsDir = Arg(args, "--packs");
            var bareObjects = args.Contains("--bare-objects");
            if (!tsoDir.EndsWith(Path.DirectorySeparatorChar.ToString())) tsoDir += Path.DirectorySeparatorChar;

            BootContent(tsoDir, packsDir, "fso-lothostlite-work", bareObjects);

            Console.WriteLine("[host] booting headless VM...");
            VM.UseWorld = false;
            VMContext.InitVMConfig(false);
            Content.Content.Init(tsoDir, ContentMode.SERVER);

            var globalLink = new VMTSOGlobalLinkStub();
            globalLink.Database = new VMTSOStandaloneDatabase();
            var driver = new VMServerDriver(globalLink);
            var server = new FSOSandboxServer();

            driver.OnDropClient += server.ForceDisconnect;
            driver.OnTickBroadcast += server.Broadcast;
            driver.OnDirectMessage += server.SendMessage;
            server.OnConnect += cli => { Console.WriteLine($"[host] client join persist={cli.PersistID} ip={cli.RemoteIP}"); driver.ConnectClient(cli); };
            server.OnDisconnect += cli => { Console.WriteLine($"[host] client leave persist={cli.PersistID}"); driver.DisconnectClient(cli); };
            server.OnMessage += driver.HandleMessage;

            var vm = new VM(new VMContext(null), driver, new VMNullHeadlineProvider());
            vm.Init();
            // Diagnostic only: SimAntics exceptions are caught per-tick and reported via
            // SignalDialog, but nothing here read it — every engine failure was silent.
            vm.OnDialog += (info) =>
                Console.WriteLine($"[host] vm dialog [{info.Title}] {info.Message}");

            Console.WriteLine("[host] restoring blueprint: " + housePath);
            vm.SendCommand(new VMBlueprintRestoreCmd
            {
                JobLevel = -1,
                XMLData = File.ReadAllBytes(housePath),
                FloorClipX = 0, FloorClipY = 0, FloorClipWidth = 0, FloorClipHeight = 0,
                OffsetX = 0, OffsetY = 0, TargetSize = 0
            });
            vm.Tick();
            Console.WriteLine($"[host] blueprint live: {vm.Entities.Count} entities, arch {vm.Context.Architecture.Width}x{vm.Context.Architecture.Height}");

            // Same lot state the desktop sandbox sets after BlueprintReset — without
            // the validator/category, joining visitors are auto-kicked into the
            // KillTimeout blink loop (avatar Hidden=1, pie menus filtered).
            vm.TSOState.PropertyCategory = 255;
            vm.TSOState.ActivateValidator(vm);
            vm.Context.Clock.Hours = 12;
            vm.TSOState.Size &= unchecked((int)0xFFFF0000);
            vm.TSOState.Size |= (10) | (3 << 8);
            vm.Context.UpdateTSOBuildableArea();

            var furnishPath = Arg(args, "--furnish");
            var manifestPath = Arg(args, "--manifest");
            if (furnishPath != null && manifestPath != null)
                Furnish(vm, furnishPath, manifestPath);

            server.Start(port);
            Console.WriteLine($"[host] sandbox server on 0.0.0.0:{port}; ticking 30Hz");

            long tick = 0;
            var sw = System.Diagnostics.Stopwatch.StartNew();
            while (true)
            {
                // Mina callbacks queue through GameThread.NextUpdate; drain them on
                // the VM thread, exactly like the desktop game loop does.
                GameThread.UpdateExecuting = true;
                GameThread.DigestUpdate(new FSO.Common.Rendering.Framework.Model.UpdateState());
                vm.Tick();
                GameThread.UpdateExecuting = false;

                tick++;
                if (tick % 300 == 0)
                    Console.WriteLine($"[host] tick={tick} entities={vm.Entities.Count} hash={EntityHash(vm)}");

                var target = tick * 33;
                var wait = target - sw.ElapsedMilliseconds;
                if (wait > 0) Thread.Sleep((int)wait);
            }
        }

        /// <summary>Place pack furniture into the live VM the way VMWorldActivator
        /// does for blueprint objects: instance OUT_OF_WORLD, SetPosition, then run
        /// entry point 11 (init) on each multitile part. Runs on the host before
        /// clients join, so the state sync carries the furnished lot.</summary>
        static void Furnish(VM vm, string furnishPath, string manifestPath)
        {
            var manifest = Newtonsoft.Json.Linq.JArray.Parse(File.ReadAllText(manifestPath));
            var guidById = new Dictionary<string, uint>();
            foreach (var m in manifest)
                guidById[(string)m["id"]] = Convert.ToUInt32(((string)m["guid"]).Replace("0x", ""), 16);

            var furnish = Newtonsoft.Json.Linq.JObject.Parse(File.ReadAllText(furnishPath));
            int placed = 0, failed = 0;
            foreach (var f in (Newtonsoft.Json.Linq.JArray)furnish["furniture"])
            {
                var id = (string)f["id"];
                if (!guidById.TryGetValue(id, out var guid)) { failed++; continue; }
                var x = (short)(int)f["x"];
                var y = (short)(int)f["y"];
                var level = (sbyte)((int?)f["level"] ?? 1);
                var dirNum = (int?)f["dir"] ?? 0;
                var dir = dirNum switch
                {
                    2 => LotView.Model.Direction.EAST,
                    4 => LotView.Model.Direction.SOUTH,
                    6 => LotView.Model.Direction.WEST,
                    _ => LotView.Model.Direction.NORTH,
                };

                var group = vm.Context.CreateObjectInstance(guid, LotView.Model.LotTilePos.OUT_OF_WORLD, dir);
                if (group?.Objects == null || group.Objects.Count == 0) { failed++; Console.WriteLine($"[host] furnish {id}: no instance"); continue; }
                var nobj = group.Objects[0];
                var result = nobj.SetPosition(LotView.Model.LotTilePos.FromBigTile(x, y, level), dir, vm.Context,
                    VMPlaceRequestFlags.AcceptSlots);
                if (result.Status != SimAntics.Model.VMPlacementError.Success)
                {
                    Console.WriteLine($"[host] furnish {id} at ({x},{y}): {result.Status}");
                    failed++;
                    continue;
                }
                for (int i = 0; i < nobj.MultitileGroup.Objects.Count; i++)
                    nobj.MultitileGroup.Objects[i].ExecuteEntryPoint(11, vm.Context, true);
                placed++;
            }
            Console.WriteLine($"[host] furnished: {placed} placed, {failed} failed");
        }

        /// <summary>Symlink the repo's FSO content overlay into a scratch working dir
        /// (Content scanners are relative-path based) and copy pack .iffs into its
        /// Content/Objects so the standalone scan registers their GUIDs.
        /// bareObjects drops the repo's own Objects iffs, leaving packs only — the
        /// content layout the browser bundle ships, and every lockstep participant
        /// must resolve the same GUID set.</summary>
        public static void BootContent(string tsoDir, string packsDir, string workName, bool bareObjects = false)
        {
            var repoRoot = RepoRoot();
            var fsoContentDir = Path.Combine(repoRoot, "TSOClient", "FSO.Content.TSO", "Content");
            var work = Path.Combine(Path.GetTempPath(), workName);
            if (Directory.Exists(work)) Directory.Delete(work, true);
            var workContent = Path.Combine(work, "Content");
            Directory.CreateDirectory(workContent);
            foreach (var entry in Directory.GetFileSystemEntries(fsoContentDir))
            {
                var link = Path.Combine(workContent, Path.GetFileName(entry));
                if (Directory.Exists(entry)) Directory.CreateSymbolicLink(link, entry);
                else File.CreateSymbolicLink(link, entry);
            }

            if (packsDir != null)
            {
                // Objects is a symlink to the repo tree; replace with a real dir so the
                // pack iffs never land inside the repo.
                var objectsLink = Path.Combine(workContent, "Objects");
                var realObjects = Path.Combine(work, "ObjectsReal");
                Directory.CreateDirectory(realObjects);
                foreach (var f in Directory.GetFiles(new DirectoryInfo(objectsLink).LinkTarget ?? objectsLink))
                {
                    if (bareObjects && f.EndsWith(".iff")) continue;
                    File.CreateSymbolicLink(Path.Combine(realObjects, Path.GetFileName(f)), f);
                }
                File.Delete(objectsLink);
                Directory.Move(realObjects, objectsLink);
                foreach (var iff in Directory.GetFiles(packsDir, "*.iff"))
                    File.Copy(iff, Path.Combine(objectsLink, Path.GetFileName(iff)), true);
                Console.WriteLine($"[host] packs installed: {Directory.GetFiles(objectsLink, "*.iff").Length} iffs in overlay Objects/");
            }

            Directory.SetCurrentDirectory(work);
        }

        public static string RepoRoot()
        {
            var dir = AppContext.BaseDirectory;
            while (dir != null && !Directory.Exists(Path.Combine(dir, "TSOClient")))
                dir = Path.GetDirectoryName(dir);
            if (dir == null) throw new Exception("repo root not found above " + AppContext.BaseDirectory);
            return dir;
        }

        public static long EntityHash(VM vm)
        {
            long h = 17;
            foreach (var ent in vm.Entities)
            {
                unchecked
                {
                    h = h * 31 + ent.ObjectID;
                    h = h * 31 + (long)(ent.Object?.OBJ?.GUID ?? 0);
                    h = h * 31 + ent.Position.x;
                    h = h * 31 + ent.Position.y;
                    h = h * 31 + ent.Position.Level;
                }
            }
            return h;
        }
    }
}
