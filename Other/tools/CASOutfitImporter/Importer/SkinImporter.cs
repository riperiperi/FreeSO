using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using CASOutfitImporter.Formats;
using CASOutfitImporter.Imaging;

namespace CASOutfitImporter.Importer
{
    internal enum AvatarPart { Head, Body }

    // Where the new outfit gets registered. The 9 trunk values map 1:1 to the
    // VMEODTrunkPluginCollections enum in tso.simantics; adding a new value
    // here also requires extending that enum (server side) — none of these do.
    internal enum ImportMode
    {
        Cas,
        TrunkWedding,
        TrunkScifi,
        TrunkVegas,
        TrunkVaudwest,
        TrunkUniforms,
        TrunkCostumes,
        TrunkOldworld,
        TrunkSports,
        TrunkToga,
    }

    // Single source of truth for everything mode-specific. Adding a trunk type
    // is one row here. CLI parsing, category routing, and collection-filename
    // resolution all read from this table.
    internal static class ModeRegistry
    {
        public sealed class Spec
        {
            public string CliName;       // what users type for --mode
            public string TrunkPrefix;   // "<prefix>_<gender>.col" — null for non-trunk
            public string Category;      // content-repo subdir (excl. CAS gender suffix)
        }

        public static readonly IReadOnlyDictionary<ImportMode, Spec> Modes = new Dictionary<ImportMode, Spec>
        {
            [ImportMode.Cas]            = new Spec { CliName = "cas",            TrunkPrefix = null,        Category = "cas" },
            [ImportMode.TrunkWedding]   = new Spec { CliName = "trunk:wedding",  TrunkPrefix = "wedding",   Category = "trunks/wedding"  },
            [ImportMode.TrunkScifi]     = new Spec { CliName = "trunk:scifi",    TrunkPrefix = "scifi",     Category = "trunks/scifi"    },
            [ImportMode.TrunkVegas]     = new Spec { CliName = "trunk:vegas",    TrunkPrefix = "vegas",     Category = "trunks/vegas"    },
            [ImportMode.TrunkVaudwest]  = new Spec { CliName = "trunk:vaudwest", TrunkPrefix = "vaudwest",  Category = "trunks/vaudwest" },
            [ImportMode.TrunkUniforms]  = new Spec { CliName = "trunk:uniforms", TrunkPrefix = "uniforms",  Category = "trunks/uniforms" },
            [ImportMode.TrunkCostumes]  = new Spec { CliName = "trunk:costumes", TrunkPrefix = "costumes",  Category = "trunks/costumes" },
            [ImportMode.TrunkOldworld]  = new Spec { CliName = "trunk:oldworld", TrunkPrefix = "oldworld",  Category = "trunks/oldworld" },
            [ImportMode.TrunkSports]    = new Spec { CliName = "trunk:sports",   TrunkPrefix = "sports",    Category = "trunks/sports"   },
            [ImportMode.TrunkToga]      = new Spec { CliName = "trunk:toga",     TrunkPrefix = "toga",      Category = "trunks/toga"     },
        };

        public static bool TryParse(string cliName, out ImportMode mode)
        {
            foreach (var kv in Modes)
            {
                if (kv.Value.CliName == cliName) { mode = kv.Key; return true; }
            }
            mode = ImportMode.Cas;
            return false;
        }

        public static string AllCliNames() => string.Join(", ", Modes.Values.Select(v => v.CliName));
    }

    internal sealed class ImportRequest
    {
        public string InputDir;        // folder containing .skn / .cmx / .bmp(s)
        public AvatarPart Part;
        public char Gender;            // 'm' or 'f'
        public string Name;            // short id used in filenames, e.g. "k8groupie"
        public string OutputDir;       // staging dir; package built underneath as Content/...
        public ImportMode Mode = ImportMode.Cas;
        public string SourceCollection;// optional path to existing .col to merge with
        public string RepoRoot;        // when set, save into <root>/<category>/<name>/ + write pack.json;
                                       // single-entry .col only (manage-packs handles merging)
    }

