using System;
using System.Collections.Generic;
using System.IO;
using CASOutfitImporter.Importer;
using CASOutfitImporter.Packaging;
using CASOutfitImporter.Verify;

namespace CASOutfitImporter
{
    public static class Program
    {
        public static int Main(string[] args)
        {
            try
            {
                var opts = ParseArgs(args);
                if (opts == null)
                {
                    PrintUsage();
                    return 1;
                }

                // Verify-only short-circuit: don't import anything, just walk the
                // staging tree and report.
                if (!string.IsNullOrEmpty(opts.VerifyOnly))
                {
                    return Verifier.Run(opts.VerifyOnly);
                }

                if (string.IsNullOrEmpty(opts.RepoRoot))
                    Directory.CreateDirectory(opts.StagingRoot);
                else
                    Directory.CreateDirectory(opts.RepoRoot);

                var importer = new SkinImporter();
                var results = new List<ImportResult>();
                foreach (var item in opts.Items)
                {
                    Console.WriteLine($"-> {item.Name}  [{item.Part} / {item.Gender}]");
                    var req = new ImportRequest
                    {
                        InputDir = item.InputDir,
                        Part = item.Part,
                        Gender = item.Gender,
                        Name = item.Name,
                        OutputDir = opts.StagingRoot,
                        Mode = item.Mode,
                        SourceCollection = item.SourceCollection,
                        RepoRoot = opts.RepoRoot
                    };
                    results.Add(importer.Run(req));
                }

                if (string.IsNullOrEmpty(opts.RepoRoot))
                    PackageBuilder.WriteReadme(opts.StagingRoot, results);

                if (opts.RunVerify)
                {
                    Console.WriteLine();
                    Console.WriteLine("=== Verifying staged output ===");
                    foreach (var r in results)
                    {
                        int rc = Verifier.Run(r.StagingRoot);
                        if (rc != 0) return rc;
                    }
                }

                if (!string.IsNullOrEmpty(opts.OutputZip) && string.IsNullOrEmpty(opts.RepoRoot))
                {
                    var zipPath = PackageBuilder.BuildZip(opts.StagingRoot, opts.OutputZip);
                    Console.WriteLine();
                    Console.WriteLine($"Package: {zipPath}");
                }

                if (!string.IsNullOrEmpty(opts.RepoRoot))
                {
                    Console.WriteLine();
                    foreach (var r in results)
                        Console.WriteLine($"Saved to repo: {r.PackDir}");
                }
                else
                {
                    Console.WriteLine($"Staging: {opts.StagingRoot}");
                }
                return 0;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine("ERROR: " + ex.Message);
                Console.Error.WriteLine(ex.StackTrace);
                return 2;
            }
        }

        private sealed class CliOptions
        {
            public string StagingRoot;
            public string OutputZip;
            public string RepoRoot;        // --save-to-repo
            public bool RunVerify;
            public string VerifyOnly;
            public List<CliItem> Items = new List<CliItem>();
        }

        private sealed class CliItem
        {
            public string InputDir;
            public AvatarPart Part;
            public char Gender;
            public string Name;
            public ImportMode Mode = ImportMode.Cas;
            public string SourceCollection;
        }

