using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using FSO.PackCompiler;
using Newtonsoft.Json.Linq;

namespace FSO.ModServer
{
    /// <summary>
    /// Implements test_in_vm per MCP-DESIGN.md §3: compiles the session's pack, then shells
    /// out to FSO.VMHarness (a separate net8.0/MonoGame process — not referenced in-process,
    /// unlike §5's preference, because it needs its own MonoGame/graphics-stub init path that
    /// would drag those dependencies into the MCP server for every tool call) to actually run
    /// the object in the VM.
    /// </summary>
    internal static class VmHarnessRunner
    {
        private const int TimeoutMs = 120_000;

        public static object Run(PackSession session, string scenarioJson)
        {
            JObject scenario;
            try { scenario = string.IsNullOrWhiteSpace(scenarioJson) ? new JObject() : JObject.Parse(scenarioJson); }
            catch (Exception e) { return Errors.InvalidJson("scenarioJson", e.Message); }

            var gameLocation = GameContent.ResolveDir((string)scenario["game_location"]);
            if (gameLocation == null)
                return Errors.Make("game_content_not_found", null, null, null, "game_location",
                    $"TSO game content not found at \"{GameContent.DefaultPathForMessage()}\" — pass scenario.game_location or set FSO_VM_GAME_LOCATION");

            var packPath = PackToolHandlers.WriteTemp(session);
            var outDir = Path.Combine(Path.GetDirectoryName(packPath)!, "out");
            var compileResult = PackCompilerApi.Build(packPath, outDir, gameLocation);
            var compileEnvelope = DiagnosticMapper.Map(compileResult.Diagnostics, session.Pack);
            if (!compileEnvelope.Ok) return compileEnvelope;

            var placeObjectId = (string)scenario["place_object"];
            ObjectReport objReport;
            if (!string.IsNullOrEmpty(placeObjectId))
            {
                objReport = compileResult.Report.Objects.FirstOrDefault(o => o.Id == placeObjectId);
                if (objReport == null)
                    return Errors.UnknownObject(placeObjectId);
            }
            else
            {
                objReport = compileResult.Report.Objects.FirstOrDefault();
                if (objReport == null)
                    return Errors.Make("empty_pack", null, null, null, "place_object", "pack session has no objects to test");
            }

            var harnessDll = FindHarnessDll();
            if (harnessDll == null)
                return Errors.Make("vm_harness_not_built", null, null, null, null,
                    "FSO.VMHarness/bin/{Debug,Release}/net8.0/FSO.VMHarness.dll not found — run `dotnet build` on PackTools/FSO.VMHarness first");

            var pushInteraction = (string)scenario["push_interaction"] ?? "";
            var maxTicks = scenario["max_ticks"]?.Value<int>() ?? 200;

            var warnings = new List<ToolError>();
            if (scenario["spawn_sim"] != null)
                warnings.Add(new ToolError { Code = "unsupported_scenario_field", Field = "spawn_sim", Message = "spawn_sim (initial motive overrides) is not wired up yet — sim spawns with default motives" });

            var objectIffPath = Path.Combine(outDir, objReport.Iff);
            var (exitCode, stdout, stderr, timedOut) = RunProcess(harnessDll, new[]
            {
                gameLocation, objectIffPath, pushInteraction, maxTicks.ToString()
            }, TimeoutMs);

            if (timedOut)
                return Errors.Make("vm_harness_timeout", objReport.Id, null, null, null,
                    $"FSO.VMHarness did not exit within {TimeoutMs}ms; stderr tail: {Tail(stderr)}");

            JObject harnessReport;
            try { harnessReport = JObject.Parse(stdout); }
            catch (Exception e)
            {
                return Errors.Make("vm_harness_failed", objReport.Id, null, null, null,
                    $"FSO.VMHarness exited {exitCode} with unparseable output ({e.Message}); stderr tail: {Tail(stderr)}");
            }

            var trace = new JArray();
            foreach (var evt in (JArray)harnessReport["trace"])
            {
                trace.Add(new JObject
                {
                    ["tick"] = evt["Tick"],
                    ["event"] = evt["Event"],
                    ["detail"] = evt["Detail"],
                });
            }

            var finalState = (JObject)harnessReport["final_state"];
            var assertionsIn = scenario["assertions"] as JArray ?? new JArray();
            var assertionResults = EvaluateAssertions(assertionsIn, objReport, placeObjectId ?? objReport.Id, finalState, warnings);