    internal sealed class ImportResult
    {
        public ContentRef Outfit;          // .oft id
        public ContentRef Purchasable;     // .po id
        public string CollectionName;      // e.g. "ea_female_heads.col" or "wedding_female.col"
        public ImportMode Mode;
        public bool MergedWithSource;      // true if --source-collection was applied
        public int CollectionEntryCount;   // total entries in produced .col
        public string StagingRoot;         // root of staged Content/ tree
        public string PackDir;             // when in repo mode: <repoRoot>/<category>/<name>/
        public string Category;            // e.g. "trunks/wedding" or "cas/heads/f"
        public List<string> WrittenFiles = new List<string>();
    }

    internal sealed class SkinImporter
    {
        public const string ToolVersion = "0.3.0";

        // Per-kind TypeIDs. Choice is somewhat arbitrary — they only have to be
        // self-consistent across the files this tool emits. Each provider has its
        // own namespace, so cross-kind clash is impossible. Random pick is fine.
        private const uint TypeIdMesh        = 0x6B5DB827;
        private const uint TypeIdTexture     = 0x4D515437;
        private const uint TypeIdBinding     = 0x42434543;
        private const uint TypeIdAppearance  = 0x41505246;
        private const uint TypeIdOutfit      = 0x4F465432;
        private const uint TypeIdPurchasable = 0x504F5446;

        // Default hand group — TSO-shipped female "huao" (idle) etc. The Outfit only
        // stores a uint reference, looked up by HandgroupProvider on the client. Bodies
        // need this; heads pass 0 (engine ignores it for region=head).
        private const uint DefaultFemaleHandGroup = 1;
        private const uint DefaultMaleHandGroup   = 2;

        private readonly Random _rng = new Random();

