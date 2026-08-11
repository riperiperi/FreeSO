using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace FSO.HouseGen
{
    /// <summary>
    /// layout JSON -> blueprint XML. Verify the result by running it through the harness:
    ///   dotnet FSO.HouseGen.dll &lt;layout.json&gt; out.xml
    ///   dotnet FSO.VMHarness.dll --house out.xml
    /// </summary>
    public static class Program
    {
        public static int Main(string[] args)
        {
            if (args.Length < 1)
            {
                Console.Error.WriteLine("usage: FSO.HouseGen <layout.json> [out.xml] [--base <emptyLot.xml>]");
                return 2;
            }

            // Without a base lot the output is architecture only — it loads and encloses rooms,
            // but the client draws nothing, because no lot phone means no VM.TSOState.Size.
            string baseLot = null;
            var positional = new List<string>();
            for (int i = 0; i < args.Length; i++)
            {
                if (args[i] == "--base" && i + 1 < args.Length) { baseLot = args[++i]; }
                else positional.Add(args[i]);
            }
            args = positional.ToArray();

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
    }
}
