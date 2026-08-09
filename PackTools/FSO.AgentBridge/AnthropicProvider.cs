using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using Anthropic;
using Anthropic.Models.Messages;

namespace FSO.AgentBridge
{
    /// <summary>
    /// Anthropic backend. Keeps history as native MessageParams so thinking blocks round-trip
    /// with their signatures intact — the API rejects a modified thinking block, and there is
    /// no provider-neutral shape that survives that.
    /// </summary>
    public class AnthropicProvider : ILlmProvider
    {
        public const string DefaultModel = "claude-opus-5";

        // Opus 5 list price per million tokens; cache reads ~0.1x input, writes ~1.25x.
        private const double InputPerM = 5.00, OutputPerM = 25.00, CacheReadPerM = 0.50, CacheWritePerM = 6.25;

        private readonly AnthropicClient _client;
        private readonly List<MessageParam> _history = new();

        public string Name => "anthropic";
        public string Model { get; }
        public LlmUsage Usage { get; } = new();

        /// <param name="client">Injected, never constructed here — the seam that lets the same
        /// loop run against api.anthropic.com now and a project-run endpoint later.</param>
        public AnthropicProvider(AnthropicClient client, string model = DefaultModel)
        {
            _client = client ?? throw new ArgumentNullException(nameof(client));
            Model = model;
        }

        public void AddUserMessage(string text) =>
            _history.Add(new MessageParam { Role = Role.User, Content = text });

        // A cache breakpoint looks back at most 20 content blocks to find the previous
        // entry. One turn contributes 2 blocks per tool call (the tool_use and its result),
        // so past ~10 calls in a turn the next request can't see the previous breakpoint and
        // the whole conversation is re-written at full price. Measured: a 13-call turn cost
        // ~20k re-written tokens (~$0.13) on the turn that followed it.
        private const int ToolCallsPerTurnBeforeExtraBreakpoint = 8;

        public void AddToolResults(IReadOnlyList<LlmToolResult> results)
        {
            // All results in ONE user message — splitting them across messages trains the
            // model out of making parallel tool calls.
            var blocks = results.Select(r => (ContentBlockParam)new ToolResultBlockParam
            {
                ToolUseID = r.Id,
                Content = r.ResultJson,
                IsError = r.IsError,
            }).ToList();

            // On a big turn, plant an extra breakpoint partway through so the next request
            // has one within the lookback window. Costs one of the 4 breakpoint slots and
            // only on turns that would otherwise lose the cache entirely.
            if (results.Count > ToolCallsPerTurnBeforeExtraBreakpoint)
            {
                var mid = blocks.Count / 2;
                blocks[mid] = new ToolResultBlockParam
                {
                    ToolUseID = results[mid].Id,
                    Content = results[mid].ResultJson,
                    IsError = results[mid].IsError,
                    CacheControl = new CacheControlEphemeral(),
                };
            }

            _history.Add(new MessageParam { Role = Role.User, Content = blocks });
        }