        public ImportResult Run(ImportRequest req)
        {
            if (!Directory.Exists(req.InputDir))
                throw new DirectoryNotFoundException($"input dir not found: {req.InputDir}");

            // Heads aren't wearable outfits — the trunk/wardrobe primitive only
            // swaps body outfits. Reject the combination early with a clear msg.
            if (req.Mode != ImportMode.Cas && req.Part == AvatarPart.Head)
                throw new ArgumentException(
                    $"--type head is incompatible with --mode {req.Mode.ToString().ToLowerInvariant()}: " +
                    "heads can only be CAS selectables, not donnable outfits.");

            // Repo mode and --source-collection are mutually exclusive: in repo mode
            // the install tool (manage-packs) reads vanilla snapshots from
            // content-repo/_vanilla/ and merges all installed packs at install time.
            // The pack on disk only ever carries its own contribution.
            if (!string.IsNullOrEmpty(req.RepoRoot) && !string.IsNullOrEmpty(req.SourceCollection))
                throw new ArgumentException(
                    "--save-to-repo is incompatible with --source-collection. " +
                    "Repo packs hold a single-entry .col; manage-packs merges with vanilla at install time.");

            // When saving to a repo, redirect OutputDir under <repoRoot>/<category>/<name>/.
            string category = ResolveCategory(req);
            if (!string.IsNullOrEmpty(req.RepoRoot))
            {
                var packDir = Path.Combine(req.RepoRoot,
                    category.Replace('/', Path.DirectorySeparatorChar),
                    Sanitize(req.Name));
                Directory.CreateDirectory(packDir);
                req.OutputDir = packDir;
            }

            // ---- 1. Discover input files ----
            var files = Directory.GetFiles(req.InputDir);
            var sknPath = files.FirstOrDefault(p =>
                p.EndsWith(".skn", StringComparison.OrdinalIgnoreCase));
            if (sknPath == null)
                throw new FileNotFoundException("no xskin-*.skn file in input dir");

            var bmps = files
                .Where(p => p.EndsWith(".bmp", StringComparison.OrdinalIgnoreCase))
                .ToList();
            if (bmps.Count == 0)
                throw new FileNotFoundException("no .bmp textures in input dir");

            // Pick textures for light/medium/dark.
            var (lgtBmp, medBmp, drkBmp) = PickSkinTones(bmps);
            if (lgtBmp == null)
                throw new InvalidDataException("could not find a 'lgt' (light) texture by filename");

            // ---- 2. Parse mesh ----
            var mesh = SknReader.ReadFile(sknPath);
            if (string.IsNullOrEmpty(mesh.PrimaryBone))
            {
                // Default per part if filename doesn't carry one.
                mesh.PrimaryBone = req.Part == AvatarPart.Head ? "HEAD" : "PELVIS";
            }

            // ---- 3. Allocate IDs ----
            var meshRef = NewRef(TypeIdMesh);
            var lgtTexRef = NewRef(TypeIdTexture);
            var medTexRef = NewRef(TypeIdTexture);
            var drkTexRef = NewRef(TypeIdTexture);
            var lgtBndRef = NewRef(TypeIdBinding);
            var medBndRef = NewRef(TypeIdBinding);
            var drkBndRef = NewRef(TypeIdBinding);
            var lgtAprRef = NewRef(TypeIdAppearance);
            var medAprRef = NewRef(TypeIdAppearance);
            var drkAprRef = NewRef(TypeIdAppearance);
            var oftRef    = NewRef(TypeIdOutfit);
            var poRef     = NewRef(TypeIdPurchasable);

            // ---- 4. Build mesh + textures bytes ----
            var meshBytes = MeshWriter.Write(mesh);

            bool keyMagenta = req.Part == AvatarPart.Body; // bodies have transparent areas
            byte[] lgtPng = TextureBuilder.BmpToPng(lgtBmp, keyMagenta);
            byte[] medPng = medBmp != null
                ? TextureBuilder.BmpToPng(medBmp, keyMagenta)
                : TextureBuilder.SynthesizeTone(lgtBmp, 0.78f, keyMagenta);
            byte[] drkPng = drkBmp != null
                ? TextureBuilder.BmpToPng(drkBmp, keyMagenta)
                : TextureBuilder.SynthesizeTone(lgtBmp, 0.55f, keyMagenta);

            // ---- 5. Bindings (one per skin tone, all referencing the same mesh) ----
            byte[] lgtBnd = BindingWriter.Write(mesh.PrimaryBone, meshRef, lgtTexRef);
            byte[] medBnd = BindingWriter.Write(mesh.PrimaryBone, meshRef, medTexRef);
            byte[] drkBnd = BindingWriter.Write(mesh.PrimaryBone, meshRef, drkTexRef);

            // ---- 6. Appearances ----
            // Thumbnail id is left zero (engine falls back to rendering the mesh).
            var noThumb = new ContentRef(0, 0);
            byte[] lgtApr = AppearanceWriter.Write(noThumb, new[] { lgtBndRef });
            byte[] medApr = AppearanceWriter.Write(noThumb, new[] { medBndRef });
            byte[] drkApr = AppearanceWriter.Write(noThumb, new[] { drkBndRef });

            // ---- 7. Outfit ----
            uint region = req.Part == AvatarPart.Head ? OutfitWriter.RegionHead : OutfitWriter.RegionBody;
            uint hand = req.Part == AvatarPart.Head
                ? 0u
                : (req.Gender == 'f' ? DefaultFemaleHandGroup : DefaultMaleHandGroup);
            byte[] oft = OutfitWriter.Write(lgtAprRef, medAprRef, drkAprRef, hand, region);

            // ---- 8. PurchasableOutfit ----
            uint genderFlag = req.Gender == 'f' ? PurchasableWriter.GenderFemale : PurchasableWriter.GenderMale;
            byte[] po = PurchasableWriter.Write(genderFlag, oftRef);

            // ---- 9. Stage everything under Content/Avatar/.../User/ ----
            var staging = req.OutputDir;
            var result = new ImportResult
            {
                StagingRoot = staging,
                Outfit = oftRef,
                Purchasable = poRef,
                Category = category,
                PackDir = !string.IsNullOrEmpty(req.RepoRoot) ? staging : null
            };

            string baseName = $"{Sanitize(req.Name)}_{(req.Part == AvatarPart.Head ? "head" : "body")}_{req.Gender}";

            WriteContent(result, "Avatar/Meshes/User",     IdEncoder.EmbedId(baseName,                ".mesh", meshRef.TypeId, meshRef.FileId), meshBytes);
            WriteContent(result, "Avatar/Textures/User",   IdEncoder.EmbedId($"{baseName}_lgt",       ".png",  lgtTexRef.TypeId, lgtTexRef.FileId), lgtPng);
            WriteContent(result, "Avatar/Textures/User",   IdEncoder.EmbedId($"{baseName}_med",       ".png",  medTexRef.TypeId, medTexRef.FileId), medPng);
            WriteContent(result, "Avatar/Textures/User",   IdEncoder.EmbedId($"{baseName}_drk",       ".png",  drkTexRef.TypeId, drkTexRef.FileId), drkPng);
            WriteContent(result, "Avatar/Bindings/User",   IdEncoder.EmbedId($"{baseName}_lgt",       ".bnd",  lgtBndRef.TypeId, lgtBndRef.FileId), lgtBnd);
            WriteContent(result, "Avatar/Bindings/User",   IdEncoder.EmbedId($"{baseName}_med",       ".bnd",  medBndRef.TypeId, medBndRef.FileId), medBnd);
            WriteContent(result, "Avatar/Bindings/User",   IdEncoder.EmbedId($"{baseName}_drk",       ".bnd",  drkBndRef.TypeId, drkBndRef.FileId), drkBnd);
            WriteContent(result, "Avatar/Appearances/User", IdEncoder.EmbedId($"{baseName}_lgt",      ".apr",  lgtAprRef.TypeId, lgtAprRef.FileId), lgtApr);
            WriteContent(result, "Avatar/Appearances/User", IdEncoder.EmbedId($"{baseName}_med",      ".apr",  medAprRef.TypeId, medAprRef.FileId), medApr);
            WriteContent(result, "Avatar/Appearances/User", IdEncoder.EmbedId($"{baseName}_drk",      ".apr",  drkAprRef.TypeId, drkAprRef.FileId), drkApr);
            WriteContent(result, "Avatar/Outfits/User",    IdEncoder.EmbedId(baseName,                ".oft",  oftRef.TypeId, oftRef.FileId), oft);
            WriteContent(result, "Avatar/Purchasables/User", IdEncoder.EmbedId(baseName,              ".po",   poRef.TypeId, poRef.FileId), po);

            // ---- 10. Collection registration ----
            result.Mode = req.Mode;
            result.CollectionName = ResolveCollectionName(req);

            // Start with existing entries if --source-collection was provided so we
            // don't shadow the defaults. Otherwise the produced .col contains only
            // the new entry (and the README warns about default loss).
            //
            // The flag accepts either:
            //   - a raw .col file extracted from FAR3 via FarExtractor, or
            //   - a FAR3 .dat archive (we sniff "FAR!byAZ", look up the entry that
            //     matches the resolved collection name, decompress, parse).
            var collection = new List<CollectionWriter.Entry>();
            if (!string.IsNullOrEmpty(req.SourceCollection))
            {
                if (!File.Exists(req.SourceCollection))
                    throw new FileNotFoundException(
                        $"--source-collection path not found: {req.SourceCollection}");

                byte[] colBytes = LoadSourceCollection(req.SourceCollection, result.CollectionName);
                var existing = CollectionWriter.Read(colBytes);
                // Drop any entry that already references our new .po id (re-runs).
                collection.AddRange(existing.Where(e =>
                    !(e.FileId == poRef.FileId && e.TypeId == poRef.TypeId)));
                result.MergedWithSource = true;
            }

            collection.Add(new CollectionWriter.Entry
            {
                Index = collection.Count,
                FileId = poRef.FileId,
                TypeId = poRef.TypeId
            });
            // Reindex sequentially so existing-source ordering stays clean.
            for (int i = 0; i < collection.Count; i++) collection[i].Index = i;
            result.CollectionEntryCount = collection.Count;

            byte[] col = CollectionWriter.Write(collection);
            // .col loose path matches `Avatar/Collections/.*\.col` per
            // AvatarCollectionsProvider's file-mode regex.
            WriteContent(result, "Avatar/Collections", result.CollectionName, col);

            // ---- 11. pack.json (repo mode only) ----
            if (!string.IsNullOrEmpty(req.RepoRoot))
                WritePackJson(result, req, poRef);

            return result;
        }

