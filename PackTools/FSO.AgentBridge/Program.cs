using System;
using System.Linq;
using System.Text.Json;
using System.Threading;
using Anthropic;

namespace FSO.AgentBridge
{
    /// <summary>
    /// Console harness: plain-language request in, compiled object out. The second consumer of
    /// IMakeSomethingAgent alongside the UI panel, and the one that can run without a game
    /// client — so the loop can be exercised before any UI exists.
    ///
    ///   FSO.AgentBridge "a garden gnome that gossips with me"
    ///   FSO.AgentBridge --schemas     (print the generated tool table; needs no credentials)
    /// </summary>
    public static class Program
    {
        public static int Main(string[] args)
        {
            if (args.Length == 0 || args[0] == "--help")
            {
                Console.Error.WriteLine("usage: FSO.AgentBridge \"<what you want to make>\"");
                Console.Error.WriteLine("       FSO.AgentBridge --schemas");
                return 2;
            }

            if (args[0] == "--schemas") return PrintSchemas();
            if (args[0] == "--dispatch-test") return DispatchTest();
            if (args[0] == "--ipc") return RunIpc(args.Length > 1 ? args[1] : "");

            ILlmProvider provider;
            try { provider = BuildProvider(); }
            catch (Exception e) { Console.Error.WriteLine(e.Message); return 2; }

            var done = new ManualResetEventSlim(false);
            var agent = new MakeSomethingAgent(
                provider,
                // Console harness has no game thread to marshal onto; run events inline.
                dispatch: a => a(),
                deliver: DeliverToDisk);

            agent.OnNarration += line => Console.WriteLine("  " + line);
            agent.OnObjectComplete += guid =>
            {
                Console.WriteLine($"\nReady to place. (0x{guid:X8})");
                done.Set();
            };
            agent.OnError += msg =>
            {
                Console.WriteLine("\n" + msg);
                done.Set();
            };

            var sw = System.Diagnostics.Stopwatch.StartNew();
            Console.WriteLine($"> {args[0]}\n");
            agent.SendMessage(args[0]);

            var timedOut = !done.Wait(TimeSpan.FromMinutes(10));
            if (timedOut) Console.WriteLine("\n[harness gave up at 10 minutes — metrics below are partial]");

            var wall = sw.Elapsed.TotalSeconds;
            Console.WriteLine("\n turn |  model s | tools s | in tok | cache rd | cache wr | out tok | tools called");
            foreach (var m in agent.Metrics)
                Console.WriteLine($" {m.Turn,4} | {m.ModelSeconds,8:F1} | {m.ToolSeconds,7:F1} | {m.InputTokens,6} | {m.CacheReadTokens,8} | {m.CacheWriteTokens,8} | {m.OutputTokens,7} | {string.Join(", ", m.ToolsCalled)}");

            var modelTotal = agent.Metrics.Sum(m => m.ModelSeconds);
            var toolTotal = agent.Metrics.Sum(m => m.ToolSeconds);
            Console.WriteLine($"\nwall {wall:F1}s = model {modelTotal:F1}s ({modelTotal / wall * 100:F0}%) + tools {toolTotal:F1}s ({toolTotal / wall * 100:F0}%) + other {wall - modelTotal - toolTotal:F1}s");

            var u = agent.Usage;
            Console.WriteLine($"\n[{provider.Name}/{provider.Model} | {u.Turns} turns | " +
                              $"in {u.InputTokens} + cache(rd {u.CacheReadTokens} / wr {u.CacheWriteTokens}) | out {u.OutputTokens} | ~${u.EstimatedCostUsd:F3} at assumed rates]");
            return timedOut ? 1 : 0;
        }

