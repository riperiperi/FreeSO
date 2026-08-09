using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading.Tasks;

namespace FSO.AgentBridge
{
    /// <summary>
    /// Holds the conversation with the player and drives PackToolHandlers to build the object
    /// they asked for. Runs off the caller's thread — a single tool call (a compile, a VM run)
    /// takes real seconds and must not block a render loop.
    ///
    /// Provider-agnostic: the loop, tool dispatch, narration handling, and failure taxonomy
    /// are identical for every backend. Only ILlmProvider knows a wire format.
    /// </summary>
    public class MakeSomethingAgent : IMakeSomethingAgent
    {
        // Thinking is on by default on current Anthropic models and max_tokens caps thinking
        // + response text together, so this needs headroom past the visible reply length.
        private const int MaxTokens = 16000;


        private readonly ILlmProvider _provider;
        private readonly Action<Action> _dispatch;
        private readonly Func<string, uint?> _deliver;
        private readonly List<ToolSchemaGenerator.ToolDefinition> _tools;
        private readonly RunLimits _limits;

        public event Action<string> OnNarration;
        public event Action<uint> OnObjectComplete;
        public event Action<string> OnError;

        public LlmUsage Usage => _provider.Usage;

        /// <summary>
        /// Per-turn timing and token deltas. The 10-minute wall is a product risk, and
        /// "where did the time go" is unanswerable from cumulative totals — a turn that spent
        /// 40s waiting on the model and one that spent 40s in a VM run need different fixes.
        /// </summary>
        public class TurnMetric
        {
            public int Turn;
            public double ModelSeconds;
            public double ToolSeconds;
            public List<string> ToolsCalled = new();
            public long InputTokens, OutputTokens, CacheReadTokens, CacheWriteTokens;
        }

        public readonly List<TurnMetric> Metrics = new();

        /// <param name="provider">The LLM backend. Injected so provider choice is a config
        /// decision and so the same loop can run against a project-run endpoint later.</param>
        /// <param name="dispatch">Marshals an event onto the consumer's safe thread. FreeSO's
        /// UI requires every state-touching callback to run on the game thread, so the panel
        /// passes <c>a =&gt; GameThread.NextUpdate(_ =&gt; a())</c>; the console harness passes
        /// <c>a =&gt; a()</c>. Every event routes through this, so the rule lives here once.</param>
        /// <param name="deliver">Takes the finished pack session id, puts the object into the
        /// player's game, returns its GUID (null if delivery produced nothing).</param>
        public MakeSomethingAgent(
            ILlmProvider provider,
            Action<Action> dispatch,
            Func<string, uint?> deliver,
            RunLimits limits = null)
        {
            _limits = limits ?? RunLimits.FromEnvironment();
            _provider = provider ?? throw new ArgumentNullException(nameof(provider));
            _dispatch = dispatch ?? throw new ArgumentNullException(nameof(dispatch));
            _deliver = deliver ?? throw new ArgumentNullException(nameof(deliver));
            _tools = ToolSchemaGenerator.Generate(typeof(FSO.ModServer.PackToolHandlers));
        }

        public void SendMessage(string playerText)
        {
            _provider.AddUserMessage(playerText);
            Task.Run(() => RunLoop());
        }