            var ticksRun = finalState["ticks_run"]?.Value<int>() ?? 0;
            var tickLimitHit = finalState["tick_limit_hit"]?.Value<bool>() ?? false;

            return new
            {
                ok = true,
                errors = new List<ToolError>(),
                warnings = warnings,
                pushed_interaction = (string)harnessReport["pushed_interaction"],
                placement_status = (string)harnessReport["placement_status"],
                ticks_run = ticksRun,
                tick_limit_hit = tickLimitHit,
                trace = PackToolHandlers.ToPlain(trace),
                final_state = PackToolHandlers.ToPlain(finalState),
                assertions = assertionResults,
            };
        }

        private static List<object> EvaluateAssertions(JArray assertions, ObjectReport objReport, string objectId, JObject finalState, List<ToolError> warnings)
        {
            var results = new List<object>();
            foreach (var a in assertions)
            {
                var type = (string)a["type"];
                var target = (string)a["target"];
                object actual = null;
                bool? passed = null;
                string note = null;

                switch (type)
                {
                    case "motive_at_least":
                    case "motive_at_most":
                    case "motive_equals":
                        {
                            var motive = (string)a["motive"];
                            var key = "sim_motive_" + motive;
                            if (target != "sim" || finalState[key] == null)
                            {
                                note = "unsupported: VMHarness only reports sim motives \"social\" and \"fun\" for target \"sim\"";
                            }
                            else
                            {
                                var actualVal = finalState[key].Value<double>();
                                var expected = a["value"].Value<double>();
                                actual = actualVal;
                                passed = type == "motive_at_least" ? actualVal >= expected
                                    : type == "motive_at_most" ? actualVal <= expected
                                    : actualVal == expected;
                            }
                            break;
                        }
                    case "attribute_equals":
                        {
                            var attribute = (string)a["attribute"];
                            if (target != objectId || !objReport.Attributes.TryGetValue(attribute, out var idx) || idx > 1)
                            {
                                note = "unsupported: VMHarness only reports attributes 0 and 1 on the placed object";
                            }
                            else
                            {
                                var key = "object_attribute_" + idx;
                                var actualVal = finalState[key].Value<long>();
                                var expected = a["value"].Value<long>();
                                actual = actualVal;
                                passed = actualVal == expected;
                            }
                            break;
                        }
                    case "node_reached":
                    case "node_not_reached":
                    case "tree_returned":
                        note = "unsupported: FSO.VMHarness's trace doesn't carry per-node ids yet, only routine name + instruction pointer";
                        break;
                    default:
                        note = $"unknown assertion type \"{type}\"";
                        break;
                }

                if (note != null)
                    warnings.Add(new ToolError { Code = "assertion_not_evaluable", Field = "assertions", Message = $"{type}/{target}: {note}" });

                results.Add(new
                {
                    type,
                    target,
                    expected = a["value"] == null ? null : PackToolHandlers.ToPlain(a["value"]),
                    actual,
                    passed,
                });
            }
            return results;
        }

        private static string FindHarnessDll()
        {
            var dir = new DirectoryInfo(AppContext.BaseDirectory);
            while (dir != null && !Directory.Exists(Path.Combine(dir.FullName, "PackTools")))
                dir = dir.Parent;
            if (dir == null) return null;

            foreach (var config in new[] { "Debug", "Release" })
            {
                var candidate = Path.Combine(dir.FullName, "PackTools", "FSO.VMHarness", "bin", config, "net8.0", "FSO.VMHarness.dll");
                if (File.Exists(candidate)) return candidate;
            }
            return null;
        }

        private static (int exitCode, string stdout, string stderr, bool timedOut) RunProcess(string dllPath, string[] args, int timeoutMs)
        {
            var psi = new ProcessStartInfo
            {
                FileName = "dotnet",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
            };
            psi.ArgumentList.Add(dllPath);
            foreach (var a in args) psi.ArgumentList.Add(a);

            using var process = Process.Start(psi);
            var stdout = process.StandardOutput.ReadToEnd();
            var stderr = process.StandardError.ReadToEnd();
            var exited = process.WaitForExit(timeoutMs);
            if (!exited)
            {
                try { process.Kill(true); } catch { /* best-effort */ }
                return (-1, stdout, stderr, true);
            }
            return (process.ExitCode, stdout, stderr, false);
        }

        private static string Tail(string s, int chars = 2000) =>
            string.IsNullOrEmpty(s) || s.Length <= chars ? s : s.Substring(s.Length - chars);
    }
}
