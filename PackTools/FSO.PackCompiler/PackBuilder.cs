using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using FSO.Files.Formats.IFF;
using FSO.Files.Formats.IFF.Chunks;
using FSO.PackCompiler.ArtGen;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace FSO.PackCompiler
{
    /// <summary>
    /// Builds one .iff per pack object out of FSO.Files chunk classes, plus a build report.
    /// Chunk id conventions: OBJD 1, TTAB/TTAs 128, anim table STR# 129 (per FSO.IDE's
    /// NewObjectDialog), private dialog STR# 301, CTSS 2000, private BHAVs 4096+.
    /// </summary>
    public class PackBuilder
    {
        public const ushort OBJD_ID = 1;
        public const ushort TTAB_ID = 128;
        public const ushort ANIM_TABLE_ID = 129;
        public const ushort DIALOG_STR_ID = 301;
        public const ushort CTSS_ID = 2000;
        public const ushort PRIVATE_TREE_BASE = 4096;
        public const ushort GENERATED_APPEARANCE_ID = 500;

        private readonly Diagnostics D;

        // Base game content dir, or null. When null, appearance.clone_from_guid stays a
        // recorded note (no sprites) exactly as before — so compiling without a TSO install
        // keeps working.
        //
        // NOT the same directory as Install(pack, gameDir)'s "gameDir" parameter below, despite
        // the matching name — this field is the SPRITE SOURCE (e.g. ~/Library/Application
        // Support/The Sims Online/TSOClient), Install's parameter is the INSTALL TARGET (e.g.
        // /Applications/FreeSO.app/Contents/MacOS/Content, where the .iff gets written). Mixing
        // these up is the exact trap that let objects install silently invisible before Install()
        // started checking ObjectReport.GraphicsMissing.
        private readonly string GameDir;

        public PackBuilder(Diagnostics d, string gameDir = null)
        {
            D = d;
            GameDir = gameDir;
        }

        public BuildReport Build(PackFile pack, string outDir, bool write)
        {
            var report = Compile(pack, out var built);

            if (write && !D.HasErrors)
            {
                Directory.CreateDirectory(outDir);
                WriteIffs(built, outDir);
                CatalogXml.WriteFragment(Path.Combine(outDir, "catalog-entries.xml"),
                    pack.Objects.Select(CatalogEntry.For));
                File.WriteAllText(Path.Combine(outDir, "build-report.json"),
                    JsonConvert.SerializeObject(report, Formatting.Indented));
            }
            return report;
        }

        /// <summary>
        /// Compiles the pack, copies the iffs into {gameDir}/Objects/ and upserts the
        /// objects' Buy Mode entries into {gameDir}/Objects/catalog_downloads.xml.
        /// </summary>
        /// <param name="gameDir">The INSTALL TARGET — where the .iff and catalog_downloads.xml
        /// get written (e.g. /Applications/FreeSO.app/Contents/MacOS/Content). NOT the same
        /// directory as this class's GameDir field, which is the SPRITE SOURCE passed to the
        /// constructor. Same word, two different directories — see GameDir's declaration.</param>
        public BuildReport Install(PackFile pack, string gameDir)
        {
            var report = Compile(pack, out var built);

            // Install() means "deploy into an actual game" — unlike Build()/Validate(), there
            // is no legitimate reason to install an object that will render as invisible, so
            // GraphicsMissing (a soft Note everywhere else) becomes a hard error here, BEFORE
            // anything is written. Naming both directories explicitly because they are easy to
            // confuse: gameDir (this method's parameter — the INSTALL TARGET, where the .iff
            // and catalog_downloads.xml get written, e.g. /Applications/FreeSO.app/Contents/
            // MacOS/Content) is a different directory from this PackBuilder's GameDir field
            // (the SPRITE SOURCE passed to the constructor, e.g. ~/Library/Application
            // Support/The Sims Online/TSOClient, where clone_from_guid reads sprites FROM).
            foreach (var obj in report.Objects)
            {
                if (obj.GraphicsMissing)
                {
                    D.Error(obj.Id,
                        "install would deploy an object with no cloned sprites — it renders as invisible in Buy Mode. " +
                        (GameDir == null
                            ? "No sprite-source directory (the base game/TSO content install, e.g. ~/Library/Application Support/The Sims Online/TSOClient — NOT the install target " + gameDir + ") was supplied. Pass --tso-dir <dir>."
                            : "A sprite-source directory (" + GameDir + ") was supplied but the clone copied zero graphics — this is a bug in AppearanceCloner, not a missing directory."));
                }
            }

            if (!D.HasErrors)
            {
                var objectsDir = Path.Combine(gameDir, "Objects");
                Directory.CreateDirectory(objectsDir);
                WriteIffs(built, objectsDir);
                CatalogXml.Upsert(Path.Combine(objectsDir, "catalog_downloads.xml"),
                    pack.Objects.Select(CatalogEntry.For));
            }
            return report;
        }

        // compile + validate everything first; callers write files only when the whole pack is clean
        private BuildReport Compile(PackFile pack, out List<(ObjectReport Report, IffFile Iff)> built)
        {
            var report = new BuildReport
            {
                PackId = pack.Meta.Id,
                PackVersion = pack.Meta.Version,
            };

            built = new List<(ObjectReport Report, IffFile Iff)>();
            foreach (var obj in pack.Objects)
            {
                built.Add(BuildObject(obj, pack.PackDirectory));
            }
            foreach (var b in built) report.Objects.Add(b.Report);
            report.Warnings = D.Warnings.ToList();
            if (!D.HasErrors) report.CatalogEntries = pack.Objects.Select(CatalogEntry.For).ToList();
            return report;
        }

        private void WriteIffs(List<(ObjectReport Report, IffFile Iff)> built, string dir)
        {
            foreach (var (objReport, iff) in built)
            {
                var path = Path.Combine(dir, objReport.Iff);
                using (var stream = new FileStream(path, FileMode.Create))
                    iff.Write(stream);
            }
        }

        private (ObjectReport, IffFile) BuildObject(PackObject obj, string packDirectory)
        {
            var objReport = new ObjectReport
            {
                Id = obj.Id,
                Guid = "0x" + obj.Guid.ToString("X8"),
                Iff = obj.Id + ".iff",
                CloneFromGuid = obj.CloneFromGuid == null ? null : "0x" + obj.CloneFromGuid.Value.ToString("X8"),
                Category = obj.Category,
            };
            if (obj.CloneFromGuid != null && GameDir == null)
            {
                objReport.Notes.Add("appearance.clone_from_guid recorded only: no base game content directory was supplied at compile time, so no sprites were copied — this object will be INVISIBLE in the game client");
                objReport.GraphicsMissing = true;
            }

            // tree ids, in declaration order
            var treeIds = new Dictionary<string, ushort>();
            for (int i = 0; i < obj.Trees.Count; i++)
            {
                treeIds[obj.Trees[i].Name] = (ushort)(PRIVATE_TREE_BASE + i);
                objReport.Trees[obj.Trees[i].Name] = PRIVATE_TREE_BASE + i;
            }

            for (int i = 0; i < obj.Attributes.Count; i++) objReport.Attributes[obj.Attributes[i]] = i;

            // interaction name -> TTAIndex (position in list)
            var interactionIndex = new Dictionary<string, int>();
            for (int i = 0; i < obj.Interactions.Count; i++)
            {
                if (obj.Interactions[i].Name != null) interactionIndex[obj.Interactions[i].Name] = i;
                objReport.Interactions[obj.Interactions[i].Name ?? ("#" + i)] = i;
            }

            // compile trees
            var compiler = new TreeCompiler(D, obj, treeIds, interactionIndex);
            var compiled = new List<(PackTree Tree, BHAVInstruction[] Instructions)>();
            foreach (var tree in obj.Trees)
            {
                compiled.Add((tree, compiler.Compile(tree)));
            }

            // Every object needs AllowedHeightFlags bit 0 set (my_object[4] = 1) to be placeable
            // at all — without it, VMContext.GetObjPlace (tso.simantics/VMContext.cs) reports
            // "Must place on floor tile" and UIObjectHolder renders the object floating a level
            // up, on every object we compile. Base-game objects get this from a semiglobal
            // routine that runs before their own init logic; nothing wires our compiled objects
            // into that inheritance, so it's never set. Rather than chase down and decompile
            // that semiglobal, set it directly: a synthetic tree that runs first, sets the flag,
            // then calls the pack's own init tree (if any) so authored init logic still runs.
            var placementInitId = (ushort)(PRIVATE_TREE_BASE + obj.Trees.Count);
            treeIds["__placement_init"] = placementInitId;
            var placementInitTree = BuildPlacementInitTree(obj);
            compiled.Add((placementInitTree, compiler.Compile(placementInitTree)));

            if (D.HasErrors) return (objReport, null); // refuse to emit on any error

            var iff = new IffFile();

            // OBJD
            var objd = NewChunk<OBJD>(iff, OBJD_ID, obj.Name);
            objd.ObjectType = OBJDType.Normal;
            objd.GUID = obj.Guid;
            objd.Price = (ushort)obj.Price;
            objd.SalePrice = (ushort)obj.Price;
            objd.NumAttributes = (ushort)obj.Attributes.Count;
            objd.AnimationTableID = ANIM_TABLE_ID;
            objd.CatalogStringsID = CTSS_ID;
            objd.SubIndex = -1;
            if (obj.Interactions.Count > 0) objd.TreeTableID = TTAB_ID;
            if (obj.EntryMain != null) objd.BHAV_MainID = treeIds[obj.EntryMain];
            // Always the synthetic placement-flags tree, never obj.EntryInit directly — see its
            // construction above. It calls obj.EntryInit itself when the pack declares one.
            objd.BHAV_Init = placementInitId;

            // Appearance: copy the source object's draw groups + sprites inline. Must happen
            // into this same IffFile — DGRP.GetTexture resolves its SPR2 through the chunk's
            // own ChunkParent, so a cross-file reference would silently resolve to null.
            if (obj.CloneFromGuid != null && GameDir != null)
            {
                var clone = AppearanceCloner.Clone(obj.CloneFromGuid.Value, GameDir, iff, D,
                    $"$.objects[{obj.Id}].appearance.clone_from_guid");
                if (clone.Ok)
                {
                    objd.BaseGraphicID = clone.BaseGraphicID;
                    objd.NumGraphics = clone.NumGraphics;
                    objReport.Notes.Add($"appearance cloned from \"{clone.SourceFile}\": {clone.DrawGroupsCopied} draw groups, {clone.SpritesCopied} sprites, {clone.PalettesCopied} palettes (BaseGraphicID={clone.BaseGraphicID}, NumGraphics={clone.NumGraphics})");
                    if (clone.DrawGroupsCopied == 0 || clone.SpritesCopied == 0)
                    {
                        // A game content directory was supplied and the source GUID resolved, but
                        // nothing was actually copied — "cloned nothing" is never a success, even
                        // though clone.Ok is true here. Distinct from the no-GameDir case above,
                        // which is an intentional, documented headless-testing mode; this is a bug.
                        objReport.GraphicsMissing = true;
                        D.Error($"$.objects[{obj.Id}].appearance.clone_from_guid",
                            $"a game content directory was supplied and GUID {objReport.CloneFromGuid} resolved, but the clone copied {clone.DrawGroupsCopied} draw groups and {clone.SpritesCopied} sprites — the object would be invisible despite content being available; this indicates a bug in AppearanceCloner, not a missing directory");
                    }
                }
            }

            // Generated appearance: render original art from scratch and assemble the
            // DGRP/SPR2/PALT chunks inline (same file-locality constraint as clone_from_guid
            // above — DGRP resolves SPR2 through its own ChunkParent only). Needs no game
            // content directory, since nothing is copied from the base game.
            if (obj.Generated != null && obj.Generated.Generator == "chair")
            {
                var mesh = ChairGenerator.Build(obj.Generated.ChairParams);
                var rendered = SpriteAssembler.RenderAllFrames(mesh);
                SpriteAssembler.AddAppearanceChunks(iff, objd, obj.Name, GENERATED_APPEARANCE_ID, rendered);
                objReport.Notes.Add($"appearance generated by \"chair\" generator: {rendered.Count} frames assembled (BaseGraphicID={objd.BaseGraphicID}, NumGraphics={objd.NumGraphics})");
            }
            else if (obj.Generated != null && obj.Generated.Generator == "table")
            {
                var tp = obj.Generated.TableParams;
                var mesh = TableGenerator.Build(tp);
                var rendered = TableGenerator.IsRotationallySymmetric(tp)
                    ? SymmetricAssembler.RenderSymmetricFrames(mesh)
                    : SpriteAssembler.RenderAllFrames(mesh);
                SpriteAssembler.AddAppearanceChunks(iff, objd, obj.Name, GENERATED_APPEARANCE_ID, rendered);
                objReport.Notes.Add($"appearance generated by \"table\" generator: {rendered.Count} frames assembled (BaseGraphicID={objd.BaseGraphicID}, NumGraphics={objd.NumGraphics})");
            }
            else if (obj.Generated != null && obj.Generated.Generator == "bed")
            {
                var mesh = BedGenerator.Build(obj.Generated.BedParams);
                var rendered = SpriteAssembler.RenderAllFrames(mesh);
                SpriteAssembler.AddAppearanceChunks(iff, objd, obj.Name, GENERATED_APPEARANCE_ID, rendered);
                objReport.Notes.Add($"appearance generated by \"bed\" generator: {rendered.Count} frames assembled (BaseGraphicID={objd.BaseGraphicID}, NumGraphics={objd.NumGraphics})");
            }
            else if (obj.Generated != null && obj.Generated.Generator == "lamp")
            {
                // Always rotationally symmetric — see LampGenerator's class remarks.
                var mesh = LampGenerator.Build(obj.Generated.LampParams);
                var rendered = SymmetricAssembler.RenderSymmetricFrames(mesh);
                SpriteAssembler.AddAppearanceChunks(iff, objd, obj.Name, GENERATED_APPEARANCE_ID, rendered);
                objReport.Notes.Add($"appearance generated by \"lamp\" generator: {rendered.Count} frames assembled (BaseGraphicID={objd.BaseGraphicID}, NumGraphics={objd.NumGraphics})");
            }
            else if (obj.Generated != null && obj.Generated.Generator == "storage")
            {
                var mesh = StorageGenerator.Build(obj.Generated.StorageParams);
                var rendered = SpriteAssembler.RenderAllFrames(mesh);
                SpriteAssembler.AddAppearanceChunks(iff, objd, obj.Name, GENERATED_APPEARANCE_ID, rendered);
                objReport.Notes.Add($"appearance generated by \"storage\" generator: {rendered.Count} frames assembled (BaseGraphicID={objd.BaseGraphicID}, NumGraphics={objd.NumGraphics})");
            }
            else if (obj.Generated != null && obj.Generated.Generator == "sofa")
            {
                var mesh = SofaGenerator.Build(obj.Generated.SofaParams);
                var rendered = SpriteAssembler.RenderAllFrames(mesh);
                SpriteAssembler.AddAppearanceChunks(iff, objd, obj.Name, GENERATED_APPEARANCE_ID, rendered);
                objReport.Notes.Add($"appearance generated by \"sofa\" generator: {rendered.Count} frames assembled (BaseGraphicID={objd.BaseGraphicID}, NumGraphics={objd.NumGraphics})");
            }
            else if (obj.Generated != null && obj.Generated.Generator == "primitives")
            {
                var pp = obj.Generated.PartsParams;
                var mesh = PartsGenerator.Build(pp);
                var rendered = pp.Symmetric
                    ? SymmetricAssembler.RenderSymmetricFrames(mesh)
                    : SpriteAssembler.RenderAllFrames(mesh);
                SpriteAssembler.AddAppearanceChunks(iff, objd, obj.Name, GENERATED_APPEARANCE_ID, rendered);
                objReport.Notes.Add($"appearance generated by \"primitives\" generator: {rendered.Count} frames assembled (BaseGraphicID={objd.BaseGraphicID}, NumGraphics={objd.NumGraphics})");
            }
            else if (obj.Imported != null)
            {
                var meshPath = Path.IsPathRooted(obj.Imported.Mesh)
                    ? obj.Imported.Mesh
                    : Path.Combine(packDirectory ?? ".", obj.Imported.Mesh);
                try
                {
                    var mesh = ObjImporter.Load(new ObjImporter.Params
                    {
                        MeshPath = meshPath,
                        Height = obj.Imported.Height,
                        Symmetric = obj.Imported.Symmetric,
                    });
                    var rendered = obj.Imported.Symmetric
                        ? SymmetricAssembler.RenderSymmetricFrames(mesh)
                        : SpriteAssembler.RenderAllFrames(mesh);
                    SpriteAssembler.AddAppearanceChunks(iff, objd, obj.Name, GENERATED_APPEARANCE_ID, rendered);
                    var model = obj.Imported.Provenance?.Model ?? Path.GetFileNameWithoutExtension(meshPath);
                    objReport.Notes.Add($"appearance imported from \"{obj.Imported.Mesh}\" ({model}): {rendered.Count} frames assembled (BaseGraphicID={objd.BaseGraphicID}, NumGraphics={objd.NumGraphics})");
                }
                catch (Exception e)
                {
                    D.Error($"{obj.Path}.appearance.imported", "failed to import mesh: " + e.Message);
                }
            }

            // BHAVs
            foreach (var (tree, instructions) in compiled)
            {
                var bhav = NewChunk<BHAV>(iff, treeIds[tree.Name], tree.Name);
                bhav.Type = 0;
                bhav.Args = (byte)tree.Args.Count;
                bhav.Locals = (ushort)tree.Locals.Count;
                bhav.Version = 0;
                bhav.Instructions = instructions;
            }

            // TTAB + TTAs
            if (obj.Interactions.Count > 0)
            {
                var ttab = NewChunk<TTAB>(iff, TTAB_ID, obj.Name);
                ttab.Interactions = new TTABInteraction[obj.Interactions.Count];
                for (int i = 0; i < obj.Interactions.Count; i++)
                {
                    ttab.Interactions[i] = BuildInteraction(obj.Interactions[i], (uint)i, treeIds);
                }

                var ttas = NewChunk<TTAs>(iff, TTAB_ID, obj.Name);
                SetStrings(ttas, obj.Interactions.Select(x => x.Name ?? "").ToArray());
            }

            // dialog strings (private STR# 301)
            if (obj.DialogStrings.Count > 0)
            {
                var str = NewChunk<STR>(iff, DIALOG_STR_ID, obj.Name + " dialog");
                var max = obj.DialogStrings.Keys.Max();
                var values = new string[max];
                for (int i = 1; i <= max; i++)
                {
                    values[i - 1] = obj.DialogStrings.TryGetValue(i, out var v) ? v : "";
                }
                SetStrings(str, values);
            }

            // catalog strings
            var ctss = NewChunk<CTSS>(iff, CTSS_ID, obj.Name);
            SetStrings(ctss, new[] { obj.Name ?? "", "" });

            // default empty animation table, mirrors FSO.IDE NewObjectDialog
            var anim = NewChunk<STR>(iff, ANIM_TABLE_ID, obj.Name);
            SetStrings(anim, new[] { "" });

            return (objReport, iff);
        }

        // Builds a compiler-owned tree (not authored in pack JSON, so there's no PackParser
        // path that produces one of these) that sets AllowedHeightFlags (my_object scope,
        // index 4 = VMStackObjectVariable.AllowedHeightFlags) to 1 — bit 0, "allowed on floor"
        // — and PlacementFlags (index 42 = VMStackObjectVariable.PlacementFlags) to 3 —
        // OnFloor | OnTerrain, the base-furniture default; without it the flags stay 0 and
        // buy mode rejects every tile ("must be placed on terrain") — then falls through to
        // the pack's own init tree if it declared one.
        private PackTree BuildPlacementInitTree(PackObject obj)
        {
            var path = obj.Path + ".__placement_init";
            var hasUserInit = obj.EntryInit != null;

            var setHeightFlags = new PackNode
            {
                Id = "set_height_flags",
                Prim = "expression",
                Then = "set_placement_flags",
                Else = "error",
                Path = path + ".nodes[0]",
            };
            setHeightFlags.Fields = JsonObj.From(JObject.Parse(
                "{\"lhs\":{\"scope\":\"my_object\",\"value\":4},\"op\":\"=\",\"rhs\":{\"scope\":\"literal\",\"value\":1}}"),
                setHeightFlags.Path, D);

            var setPlacementFlags = new PackNode
            {
                Id = "set_placement_flags",
                Prim = "expression",
                Then = hasUserInit ? "call_user_init" : "return true",
                Else = "error",
                Path = path + ".nodes[1]",
            };
            setPlacementFlags.Fields = JsonObj.From(JObject.Parse(
                "{\"lhs\":{\"scope\":\"my_object\",\"value\":42},\"op\":\"=\",\"rhs\":{\"scope\":\"literal\",\"value\":3}}"),
                setPlacementFlags.Path, D);

            var tree = new PackTree
            {
                Name = "__placement_init",
                Path = path,
                Args = new List<string>(),
                Locals = new List<string>(),
            };
            tree.Nodes.Add(setHeightFlags);
            tree.Nodes.Add(setPlacementFlags);

            if (hasUserInit)
            {
                var callUserInit = new PackNode
                {
                    Id = "call_user_init",
                    Call = obj.EntryInit,
                    Then = "return true",
                    Else = "return false",
                    Path = path + ".nodes[2]",
                };
                callUserInit.Fields = JsonObj.From(new JObject(), callUserInit.Path, D);
                tree.Nodes.Add(callUserInit);
            }

            return tree;
        }

        private TTABInteraction BuildInteraction(PackInteraction inter, uint ttaIndex, Dictionary<string, ushort> treeIds)
        {
            var result = new TTABInteraction
            {
                TTAIndex = ttaIndex,
                ActionFunction = (inter.ActionTree != null) ? treeIds[inter.ActionTree] : (ushort)0,
                TestFunction = (inter.TestTree != null) ? treeIds[inter.TestTree] : (ushort)0,
                MotiveEntries = new TTABMotiveEntry[16], // one per VMMotive
                AttenuationCode = inter.AttenuationCode,
                AttenuationValue = inter.AttenuationValue,
                AutonomyThreshold = inter.AutonomyThreshold,
                JoiningIndex = inter.JoiningIndex,
            };
            result.InitMotiveEntries();
            foreach (var entry in inter.AdvertisedMotives)
            {
                result.MotiveEntries[entry.Key].EffectRangeDelta = entry.Value;
            }

            if (inter.HasAllow)
            {
                result.Flags2 = TSOFlags.NonEmpty;
                result.AllowVisitors = inter.AllowVisitors; // sets TTABFlags.AllowVisitors + TSOFlags.AllowVisitors
                result.AllowObjectOwner = inter.AllowOwner;
                result.AllowRoommates = inter.AllowRoommates;
                result.AllowFriends = inter.AllowFriends;
                result.AllowGhosts = inter.AllowGhosts;
                result.AllowCSRs = inter.AllowCSRs;
                result.AllowCats = inter.AllowCats;
                result.AllowDogs = inter.AllowDogs;
            }
            // no allow block: keep TTABInteraction's default Flags2 (0x1e: owner, roommates, friends)

            result.Debug = inter.FlagDebug;
            result.AutoFirst = inter.FlagAutoFirst;
            result.RunImmediately = inter.FlagRunImmediately;
            result.MustRun = inter.FlagMustRun;
            result.AllowConsecutive = inter.FlagAllowConsecutive;
            result.Joinable = inter.FlagJoinable;
            result.Leapfrog = inter.FlagLeapfrog;
            result.Carrying = inter.FlagCarrying;
            result.Repair = inter.FlagRepair;
            result.AlwaysCheck = inter.FlagAlwaysCheck;
            result.WhenDead = inter.FlagWhenDead;

            return result;
        }

        private T NewChunk<T>(IffFile iff, ushort id, string label) where T : IffChunk, new()
        {
            var chunk = new T
            {
                ChunkID = id,
                ChunkLabel = label ?? "",
                ChunkProcessed = true,
                ChunkParent = iff,
                ChunkType = IffFile.CHUNK_TYPES.First(x => x.Value == typeof(T)).Key,
            };
            iff.AddChunk(chunk);
            return chunk;
        }

        private void SetStrings(STR chunk, string[] values)
        {
            // language code 1 = English (US); TSO writer stores code-1
            chunk.LanguageSets[0].Strings = values
                .Select(v => new STRItem { LanguageCode = 1, Value = v ?? "", Comment = "" })
                .ToArray();
        }
    }

    public class BuildReport
    {
        public string PackId;
        public string PackVersion;
        public List<ObjectReport> Objects = new List<ObjectReport>();
        public List<string> Warnings = new List<string>();
        public List<CatalogEntry> CatalogEntries = new List<CatalogEntry>();
    }

    public class ObjectReport
    {
        public string Id;
        public string Guid;
        public string Iff;
        public string Category;
        public string CloneFromGuid;
        // True when a clone_from_guid appearance ended up with zero copied graphics, for
        // whatever reason (no game dir supplied, or a bug in AppearanceCloner) — the object
        // will render as invisible. Install() upgrades this into a hard error; Build()/
        // Validate() leave it as a Notes entry, since compiling without game content is an
        // intentional, documented headless-testing mode (SCHEMA.md).
        public bool GraphicsMissing;
        public Dictionary<string, int> Trees = new Dictionary<string, int>();
        public Dictionary<string, int> Attributes = new Dictionary<string, int>();
        public Dictionary<string, int> Interactions = new Dictionary<string, int>();
        public List<string> Notes = new List<string>();
    }
}