        private async Task RunLoop()
        {
            try
            {
                string packSessionId = null;

                for (int turn = 0; turn < _limits.MaxTurns; turn++)
                {
                    var before = (_provider.Usage.InputTokens, _provider.Usage.OutputTokens, _provider.Usage.CacheReadTokens, _provider.Usage.CacheWriteTokens);
                    var sw = System.Diagnostics.Stopwatch.StartNew();
                    var result = await _provider.NextTurnAsync(SystemPrompt, _tools, MaxTokens);
                    sw.Stop();

                    var metric = new TurnMetric
                    {
                        Turn = turn + 1,
                        ModelSeconds = sw.Elapsed.TotalSeconds,
                        InputTokens = _provider.Usage.InputTokens - before.InputTokens,
                        OutputTokens = _provider.Usage.OutputTokens - before.OutputTokens,
                        CacheReadTokens = _provider.Usage.CacheReadTokens - before.CacheReadTokens,
                        CacheWriteTokens = _provider.Usage.CacheWriteTokens - before.CacheWriteTokens,
                    };
                    Metrics.Add(metric);

                    // The final turn's prose is a success claim by construction — the prompt
                    // ends with "say what they've got and that it's ready to place". But
                    // whether it IS ready is only known after delivery, which happens after
                    // this turn. So hold the last turn's narration and let delivery decide
                    // whether the player ever reads it. The model cannot make this call
                    // itself; withholding is structural, not a prompt instruction.
                    if (result.ToolCalls.Count > 0)
                        foreach (var line in result.Narration) Raise(OnNarration, line);

                    if (result.ToolCalls.Count == 0)
                    {
                        // The model stopped calling tools. Exactly one terminal event must
                        // fire on every path out of this loop: a consumer (the harness, and
                        // the UI panel) blocks until it hears one, so returning quietly here
                        // hangs it forever with no error to show. That is what a model
                        // claiming "all done!" without ever having built anything used to do.
                        if (packSessionId != null) Deliver(packSessionId, result.Narration);
                        else Raise(OnError, PlayerMessage(AgentFailureKind.Bug));
                        return;
                    }

                    var toolResults = new List<LlmToolResult>();
                    foreach (var call in result.ToolCalls)
                    {
                        metric.ToolsCalled.Add(call.ToolName);
                        var toolSw = System.Diagnostics.Stopwatch.StartNew();
                        var (json, isError, sessionId) = ExecuteTool(call);
                        toolSw.Stop();
                        metric.ToolSeconds += toolSw.Elapsed.TotalSeconds;
                        if (sessionId != null) packSessionId = sessionId;
                        toolResults.Add(new LlmToolResult
                        {
                            Id = call.Id,
                            ToolName = call.ToolName,
                            ResultJson = json,
                            IsError = isError,
                        });
                    }
                    _provider.AddToolResults(toolResults);
                }

                // Cap reached. Log the ceiling that stopped it so this reads as a deliberate
                // stop rather than a mysterious failure.
                Log($"stopped at turn cap ({_limits}) — run did not complete");
                Raise(OnError, "That one's turning into more of a project than I expected, so I've stopped " +
                               "rather than let it run away. Try describing something a little simpler?");
            }
            catch (AgentFailure f)
            {
                Log($"{f.Kind}: {f.Message}");
                Raise(OnError, PlayerMessage(f.Kind));
            }
            catch (Exception e)
            {
                Log(e.ToString());
                Raise(OnError, PlayerMessage(AgentFailureKind.Bug));
            }
        }

        /// <summary>
        /// Player-safe text for each failure, per NARRATION-CONTRACT.md: no stack traces, no
        /// error codes, no provider names. The advice must also be TRUE — telling a player to
        /// retry something that can never succeed is the specific bug this replaces.
        /// </summary>
        internal static string PlayerMessage(AgentFailureKind kind) => kind switch
        {
            AgentFailureKind.Transient =>
                "I lost my train of thought for a second there. Try that again?",

            AgentFailureKind.OutOfCredit =>
                "I'm out of the credit I use to make things. Trying again won't help until it's topped up — " +
                "you can add more credit, or plug in your own key to keep going.",

            AgentFailureKind.RateLimited =>
                "I've been making a lot of things lately and need to slow down for a bit. " +
                "Give it a few minutes — or plug in your own key and we can keep going now.",

            AgentFailureKind.Unauthorized =>
                "I can't get to my workshop right now — the key I use isn't working. " +
                "That one needs a person to sort out; it isn't something you did.",

            _ =>
                "I hit a snag making that one, and it's on me rather than on you. " +
                "If you describe it a little differently I'll take another run at it.",
        };

        /// <param name="closingNarration">The final turn's prose, withheld until delivery
        /// succeeds. Emitted only on success — otherwise the player reads "ready to place!"
        /// immediately followed by "I couldn't get it into your game", which is incoherent
        /// and happens on any model whose delivery fails.</param>
        private void Deliver(string packSessionId, List<string> closingNarration)
        {
            uint? guid;
            try
            {
                guid = _deliver(packSessionId);
            }
            catch (Exception e)
            {
                Log("delivery failed: " + e);
                Raise(OnError, "I built it, but couldn't get it into your game. It's saved — try again in a moment?");
                return;
            }

            if (guid == null)
            {
                Raise(OnError, "I built it, but couldn't get it into your game. It's saved — try again in a moment?");
                return;
            }

            // Object exists — now the success claim is true, so release it.
            foreach (var line in closingNarration) Raise(OnNarration, line);
            _dispatch(() => OnObjectComplete?.Invoke(guid.Value));
        }