        /// <summary>
        /// Line protocol for the in-game panel. The game spawns this process and reads stdout;
        /// one tagged line per event, so the client needs no reference to this project and no
        /// LLM SDK of its own.
        ///
        ///   N &lt;text&gt;   narration, already player-safe — show it verbatim
        ///   G &lt;hex&gt;    finished; the object's GUID
        ///   E &lt;text&gt;   player-safe error
        ///
        /// Nothing else goes to stdout: no metrics, no tool names, no JSON. That's the same
        /// contract IMakeSomethingAgent states — the panel must never be able to leak a tool
        /// name or a stack trace at the player. Diagnostics go to stderr, which the game logs
        /// but never displays.
        /// </summary>
        static int RunIpc(string prompt)
        {
            if (string.IsNullOrWhiteSpace(prompt))
            {
                Console.WriteLine("E Tell me what you'd like me to make.");
                return 2;
            }

            ILlmProvider provider;
            try { provider = BuildProvider(); }
            catch (Exception e)
            {
                // Configuration problems are ours, not the player's — say something they can act on.
                Console.Error.WriteLine(e.Message);
                Console.WriteLine("E I can't reach my workshop right now. Try again in a moment.");
                return 2;
            }

            var done = new ManualResetEventSlim(false);
            var exit = 1;
            var agent = new MakeSomethingAgent(
                provider,
                dispatch: a => a(),
                deliver: DeliverToDisk);

            agent.OnNarration += line => { Console.WriteLine("N " + Sanitize(line)); Console.Out.Flush(); };
            agent.OnObjectComplete += guid =>
            {
                // O carries everything the client needs to register the object live, before G
                // announces completion — so the panel never says "ready" before it is.
                var d = LastDelivery;
                if (d != null)
                {
                    Console.WriteLine($"O {guid:X8}|{d.Category}|{d.Price}|{Sanitize(d.Name)}|{Sanitize(d.IffPath)}");
                }
                Console.WriteLine($"G {guid:X8}");
                Console.Out.Flush();
                exit = 0;
                done.Set();
            };
            agent.OnError += msg =>
            {
                Console.WriteLine("E " + Sanitize(msg));
                Console.Out.Flush();
                done.Set();
            };

            agent.SendMessage(prompt);

            if (!done.Wait(TimeSpan.FromMinutes(10)))
            {
                Console.WriteLine("E That took longer than expected. Nothing was made — try asking for something simpler.");
                Console.Out.Flush();
            }

            var u = agent.Usage;
            Console.Error.WriteLine($"[{provider.Name}/{provider.Model} | {u.Turns} turns | ~${u.EstimatedCostUsd:F3}]");
            return exit;
        }

        /// <summary>
        /// Newlines would split one event across several protocol lines and desynchronise the
        /// reader, so they're flattened rather than trusted.
        /// </summary>
        static string Sanitize(string s) =>
            (s ?? "").Replace("\r", " ").Replace("\n", " ").Trim();

        /// <summary>What the game needs to register a freshly compiled object without recompiling.</summary>
        class Delivery
        {
            public string IffPath;
            public string Name;
            public int Category;
            public int Price;
        }

        static Delivery LastDelivery;

