using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using CASOutfitImporter.Formats;

namespace CASOutfitImporter.Verify
{
    // Walks a staged Content/ tree as if it were the live FSO content provider:
    //   .col → .po (by typeId+fileId) → .oft → 3 .apr → bindings → mesh + texture
    // Each step proves the file parses and that the IDs it references resolve to
    // a real file in the package.
    internal static class Verifier
    {
        public static int Run(string stagingRoot)
        {
            var contentRoot = Path.Combine(stagingRoot, "Content");
            if (!Directory.Exists(contentRoot))
            {
                Console.Error.WriteLine($"FAIL: no Content/ under {stagingRoot}");
                return 1;
            }

            // Index loose files with embedded ".HEXID16.<ext>" naming convention.
            var idx = BuildIndex(contentRoot);
            int errors = 0;
            int passed = 0;

            // Collections: by literal filename (no embedded id).
            var collDir = Path.Combine(contentRoot, "Avatar", "Collections");
            if (!Directory.Exists(collDir))
            {
                Console.Error.WriteLine($"FAIL: no Avatar/Collections/ dir");
                return 1;
            }

            var colFiles = Directory.GetFiles(collDir, "*.col");
            if (colFiles.Length == 0)
            {
                Console.Error.WriteLine("FAIL: no .col files staged");
                return 1;
            }

            foreach (var colPath in colFiles.OrderBy(p => p))
            {
                Console.WriteLine();
                Console.WriteLine($"Collection: {Path.GetFileName(colPath)}");
                List<CollectionWriter.Entry> entries;
                try
                {
                    entries = CollectionWriter.Read(File.ReadAllBytes(colPath));
                }
                catch (Exception ex)
                {
                    Fail(ref errors, $"  parse failed: {ex.Message}");
                    continue;
                }
                Console.WriteLine($"  entries: {entries.Count}");

                int localResolved = 0;
                int inherited = 0;
                foreach (var e in entries)
                {
                    string label = $"  [{e.Index}] po typeId=0x{e.TypeId:X8} fileId=0x{e.FileId:X8}";
                    if (!idx.Purchasables.TryGetValue((e.TypeId, e.FileId), out var poPath))
                    {
                        // Entry doesn't resolve to a package-local .po — almost
                        // certainly inherited from the user's FAR3 via --source-collection.
                        // The live engine will resolve it from FAR3 at runtime.
                        Console.WriteLine($"{label}  → (inherited from source archive)");
                        inherited++;
                        continue;
                    }
                    Console.WriteLine($"{label}  → {Path.GetFileName(poPath)}");
                    localResolved++;

                    if (!VerifyPurchasable(poPath, idx, ref errors, ref passed))
                        continue;
                }

                if (inherited > 0)
                    Console.WriteLine($"  ({inherited} inherited entries — live engine resolves these from FAR3)");

                // A collection that's wired into nothing of ours is suspicious —
                // it would mean we shipped a .col that doesn't reference any of
                // the .po files in this package, defeating the point.
                if (localResolved == 0)
                    Fail(ref errors, $"  ✗ no entries in this collection reference a package-local .po");
            }

            Console.WriteLine();
            if (errors == 0)
                Console.WriteLine($"OK — {passed} cross-links verified, 0 failures");
            else
                Console.WriteLine($"FAIL — {errors} error(s), {passed} pass(es)");
            return errors == 0 ? 0 : 1;
        }

        private static bool VerifyPurchasable(string poPath, FileIndex idx, ref int errors, ref int passed)
        {
            PurchasableFile po;
            try { po = PurchasableFile.Read(File.ReadAllBytes(poPath)); }
            catch (Exception ex) { Fail(ref errors, $"      .po parse failed: {ex.Message}"); return false; }

            Console.WriteLine($"      gender={(po.Gender == 1 ? "F" : "M")} oft typeId=0x{po.Outfit.TypeId:X8} fileId=0x{po.Outfit.FileId:X8}");
            passed++;

            if (!idx.Outfits.TryGetValue((po.Outfit.TypeId, po.Outfit.FileId), out var oftPath))
            {
                Fail(ref errors, $"      → MISSING .oft");
                return false;
            }

            return VerifyOutfit(oftPath, idx, ref errors, ref passed);
        }

        private static bool VerifyOutfit(string oftPath, FileIndex idx, ref int errors, ref int passed)
        {
            OutfitFile oft;
            try { oft = OutfitFile.Read(File.ReadAllBytes(oftPath)); }
            catch (Exception ex) { Fail(ref errors, $"        .oft parse failed: {ex.Message}"); return false; }

            string region = oft.Region switch { 1 => "head", 2 => "body", _ => $"region={oft.Region}" };
            Console.WriteLine($"        region={region} hand={oft.HandGroup}");
            passed++;

            bool ok = true;
            ok &= VerifyAppearance("light",  oft.Light,  idx, ref errors, ref passed);
            ok &= VerifyAppearance("medium", oft.Medium, idx, ref errors, ref passed);
            ok &= VerifyAppearance("dark",   oft.Dark,   idx, ref errors, ref passed);
            return ok;
        }