        // Writes a manifest the manage-packs tool reads when listing/installing/uninstalling.
        // Hand-rolled JSON to avoid pulling in a serializer package — strict ASCII, simple
        // string escaping for the description-free fields.
        private static void WritePackJson(ImportResult r, ImportRequest req, ContentRef po)
        {
            var sb = new StringBuilder();
            sb.AppendLine("{");
            sb.Append("  \"name\": ").Append(JsonStr(req.Name)).AppendLine(",");
            sb.Append("  \"category\": ").Append(JsonStr(r.Category)).AppendLine(",");
            sb.Append("  \"gender\": \"").Append(req.Gender).AppendLine("\",");
            sb.Append("  \"type\": \"").Append(req.Part.ToString().ToLowerInvariant()).AppendLine("\",");
            sb.Append("  \"mode\": ").Append(JsonStr(ModeToString(req.Mode))).AppendLine(",");
            sb.Append("  \"created_at\": \"").Append(DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ", CultureInfo.InvariantCulture)).AppendLine("\",");
            sb.Append("  \"tool_version\": \"").Append(ToolVersion).AppendLine("\",");
            sb.Append("  \"outfit_id\": \"").Append(r.Outfit.PackedId.ToString("X16", CultureInfo.InvariantCulture)).AppendLine("\",");
            sb.Append("  \"purchasable_id\": \"").Append(r.Purchasable.PackedId.ToString("X16", CultureInfo.InvariantCulture)).AppendLine("\",");
            sb.AppendLine("  \"collection_entry\": {");
            sb.Append("    \"target\": ").Append(JsonStr(r.CollectionName)).AppendLine(",");
            sb.Append("    \"type_id\": \"0x").Append(po.TypeId.ToString("X8", CultureInfo.InvariantCulture)).AppendLine("\",");
            sb.Append("    \"file_id\": \"0x").Append(po.FileId.ToString("X8", CultureInfo.InvariantCulture)).AppendLine("\"");
            sb.AppendLine("  },");
            sb.AppendLine("  \"files\": [");
            for (int i = 0; i < r.WrittenFiles.Count; i++)
            {
                var rel = r.WrittenFiles[i].Replace('\\', '/');
                sb.Append("    ").Append(JsonStr(rel));
                if (i < r.WrittenFiles.Count - 1) sb.AppendLine(",");
                else sb.AppendLine();
            }
            sb.AppendLine("  ]");
            sb.AppendLine("}");

            var path = Path.Combine(r.PackDir, "pack.json");
            File.WriteAllText(path, sb.ToString());
            r.WrittenFiles.Add("pack.json");
        }

