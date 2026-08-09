using System;
using System.Collections.Generic;
using System.IO;
using FSO.Content;
using FSO.Content.Interfaces;
using FSO.Files.Formats.IFF;
using FSO.LotView.Model;
using FSO.PackCompiler;
using FSO.SimAntics;
using FSO.SimAntics.Entities;

namespace FSO.LiveInject
{
    /// <summary>
    /// Registers a freshly-compiled pack object into an *already-running* game's
    /// Content singleton, so it becomes usable without restarting the client or the VM.
    /// Uses the same AbstractObjectProvider.AddObject/ChangeManager.RegisterObjects
    /// primitive FSO.IDE's NewObjectDialog already uses for live object-editor reloads —
    /// this isn't a new engine capability, just the first caller outside the IDE.
    /// </summary>
    public static class LiveObjectInjector
    {
        public class InjectedObject
        {
            public string Id;
            public uint Guid;
            public string Name;
            public string IffPath;
        }

        public class InjectResult
        {
            public bool Ok;
            public List<string> Errors = new List<string>();
            public List<string> Warnings = new List<string>();
            public List<InjectedObject> Objects = new List<InjectedObject>();
        }

        /// <summary>
        /// Compiles the pack at packJsonPath and registers every emitted object into
        /// Content.Get()'s live WorldObjectProvider. Safe to call while a VM is ticking —
        /// registration only touches Entries/Cache dictionaries, both lock-guarded by
        /// AbstractObjectProvider, and doesn't touch anything the VM reads mid-tick.
        /// </summary>
        public static InjectResult InjectPack(string packJsonPath, string outDir = null, string gameDir = null)
        {
            var result = new InjectResult();

            outDir ??= Path.Combine(Path.GetTempPath(), "fso-live-inject", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(outDir);

            // Injecting into a running client means there IS a content dir — default to the
            // one Content was booted from so cloned appearances get their sprites, otherwise
            // the object appears in the world as nothing.
            gameDir ??= Content.Content.Get()?.BasePath;
            var compileResult = PackCompilerApi.Build(packJsonPath, outDir, gameDir);
            result.Errors.AddRange(compileResult.Diagnostics.Errors);
            result.Warnings.AddRange(compileResult.Diagnostics.Warnings);
            if (!compileResult.Success || compileResult.Report == null)
            {
                result.Ok = false;
                return result;
            }

            var changeManager = FSO.Content.Content.Get().Changes;

            foreach (var objReport in compileResult.Report.Objects)
            {
                // ObjectReport.Iff is the bare filename PackBuilder wrote inside outDir (see
                // PackBuilder.WriteIffs), not a full path — join it before touching disk.
                var iffPath = Path.Combine(outDir, objReport.Iff);
                if (!File.Exists(iffPath))
                {
                    result.Errors.Add($"{objReport.Id}: build report points at \"{iffPath}\" but the file doesn't exist");
                    continue;
                }

                var iff = new IffFile(iffPath);
                iff.InitHash();
                iff.RuntimeInfo.Path = iffPath;
                iff.RuntimeInfo.State = IffRuntimeState.Standalone;

                changeManager.RegisterObjects(iff);

                var guid = Convert.ToUInt32(objReport.Guid, 16);
                result.Objects.Add(new InjectedObject
                {
                    Id = objReport.Id,
                    Guid = guid,
                    Name = objReport.Id,
                    IffPath = iffPath,
                });
            }

            // Buy Mode entries: WorldObjectCatalog builds its item lists once at startup
            // (Init()), so a compiled-after-boot object needs to be pushed in live, same
            // reasoning as ChangeManager.RegisterObjects above for WorldObjects.
            foreach (var entry in compileResult.Report.CatalogEntries)
            {
                WorldObjectCatalog.AddLive(new ObjectCatalogItem
                {
                    GUID = entry.Guid,
                    Category = entry.Category,
                    Price = entry.Price,
                    Name = entry.Name,
                    Tags = entry.Tags,
                });
            }

            result.Ok = result.Errors.Count == 0;
            return result;
        }

        /// <summary>
        /// Spawns a just-injected object into a live, already-ticking VM. Thin wrapper over
        /// VMContext.CreateObjectInstance — same call VMHarness and the buy-catalog path
        /// (VMNetBuyObjectCmd) already use, so this is placing the object the normal way,
        /// not a special-cased "test" spawn.
        /// </summary>
        public static VMMultitileGroup Spawn(VMContext context, uint guid, LotTilePos pos, Direction direction)
        {
            return context.CreateObjectInstance(guid, pos, direction);
        }
    }
}
