using System;
using System.IO;
using System.Linq;
using FSO.Files.Formats.IFF;
using FSO.Files.Formats.IFF.Chunks;
using FSO.PackCompiler;
using FSO.PackCompiler.ArtGen;

namespace FSO.ArtCalibration
{
    /// <summary>
    /// Installs the generated chair (§11c) into a real TSO content directory, placeable in
    /// Buy Mode. Behavior (OBJD/BHAV/CTSS/anim table) comes from the real pack JSON compiler
    /// pipeline (PackCompilerApi.Build) — a trivial main-loop tree, no interactions, just
    /// enough to be a valid placeable object. Appearance comes from ArtGen (SpriteAssembler),
    /// merged into that same compiled .iff afterward — there's no "appearance.generated"
    /// schema field yet (ART-PIPELINE-DESIGN.md §6 step 4, future work), so this merge step is
    /// the stand-in until that lands.
    ///
    /// Two-phase by design: BuildChairIff() (write the .iff to gameDir/Objects/, no shared
    /// state) is safe to run anytime. InstallCatalogEntry() (the catalog_downloads.xml
    /// upsert) touches state shared with any other concurrent install and must only run once
    /// the caller has confirmed no other install is mid-write to that same file.
    /// </summary>
    public static class InstallChair
    {
        public const string ObjectId = "original_chair";
        public const string PackId = "original-chair";
        public const string DisplayName = "Handmade Chair";
        public const int Price = 150;
        public const string Category = "seating";
        public const ushort AppearanceChunkId = 9100;

        public static uint Guid_() => FSO.PackCompiler.GuidAllocator.Allocate(PackId, ObjectId);

        /// <summary>Compiles behavior via the real pack JSON pipeline, merges in the rendered
        /// appearance, and writes the .iff into {gameDir}/Objects/. Does NOT touch
        /// catalog_downloads.xml — see InstallCatalogEntry.</summary>
        public static string BuildChairIff(string gameDir)
        {
            var guid = Guid_();
            var packJson = $@"{{
  ""schema"": ""fso-pack/0.1"",
  ""engine"": ""tso"",
  ""pack"": {{
    ""id"": ""{PackId}"",
    ""name"": ""{DisplayName}"",
    ""author"": ""vibecode-sims"",
    ""version"": ""1.0.0"",
    ""description"": ""First AI-generated original furniture piece, owned outright: a parametric mid-century-style chair, rendered through a from-scratch renderer calibrated against TSO's real camera/lighting/depth.""
  }},
  ""objects"": [
    {{
      ""id"": ""{ObjectId}"",
      ""guid"": ""0x{guid:X8}"",
      ""name"": ""{DisplayName}"",
      ""price"": {Price},
      ""category"": ""{Category}"",
      ""trees"": {{
        ""main_loop"": {{
          ""args"": [], ""locals"": [],
          ""nodes"": [
            {{ ""id"": ""idle"", ""prim"": ""idle_for_input"", ""ticks_param"": 0, ""allow_push"": false, ""then"": ""idle"", ""else"": ""idle"" }}
          ]
        }}
      }},
      ""entry_points"": {{ ""main"": ""main_loop"" }}
    }}
  ]
}}";
            var tmpJsonPath = Path.Combine(Path.GetTempPath(), "fso-original-chair-pack.json");
            File.WriteAllText(tmpJsonPath, packJson);

            var tmpOutDir = Path.Combine(Path.GetTempPath(), "fso-original-chair-build");
            Directory.CreateDirectory(tmpOutDir);
            var buildResult = PackCompilerApi.Build(tmpJsonPath, tmpOutDir);
            if (!buildResult.Success)
                throw new Exception("behavior compile failed: " + string.Join("; ", buildResult.Diagnostics.Errors.Select(e => e.ToString())));

            var compiledIffPath = Path.Combine(tmpOutDir, ObjectId + ".iff");
            var iff = new IffFile();
            using (var stream = new FileStream(compiledIffPath, FileMode.Open))
                iff.Read(stream);
            var objd = iff.List<OBJD>().First();

            SimpleQuantizer.Install();
            var chairMesh = ChairGenerator.Build(new ChairGenerator.Params());
            var rendered = SpriteAssembler.RenderAllFrames(chairMesh);
            SpriteAssembler.AddAppearanceChunks(iff, objd, ObjectId, AppearanceChunkId, rendered);

            var objectsDir = Path.Combine(gameDir, "Objects");
            Directory.CreateDirectory(objectsDir);
            var finalPath = Path.Combine(objectsDir, ObjectId + ".iff");
            using (var stream = new FileStream(finalPath, FileMode.Create))
                iff.Write(stream);

            Console.WriteLine($"Wrote {finalPath} ({new FileInfo(finalPath).Length} bytes), GUID=0x{guid:X8}");
            return finalPath;
        }

        /// <summary>Upserts the Buy Mode catalog entry. Call only after confirming no other
        /// concurrent install is mid-write to the same catalog_downloads.xml.</summary>
        public static void InstallCatalogEntry(string gameDir)
        {
            var guid = Guid_();
            var catalogPath = Path.Combine(gameDir, "Objects", "catalog_downloads.xml");
            var entry = new CatalogEntry
            {
                Guid = guid,
                Category = Names.Categories[Category],
                Price = (uint)Price,
                Name = DisplayName,
                Tags = "chair, seating, original, handmade",
            };
            CatalogXml.Upsert(catalogPath, new[] { entry });
            Console.WriteLine($"Upserted catalog entry: g=0x{guid:X8} s={Names.Categories[Category]} p={Price} n=\"{DisplayName}\" at {catalogPath}");
        }
    }
}