        private static string JsonStr(string s)
        {
            if (s == null) return "null";
            var sb = new StringBuilder("\"");
            foreach (var ch in s)
            {
                switch (ch)
                {
                    case '\\': sb.Append("\\\\"); break;
                    case '"':  sb.Append("\\\""); break;
                    case '\n': sb.Append("\\n");  break;
                    case '\r': sb.Append("\\r");  break;
                    case '\t': sb.Append("\\t");  break;
                    default:
                        if (ch < ' ') sb.Append("\\u").Append(((int)ch).ToString("X4"));
                        else sb.Append(ch);
                        break;
                }
            }
            sb.Append('"');
            return sb.ToString();
        }

        // Subdirectory under the content-repo for this pack. CAS modes branch
        // by part + gender; trunk modes use a flat per-type subdir from the
        // ModeRegistry table.
        private static string ResolveCategory(ImportRequest req)
        {
            if (req.Mode == ImportMode.Cas)
            {
                return req.Part == AvatarPart.Head
                    ? $"cas/heads/{req.Gender}"
                    : $"cas/bodies/{req.Gender}";
            }
            return ModeRegistry.Modes[req.Mode].Category;
        }

        // CLI-shaped mode names so pack.json round-trips through `--mode`.
        // Reads from ModeRegistry — single source of truth for all mode metadata.
        private static string ModeToString(ImportMode m) => ModeRegistry.Modes[m].CliName;

