using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using Newtonsoft.Json.Linq;

namespace FSO.PackCompiler
{
    /// <summary>
    /// Reads a CSV manifest and emits a pack JSON with appearance.imported objects.
    /// CSV columns: obj_path,name,category,height,symmetric,provenance_model[,guid]
    /// Paths in obj_path are relative to the manifest file's directory.
    /// </summary>
    public static class ImportBatchGenerator
    {
        public static void Generate(string manifestCsvPath, string outPackJsonPath, string packId, string packName)
        {
            var manifestDir = Path.GetDirectoryName(Path.GetFullPath(manifestCsvPath)) ?? ".";
            var packDir = Path.GetDirectoryName(Path.GetFullPath(outPackJsonPath)) ?? ".";
            var lines = File.ReadAllLines(manifestCsvPath)
                .Select(l => l.Trim())
                .Where(l => l.Length > 0 && !l.StartsWith("#"))
                .ToList();

            if (lines.Count == 0)
                throw new InvalidDataException("manifest is empty");

            var objects = new JArray();
            foreach (var line in lines)
            {
                var cols = SplitCsvLine(line);
                if (cols.Length < 6)
                    throw new InvalidDataException("expected columns: obj_path,name,category,height,symmetric,provenance_model[,guid] — got: " + line);

                var objPath = cols[0];
                var name = cols[1];
                var category = cols[2];
                var height = double.Parse(cols[3], CultureInfo.InvariantCulture);
                var symmetric = ParseBool(cols[4]);
                var model = cols[5];
                var id = SanitizeId(Path.GetFileNameWithoutExtension(objPath));
                var guid = cols.Length >= 7 && !string.IsNullOrWhiteSpace(cols[6])
                    ? cols[6]
                    : "0x" + GuidAllocator.Allocate(packId, id).ToString("X8");

                var relMesh = MakeRelative(packDir, Path.GetFullPath(Path.Combine(manifestDir, objPath)));

                objects.Add(new JObject
                {
                    ["id"] = id,
                    ["guid"] = guid,
                    ["name"] = name,
                    ["price"] = 100,
                    ["category"] = category,
                    ["appearance"] = new JObject
                    {
                        ["imported"] = new JObject
                        {
                            ["mesh"] = relMesh,
                            ["height"] = height,
                            ["symmetric"] = symmetric,
                            ["provenance"] = new JObject
                            {
                                ["source"] = packName,
                                ["license"] = "CC0",
                                ["model"] = model,
                            },
                        },
                    },
                    ["trees"] = new JObject
                    {
                        ["main_loop"] = new JObject
                        {
                            ["args"] = new JArray(),
                            ["locals"] = new JArray(),
                            ["nodes"] = new JArray
                            {
                                new JObject
                                {
                                    ["id"] = "idle",
                                    ["prim"] = "idle_for_input",
                                    ["ticks_param"] = 0,
                                    ["allow_push"] = true,
                                    ["then"] = "idle",
                                    ["else"] = "idle",
                                },
                            },
                        },
                    },
                    ["entry_points"] = new JObject { ["main"] = "main_loop" },
                });
            }

            var pack = new JObject
            {
                ["schema"] = "fso-pack/0.1",
                ["engine"] = "tso",
                ["pack"] = new JObject
                {
                    ["id"] = packId,
                    ["name"] = packName,
                    ["author"] = "kat",
                    ["version"] = "1.0.0",
                    ["description"] = "Batch-imported CC0 meshes",
                },
                ["objects"] = objects,
            };

            File.WriteAllText(outPackJsonPath, pack.ToString(Newtonsoft.Json.Formatting.Indented));
        }

        static string[] SplitCsvLine(string line)
        {
            var result = new List<string>();
            var sb = new StringBuilder();
            bool inQuotes = false;
            for (int i = 0; i < line.Length; i++)
            {
                var ch = line[i];
                if (ch == '"') { inQuotes = !inQuotes; continue; }
                if (ch == ',' && !inQuotes)
                {
                    result.Add(sb.ToString().Trim());
                    sb.Clear();
                    continue;
                }
                sb.Append(ch);
            }
            result.Add(sb.ToString().Trim());
            return result.ToArray();
        }

        static bool ParseBool(string s) =>
            s.Equals("true", StringComparison.OrdinalIgnoreCase) ||
            s.Equals("1", StringComparison.OrdinalIgnoreCase) ||
            s.Equals("yes", StringComparison.OrdinalIgnoreCase);

        static string SanitizeId(string name)
        {
            var sb = new StringBuilder();
            foreach (var ch in name.ToLowerInvariant())
            {
                if ((ch >= 'a' && ch <= 'z') || (ch >= '0' && ch <= '9')) sb.Append(ch);
                else if (ch == '-' || ch == '_') sb.Append('_');
            }
            var id = sb.ToString().Trim('_');
            return string.IsNullOrEmpty(id) ? "imported_object" : id;
        }

        // Mesh files live next to the CSV (assets/), pack JSON lives in examples/.
        // Path.GetRelativePath is what produces the `../assets/...` form SCHEMA.md requires;
        // a StartsWith(packDir) check returns the absolute path whenever the mesh is a sibling.
        static string MakeRelative(string baseDir, string fullPath)
        {
            var rel = Path.GetRelativePath(Path.GetFullPath(baseDir), Path.GetFullPath(fullPath));
            return rel.Replace('\\', '/');
        }
    }
}
