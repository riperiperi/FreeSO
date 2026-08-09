using System.IO;

namespace FSO.PackCompiler
{
    public class CompileResult
    {
        public Diagnostics Diagnostics = new Diagnostics();
        public BuildReport Report;
        public bool Success => !Diagnostics.HasErrors;
    }

    /// <summary>
    /// Library entry points used by both the CLI and the tests.
    /// </summary>
    public static class PackCompilerApi
    {
        /// <param name="gameDir">Base game content dir. Supply it to make
        /// appearance.clone_from_guid copy real sprites; omit it and clones stay
        /// note-only (and render invisible in the client).</param>
        public static CompileResult Build(string packJsonPath, string outDir, string gameDir = null)
        {
            return Run(packJsonPath, outDir, write: true, gameDir: gameDir);
        }

        public static CompileResult Validate(string packJsonPath, string gameDir = null)
        {
            return Run(packJsonPath, null, write: false, gameDir: gameDir);
        }

        /// <summary>Deterministic community-range GUID for (packId, objectId). See GuidAllocator.</summary>
        public static uint AllocateGuid(string packId, string objectId)
        {
            return GuidAllocator.Allocate(packId, objectId);
        }

        /// <summary>
        /// Decompiles a compiler-emitted .iff back to pack JSON. The result is re-parsed
        /// through the strict pack parser as a self-check before writing.
        /// </summary>
        public static CompileResult Decompile(string iffPath, string outJsonPath)
        {
            var result = new CompileResult();
            var d = result.Diagnostics;

            if (!File.Exists(iffPath))
            {
                d.Error(iffPath, "file not found");
                return result;
            }

            Newtonsoft.Json.Linq.JObject json;
            try
            {
                json = new Decompiler(d).Decompile(iffPath);
            }
            catch (System.Exception e)
            {
                d.Error(iffPath, "failed to read iff: " + e.Message);
                return result;
            }
            if (json == null || d.HasErrors) return result;

            new PackParser(d).Parse(json.ToString());
            if (d.HasErrors) return result;

            File.WriteAllText(outJsonPath, json.ToString(Newtonsoft.Json.Formatting.Indented));
            return result;
        }

        /// <param name="gameDir">FreeSO's own content dir — where the .iff and
        /// catalog_downloads.xml are written, i.e. what FSOEnvironment.ContentDir points at.</param>
        /// <param name="tsoContentDir">The separate TSO install that holds the FAR archives
        /// clone_from_guid reads sprites out of. Distinct from gameDir: FreeSO's content dir
        /// has no objectdata/ or packingslips/. Null skips cloning (object renders invisible).</param>
        public static CompileResult Install(string packJsonPath, string gameDir, string tsoContentDir = null)
        {
            var result = new CompileResult();
            var pack = ParsePack(packJsonPath, result);
            if (pack == null) return result;

            result.Report = new PackBuilder(result.Diagnostics, tsoContentDir).Install(pack, gameDir);
            return result;
        }

        private static CompileResult Run(string packJsonPath, string outDir, bool write, string gameDir = null)
        {
            var result = new CompileResult();
            var pack = ParsePack(packJsonPath, result);
            if (pack == null) return result;

            var builder = new PackBuilder(result.Diagnostics, gameDir);
            result.Report = builder.Build(pack, outDir, write && !result.Diagnostics.HasErrors);
            return result;
        }

        private static PackFile ParsePack(string packJsonPath, CompileResult result)
        {
            if (!File.Exists(packJsonPath))
            {
                result.Diagnostics.Error(packJsonPath, "file not found");
                return null;
            }
            return new PackParser(result.Diagnostics).Parse(File.ReadAllText(packJsonPath));
        }
    }
}
