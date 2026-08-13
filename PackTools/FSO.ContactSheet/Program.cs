using System;
using System.IO;

namespace FSO.ContactSheet
{
    /// <summary>
    /// usage: FSO.ContactSheet <pack-dir> -o <sheet.png> [--tso-dir <dir>]
    /// Compiles every *.json pack in pack-dir and composites one labeled contact sheet:
    /// one row per object, one column per zoom level (Far/Medium/Near), for eyeballing a
    /// whole collection's silhouettes and palette coherence at once.
    /// </summary>
    public static class Program
    {
        public static int Main(string[] args)
        {
            if (args.Length < 1)
            {
                PrintUsage();
                return 2;
            }

            string packDir = args[0];
            string outPath = null;
            string tsoDir = null;
            string exportDir = null;
            for (int i = 1; i < args.Length; i++)
            {
                if (args[i] == "-o" && i + 1 < args.Length) outPath = args[++i];
                else if (args[i] == "--tso-dir" && i + 1 < args.Length) tsoDir = args[++i];
                else if (args[i] == "--export-dir" && i + 1 < args.Length) exportDir = args[++i];
                else
                {
                    Console.Error.WriteLine("unknown argument: " + args[i]);
                    PrintUsage();
                    return 2;
                }
            }

            if (!Directory.Exists(packDir))
            {
                Console.Error.WriteLine("no such directory: " + packDir);
                return 2;
            }
            if (outPath == null && exportDir == null)
            {
                Console.Error.WriteLine("contact sheet requires -o <sheet.png> and/or --export-dir <dir>");
                return 2;
            }
            if (tsoDir == null) tsoDir = Environment.GetEnvironmentVariable("FSO_VM_GAME_LOCATION");

            var cells = ContactSheetBuilder.BuildCells(packDir, tsoDir);
            if (cells.Count == 0)
            {
                Console.Error.WriteLine("no *.json packs found in " + packDir);
                return 1;
            }

            if (outPath != null) Compositor.WriteSheet(cells, outPath);

            if (exportDir != null)
            {
                // Per-object near-zoom PNGs + manifest, for the browser client's
                // sprite-billboard furniture (it has no TSO content system).
                Directory.CreateDirectory(exportDir);
                var manifest = new System.Text.StringBuilder();
                manifest.Append("[\n");
                var first = true;
                foreach (var cell in cells)
                {
                    if (cell.ObjectId == null || !cell.FramesByZoom.TryGetValue("NEAR", out var near)) continue;
                    var png = cell.ObjectId + ".png";
                    FSO.PackCompiler.ArtGen.PngWriter.Write(
                        Path.Combine(exportDir, png), near.Pixels, near.Width, near.Height);
                    if (!first) manifest.Append(",\n");
                    first = false;
                    manifest.Append($"  {{\"id\": \"{cell.ObjectId}\", \"guid\": \"0x{cell.Guid:X8}\", \"png\": \"{png}\", \"w\": {near.Width}, \"h\": {near.Height}}}");
                }
                manifest.Append("\n]\n");
                File.WriteAllText(Path.Combine(exportDir, "manifest.json"), manifest.ToString());
                Console.WriteLine("exported sprites + manifest to " + exportDir);
            }

            int errorRows = 0;
            foreach (var cell in cells)
            {
                var status = cell.Errors.Count > 0 ? "ISSUES: " + string.Join("; ", cell.Errors) : "ok";
                Console.WriteLine(cell.PackFile + " / " + cell.Label + " -> " + status);
                if (cell.Errors.Count > 0) errorRows++;
            }
            Console.WriteLine();
            Console.WriteLine("wrote " + outPath + " (" + cells.Count + " objects, " + errorRows + " with issues)");

            return 0;
        }

        static void PrintUsage()
        {
            Console.Error.WriteLine("usage: FSO.ContactSheet <pack-dir> -o <sheet.png> [--tso-dir <dir>]");
            Console.Error.WriteLine("  Compiles every *.json pack in <pack-dir> and composites a labeled");
            Console.Error.WriteLine("  contact sheet (one row per object, one column per zoom level) so a");
            Console.Error.WriteLine("  whole collection can be reviewed at a glance instead of PNG by PNG.");
            Console.Error.WriteLine("  --tso-dir: needed only if any pack uses appearance.clone_from_guid");
            Console.Error.WriteLine("    (default: $FSO_VM_GAME_LOCATION if set)");
        }
    }
}