        private static bool VerifyAppearance(string tone, ContentRef aprRef, FileIndex idx, ref int errors, ref int passed)
        {
            if (!idx.Appearances.TryGetValue((aprRef.TypeId, aprRef.FileId), out var aprPath))
            {
                Fail(ref errors, $"          {tone}: MISSING .apr (typeId=0x{aprRef.TypeId:X8} fileId=0x{aprRef.FileId:X8})");
                return false;
            }
            Console.WriteLine($"          {tone}: {Path.GetFileName(aprPath)}");

            AppearanceFile apr;
            try { apr = AppearanceFile.Read(File.ReadAllBytes(aprPath)); }
            catch (Exception ex) { Fail(ref errors, $"            parse failed: {ex.Message}"); return false; }
            passed++;

            bool ok = true;
            foreach (var bndRef in apr.Bindings)
            {
                ok &= VerifyBinding(bndRef, idx, ref errors, ref passed);
            }
            return ok;
        }

        private static bool VerifyBinding(ContentRef bndRef, FileIndex idx, ref int errors, ref int passed)
        {
            if (!idx.Bindings.TryGetValue((bndRef.TypeId, bndRef.FileId), out var bndPath))
            {
                Fail(ref errors, $"            MISSING .bnd (typeId=0x{bndRef.TypeId:X8} fileId=0x{bndRef.FileId:X8})");
                return false;
            }
            BindingFile bnd;
            try { bnd = BindingFile.Read(File.ReadAllBytes(bndPath)); }
            catch (Exception ex) { Fail(ref errors, $"            .bnd parse failed: {ex.Message}"); return false; }
            passed++;

            // Mesh
            bool ok = true;
            if (!idx.Meshes.TryGetValue((bnd.MeshTypeId, bnd.MeshFileId), out var meshPath))
            {
                Fail(ref errors, $"              MISSING .mesh (typeId=0x{bnd.MeshTypeId:X8} fileId=0x{bnd.MeshFileId:X8})");
                ok = false;
            }
            else
            {
                try
                {
                    var ms = MeshSummary.Read(File.ReadAllBytes(meshPath));
                    Console.WriteLine($"            bone={bnd.Bone}  mesh: v{ms.Version} bones={ms.BoneNames.Count} faces={ms.FaceCount} verts={ms.RealVertexCount}+{ms.BlendVertexCount} ({ms.BytesRead}/{ms.FileLength} B)");
                    if (ms.BytesRead != ms.FileLength)
                        Fail(ref errors, $"              mesh parse left {ms.FileLength - ms.BytesRead} unread bytes");
                    passed++;
                }
                catch (Exception ex)
                {
                    Fail(ref errors, $"              .mesh parse failed: {ex.Message}");
                    ok = false;
                }
            }

            // Texture (.png) — typeId+fileId match; just confirm file exists and is a PNG.
            if (!idx.Textures.TryGetValue((bnd.TextureTypeId, bnd.TextureFileId), out var texPath))
            {
                Fail(ref errors, $"              MISSING texture (typeId=0x{bnd.TextureTypeId:X8} fileId=0x{bnd.TextureFileId:X8})");
                ok = false;
            }
            else
            {
                var head = File.ReadAllBytes(texPath);
                bool isPng = head.Length >= 8 && head[0] == 137 && head[1] == 80 && head[2] == 78 && head[3] == 71;
                if (!isPng) Fail(ref errors, $"              texture {Path.GetFileName(texPath)} is not a PNG");
                else passed++;
            }
            return ok;
        }

        private static void Fail(ref int errors, string msg)
        {
            errors++;
            Console.WriteLine(msg);
        }

        // ----------------------------------------------------------------- index

        private sealed class FileIndex
        {
            public Dictionary<(uint typeId, uint fileId), string> Meshes      = new Dictionary<(uint, uint), string>();
            public Dictionary<(uint typeId, uint fileId), string> Textures    = new Dictionary<(uint, uint), string>();
            public Dictionary<(uint typeId, uint fileId), string> Bindings    = new Dictionary<(uint, uint), string>();
            public Dictionary<(uint typeId, uint fileId), string> Appearances = new Dictionary<(uint, uint), string>();
            public Dictionary<(uint typeId, uint fileId), string> Outfits     = new Dictionary<(uint, uint), string>();
            public Dictionary<(uint typeId, uint fileId), string> Purchasables= new Dictionary<(uint, uint), string>();
        }

        private static FileIndex BuildIndex(string contentRoot)
        {
            var idx = new FileIndex();
            void Scan(string subdir, string ext, Dictionary<(uint, uint), string> dict)
            {
                var dir = Path.Combine(contentRoot, "Avatar", subdir);
                if (!Directory.Exists(dir)) return;
                foreach (var path in Directory.GetFiles(dir, $"*{ext}", SearchOption.AllDirectories))
                {
                    if (TryParseId(Path.GetFileName(path), out uint t, out uint f))
                        dict[(t, f)] = path;
                }
            }
            Scan("Meshes",       ".mesh", idx.Meshes);
            Scan("Textures",     ".png",  idx.Textures);
            Scan("Bindings",     ".bnd",  idx.Bindings);
            Scan("Appearances",  ".apr",  idx.Appearances);
            Scan("Outfits",      ".oft",  idx.Outfits);
            Scan("Purchasables", ".po",   idx.Purchasables);
            return idx;
        }

        private static bool TryParseId(string filename, out uint typeId, out uint fileId)
        {
            // "<base>.<HEXID16>.<ext>"
            var parts = filename.Split('.');
            typeId = 0; fileId = 0;
            if (parts.Length < 3) return false;
            string hex = parts[parts.Length - 2];
            if (hex.Length != 16) return false;
            if (!ulong.TryParse(hex, System.Globalization.NumberStyles.HexNumber, null, out var packed)) return false;
            typeId = (uint)(packed & 0xFFFFFFFF);
            fileId = (uint)(packed >> 32);
            return true;
        }
    }
}