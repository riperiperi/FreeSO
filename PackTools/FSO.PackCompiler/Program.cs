using System;

namespace FSO.PackCompiler
{
    public static class Program
    {
        public const string DefaultGameDir = "/Applications/FreeSO.app/Contents/MacOS/Content";

        // The TSO install, which is NOT the same tree as DefaultGameDir — it's the one with
        // objectdata/objects/*.far and packingslips/, where clone_from_guid finds sprites.
        public static readonly string DefaultTsoDir = System.IO.Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
            "Library/Application Support/The Sims Online/TSOClient");

        public static int Main(string[] args)
        {
            if (args.Length < 2)
            {
                PrintUsage();
                return 2;
            }

            var command = args[0];
            var packPath = args[1];

            switch (command)
            {
                case "import-batch":
                    return RunImportBatch(args);
                case "build":
                {
                    string outDir = null;
                    string tsoDir = null;
                    for (int i = 2; i < args.Length; i++)
                    {
                        if (args[i] == "-o" && i + 1 < args.Length) outDir = args[++i];
                        // Named --tso-dir, not --game-dir, to match `install`'s naming: this is
                        // the SPRITE SOURCE (PackCompilerApi.Build's "gameDir" parameter is the
                        // same directory concept as install's --tso-dir, not install's
                        // --game-dir — see PackBuilder.GameDir's declaration for the full trap).
                        // Reusing "--game-dir" here for a different directory than install means
                        // would be the exact bug this whole fix exists to stop repeating.
                        else if (args[i] == "--tso-dir" && i + 1 < args.Length) tsoDir = args[++i];
                        else
                        {
                            Console.Error.WriteLine("unknown argument: " + args[i]);
                            PrintUsage();
                            return 2;
                        }
                    }
                    if (outDir == null)
                    {
                        Console.Error.WriteLine("build requires -o <outdir>");
                        return 2;
                    }
                    // SCHEMA.md documents FSO_VM_GAME_LOCATION as a way to supply the base game
                    // content dir for clone_from_guid; without this fallback the CLI silently
                    // ignored it (only FSO.ModServer ever read the env var), so a bare `build`
                    // was structurally invisible for clone_from_guid regardless of what was set.
                    if (tsoDir == null) tsoDir = Environment.GetEnvironmentVariable("FSO_VM_GAME_LOCATION");
                    var result = PackCompilerApi.Build(packPath, outDir, tsoDir);
                    Print(result);
                    if (result.Success)
                    {
                        foreach (var obj in result.Report.Objects)
                            Console.WriteLine("wrote " + System.IO.Path.Combine(outDir, obj.Iff) + " (guid " + obj.Guid + ")");
                        Console.WriteLine("wrote " + System.IO.Path.Combine(outDir, "catalog-entries.xml"));
                        Console.WriteLine("wrote " + System.IO.Path.Combine(outDir, "build-report.json"));
                    }
                    return result.Success ? 0 : 1;
                }
                case "validate":
                {
                    var result = PackCompilerApi.Validate(packPath);
                    Print(result);
                    Console.WriteLine(result.Success ? "pack is valid" : "pack is invalid");
                    return result.Success ? 0 : 1;
                }
                case "decompile":
                {
                    string outJson = null;
                    for (int i = 2; i < args.Length; i++)
                    {
                        if (args[i] == "-o" && i + 1 < args.Length) outJson = args[++i];
                        else
                        {
                            Console.Error.WriteLine("unknown argument: " + args[i]);
                            PrintUsage();
                            return 2;
                        }
                    }
                    if (outJson == null)
                    {
                        Console.Error.WriteLine("decompile requires -o <pack.json>");
                        return 2;
                    }
                    var result = PackCompilerApi.Decompile(packPath, outJson);
                    Print(result);
                    if (result.Success) Console.WriteLine("wrote " + outJson);
                    return result.Success ? 0 : 1;
                }
                case "install":
                {
                    string gameDir = null;
                    string tsoDir = null;
                    for (int i = 2; i < args.Length; i++)
                    {
                        if (args[i] == "--game-dir" && i + 1 < args.Length) gameDir = args[++i];
                        else if (args[i] == "--tso-dir" && i + 1 < args.Length) tsoDir = args[++i];
                        else
                        {
                            Console.Error.WriteLine("unknown argument: " + args[i]);
                            PrintUsage();
                            return 2;
                        }
                    }
                    if (tsoDir == null && System.IO.Directory.Exists(DefaultTsoDir)) tsoDir = DefaultTsoDir;
                    if (gameDir == null)
                    {
                        if (System.IO.Directory.Exists(DefaultGameDir)) gameDir = DefaultGameDir;
                        else
                        {
                            Console.Error.WriteLine("install requires --game-dir <dir> (default " + DefaultGameDir + " not found)");
                            return 2;
                        }
                    }
                    var result = PackCompilerApi.Install(packPath, gameDir, tsoDir);
                    Print(result);
                    if (result.Success)
                    {
                        var objectsDir = System.IO.Path.Combine(gameDir, "Objects");
                        foreach (var obj in result.Report.Objects)
                            Console.WriteLine("installed " + System.IO.Path.Combine(objectsDir, obj.Iff) + " (guid " + obj.Guid + ")");
                        Console.WriteLine("updated " + System.IO.Path.Combine(objectsDir, "catalog_downloads.xml"));
                    }
                    return result.Success ? 0 : 1;
                }
                default:
                    Console.Error.WriteLine("unknown command: " + command);
                    PrintUsage();
                    return 2;
            }
        }