        public async Task<LlmTurn> NextTurnAsync(
            string systemPrompt,
            IReadOnlyList<ToolSchemaGenerator.ToolDefinition> tools,
            int maxTokens)
        {
            Message response;
            try
            {
                response = await _client.Messages.Create(new MessageCreateParams
                {
                    Model = Model,
                    MaxTokens = maxTokens,
                    // Top-level automatic caching: places a breakpoint on the last cacheable
                    // block and MOVES IT FORWARD each turn, so the growing conversation is
                    // cached incrementally — each request reads everything up to the previous
                    // turn and writes only the newest. On a 15-30 turn loop the history, not
                    // the tool table, is where the cost is. The explicit system breakpoint
                    // below is kept as well so tools+system re-cache if evicted; the two
                    // compose, each taking one of the 4 breakpoint slots.
                    //
                    // FRAGILE BY OMISSION: caching only holds because this request sets no
                    // thinking, no output_config.effort, and no sampling params — so there is
                    // nothing that can differ between turns. Adding any of them WILL silently
                    // invalidate cached message blocks unless the value is held constant for
                    // the whole run. If you add one, hold it constant and re-check that
                    // cache_read stays non-zero after turn 1.
                    CacheControl = new CacheControlEphemeral(),
                    // Cache the tools+system prefix. Render order is tools -> system ->
                    // messages, so a breakpoint on the last system block covers both, and
                    // that prefix is byte-identical for every object and every player.
                    // Without this every turn re-pays full input price for ~1.4k tokens of
                    // tool table plus the system prompt — and a build is 15+ turns.
                    System = new List<TextBlockParam>
                    {
                        new() { Text = systemPrompt, CacheControl = new CacheControlEphemeral() },
                    },
                    Tools = tools.Select(t => new ToolUnion(new Tool
                    {
                        Name = t.Name,
                        Description = t.Description,
                        InputSchema = ToInputSchema(t.InputSchema),
                    })).ToList(),
                    Messages = _history,
                });
            }
            catch (Exception e)
            {
                throw Classify(e);
            }

            Usage.Turns++;
            Usage.InputTokens += response.Usage.InputTokens;
            Usage.OutputTokens += response.Usage.OutputTokens;
            Usage.CacheReadTokens += response.Usage.CacheReadInputTokens ?? 0;
            Usage.CacheWriteTokens += response.Usage.CacheCreationInputTokens ?? 0;
            Usage.EstimatedCostUsd =
                Usage.InputTokens / 1e6 * InputPerM +
                Usage.OutputTokens / 1e6 * OutputPerM +
                Usage.CacheReadTokens / 1e6 * CacheReadPerM +
                Usage.CacheWriteTokens / 1e6 * CacheWritePerM;

            var turn = new LlmTurn();
            var assistant = new List<ContentBlockParam>();

            foreach (var block in response.Content)
            {
                if (block.TryPickText(out TextBlock text))
                {
                    assistant.Add(new TextBlockParam { Text = text.Text });
                    var line = text.Text.Trim();
                    if (line.Length > 0) turn.Narration.Add(line);
                }
                else if (block.TryPickThinking(out ThinkingBlock thinking))
                {
                    // Signature must survive verbatim or the next request is rejected.
                    assistant.Add(new ThinkingBlockParam
                    {
                        Thinking = thinking.Thinking,
                        Signature = thinking.Signature,
                    });
                }
                else if (block.TryPickRedactedThinking(out RedactedThinkingBlock redacted))
                {
                    assistant.Add(new RedactedThinkingBlockParam { Data = redacted.Data });
                }
                else if (block.TryPickToolUse(out ToolUseBlock toolUse))
                {
                    assistant.Add(new ToolUseBlockParam
                    {
                        ID = toolUse.ID,
                        Name = toolUse.Name,
                        Input = toolUse.Input,
                    });

                    var args = new JsonObject();
                    foreach (var kv in toolUse.Input) args[kv.Key] = JsonNode.Parse(kv.Value.GetRawText());

                    turn.ToolCalls.Add(new LlmToolCall
                    {
                        Id = toolUse.ID,
                        ToolName = toolUse.Name,
                        ArgumentsJson = args.ToJsonString(),
                    });
                }
            }

            _history.Add(new MessageParam { Role = Role.Assistant, Content = assistant });
            return turn;
        }

        private static InputSchema ToInputSchema(JsonObject schema)
        {
            var properties = new Dictionary<string, JsonElement>();
            foreach (var kv in (JsonObject)schema["properties"])
                properties[kv.Key] = JsonSerializer.Deserialize<JsonElement>(kv.Value.ToJsonString());

            return new InputSchema
            {
                Properties = properties,
                Required = ((JsonArray)schema["required"]).Select(n => n.ToString()).ToList(),
            };
        }

        /// <summary>
        /// Maps SDK exceptions onto remedies. Deliberately matches on the SDK's typed
        /// exception classes rather than message text, except for the credit case — the API
        /// returns that as a 400 alongside genuine bad requests, so the message is the only
        /// thing that distinguishes "you owe money" from "your payload is malformed".
        /// </summary>
        private static AgentFailure Classify(Exception e)
        {
            var text = e.Message ?? "";

            if (e is Anthropic.Exceptions.AnthropicRateLimitException)
                return new AgentFailure(AgentFailureKind.RateLimited, text, e);

            if (e is Anthropic.Exceptions.AnthropicUnauthorizedException
                || e is Anthropic.Exceptions.AnthropicForbiddenException)
                return new AgentFailure(AgentFailureKind.Unauthorized, text, e);

            if (e is Anthropic.Exceptions.Anthropic5xxException)
                return new AgentFailure(AgentFailureKind.Transient, text, e);

            if (e is Anthropic.Exceptions.AnthropicBadRequestException)
            {
                if (text.Contains("credit balance", StringComparison.OrdinalIgnoreCase)
                    || text.Contains("Plans & Billing", StringComparison.OrdinalIgnoreCase))
                    return new AgentFailure(AgentFailureKind.OutOfCredit, text, e);
                return new AgentFailure(AgentFailureKind.Bug, text, e);
            }

            if (e is TaskCanceledException || e is TimeoutException
                || e is System.Net.Http.HttpRequestException)
                return new AgentFailure(AgentFailureKind.Transient, text, e);

            return new AgentFailure(AgentFailureKind.Bug, text, e);
        }
    }
}