        // Resolves --source-collection to raw bytes. Auto-detects FAR3 vs raw .col.
        // For a FAR3, looks up the entry whose filename matches the collection we're
        // about to produce (e.g. "wedding_female.col"). Throws with a clear msg if
        // the archive doesn't contain a matching entry.
        private static byte[] LoadSourceCollection(string path, string expectedName)
        {
            if (Far3Reader.LooksLikeFar3(path))
            {
                using var far = Far3Reader.Open(path);
                var entry = far.FindByFilename(expectedName);
                if (entry == null)
                {
                    var sample = string.Join(", ",
                        far.Entries.Take(10).Select(e => e.Filename));
                    throw new InvalidDataException(
                        $"FAR3 archive '{path}' does not contain '{expectedName}'. " +
                        $"First 10 entries: {sample}");
                }
                return far.Read(entry);
            }
            return File.ReadAllBytes(path);
        }

        // Produces the collection filename the consumer (CAS UI / VMEODTrunkPlugin)
        // loads by hardcoded name. CAS branches by head/body + gender; trunk modes
        // build "<prefix>_<gender>male.col" matching VMEODTrunkPlugin.OnConnection.
        private static string ResolveCollectionName(ImportRequest req)
        {
            if (req.Mode == ImportMode.Cas)
            {
                return req.Part == AvatarPart.Head
                    ? (req.Gender == 'f' ? "ea_female_heads.col" : "ea_male_heads.col")
                    : (req.Gender == 'f' ? "ea_female.col"       : "ea_male.col");
            }
            var prefix = ModeRegistry.Modes[req.Mode].TrunkPrefix
                ?? throw new ArgumentOutOfRangeException(nameof(req.Mode), req.Mode, "no trunk prefix configured");
            return req.Gender == 'f' ? $"{prefix}_female.col" : $"{prefix}_male.col";
        }

        // ----------------------------------------------------------------- helpers

        private static (string lgt, string med, string drk) PickSkinTones(List<string> bmps)
        {
            string Find(string token) => bmps.FirstOrDefault(p =>
                Path.GetFileNameWithoutExtension(p)
                    .ToLowerInvariant()
                    .Contains(token));

            // "_pale" variants are author/skin-tone tweaks — prefer the plain "lgt".
            string lgt = bmps.FirstOrDefault(p =>
            {
                var n = Path.GetFileNameWithoutExtension(p).ToLowerInvariant();
                return n.Contains("lgt") && !n.Contains("pale");
            }) ?? Find("lgt");

            string med = Find("med");
            string drk = Find("drk");
            return (lgt, med, drk);
        }

        private ContentRef NewRef(uint typeId)
        {
            // Bias high-bit to keep us out of the low FileID range that FAR3
            // archives tend to use for Maxis content.
            uint fid;
            do
            {
                fid = (uint)_rng.Next() | 0x80000000u;
            } while (fid == 0);
            return new ContentRef(typeId, fid);
        }

        private static string Sanitize(string s)
        {
            var sb = new System.Text.StringBuilder();
            foreach (var ch in s)
            {
                sb.Append(char.IsLetterOrDigit(ch) ? char.ToLowerInvariant(ch) : '_');
            }
            return sb.ToString();
        }

        private static void WriteContent(ImportResult r, string subdir, string filename, byte[] data)
        {
            var dir = Path.Combine(r.StagingRoot, "Content", subdir.Replace('/', Path.DirectorySeparatorChar));
            Directory.CreateDirectory(dir);
            var path = Path.Combine(dir, filename);
            File.WriteAllBytes(path, data);
            // Track relative to staging root for the README.
            r.WrittenFiles.Add(path.Substring(r.StagingRoot.Length).TrimStart(Path.DirectorySeparatorChar));
        }
    }
}