        /// <summary>
        /// Provider selection. FSO_AGENT_PROVIDER picks the backend ("anthropic" or "openai");
        /// with it unset we use whichever key is present, so a single key in .env.local just
        /// works. Model IDs are overridable so neither is pinned in code.
        /// Keys are read from the environment only — never a CLI argument, which would put
        /// them in process listings.
        /// </summary>
        private static ILlmProvider BuildProvider()
        {
            var anthropicKey = Environment.GetEnvironmentVariable("ANTHROPIC_API_KEY");
            var openAiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY");
            var choice = Environment.GetEnvironmentVariable("FSO_AGENT_PROVIDER")?.ToLowerInvariant();

            if (choice == null)
                choice = !string.IsNullOrEmpty(anthropicKey) ? "anthropic"
                       : !string.IsNullOrEmpty(openAiKey) ? "openai"
                       : throw new InvalidOperationException(
                           "No API key found. Set ANTHROPIC_API_KEY or OPENAI_API_KEY (both live in .env.local).");

            if (choice == "openai")
            {
                if (string.IsNullOrEmpty(openAiKey))
                    throw new InvalidOperationException("FSO_AGENT_PROVIDER=openai but OPENAI_API_KEY is not set.");

                var http = new System.Net.Http.HttpClient { Timeout = TimeSpan.FromMinutes(5) };
                http.DefaultRequestHeaders.Authorization =
                    new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", openAiKey);
                // Rates are ASSUMED, not measured, and go stale — so they're overridable and
                // printed with the cost, because someone will make a pricing decision on this
                // number and needs to know which rate card produced it.
                double Rate(string var, double fallback) =>
                    double.TryParse(Environment.GetEnvironmentVariable(var), out var v) ? v : fallback;

                // Any OpenAI-compatible endpoint works here — LM Studio, vLLM, a proxy.
                // Pointing at a local server is a base-URL change and nothing else, which is
                // the payoff from putting providers behind an interface.
                return new OpenAIProvider(http,
                    Environment.GetEnvironmentVariable("FSO_AGENT_MODEL") ?? OpenAIProvider.DefaultModel,
                    endpoint: Environment.GetEnvironmentVariable("FSO_OPENAI_ENDPOINT")
                              ?? "https://api.openai.com/v1/chat/completions",
                    inputPerM: Rate("FSO_PRICE_IN", 2.50),
                    outputPerM: Rate("FSO_PRICE_OUT", 10.00),
                    cachedInputPerM: Rate("FSO_PRICE_CACHED", 1.25));
            }

            if (choice == "anthropic")
            {
                if (string.IsNullOrEmpty(anthropicKey))
                    throw new InvalidOperationException("FSO_AGENT_PROVIDER=anthropic but ANTHROPIC_API_KEY is not set.");
                return new AnthropicProvider(new AnthropicClient(),
                    Environment.GetEnvironmentVariable("FSO_AGENT_MODEL") ?? AnthropicProvider.DefaultModel);
            }

            throw new InvalidOperationException($"Unknown FSO_AGENT_PROVIDER '{choice}' (expected anthropic or openai).");
        }

        /// <summary>
        /// Harness delivery: compile to disk and report the first object's GUID. The client
        /// substitutes LiveObjectInjector here to put the object into a running VM instead.
        /// </summary>
        private static uint? DeliverToDisk(string packSessionId)
        {
            var result = JsonSerializer.SerializeToNode(
                FSO.ModServer.PackToolHandlers.Compile(packSessionId));

            if (result?["ok"]?.GetValue<bool>() != true) return null;

            var outDir = result["out_dir"]?.ToString();
            var obj = result["report"]?["Objects"]?.AsArray()?.FirstOrDefault();
            var guidHex = obj?["Guid"]?.ToString();
            if (guidHex == null) return null;

            // Stash what the game needs to register this object without recompiling: the .iff
            // on disk plus its Buy Mode entry. The report's Iff is a bare filename.
            var cat = result["report"]?["CatalogEntries"]?.AsArray()?.FirstOrDefault();
            LastDelivery = new Delivery
            {
                IffPath = System.IO.Path.Combine(outDir ?? "", obj?["Iff"]?.ToString() ?? ""),
                Name = cat?["Name"]?.ToString() ?? "",
                Category = cat?["Category"]?.GetValue<int>() ?? 0,
                Price = cat?["Price"]?.GetValue<int>() ?? 0,
            };

            // stderr, not stdout: in --ipc mode stdout is a strict line protocol, and a build
            // path is a diagnostic the player must never see. The console harness prints
            // stderr too, so nothing is lost there.
            Console.Error.WriteLine($"Compiled to {outDir}");
            return Convert.ToUInt32(guidHex.Replace("0x", ""), 16);
        }