        private static int RunImportBatch(string[] args)
        {
            if (args.Length < 2)
            {
                Console.Error.WriteLine("import-batch requires <manifest.csv>");
                PrintUsage();
                return 2;
            }
            var manifest = args[1];
            string outJson = null;
            string packId = "cc0-import";
            string packName = "CC0 Import";
            for (int i = 2; i < args.Length; i++)
            {
                if (args[i] == "-o" && i + 1 < args.Length) outJson = args[++i];
                else if (args[i] == "--pack-id" && i + 1 < args.Length) packId = args[++i];
                else if (args[i] == "--pack-name" && i + 1 < args.Length) packName = args[++i];
                else
                {
                    Console.Error.WriteLine("unknown argument: " + args[i]);
                    PrintUsage();
                    return 2;
                }
            }
            if (outJson == null)
            {
                Console.Error.WriteLine("import-batch requires -o <pack.json>");
                return 2;
            }
            try
            {
                ImportBatchGenerator.Generate(manifest, outJson, packId, packName);
                Console.WriteLine("wrote " + outJson);
                return 0;
            }
            catch (Exception e)
            {
                Console.Error.WriteLine("error: " + e.Message);
                return 1;
            }
        }

        private static void Print(CompileResult result)
        {
            foreach (var w in result.Diagnostics.Warnings) Console.WriteLine("warning: " + w);
            foreach (var e in result.Diagnostics.Errors) Console.Error.WriteLine("error: " + e);
            if (result.Diagnostics.Errors.Count > 0)
                Console.Error.WriteLine(result.Diagnostics.Errors.Count + " error(s), " + result.Diagnostics.Warnings.Count + " warning(s)");
        }

        private static void PrintUsage()
        {
            Console.Error.WriteLine("usage:");
            Console.Error.WriteLine("  FSO.PackCompiler import-batch <manifest.csv> -o <pack.json> [--pack-id <id>] [--pack-name <name>]");
            Console.Error.WriteLine("    CSV columns: obj_path,name,category,height,symmetric,provenance_model[,guid]");
            Console.Error.WriteLine("  FSO.PackCompiler build <pack.json> -o <outdir> [--tso-dir <dir>]");
            Console.Error.WriteLine("    --tso-dir:  TSO install, where clone_from_guid reads sprites from");
            Console.Error.WriteLine("      (default: $FSO_VM_GAME_LOCATION if set, else no sprites are cloned)");
            Console.Error.WriteLine("  FSO.PackCompiler validate <pack.json>");
            Console.Error.WriteLine("  FSO.PackCompiler decompile <object.iff> -o <pack.json>");
            Console.Error.WriteLine("  FSO.PackCompiler install <pack.json> [--game-dir <dir>] [--tso-dir <dir>]");
            Console.Error.WriteLine("    --game-dir: FreeSO content dir, where the object is installed");
            Console.Error.WriteLine("      (default: " + DefaultGameDir + ")");
            Console.Error.WriteLine("    --tso-dir:  TSO install, where clone_from_guid reads sprites from");
            Console.Error.WriteLine("      (default: " + DefaultTsoDir + ")");
        }
    }
}