        private static CliOptions ParseArgs(string[] args)
        {
            var opts = new CliOptions();
            CliItem cur = null;

            for (int i = 0; i < args.Length; i++)
            {
                string a = args[i];
                string Need() { if (++i >= args.Length) throw new ArgumentException($"{a} needs a value"); return args[i]; }

                switch (a)
                {
                    case "--staging":   opts.StagingRoot = Need(); break;
                    case "--zip":       opts.OutputZip   = Need(); break;
                    case "--save-to-repo": opts.RepoRoot = Need(); break;
                    case "--verify":    opts.RunVerify = true; break;
                    case "--verify-only": opts.VerifyOnly = Need(); break;
                    case "--input":
                        if (cur != null) opts.Items.Add(cur);
                        cur = new CliItem { InputDir = Need() };
                        break;
                    case "--type":
                        if (cur == null) throw new ArgumentException("--type before --input");
                        cur.Part = Need().ToLowerInvariant() switch
                        {
                            "head" => AvatarPart.Head,
                            "body" => AvatarPart.Body,
                            var v  => throw new ArgumentException($"--type must be head|body, got {v}")
                        };
                        break;
                    case "--gender":
                        if (cur == null) throw new ArgumentException("--gender before --input");
                        var g = Need().ToLowerInvariant();
                        cur.Gender = (g == "m" || g == "male") ? 'm'
                                  : (g == "f" || g == "female") ? 'f'
                                  : throw new ArgumentException($"--gender must be m|f, got {g}");
                        break;
                    case "--name":
                        if (cur == null) throw new ArgumentException("--name before --input");
                        cur.Name = Need();
                        break;
                    case "--mode":
                        if (cur == null) throw new ArgumentException("--mode before --input");
                        var modeRaw = Need().ToLowerInvariant();
                        if (!ModeRegistry.TryParse(modeRaw, out var parsedMode))
                            throw new ArgumentException(
                                $"--mode must be one of: {ModeRegistry.AllCliNames()} (got '{modeRaw}')");
                        cur.Mode = parsedMode;
                        break;
                    case "--source-collection":
                        if (cur == null) throw new ArgumentException("--source-collection before --input");
                        cur.SourceCollection = Need();
                        break;
                    case "-h":
                    case "--help":
                        return null;
                    default:
                        throw new ArgumentException($"unknown arg: {a}");
                }
            }
            if (cur != null) opts.Items.Add(cur);

            if (!string.IsNullOrEmpty(opts.VerifyOnly)) return opts;
            if (opts.Items.Count == 0) return null;
            if (string.IsNullOrEmpty(opts.StagingRoot)) opts.StagingRoot = "./out";
            foreach (var it in opts.Items)
            {
                if (string.IsNullOrEmpty(it.Name))
                    it.Name = Path.GetFileName(it.InputDir.TrimEnd('/'));
                if (it.Gender == 0)
                    throw new ArgumentException($"--gender required for {it.InputDir}");
            }

            return opts;
        }

        private static void PrintUsage()
        {
            Console.WriteLine(@"CASOutfitImporter — convert TS1 .skn/.bmp asset folders into a FreeSO CAS pack.

Usage:
  # Save into a content-repo for management by manage-packs (recommended):
  CASOutfitImporter --save-to-repo <repo-dir> [--verify]
                    --input <dir> --type head|body --gender m|f --name <id>
                                  [--mode cas|trunk:wedding]
                    [--input ... ] ...

  # One-off staging dir (legacy mode):
  CASOutfitImporter [--staging <dir>] [--zip <path.zip>] [--verify]
                    --input <dir> --type head|body --gender m|f --name <id>
                                  [--mode cas|trunk:wedding]
                                  [--source-collection <path.col-or-FAR3>]
                    [--input ... ] ...

  CASOutfitImporter --verify-only <staged-dir>

--save-to-repo <dir>   place pack under <dir>/<category>/<name>/ + write pack.json.
                       Single-entry .col only — manage-packs handles merging at install.
                       Mutually exclusive with --source-collection.

--mode    cas             (default) outfit selectable in Create-A-Sim
          trunk:wedding   outfit appears in any wedding trunk (body only)
          trunk:scifi     sci-fi trunk
          trunk:vegas     Vegas trunk
          trunk:vaudwest  vaudwest trunk
          trunk:uniforms  uniforms trunk
          trunk:costumes  costumes trunk
          trunk:oldworld  old-world trunk
          trunk:sports    sports trunk
          trunk:toga      toga trunk

--source-collection <path>   (legacy mode) reads existing collection (raw .col or
                             FAR3 .dat) and merges the new entry. Used for one-off
                             zips intended for direct copy onto an install.

--verify        cross-link verification of each staged pack after building.
--verify-only   skip import; just verify an existing staged or pack directory.

Examples:
  # Save into the content-repo (the standard workflow with manage-packs)
  CASOutfitImporter \
    --save-to-repo /srv/dev_projects/personal/FreeSO/content-repo --verify \
    --input ./test_skin_head/b076fa_k8groupie --type body --gender f --name k8groupie \
    --mode trunk:wedding

  # One-off zip for direct copy onto a single install
  CASOutfitImporter \
    --staging ./out --zip ./out.zip --verify \
    --input ./test_skin_head/b076fa_k8groupie --type body --gender f --name k8groupie \
    --mode trunk:wedding \
    --source-collection /home/ian/games/edenso/game/TSOClient/avatardata/bodies/collections/collections.dat
");
        }
    }
}