        /// <summary>
        /// Drives the reflection dispatcher with the exact JSON shapes a model would emit,
        /// building a working object without an LLM in the loop. Isolates "can named JSON
        /// arguments reach these static methods correctly" from "does the model choose the
        /// right calls" — so a failure in the real run can be attributed to one or the other.
        /// </summary>
        private static int DispatchTest()
        {
            var tools = ToolSchemaGenerator.Generate(typeof(FSO.ModServer.PackToolHandlers));
            System.Text.Json.Nodes.JsonNode Call(string name, string argsJson)
            {
                var tool = tools.First(t => t.Name == name);
                var args = (System.Text.Json.Nodes.JsonObject)System.Text.Json.Nodes.JsonNode.Parse(argsJson);
                var result = ToolSchemaGenerator.Invoke(tool, args);
                var node = JsonSerializer.SerializeToNode(result);
                Console.WriteLine($"  {name} -> ok={node?["ok"]}");
                return node;
            }

            Console.WriteLine("Dispatching a full object build through reflection only (no LLM):\n");

            var session = Call("create_pack", "{\"id\":\"dispatch-test\",\"name\":\"Dispatch Test\"}")
                ?["pack_session_id"]?.ToString();

            // Note the optional-parameter path: price/category are supplied, guid is omitted
            // and must fall through to the C# default (auto-allocate).
            Call("add_object", $"{{\"pack_session_id\":\"{session}\",\"id\":\"test_rock\",\"name\":\"Test Rock\",\"price\":25,\"category\":\"decorative\",\"clone_from_guid\":\"0xC14849AC\",\"entry_main\":\"main_loop\"}}");
            Call("add_tree", $"{{\"pack_session_id\":\"{session}\",\"object_id\":\"test_rock\",\"tree_name\":\"main_loop\"}}");
            Call("edit_tree_node", $"{{\"pack_session_id\":\"{session}\",\"object_id\":\"test_rock\",\"tree_name\":\"main_loop\",\"node_json\":\"{{\\\"id\\\":\\\"idle\\\",\\\"prim\\\":\\\"idle_for_input\\\",\\\"ticks_param\\\":0,\\\"allow_push\\\":true,\\\"then\\\":\\\"idle\\\",\\\"else\\\":\\\"idle\\\"}}\"}}");
            Call("list_vocabulary", "{\"kind\":\"categories\"}");
            var validated = Call("validate", $"{{\"pack_session_id\":\"{session}\"}}");
            var compiled = Call("compile", $"{{\"pack_session_id\":\"{session}\"}}");

            var ok = validated?["ok"]?.GetValue<bool>() == true && compiled?["ok"]?.GetValue<bool>() == true;
            Console.WriteLine(ok
                ? "\nPASS — named JSON arguments dispatch correctly to the static handlers."
                : "\nFAIL — see the ok=false above.");
            return ok ? 0 : 1;
        }

        private static int PrintSchemas()
        {
            var tools = ToolSchemaGenerator.Generate(typeof(FSO.ModServer.PackToolHandlers));
            Console.WriteLine($"{tools.Count} tools (ordered for stable prompt-cache prefix):\n");
            foreach (var t in tools)
            {
                var required = ((System.Text.Json.Nodes.JsonArray)t.InputSchema["required"]).Count;
                var total = ((System.Text.Json.Nodes.JsonObject)t.InputSchema["properties"]).Count;
                Console.WriteLine($"  {t.Name}  ({required} required / {total} params)");
            }
            // The tool table is the bulk of the cached prefix; sizing it locally avoids
            // spending an API call on count_tokens just to estimate cost.
            var wireBytes = tools.Sum(t =>
                t.Name.Length + t.Description.Length + t.InputSchema.ToJsonString().Length);
            Console.WriteLine($"\nTool table serializes to ~{wireBytes} chars (~{wireBytes / 4} tokens), " +
                              "the stable prefix shared by every request and every player.\n");

            Console.WriteLine("Full schema for the most complex tool:\n");
            var sample = tools.First(t => t.Name == "add_object");
            Console.WriteLine(ToolSchemaGenerator.SchemaJson(sample));
            return 0;
        }
    }
}
