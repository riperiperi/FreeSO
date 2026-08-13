using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;

namespace FSO.HouseGen
{
    /// <summary>
    /// Two modes:
    ///   layout JSON → blueprint XML (deterministic; BlueprintWriter)
    ///   --from-image &lt;plan.png&gt; → layout JSON (vision; then same writer validates)
    ///
    /// Verify XML with the harness:
    ///   dotnet FSO.HouseGen.dll layout.json out.xml --base Content/Blueprints/empty_lot_fso.xml
    ///   dotnet FSO.VMHarness.dll --house out.xml
    /// </summary>
    public static class Program
    {
        public static int Main(string[] args)
        {
            try
            {
                return MainAsync(args).GetAwaiter().GetResult();
            }
            catch (Exception e)
            {
                Console.Error.WriteLine(e.Message);
                return 1;
            }
        }

        private static async Task<int> MainAsync(string[] args)
        {
            if (args.Length < 1)
            {
                PrintUsage();
                return 2;
            }

            // AgentBridge documents keys in .env.local; load so a bare `dotnet run` works.
            TryLoadEnv();

            if (args[0] == "--from-image")
                return await FromImageAsync(args);

            return FromLayout(args);
        }

        private static async Task<int> FromImageAsync(string[] args)
        {
            // usage: --from-image <plan.png> [out.json] [--xml out.xml] [--base emptyLot.xml]
            if (args.Length < 2)
            {
                PrintUsage();
                return 2;
            }

            string imagePath = args[1];
            string outJson = null;
            string outXml = null;
            string baseLot = null;
            var positional = new List<string>();
            for (int i = 2; i < args.Length; i++)
            {
                if (args[i] == "--xml" && i + 1 < args.Length) outXml = args[++i];
                else if (args[i] == "--base" && i + 1 < args.Length) baseLot = args[++i];
                else positional.Add(args[i]);
            }
            if (positional.Count > 0) outJson = positional[0];

            var layout = await FloorPlanVision.FromImageAsync(imagePath);
            var json = FloorPlanVision.ToJson(layout);

            if (outJson != null)
            {
                File.WriteAllText(outJson, json);
                Console.WriteLine(
                    $"{layout.Rooms.Count} room(s), {layout.Doors.Count} door(s), " +
                    $"{layout.Windows.Count} window(s) -> {outJson}");
            }
            else
            {
                Console.Write(json);
            }

            // Optional: also emit XML in the same invocation so the Sandbox-Mode path is one step.
            if (outXml != null)
            {
                var xml = BlueprintWriter.Write(layout, baseLot);
                File.WriteAllText(outXml, xml);
                Console.WriteLine($"blueprint -> {outXml}");
            }

            return 0;
        }

        private static int FromLayout(string[] args)
        {
            string baseLot = null;
            var positional = new List<string>();
            for (int i = 0; i < args.Length; i++)
            {
                if (args[i] == "--base" && i + 1 < args.Length) { baseLot = args[++i]; }
                else positional.Add(args[i]);
            }
            args = positional.ToArray();

            if (args.Length < 1)
            {
                PrintUsage();
                return 2;
            }

            var layout = JsonSerializer.Deserialize<HouseLayout>(
                File.ReadAllText(args[0]),
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true, IncludeFields = true });

            if (layout == null)
            {
                Console.Error.WriteLine("Could not read a layout from " + args[0]);
                return 1;
            }

            string xml;
            try
            {
                xml = BlueprintWriter.Write(layout, baseLot);
            }
            catch (ArgumentException e)
            {
                Console.Error.WriteLine("Invalid layout: " + e.Message);
                return 1;
            }

            if (args.Length > 1)
            {
                File.WriteAllText(args[1], xml);
                Console.WriteLine($"{layout.Rooms.Count} room(s) -> {args[1]}");
            }
            else
            {
                Console.Write(xml);
            }
            return 0;
        }

        private static void TryLoadEnv()
        {
            // Walk up from cwd and from the assembly dir looking for .env.local.
            var candidates = new List<string>();
            void AddWalk(string start)
            {
                try
                {
                    var dir = new DirectoryInfo(start);
                    for (int i = 0; i < 6 && dir != null; i++, dir = dir.Parent)
                        candidates.Add(Path.Combine(dir.FullName, ".env.local"));
                }
                catch { /* ignore */ }
            }
            AddWalk(Directory.GetCurrentDirectory());
            AddWalk(AppContext.BaseDirectory);

            foreach (var path in candidates)
            {
                if (File.Exists(path))
                {
                    FloorPlanVision.LoadDotEnv(path);
                    return;
                }
            }
        }

        private static void PrintUsage()
        {
            Console.Error.WriteLine("usage: FSO.HouseGen <layout.json> [out.xml] [--base <emptyLot.xml>]");
            Console.Error.WriteLine("       FSO.HouseGen --from-image <plan.png> [out.json] [--xml out.xml] [--base <emptyLot.xml>]");
        }
    }
}