        private (string json, bool isError, string sessionId) ExecuteTool(LlmToolCall call)
        {
            var tool = _tools.FirstOrDefault(t => t.Name == call.ToolName);
            if (tool == null)
                return ($"{{\"ok\":false,\"errors\":[{{\"message\":\"no such tool '{call.ToolName}'\"}}]}}", true, null);

            try
            {
                var args = JsonNode.Parse(string.IsNullOrWhiteSpace(call.ArgumentsJson) ? "{}" : call.ArgumentsJson)
                    as JsonObject ?? new JsonObject();

                var json = JsonSerializer.Serialize(ToolSchemaGenerator.Invoke(tool, args));

                // create_pack's session id is the handle every later call needs; capture it
                // rather than making the model repeat it back at the end.
                string sessionId = null;
                if (tool.Name == "create_pack") sessionId = JsonNode.Parse(json)?["pack_session_id"]?.ToString();

                // Diagnostic logging (stderr, never the player). Two cases matter:
                //  - any failure, because a repeated failure is how a thrash loop starts;
                //  - every `validate` result even when it passes, because the question
                //    "is it rejecting the SAME thing repeatedly, or something new each
                //    time?" is what separates a bad-diagnostics bug (ours, fixable) from
                //    genuine schema difficulty (points at simplifying tree authoring).
                var failed = JsonNode.Parse(json)?["ok"]?.GetValueKind() == JsonValueKind.False;
                if (failed || tool.Name == "validate")
                    Log($"{tool.Name} -> {Truncate(json, 700)}");

                // Log what the model is actually authoring into trees. Turn counts alone
                // can't distinguish "the validator keeps rejecting it" from "expressing this
                // behaviour genuinely takes a dozen nodes" — and those want opposite fixes
                // (better diagnostics vs. a coarser authoring primitive).
                if (tool.Name is "edit_tree_node" or "add_tree")
                    Log($"  ARGS {tool.Name}: {Truncate(call.ArgumentsJson, 320)}");

                // A tool reporting ok:false is a RECOVERABLE result, not a transport error —
                // the model reads the diagnostics and fixes its own mistake, which is the
                // whole point of MCP-DESIGN.md §2's error contract.
                return (json, false, sessionId);
            }
            catch (Exception e)
            {
                var inner = e.InnerException ?? e;
                Log($"tool '{call.ToolName}' threw: {inner}");
                return ($"{{\"ok\":false,\"errors\":[{{\"message\":{JsonSerializer.Serialize(inner.Message)}}}]}}", true, null);
            }
        }

        private void Raise(Action<string> handler, string message)
        {
            if (handler == null) return;
            _dispatch(() => handler(message));
        }

        private static string Truncate(string s, int max) =>
            s != null && s.Length > max ? s.Substring(0, max) + "…[truncated]" : s;

        private void Log(string detail) =>
            Console.Error.WriteLine($"[MakeSomethingAgent/{_provider.Name}] {detail}");

        private const string SystemPrompt = @"You help a player of The Sims Online invent a new object for their game, just by describing it. You have tools that author, validate, test, and compile the object.

The player is not a programmer and never sees your tools. Everything you say goes straight onto their screen.

How to talk:
- Write one short line at a time, in plain warm language, about what you're doing for their object — ""giving him something to say..."", ""teaching him to notice you..."".
- Never name a tool, never show JSON, code, a GUID, a file path, or an error code. Never explain the toolchain. If a tool fails, don't mention it — quietly fix it and keep going.
- Don't ask the player to make technical choices. Pick sensible defaults and tell them what you made in ordinary words.

How to build:
- Look up the vocabulary rather than guessing at primitive, scope, or category names.
- EVERY object must have an appearance, or it is invisible in the game and the player sees nothing. Always set the object's appearance by cloning the sprites of a fitting base-game object. Choose one that resembles what the player described, and do this even when they say nothing about how it should look.
- Build ONLY what the player asked for. Give the object the one behaviour they described and stop — do not add extra interactions, abilities, or flourishes they did not ask for. A thing that just sits there needs only an idle loop; it does not need to be pettable. Every extra behaviour costs the player waiting time, and they can always ask for more afterwards.
- Validate as you go and fix what comes back; the diagnostics tell you exactly what's wrong.
- Test the object actually behaves the way the player asked before you finish — compiling is not the same as working.
- When you're done, say in one friendly sentence what they've got and that it's ready to place. Then stop.";
    }
}
